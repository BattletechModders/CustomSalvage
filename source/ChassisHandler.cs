using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
#if USE_CC
using CustomComponents;
#endif
using Harmony;
using Localize;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

using Object = System.Object;
using Random = System.Random;

namespace CustomSalvage
{
    public static class ChassisHandler
    {
        private static int min_parts = 0;
        private static int min_parts_special = 0;

        public class mech_info
        {
            public int MinParts;
            public float PriceMult;
            public bool Excluded;
            public bool Omni;
            public bool Special;

            public mech_info()
            {
                MinParts = min_parts;
                PriceMult = 1f;
                Excluded = false;
                Omni = false;
                Special = false;
            }
        }


        private static Dictionary<string, MechDef> ChassisToMech = new Dictionary<string, MechDef>();
        private static Dictionary<string, int> PartCount = new Dictionary<string, int>();
        private static Dictionary<string, List<MechDef>> Compatible = new Dictionary<string, List<MechDef>>();

        private static Dictionary<string, mech_info> Proccesed = new Dictionary<string, mech_info>();

        public static void RegisterMechDef(MechDef mech, int part_count = 0)
        {
            int max_parts = UnityGameInstance.BattleTechGame.Simulation.Constants.Story.DefaultMechPartMax;
            min_parts = Mathf.CeilToInt(max_parts * Control.Settings.MinPartsToAssembly);
            min_parts_special = Mathf.CeilToInt(max_parts * Control.Settings.MinPartsToAssemblySpecial);

            ChassisToMech[mech.ChassisID] = mech;
            if (part_count > 0)
                PartCount[mech.Description.Id] = part_count;
            string id = mech.Description.Id;

            if (!Proccesed.ContainsKey(id))
            {
#if USE_CC
                var assembly = mech.Chassis.GetComponent<AssemblyVariant>();
#endif
                var info = new mech_info();
                info.Omni = !string.IsNullOrEmpty(Control.Settings.OmniTechTag) && (
                            mech.Chassis.ChassisTags.Contains(Control.Settings.OmniTechTag) ||
                                mech.MechTags.Contains(Control.Settings.OmniTechTag));

#if USE_CC
                if (assembly != null && assembly.Exclude)
                    info.Excluded = true;
                else if (assembly != null && assembly.Include)
                    info.Excluded = false;
                else
#endif
                if (Control.Settings.ExcludeVariants.Contains(id))
                    info.Excluded = true;
                else
                    if (Control.Settings.ExcludeTags.Any(extag => mech.MechTags.Contains(extag)))
                    info.Excluded = true;

                if (Control.Settings.SpecialTags != null && Control.Settings.SpecialTags.Length > 0)


                    foreach (var tag_info in Control.Settings.SpecialTags)
                    {
                        if (mech.MechTags.Contains(tag_info.Tag))
                        {
                            info.MinParts = min_parts_special;
                            info.PriceMult *= tag_info.Mod;
                            info.Special = true;
                        }
                    }

                if (info.Omni)
                    info.MinParts = 1;

#if USE_CC
                if (assembly != null)
                {
                    if (assembly.ReplacePriceMult)
                        info.PriceMult = assembly.PriceMult;
                    else
                        info.PriceMult *= assembly.PriceMult;

                    if (assembly.PartsMin >= 0)
                        info.MinParts = Mathf.CeilToInt(max_parts * assembly.PartsMin);
                }
#endif

                if (!info.Excluded && Control.Settings.AssemblyVariants)
                {
                    string prefabid = GetPrefabId(mech);

                    if (!Compatible.TryGetValue(prefabid, out var list))
                    {
                        list = new List<MechDef>();
                        Compatible[prefabid] = list;
                    }

                    if (list.All(i => i.Description.Id != id))
                        list.Add(mech);
                }

                Control.LogDebug($"Registring {mech.Description.Id}({mech.Description.UIName}) => {mech.ChassisID}");
                Control.LogDebug($"-- Exclude:{info.Excluded} MinParts:{info.MinParts} PriceMult:{info.PriceMult}");
#if USE_CC
                if (assembly != null)
                    Control.LogDebug($"-- PrefabID:{assembly.PrefabID} Exclude:{assembly.Exclude} include:{assembly.Include} Mult:{assembly.PriceMult} Parts:{assembly.PartsMin}");
#endif

                Proccesed[id] = info;
            }
        }

