using Harmony;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BattleTech;
using CustomSalvage.MechBroke;
using HBS.Logging;
using HBS.Util;
using HoudiniEngineUnity;


namespace CustomSalvage
{

    public delegate void AdditionalSalvageStep(ContractHelper contract);

    public class Control
    {
        public static Control Instance { get; } = new();

        public Settings Settings = new Settings();

        internal RecoveryDelegate NeedRecovery;
        internal LostUnitActionDelegate LostUnitAction;
        internal PartsNumDelegeate GetNumParts;

        public event AdditionalSalvageStep OnAdditionalSalvageStep;

        internal void CallForAdditionalSavage(ContractHelper contract)
        {
            OnAdditionalSalvageStep?.Invoke(contract);
        }

        public static void Init(string directory, string settingsJSON)
        {
            Instance.InitNonStatic(directory, settingsJSON);


        }

        public static void FinishedLoading(Dictionary<string, Dictionary<string, VersionManifestEntry>> customResources)
        {
            Log.Main.Info?.Log("Finish Loading");
            Dictionary<string, VersionManifestEntry> manifest = null;
            if (customResources.TryGetValue("CSTags", out manifest))
            {
                Log.Main.Debug?.Log("- Loading CSTags");
                Tags.Instance.LoadTags(CustomLoader<CSTag>.Load(manifest));
            }
        }

        private void InitNonStatic(string directory, string settingsJson)
        {
            try
            {
                try
                {
                    Settings = new Settings();
                    JSONSerializationUtility.FromJSON(Settings, settingsJson);
                }
                catch (Exception e)
                {
                    Settings = new Settings();
                    Log.Main.Error?.Log(e);
                }

                Settings.Complete();

                var harmony = HarmonyInstance.Create("io.github.denadan.CustomSalvage");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

#if USE_CC
                Log.Main.Info?.Log("Loaded CustomSalvageCC v1.0 for bt 1.9.1");
#else
                Log.Main.Info?.Log("Loaded CustomSalvageNonCC v1.0 for bt 1.9.1");
#endif

                switch (Settings.RecoveryType)
                {
                    case RecoveryCalculationType.AlwaysRecover:
                        NeedRecovery = (result, contract) => true;
                        break;
                    case RecoveryCalculationType.NeverRecover:
                        NeedRecovery = (result, contract) => false;
                        break;
                    case RecoveryCalculationType.PartDestroyed:
                        NeedRecovery = RecoveryDelegates.PartDestroyed;
                        break;
                    default:
                        NeedRecovery = RecoveryDelegates.VanilaRecovery;
                        break;
                }


                switch (Settings.LostMechAction)
                {
                    case LostMechActionType.ReturnItemsAndPartsToPlayer:
                        LostUnitAction = LostUnitActions.ReturnItemsAndParts;
                        break;
                    case LostMechActionType.MoveItemsToSalvage:
                        LostUnitAction = LostUnitActions.SalvageItems;
                        break;
                    case LostMechActionType.MoveItemsAndPartsToSalvage:
                        LostUnitAction = LostUnitActions.SalvageItemsAndParts;
                        break;
                    default:
                        LostUnitAction = LostUnitActions.ReturnItems;
                        break;
                }

                switch (Settings.PartCountType)
                {
                    case PartCalculationType.VanilaAdjusted:
                        GetNumParts = PartsNumCalculations.VanilaAdjusted;
                        break;
                    case PartCalculationType.PartDestroyed:
                        GetNumParts = PartsNumCalculations.PartDestroyed;
                        break;
                    case PartCalculationType.PartDestroyedIgnoreCT:
                        GetNumParts = PartsNumCalculations.PartDestroyedNoCT;
                        break;
                    default:
                        GetNumParts = PartsNumCalculations.Vanila;
                        break;
                }
#if USE_CC
                CustomComponents.Registry.RegisterSimpleCustomComponents(Assembly.GetExecutingAssembly());
#endif

                Log.Main.Debug?.Log("done");
                if (Settings.DEBUG_ShowConfig)
                    Log.Main.Debug?.Log(JSONSerializationUtility.ToJSON(Settings));
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log(e);
            }
        }

        public bool IsDestroyed(UnitResult lostUnit)
        {
#if USE_CC
            return CustomComponents.Contract_GenerateSalvage.IsDestroyed(lostUnit.mech);
#else


            if (lostUnit.mech.IsDestroyed)
                return true;

            if (lostUnit.pilot.HasEjected || lostUnit.pilot.IsIncapacitated)
                return true;

            if (lostUnit.mech.Inventory.Any(i =>
                    i.Def.CriticalComponent && i.DamageLevel == ComponentDamageLevel.Destroyed))
                return true;
            return false;
#endif
        }
    }
}
