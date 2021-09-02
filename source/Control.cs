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
        private static Control _instance;
        public static Control Instance => _instance ?? (_instance = new Control());

        public Settings Settings = new Settings();

        private ILog Logger;
        private FileLogAppender logAppender;
        private const string ModName = "CustomSalvage";
        private string LogPrefix = "[CSalv]";

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
            Instance.Log("Finish Loading");
            Dictionary<string, VersionManifestEntry> manifest = null;
            if (customResources.TryGetValue("CSTags", out manifest))
            {
                Instance.LogDebug("- Loading CSTags");
                Tags.Instance.LoadTags(CustomLoader<CSTag>.Load(manifest));
            }
        }

        private void InitNonStatic(string directory, string settingsJson)
        {
            Logger = HBS.Logging.Logger.GetLogger("CustomSalvage", LogLevel.Debug);

            try
            {
                try
                {
                    Settings = new Settings();
                    JSONSerializationUtility.FromJSON(Settings, settingsJson);
                    HBS.Logging.Logger.SetLoggerLevel(Logger.Name, Settings.LogLevel);
                }
                catch (Exception)
                {
                    Settings = new Settings();
                }

                if (!Settings.ShowLogPrefix)
                    LogPrefix = "";


                Settings.Complete();
                SetupLogging(directory);

                var harmony = HarmonyInstance.Create("io.github.denadan.CustomSalvage");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

#if USE_CC
                Logger.Log("Loaded CustomSalvageCC v1.0 for bt 1.9.1");
#else
                Logger.Log("Loaded CustomSalvageNonCC v1.0 for bt 1.9.1");
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

                Logger.LogDebug("done");
                if (Settings.DEBUG_ShowConfig)
                    Logger.LogDebug(JSONSerializationUtility.ToJSON(Settings));
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        #region LOGGING
        [Conditional("CCDEBUG")]
        public void LogDebug(string message)
        {
            Logger.LogDebug(LogPrefix + message);
        }
        [Conditional("CCDEBUG")]
        public void LogDebug(string message, Exception e)
        {
            Logger.LogDebug(LogPrefix + message, e);
        }

        public void LogError(string message)
        {
            Logger.LogError(LogPrefix + message);
        }
        public void LogError(string message, Exception e)
        {
            Logger.LogError(LogPrefix + message, e);
        }
        public void LogError(Exception e)
        {
            Logger.LogError(e);
        }

        public void Log(string message)
        {
            Logger.Log(message);
        }



        internal void SetupLogging(string Directory)
        {
            var logFilePath = Path.Combine(Directory, "log.txt");

            try
            {
                ShutdownLogging();
                AddLogFileForLogger(logFilePath);
            }
            catch (Exception e)
            {
                Logger.Log("CustomSalvage: can't create log file", e);
            }
        }

        internal void ShutdownLogging()
        {
            if (logAppender == null)
            {
                return;
            }

            try
            {
                HBS.Logging.Logger.ClearAppender("CustomSalvage");
                logAppender.Flush();
                logAppender.Close();
            }
            catch
            {
            }

            logAppender = null;
        }

        private void AddLogFileForLogger(string logFilePath)
        {
            try
            {
                logAppender = new FileLogAppender(logFilePath, FileLogAppender.WriteMode.INSTANT);
                HBS.Logging.Logger.AddAppender("CustomSalvage", logAppender);

            }
            catch (Exception e)
            {
                Logger.Log("CustomSalvage: can't create log file", e);
            }
        }

        #endregion

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