        private static string GetPrefabId(MechDef mech)
        {
#if USE_CC
            if (mech.Chassis.Is<AssemblyVariant>(out var a) && !string.IsNullOrEmpty(a.PrefabID))
                return a.PrefabID + mech.Chassis.Tonnage.ToString();
#endif
            return mech.Chassis.PrefabIdentifier + mech.Chassis.Tonnage.ToString();
        }

        public static MechDef GetMech(string chassisid)
        {
            return ChassisToMech[chassisid];
        }

        public static bool IfExcluded(string chassisid)
        {
            return Proccesed[ChassisToMech[chassisid].Description.Id].Excluded;
        }

        public static mech_info GetInfo(string chassisid)
        {
            return Proccesed[ChassisToMech[chassisid].Description.Id];
        }

        public static void ClearParts()
        {
            PartCount.Clear();
        }

        public static List<MechDef> GetCompatible(string chassisid)
        {

            var mech = ChassisToMech[chassisid];
            if (Proccesed[mech.Description.Id].Excluded)
                return null;

            var prefabid = GetPrefabId(mech);
            return Compatible[prefabid];
        }

        public static int GetCount(string mechid)
        {
            return PartCount.TryGetValue(mechid, out var num) ? num : 0;
        }

        public static void ShowInfo()
        {
            Control.LogDebug("================= CHASSIS TO MECH ===================");
            foreach (var mechDef in ChassisToMech)
                Control.LogDebug($"{mechDef.Key} => {mechDef.Value.Description.Id}");

            Control.LogDebug("================= EXCLUDED ===================");
            foreach (var info in Proccesed.Where(i => i.Value.Excluded))
                Control.LogDebug($"{info.Key}");

            Control.LogDebug("================= GROUPS ===================");
            foreach (var list in Compatible)
            {
                Control.LogDebug($"{list.Key}");

                foreach (var item in list.Value)
                    Control.LogDebug($"--- {item.Description.Id}");
            }
            Control.LogDebug("================= INVENTORY ===================");
            foreach (var mechDef in PartCount)
                Control.LogDebug($"{mechDef.Key}: [{mechDef.Value:00}]");
            Control.LogDebug("============================================");
        }

        public class parts_info
        {
            public int count;
            public int used;
            public int cbills;
            public string mechname;
            public string mechid;

            public parts_info(int c, int u, int cb, string mn, string mi)
            {
                count = c;
                used = u;
                cbills = cb;
                mechname = new Text(mn).ToString();
                mechid = mi;
            }
        }

        private static MechBayPanel mechBay;
        private static MechBayChassisUnitElement unitElement;
        private static MechBayChassisInfoWidget infoWidget;
        private static ChassisDef chassis;
        private static MechDef mech;
        private static List<parts_info> used_parts;
        private static SimGameEventTracker eventTracker = new SimGameEventTracker();
        private static bool _hasInitEventTracker = false;
        public static int page = 0;

        public static void OnChassisReady()
        {
            mechBay.OnReadyMech(unitElement);
            infoWidget.SetData(mechBay, null);
        }

        public static void PreparePopup(ChassisDef chassisDef, MechBayPanel mechbay, MechBayChassisInfoWidget widget, MechBayChassisUnitElement unitElement)
        {
            mechBay = mechbay;
            ChassisHandler.unitElement = unitElement;
            infoWidget = widget;
            chassis = chassisDef;
            mech = ChassisToMech[chassis.Description.Id];
        }

        public static void OnPartsAssembly()
        {
            try
            {
                Control.LogDebug($"-- remove parts");
                RemoveMechPart(mech.Description.Id, chassis.MechPartMax);
                infoWidget.SetData(mechBay, null);
                Control.LogDebug($"-- making mech");
                MakeMech(mechBay.Sim);
                Control.LogDebug($"-- refresh mechlab");
                mechBay.RefreshData(false);
            }
            catch (Exception e)
            {
                Control.LogError("Error in Complete Mech", e);
            }
        }

