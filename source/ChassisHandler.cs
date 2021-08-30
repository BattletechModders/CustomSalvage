﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
#if USE_CC
using CustomComponents;
#endif
using Harmony;
using HBS.Collections;
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
            public string PrefabID;

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
        public static Dictionary<string, List<MechDef>> Compatible = new Dictionary<string, List<MechDef>>();

        public static Dictionary<string, mech_info> Proccesed = new Dictionary<string, mech_info>();

        public static void RegisterMechDef(MechDef mech, int part_count = 0)
        {
            int max_parts = UnityGameInstance.BattleTechGame.Simulation.Constants.Story.DefaultMechPartMax;
            min_parts = Mathf.CeilToInt(max_parts * Control.Instance.Settings.MinPartsToAssembly);
            min_parts_special = Mathf.CeilToInt(max_parts * Control.Instance.Settings.MinPartsToAssemblySpecial);

            ChassisToMech[mech.ChassisID] = mech;
            if (part_count > 0)
                PartCount[mech.Description.Id] = part_count;
            string id = mech.Description.Id;

            if (!Proccesed.ContainsKey(id))
            {

                var info = GetMechInfo(mech);

                if (!info.Excluded && Control.Instance.Settings.AssemblyVariants)
                {

                    if (!Compatible.TryGetValue(info.PrefabID, out var list))
                    {
                        list = new List<MechDef>();
                        Compatible[info.PrefabID] = list;
                    }

                    if (list.All(i => i.Description.Id != id))
                        list.Add(mech);
                }

                Control.Instance.LogDebug($"Registring {mech.Description.Id}({mech.Description.UIName}) => {mech.ChassisID}");
                Control.Instance.LogDebug($"-- PrefabID:{info.PrefabID} Exclude:{info.Excluded} MinParts:{info.MinParts} PriceMult:{info.PriceMult}");


                Proccesed[id] = info;
            }
        }
        private static mech_info GetMechInfo(MechDef mech)
        {
            string id = mech.Description.Id;
            int max_parts = UnityGameInstance.BattleTechGame.Simulation.Constants.Story.DefaultMechPartMax;
#if USE_CC
            var assembly = mech.Chassis.GetComponent<AssemblyVariant>();
#endif
            var info = new mech_info();
            info.Omni = !String.IsNullOrEmpty(Control.Instance.Settings.OmniTechTag) && (
                mech.Chassis.ChassisTags.Contains(Control.Instance.Settings.OmniTechTag) ||
                mech.MechTags.Contains(Control.Instance.Settings.OmniTechTag));

#if USE_CC
            if (assembly != null && assembly.Exclude)
                info.Excluded = true;
            else if (assembly != null && assembly.Include)
                info.Excluded = false;
            else
#endif
            if (Control.Instance.Settings.ExcludeVariants.Contains(id))
                info.Excluded = true;
            else if (Control.Instance.Settings.ExcludeTags.Any(extag => mech.MechTags.Contains(extag)))
                info.Excluded = true;

            if (Control.Instance.Settings.SpecialTags != null && Control.Instance.Settings.SpecialTags.Length > 0)


                foreach (var tag_info in Control.Instance.Settings.SpecialTags)
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
            info.PrefabID = GetPrefabId(mech);
            return info;
        }

        public static string GetPrefabId(MechDef mech)
        {
#if USE_CC
            if (mech.Chassis.Is<AssemblyVariant>(out var a) && !String.IsNullOrEmpty(a.PrefabID))
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
            Control.Instance.LogDebug("================= CHASSIS TO MECH ===================");
            foreach (var mechDef in ChassisToMech)
                Control.Instance.LogDebug($"{mechDef.Key} => {mechDef.Value.Description.Id}");

            Control.Instance.LogDebug("================= EXCLUDED ===================");
            foreach (var info in Proccesed.Where(i => i.Value.Excluded))
                Control.Instance.LogDebug($"{info.Key}");

            Control.Instance.LogDebug("================= GROUPS ===================");
            foreach (var list in Compatible)
            {
                Control.Instance.LogDebug($"{list.Key}");

                foreach (var item in list.Value)
                    Control.Instance.LogDebug($"--- {item.Description.Id}");
            }
            Control.Instance.LogDebug("================= INVENTORY ===================");
            foreach (var mechDef in PartCount)
                Control.Instance.LogDebug($"{mechDef.Key}: [{mechDef.Value:00}]");
            Control.Instance.LogDebug("============================================");
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
        private static string mech_type;
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
                Control.Instance.LogDebug($"-- remove parts");
                RemoveMechPart(mech.Description.Id, chassis.MechPartMax);
                infoWidget.SetData(mechBay, null);
                Control.Instance.LogDebug($"-- making mech");
                MakeMech(mechBay.Sim, 0);
                Control.Instance.LogDebug($"-- refresh mechlab");
                mechBay.RefreshData(false);
            }
            catch (Exception e)
            {
                Control.Instance.LogError("Error in Complete Mech", e);
            }
        }

        private static void MakeMech(SimGameState sim, int other_parts)
        {
            Control.Instance.LogDebug($"Mech Assembly started for {mech.Description.UIName}");
            MechDef new_mech = new MechDef(mech, mechBay.Sim.GenerateSimGameUID(), true);

            try
            {
                var clear = Control.Instance.Settings.UseGameSettingsUnequiped
                    ? !sim.Constants.Salvage.EquipMechOnSalvage
                    : Control.Instance.Settings.UnEquipedMech;

                if (clear)
                {
                    Control.Instance.LogDebug($"-- Clear Inventory");
#if USE_CC
                    new_mech.SetInventory(DefaultHelper.ClearInventory(new_mech, mechBay.Sim));
#else
                new_mech.SetInventory(new MechComponentRef[0]);
#endif
                }
            }
            catch (Exception e)
            {
                Control.Instance.LogError($"ERROR in ClearInventory", e);
            }

            if (Control.Instance.Settings.BrokenMech)
            {
                BrokeMech(new_mech, sim, other_parts);

            }

            try
            {
                Control.Instance.LogDebug("-- Adding mech");
                mechBay.Sim.AddMech(0, new_mech, true, false, true, null);
                Control.Instance.LogDebug("-- Posting Message");
                mechBay.Sim.MessageCenter.PublishMessage(new SimGameMechAddedMessage(new_mech, chassis.MechPartMax, true));
            }
            catch (Exception e)
            {
                Control.Instance.LogError($"ERROR in MakeMach", e);
            }

        }

        public class AssemblyChancesResult
        {
            public float LimbChance { get; private set; } = Control.Instance.Settings.RepairMechLimbsChance;
            public float CompFChance { get; private set; } = Control.Instance.Settings.RepairComponentsFunctionalThreshold;
            public float CompNFChance { get; private set; } = Control.Instance.Settings.RepairComponentsNonFunctionalThreshold;

            public int BaseTP { get; private set; } = Control.Instance.Settings.BaseTP;
            public float LimbTP { get; private set; } = Control.Instance.Settings.LimbChancePerTp;
            public float CompTP { get; private set; } = Control.Instance.Settings.ComponentChancePerTp;
#if CCDEBUG
            public string DEBUGText { get; private set; } = "";
#endif
            public AssemblyChancesResult(MechDef mech, SimGameState sim, int other_parts)
            {
                var settings = Control.Instance.Settings;
                if (settings.RepairChanceByTP)
                {
                    if (settings.BrokeByTag != null && settings.BrokeByTag.Length > 1)
                    {
                        int numb = 0;
                        int numl = 0;
                        int numc = 0;

                        int sumb = 0;
                        float suml = 0;
                        float sumc = 0;

                        foreach (var info in settings.BrokeByTag)
                        {
                            if (mech.MechTags.Contains(info.tag) || mech.Chassis.ChassisTags.Contains(info.tag))
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
                                Control.Instance.LogDebug(logstr);
#endif

                            }
                        }

                        if (numb > 0)
                            BaseTP = sumb / numb;
                        if (numl > 0)
                            LimbTP = suml / numl;
                        if (numc > 0)
                            CompTP = sumc / numc;

                        Control.Instance.LogDebug($"totals: base:{BaseTP}, limb:{LimbTP:0.000}, component:{CompTP:0.000}");

                        var tp = sim.MechTechSkill - BaseTP;
                        var ltp = Mathf.Clamp(tp * LimbTP, -settings.RepairTPMaxEffect, settings.RepairTPMaxEffect);
                        var ctp = Mathf.Clamp(tp * CompTP, -settings.RepairTPMaxEffect, settings.RepairTPMaxEffect);

                        Control.Instance.LogDebug($"LeftTP: {tp} limb_change = {ltp:0.000} comp_change = {ctp * CompTP:0.000}");
#if CCDEBUG
                        var oLimbChance = LimbChance;
                        var oCompFChance = CompFChance;
                        var oCompNFChance = CompNFChance;
#endif
                        LimbChance = Mathf.Clamp(LimbChance + ltp, settings.LimbMinChance, settings.LimbMaxChance);
                        CompFChance = Mathf.Clamp(CompFChance + ctp, settings.ComponentMinChance, settings.ComponentMaxChance);
                        CompNFChance = Mathf.Clamp(CompNFChance + ctp, CompFChance, settings.ComponentMaxChance);

#if CCDEBUG
                        DEBUGText = $"\nLTP : {LimbTP:0.000}/{ltp:0.000}/{(int)(oLimbChance * 100)}%";
                        DEBUGText = $"\nCTP : {CompTP:0.000}/{ctp:0.000}/{(int)(oCompFChance * 100)}%/{(int)(oCompNFChance * 100)}%";
#endif
                    }
                }
            }
        }

        private static void BrokeMech(MechDef new_mech, SimGameState sim, int other_parts)
        {
            try
            {

                Control.Instance.LogDebug($"-- broke parts");
                var rnd = new Random();
                var chances = new AssemblyChancesResult(new_mech, sim, other_parts);
                var settings = Control.Instance.Settings;

                Control.Instance.LogDebug($"--- RepairMechLimbsChance: {chances.LimbChance}, RepairMechLimbs: {settings.RepairMechLimbs} ");
                float roll = 0;
                //hd
                roll = (float)rnd.NextDouble();
                Control.Instance.LogDebug($"--- HeadRepaired: {settings.HeadRepaired}, roll: {roll} ");
                if (!settings.HeadRepaired && (!settings.RepairMechLimbs ||
                                               roll > chances.LimbChance))
                    new_mech.Head.CurrentInternalStructure = 0f;
                else if (settings.RandomStructureOnRepairedLimbs)
                    new_mech.Head.CurrentInternalStructure *= Math.Min(settings.MinStructure, (float)rnd.NextDouble());

                //ct
                roll = (float)rnd.NextDouble();
                Control.Instance.LogDebug($"--- CentralTorsoRepaired: {settings.CentralTorsoRepaired}, roll: {roll} ");
                if (!settings.CentralTorsoRepaired && (!settings.RepairMechLimbs ||
                                                       roll > chances.LimbChance))
                    new_mech.CenterTorso.CurrentInternalStructure = 0f;
                else if (settings.RandomStructureOnRepairedLimbs)
                    new_mech.CenterTorso.CurrentInternalStructure *= Math.Min(settings.MinStructure, (float)rnd.NextDouble());

                //rt
                roll = (float)rnd.NextDouble();
                Control.Instance.LogDebug($"--- RightTorsoRepaired: {settings.RightTorsoRepaired}, roll: {roll} ");
                if (!settings.RightTorsoRepaired && (!settings.RepairMechLimbs ||
                                                     roll > chances.LimbChance))
                    new_mech.RightTorso.CurrentInternalStructure = 0f;
                else if (settings.RandomStructureOnRepairedLimbs)
                    new_mech.RightTorso.CurrentInternalStructure *= Math.Min(settings.MinStructure, (float)rnd.NextDouble());

                //lt
                roll = (float)rnd.NextDouble();
                Control.Instance.LogDebug($"--- LeftTorsoRepaired: {settings.LeftTorsoRepaired}, roll: {roll} ");
                if (!settings.LeftTorsoRepaired && (!settings.RepairMechLimbs ||
                                                    roll > chances.LimbChance))
                    new_mech.LeftTorso.CurrentInternalStructure = 0f;
                else if (settings.RandomStructureOnRepairedLimbs)
                    new_mech.LeftTorso.CurrentInternalStructure *= Math.Min(settings.MinStructure, (float)rnd.NextDouble());

                //ra
                roll = (float)rnd.NextDouble();
                Control.Instance.LogDebug($"--- RightArmRepaired: {settings.RightArmRepaired}, roll: {roll} ");
                if (!settings.RightArmRepaired && (!settings.RepairMechLimbs ||
                                                   roll > chances.LimbChance))
                    new_mech.RightArm.CurrentInternalStructure = 0f;
                else if (settings.RandomStructureOnRepairedLimbs)
                    new_mech.RightArm.CurrentInternalStructure *= Math.Min(settings.MinStructure, (float)rnd.NextDouble());

                //la
                roll = (float)rnd.NextDouble();
                Control.Instance.LogDebug($"--- LeftArmRepaired: {settings.LeftArmRepaired}, roll: {roll} ");
                if (!settings.LeftArmRepaired && (!settings.RepairMechLimbs ||
                                                  roll > chances.LimbChance))
                    new_mech.LeftArm.CurrentInternalStructure = 0f;
                else if (settings.RandomStructureOnRepairedLimbs)
                    new_mech.LeftArm.CurrentInternalStructure *= Math.Min(settings.MinStructure, (float)rnd.NextDouble());

                //rl

                roll = (float)rnd.NextDouble();
                Control.Instance.LogDebug($"--- RightLegRepaired: {settings.RightLegRepaired}, roll: {roll} ");
                if (!settings.RightLegRepaired && (!settings.RepairMechLimbs ||
                                                   roll > chances.LimbChance))
                    new_mech.RightLeg.CurrentInternalStructure = 0f;
                else if (settings.RandomStructureOnRepairedLimbs)
                    new_mech.RightLeg.CurrentInternalStructure *= Math.Min(settings.MinStructure, (float)rnd.NextDouble());

                //ll
                Control.Instance.LogDebug($"--- LeftLegRepaired: {settings.LeftLegRepaired}, roll: {roll} ");
                roll = (float)rnd.NextDouble();
                if (!settings.LeftLegRepaired && (!settings.RepairMechLimbs ||
                                                  roll > chances.LimbChance))
                    new_mech.LeftLeg.CurrentInternalStructure = 0f;
                else if (settings.RandomStructureOnRepairedLimbs)
                    new_mech.LeftLeg.CurrentInternalStructure *= Math.Min(settings.MinStructure, (float)rnd.NextDouble());

                Control.Instance.LogDebug($"-- broke equipment");

                foreach (var cref in new_mech.Inventory)
                {
                    if (new_mech.IsLocationDestroyed(cref.MountedLocation))
                    {
                        Control.Instance.LogDebug($"---- {cref.ComponentDefID} - location destroyed");
                        cref.DamageLevel = ComponentDamageLevel.Destroyed;
                    }
                    else if (settings.RepairMechComponents)
                    {
                        roll = (float)rnd.NextDouble();

                        if (roll < chances.CompFChance)
                        {
                            Control.Instance.LogDebug(
                                $"---- {cref.ComponentDefID} - {roll} vs {chances.CompFChance} - repaired ");
                            cref.DamageLevel = ComponentDamageLevel.Functional;
                        }
                        else if (roll < chances.CompNFChance)
                        {
                            Control.Instance.LogDebug(
                                $"---- {cref.ComponentDefID} - {roll} vs {chances.CompNFChance} - broken ");
                            cref.DamageLevel = ComponentDamageLevel.NonFunctional;
                        }
                        else
                        {
                            Control.Instance.LogDebug(
                                $"---- {cref.ComponentDefID} - {roll} vs {chances.CompNFChance} - fubar ");
                            cref.DamageLevel = ComponentDamageLevel.Destroyed;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Control.Instance.LogError($"ERROR in BrokeParts", e);
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

            var settings = Control.Instance.Settings;

            float cb = settings.AdaptPartBaseCost * mech.Description.Cost / chassis.MechPartMax;
            Control.Instance.LogDebug($"base part price for {mech.Description.UIName}({mech.Description.Id}): {cb}. mechcost: {mech.Description.Cost} ");
            Control.Instance.LogDebug($"-- setting:{settings.AdaptPartBaseCost}, maxparts:{chassis.MechPartMax}, minparts:{info.MinParts}, pricemult: {info.PriceMult}");

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
                                (float)mech.Description.Cost * settings.AdaptModWeight;
                    if (mod > settings.MaxAdaptMod)
                        mod = settings.MaxAdaptMod;

                    var info2 = Proccesed[mechDef.Description.Id];

                    if (info.Omni && info2.Omni)
                        if (info.Special && info2.Special)
                            omnimod = settings.OmniSpecialtoSpecialMod;
                        else if (!info.Special && !info2.Special)
                            omnimod = settings.OmniNormalMod;
                        else
                            omnimod = settings.OmniSpecialtoNormalMod;



                    var price = (int)(cb * omnimod * mod * info.PriceMult * (settings.ApplyPartPriceMod ? info2.PriceMult : 1));

                    Control.Instance.LogDebug($"-- price for {mechDef.Description.UIName}({mechDef.Description.Id}) mechcost: {mechDef.Description.Cost}. price mod: {mod:0.000}, tag mod:{info2.PriceMult:0.000} omnimod:{omnimod:0.000} adopt price: {price}");
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

            mech_type = GetMechType(mech);


            var eventDef = new SimGameEventDef(
                SimGameEventDef.EventPublishState.PUBLISHED,
                SimGameEventDef.SimEventType.UNSELECTABLE,
                EventScope.Company,
                new DescriptionDef(
                    "CustomSalvageAssemblyEvent",
                    mech_type +" Assembly",
                    GetCurrentDescription(),
                    "uixTxrSpot_YangWorking.png",
                    0, 0, false, "", "", ""),
                new RequirementDef { Scope = EventScope.Company },
                new RequirementDef[0],
                new SimGameEventObject[0],
                options.ToArray(),
                1, true, new TagSet());

            if (!_hasInitEventTracker)
            {
                eventTracker.Init(new[] { EventScope.Company }, 0, 0, SimGameEventDef.SimEventType.NORMAL, mechBay.Sim);
                _hasInitEventTracker = true;
            }

            mechBay.Sim.InterruptQueue.QueueEventPopup(eventDef, EventScope.Company, eventTracker);

        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetMechType(MechDef mech)
        {
           return "'Mech";
        }

        public static string GetCurrentDescription()
        {
            var text = new Text(mech.Description.UIName);
            var result = $"Assembling <b><color=#20ff20>" + text.ToString() + $"</color></b> Using {mech_type} Parts:\n";

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
                            set_info(option, $"Add <color=#20ff20>{info.mechname}</color> for <color=#ffff00>{SimGameState.GetCBillString(info.cbills)}</color>, {info.count - info.used} {(info.count - info.used == 1 ? "part" : "parts") }  left",
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

            Control.Instance.LogDebug($"Compete mech {mech.Description.UIName}({mech.Description.Id})");
            try
            {
                Control.Instance.LogDebug($"-- remove parts");
                infoWidget.SetData(mechBay, null);
                foreach (var info in used_parts)
                {
                    if (info.used > 0)
                        RemoveMechPart(info.mechid, info.used);
                }

                int total = used_parts.Sum(i => i.cbills * i.used);
                Control.Instance.LogDebug($"-- take money {total}");
                mechBay.Sim.AddFunds(-total);
                Control.Instance.LogDebug($"-- making mech");
                MakeMech(mechBay.Sim, used_parts.Where(i => i.mechid != mech.Description.Id).Sum(i => i.count));
                used_parts.Clear();
                Control.Instance.LogDebug($"-- refresh mechlab");
                mechBay.RefreshData(false);
            }
            catch (Exception e)
            {
                Control.Instance.LogError("Error in Complete Mech", e);
            }
        }

        public static string GetMDefFromCDef(string cdefid)
        {
            return cdefid.Replace("chassisdef", "mechdef");
        }

        public static MechDef FindMechReplace(MechDef mech)
        {
            if (mech == null)
                return null;
            var result = mech;

#if USE_CC
            if (mech.Chassis.Is<LootableMech>(out var lm))
            {
                Control.Instance.LogDebug($"--- Mech Replacing with {lm.ReplaceID}");
                try
                {
                    result = UnityGameInstance.BattleTechGame.Simulation.DataManager.MechDefs.Get(lm.ReplaceID);
                    if (result == null)
                    {
                        Control.Instance.LogError($"---unknown mech {lm.ReplaceID}, rollback");
                    }
                }
                catch
                {
                    result = null;
                }

                if (result == null)
                {
                    Control.Instance.LogError($"---unknown mech {lm.ReplaceID}, rollback");
                    result = mech;
                }
            }

#endif

            var id = GetMDefFromCDef(mech.ChassisID);
            return UnityGameInstance.BattleTechGame.DataManager.MechDefs.TryGet(id, out mech) ? result : null;
        }
    }
}