        private static void MakeMech(SimGameState sim)
        {

            Control.LogDebug($"Mech Assembly started for {mech.Description.UIName}");
            MechDef new_mech = new MechDef(mech, mechBay.Sim.GenerateSimGameUID(), true);

            try
            {
                if (Control.Settings.UnEquipedMech)
                {
                    Control.LogDebug($"-- Clear Inventory");
#if USE_CC
                    new_mech.SetInventory(DefaultHelper.ClearInventory(new_mech, mechBay.Sim));
#else
                new_mech.SetInventory(new MechComponentRef[0]);
#endif
                }
            }
            catch (Exception e)
            {
                Control.LogError($"ERROR in ClearInventory", e);
            }

            if (Control.Settings.BrokenMech)
            {
                BrokeMech(new_mech, sim);

            }

            try
            {
                Control.LogDebug("-- Adding mech");
                mechBay.Sim.AddMech(0, new_mech, true, false, true, null);
                Control.LogDebug("-- Posting Message");
                mechBay.Sim.MessageCenter.PublishMessage(new SimGameMechAddedMessage(new_mech, chassis.MechPartMax, true));
            }
            catch (Exception e)
            {
                Control.LogError($"ERROR in MakeMach", e);
            }

        }

        private static void BrokeMech(MechDef new_mech, SimGameState sim)
        {
            try
            {

                Control.LogDebug($"-- broke parts");
                var rnd = new Random();


                float LimbChance = Control.Settings.RepairMechLimbsChance;
                float CompFChance = Control.Settings.RepairComponentsFunctionalThreshold;
                float CompNFChance = Control.Settings.RepairComponentsNonFunctionalThreshold;


                if (Control.Settings.RepairChanceByTP)
                {
                    var basetp = Control.Settings.BaseTP;
                    var limbtp = Control.Settings.LimbChancePerTp;
                    var comptp = Control.Settings.ComponentChancePerTp;

                    if (Control.Settings.BrokeByTag != null && Control.Settings.BrokeByTag.Length > 1)
                    {
                        int numb = 0;
                        int numl = 0;
                        int numc = 0;

                        int sumb = 0;
                        float suml = 0;
                        float sumc = 0;



                        foreach (var info in Control.Settings.BrokeByTag)
                        {
                            if (new_mech.MechTags.Contains(info.tag) || new_mech.Chassis.ChassisTags.Contains(info.tag))
                            {
#if CCDEBUG
                                string logstr = info.tag;
#endif
                                if (info.BaseTp > 0)
                                {
                                    sumb += info.BaseTp;
                                    numb += 1;
#if CCDEBUG
                                    logstr += $" base:{info.BaseTp}";
#endif
                                }

                                if (info.Limb > 0)
                                {
                                    suml += info.Limb;
                                    numl += 1;
#if CCDEBUG
                                    logstr += $" limb:{info.Limb:0.000}";
#endif
                                }
                                if (info.Component > 0)
                                {
                                    sumc += info.Component;
                                    numc += 1;
#if CCDEBUG
                                    logstr += $" comp:{info.Component:0.000}";
#endif
                                }
#if CCDEBUG
                                Control.LogDebug(logstr);
#endif

                            }
                        }

                        if (numb > 0)
                            basetp = sumb / numb;
                        if (numl > 0)
                            limbtp = suml / numl;
                        if (numc > 0)
                            comptp = sumc / numc;

                        Control.LogDebug($"totals: base:{basetp}, limb:{limbtp:0.000}, component:{comptp:0.000}");

                        var tp = sim.MechTechSkill - basetp;

                        LimbChance = Mathf.Clamp(LimbChance + tp * limbtp, Control.Settings.LimbMinChance,Control.Settings.LimbMaxChance);
                        CompFChance = Mathf.Clamp(CompFChance + tp * limbtp, Control.Settings.ComponentMinChance, Control.Settings.ComponentMaxChance);
                        CompNFChance = Mathf.Clamp(CompNFChance + tp * limbtp, CompFChance, Control.Settings.ComponentMaxChance);
                    }
                }




                Control.LogDebug($"--- RepairMechLimbsChance: {LimbChance}, RepairMechLimbs: {Control.Settings.RepairMechLimbs} ");
                float roll = 0;
                //hd
                roll = (float)rnd.NextDouble();
                Control.LogDebug($"--- HeadRepaired: {Control.Settings.HeadRepaired}, roll: {roll} ");
                if (!Control.Settings.HeadRepaired && (!Control.Settings.RepairMechLimbs ||
                                                      roll < LimbChance))
                    new_mech.Head.CurrentInternalStructure = 0f;
                else if (Control.Settings.RandomStructureOnRepairedLimbs)
                    new_mech.Head.CurrentInternalStructure *= Math.Min(Control.Settings.MinStructure, (float)rnd.NextDouble());

                //ct
                roll = (float)rnd.NextDouble();
                Control.LogDebug($"--- CentralTorsoRepaired: {Control.Settings.CentralTorsoRepaired}, roll: {roll} ");
                if (!Control.Settings.CentralTorsoRepaired && (!Control.Settings.RepairMechLimbs ||
                                                               roll < LimbChance))
                    new_mech.CenterTorso.CurrentInternalStructure = 0f;
                else if (Control.Settings.RandomStructureOnRepairedLimbs)
                    new_mech.CenterTorso.CurrentInternalStructure *= Math.Min(Control.Settings.MinStructure, (float)rnd.NextDouble());

                //rt
                roll = (float)rnd.NextDouble();
                Control.LogDebug($"--- RightTorsoRepaired: {Control.Settings.RightTorsoRepaired}, roll: {roll} ");
                if (!Control.Settings.RightTorsoRepaired && (!Control.Settings.RepairMechLimbs ||
                                                             roll < LimbChance))
                    new_mech.RightTorso.CurrentInternalStructure = 0f;
                else if (Control.Settings.RandomStructureOnRepairedLimbs)
                    new_mech.RightTorso.CurrentInternalStructure *= Math.Min(Control.Settings.MinStructure, (float)rnd.NextDouble());

                //lt
                roll = (float)rnd.NextDouble();
                Control.LogDebug($"--- LeftTorsoRepaired: {Control.Settings.LeftTorsoRepaired}, roll: {roll} ");
                if (!Control.Settings.LeftTorsoRepaired && (!Control.Settings.RepairMechLimbs ||
                                                            roll < LimbChance))
                    new_mech.LeftTorso.CurrentInternalStructure = 0f;
                else if (Control.Settings.RandomStructureOnRepairedLimbs)
                    new_mech.LeftTorso.CurrentInternalStructure *= Math.Min(Control.Settings.MinStructure, (float)rnd.NextDouble());

                //ra
                roll = (float)rnd.NextDouble();
                Control.LogDebug($"--- RightArmRepaired: {Control.Settings.RightArmRepaired}, roll: {roll} ");
                if (!Control.Settings.RightArmRepaired && (!Control.Settings.RepairMechLimbs ||
                                                           roll < LimbChance))
                    new_mech.RightArm.CurrentInternalStructure = 0f;
                else if (Control.Settings.RandomStructureOnRepairedLimbs)
                    new_mech.RightArm.CurrentInternalStructure *= Math.Min(Control.Settings.MinStructure, (float)rnd.NextDouble());

                //la
                roll = (float)rnd.NextDouble();
                Control.LogDebug($"--- LeftArmRepaired: {Control.Settings.LeftArmRepaired}, roll: {roll} ");
                if (!Control.Settings.LeftArmRepaired && (!Control.Settings.RepairMechLimbs ||
                                                          roll < LimbChance))
                    new_mech.LeftArm.CurrentInternalStructure = 0f;
                else if (Control.Settings.RandomStructureOnRepairedLimbs)
                    new_mech.LeftArm.CurrentInternalStructure *= Math.Min(Control.Settings.MinStructure, (float)rnd.NextDouble());

                //rl
                roll = (float)rnd.NextDouble();
                Control.LogDebug($"--- RightLegRepaired: {Control.Settings.RightLegRepaired}, roll: {roll} ");
                if (!Control.Settings.RightLegRepaired && (!Control.Settings.RepairMechLimbs ||
                                                           roll < LimbChance))
                    new_mech.RightLeg.CurrentInternalStructure = 0f;
                else if (Control.Settings.RandomStructureOnRepairedLimbs)
                    new_mech.RightLeg.CurrentInternalStructure *= Math.Min(Control.Settings.MinStructure, (float)rnd.NextDouble());

                //ll
                Control.LogDebug($"--- LeftLegRepaired: {Control.Settings.LeftLegRepaired}, roll: {roll} ");
                roll = (float)rnd.NextDouble();
                if (!Control.Settings.LeftLegRepaired && (!Control.Settings.RepairMechLimbs ||
                                                          roll < LimbChance))
                    new_mech.LeftLeg.CurrentInternalStructure = 0f;
                else if (Control.Settings.RandomStructureOnRepairedLimbs)
                    new_mech.LeftLeg.CurrentInternalStructure *= Math.Min(Control.Settings.MinStructure, (float)rnd.NextDouble());

                Control.LogDebug($"-- broke equipment");

                foreach (var cref in new_mech.Inventory)
                {
                    if (new_mech.IsLocationDestroyed(cref.MountedLocation))
                    {
                        Control.LogDebug($"---- {cref.ComponentDefID} - location destroyed");
                        cref.DamageLevel = ComponentDamageLevel.Destroyed;
                    }
                    else if (Control.Settings.RepairMechComponents)
                    {
                        roll = (float)rnd.NextDouble();

                        if (roll < CompFChance)
                        {
                            Control.LogDebug(
                                $"---- {cref.ComponentDefID} - {roll} vs {CompFChance} - repaired ");
                            cref.DamageLevel = ComponentDamageLevel.Functional;
                        }
                        else if (roll < CompNFChance)
                        {
                            Control.LogDebug(
                                $"---- {cref.ComponentDefID} - {roll} vs {CompNFChance} - broken ");
                            cref.DamageLevel = ComponentDamageLevel.NonFunctional;
                        }
                        else
                        {
                            Control.LogDebug(
                                $"---- {cref.ComponentDefID} - {roll} vs {CompNFChance} - fubar ");
                            cref.DamageLevel = ComponentDamageLevel.Destroyed;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Control.LogError($"ERROR in BrokeParts", e);
                throw;
            }
        }

        private static void RemoveMechPart(string id, int count)
        {
            var method = mechBay.Sim.GetType()
                .GetMethod("RemoveItemStat", BindingFlags.NonPublic | BindingFlags.Instance);

            for (int i = 0; i < count; i++)
            {
                method.Invoke(mechBay.Sim, new Object[] { id, "MECHPART", false });
            }
        }

        public static void StartDialog()
        {
            ShowInfo();

            var list = GetCompatible(chassis.Description.Id);
            used_parts = new List<parts_info>();
            used_parts.Add(new parts_info(GetCount(mech.Description.Id), GetCount(mech.Description.Id), 0,
                mech.Description.UIName, mech.Description.Id));
            var info = Proccesed[mech.Description.Id];

            float cb = Control.Settings.AdaptPartBaseCost * mech.Description.Cost / chassis.MechPartMax;
            Control.LogDebug($"base part price for {mech.Description.UIName}({mech.Description.Id}): {cb}. mechcost: {mech.Description.Cost} ");
            Control.LogDebug($"-- setting:{Control.Settings.AdaptPartBaseCost}, maxparts:{chassis.MechPartMax}, minparts:{info.MinParts}, pricemult: {info.PriceMult}");

            foreach (var mechDef in list)
            {
                int num = GetCount(mechDef.Description.Id);
                if (num == 0)
                    continue;
                var id = mechDef.Description.Id;


                if (id == mech.Description.Id)
                    continue;
                else
                {
                    float omnimod = 1;
                    float mod = 1 + Mathf.Abs(mech.Description.Cost - mechDef.Description.Cost) /
                                (float)mech.Description.Cost * Control.Settings.AdaptModWeight;
                    if (mod > Control.Settings.MaxAdaptMod)
                        mod = Control.Settings.MaxAdaptMod;

                    var info2 = Proccesed[mechDef.Description.Id];

                    if (info.Omni && info2.Omni)
                        if (info.Special && info2.Special)
                            omnimod = Control.Settings.OmniSpecialtoSpecialMod;
                        else if (!info.Special && !info2.Special)
                            omnimod = Control.Settings.OmniNormalMod;
                        else
                            omnimod = Control.Settings.OmniSpecialtoNormalMod;



                    var price = (int)(cb * omnimod * mod * info.PriceMult * (Control.Settings.ApplyPartPriceMod ? info2.PriceMult : 1));

                    Control.LogDebug($"-- price for {mechDef.Description.UIName}({mechDef.Description.Id}) mechcost: {mechDef.Description.Cost}. price mod: {mod:0.000}, tag mod:{info2.PriceMult:0.000} omnimod:{omnimod:0.000} adopt price: {price}");
                    used_parts.Add(new parts_info(num, 0, price, mechDef.Description.UIName, mechDef.Description.Id));
                }
            }

            var options = new SimGameEventOption[4];


            for (int i = 0; i < 4; i++)
            {
                options[i] = new SimGameEventOption()
                {
                    Description = new BaseDescriptionDef($"test_{i}", $"test_{i}", $"test_{i}", ""),
                    RequirementList = null,
                    ResultSets = null
                };
            }

            var eventDef = new SimGameEventDef(
                SimGameEventDef.EventPublishState.PUBLISHED,
                SimGameEventDef.SimEventType.UNSELECTABLE,
                EventScope.Company,
                new DescriptionDef(
                    "CustomSalvageAssemblyEvent",
                    "Mech Assembly",
                    GetCurrentDescription(),
                    "uixTxrSpot_YangWorking.png",
                    0, 0, false, "", "", ""),
                new RequirementDef { Scope = EventScope.Company },
                new RequirementDef[0],
                new SimGameEventObject[0],
                options.ToArray(),
                1, true, new HBS.Collections.TagSet());

            if (!_hasInitEventTracker)
            {
                eventTracker.Init(new[] { EventScope.Company }, 0, 0, SimGameEventDef.SimEventType.NORMAL, mechBay.Sim);
                _hasInitEventTracker = true;
            }

            mechBay.Sim.InterruptQueue.QueueEventPopup(eventDef, EventScope.Company, eventTracker);

        }

        public static string GetCurrentDescription()
        {
            var text = new Text(mech.Description.UIName);
            var result = "Assembling <b><color=#20ff20>" + text.ToString() + "</color></b> Using `Mech Parts:\n";

            foreach (var info in used_parts)
            {
                if (info.used > 0)
                {
                    result +=
                        $"\n  <b>{info.mechname}</b>: <color=#20ff20>{info.used}</color> {(info.used == 1 ? "part" : "parts")}";
                    if (info.cbills > 0)
                    {
                        result += $", <color=#ffff00>{SimGameState.GetCBillString(info.cbills * info.used)}</color>";
                    }
                }
            }

            int cbills = used_parts.Sum(i => i.used * i.cbills);
            result += $"\n\n  <b>Total:</b> <color=#ffff00>{SimGameState.GetCBillString(cbills)}</color>";
            int left = chassis.MechPartMax - used_parts.Sum(i => i.used);

            if (left > 0)
                result += $"\n\nNeed <color=#ff2020>{left}</color> more {(left == 1 ? "part" : "parts")}";
            else
                result += $"\n\nPreparations complete. Proceed?";
            return result;
        }

        public static void MakeOptions(TextMeshProUGUI eventDescription, SGEventPanel sgEventPanel, DataManager dataManager, RectTransform optionParent, List<SGEventOption> optionsList)
        {
            void set_info(SGEventOption option, string text, UnityAction<SimGameEventOption> action)
            {
                Traverse.Create(option).Field<TextMeshProUGUI>("description").Value.SetText(text);
                option.OptionSelected.RemoveAllListeners();
                option.OptionSelected.AddListener(action);
            }

            void set_add_part(SGEventOption option, int num)
            {
                if (num < used_parts.Count)
                {
                    var info = used_parts[num];
                    if (info.used < info.count)
                    {
                        if (info.cbills > 0)
                            set_info(option, $"Add <color=#20ff20>{info.mechname}</color> for <color=#ffff00>{SimGameState.GetCBillString(info.cbills)}</color>, {info.count - info.used} {(info.count - info.used == 1 ? "part" : "parts") } parts left",
                                arg =>
                                {
                                    info.used += 1;
                                    MakeOptions(eventDescription, sgEventPanel, dataManager, optionParent, optionsList);
                                });
                        else
                            set_info(option, $"Add <color=#20ff20>{info.mechname}</color> {info.count - info.used} {(info.count - info.used == 1 ? "part" : "parts") } left",
                                arg =>
                                {
                                    info.used += 1;
                                    MakeOptions(eventDescription, sgEventPanel, dataManager, optionParent, optionsList);
                                });
                    }
                    else
                    {
                        set_info(option, $"<i><color=#a0a0a0>{info.mechname}</color>: <color=#ff4040>All parts used</color></i>", arg => { });
                    }
                }
                else
                {
                    set_info(option, "---", arg => { });
                }
            }

            int count = used_parts.Sum(i => i.used);

            eventDescription.SetText(GetCurrentDescription());


            if (count < mechBay.Sim.Constants.Story.DefaultMechPartMax)
            {
                if (used_parts.Count > 5)
                {
                    set_add_part(optionsList[0], 1 + page * 3);
                    set_add_part(optionsList[1], 2 + page * 3);
                    set_add_part(optionsList[2], 3 + page * 3);
                    set_info(optionsList[3], "Next Page >>", arg =>
                    {
                        page = (page + 1) % ((used_parts.Count - 1) / 3 + 1);
                        MakeOptions(eventDescription, sgEventPanel, dataManager, optionParent, optionsList);
                    });

                }
                else
                {
                    set_add_part(optionsList[0], 1);
                    set_add_part(optionsList[1], 2);
                    set_add_part(optionsList[2], 3);
                    set_add_part(optionsList[3], 4);
                }
            }
            else
            {
                int funds = mechBay.Sim.Funds;
                int total = used_parts.Sum(i => i.cbills * i.used);
                if (funds >= total)
                    set_info(optionsList[0], "Confirm", arg => { CompeteMech(); sgEventPanel.Dismiss(); });
                else
                    set_info(optionsList[0], "<color=#ff2020><i>Not enough C-Bills</i></color>", arg => { sgEventPanel.Dismiss(); });
                set_info(optionsList[1], "Cancel", arg => { sgEventPanel.Dismiss(); });
                set_info(optionsList[2], "---", arg => { });
                set_info(optionsList[3], "---", arg => { });

            }
        }

        private static void CompeteMech()
        {

            Control.LogDebug($"Compete mech {mech.Description.UIName}({mech.Description.Id})");
            try
            {
                Control.LogDebug($"-- remove parts");
                infoWidget.SetData(mechBay, null);
                foreach (var info in used_parts)
                {
                    if (info.used > 0)
                        RemoveMechPart(info.mechid, info.used);
                }

                int total = used_parts.Sum(i => i.cbills * i.used);
                Control.LogDebug($"-- take money {total}");
                mechBay.Sim.AddFunds(-total);
                used_parts.Clear();
                Control.LogDebug($"-- making mech");
                MakeMech(mechBay.Sim);
                Control.LogDebug($"-- refresh mechlab");
                mechBay.RefreshData(false);
            }
            catch (Exception e)
            {
                Control.LogError("Error in Complete Mech", e);
            }
        }
    }
}