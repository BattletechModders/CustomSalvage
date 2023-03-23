using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using CustomSalvage.MechBroke;
using CustomComponents;
using HBS.Collections;
using Localize;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

using Object = System.Object;

namespace CustomSalvage
{
    public static partial class ChassisHandler
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

                Log.Main.Debug?.Log($"Registring {mech.Description.Id}({mech.Description.UIName}) => {mech.ChassisID}");
                Log.Main.Debug?.Log($"-- PrefabID:{info.PrefabID} Exclude:{info.Excluded} MinParts:{info.MinParts} PriceMult:{info.PriceMult}");


                Proccesed[id] = info;
            }
        }


        private static mech_info GetMechInfo(MechDef mech)
        {
            string id = mech.Description.Id;
            int max_parts = UnityGameInstance.BattleTechGame.Simulation.Constants.Story.DefaultMechPartMax;
            var assembly = get_variant(mech);
            var info = new mech_info();
            var tags = GetMechTags(mech);
            info.Omni = !String.IsNullOrEmpty(Control.Instance.Settings.OmniTechTag) &&
                tags.Contains(Control.Instance.Settings.OmniTechTag);

            if (assembly != null && assembly.Exclude)
                info.Excluded = true;
            else if (assembly != null && assembly.Include)
                info.Excluded = false;
            else
            if (Control.Instance.Settings.ExcludeVariants.Contains(id))
                info.Excluded = true;
            else if (Control.Instance.Settings.ExcludeTags.Any(extag => mech.MechTags.Contains(extag)))
                info.Excluded = true;

            if (Control.Instance.Settings.SpecialTags != null && Control.Instance.Settings.SpecialTags.Length > 0)


                foreach (var tag_info in Control.Instance.Settings.SpecialTags)
                {
                    if (tags.Contains(tag_info.Tag))
                    {
                        info.MinParts = min_parts_special;
                        info.PriceMult *= tag_info.Mod;
                        info.Special = true;
                    }
                }

            if (info.Omni)
                info.MinParts = 1;

            if (assembly != null)
            {
                if (assembly.ReplacePriceMult)
                    info.PriceMult = assembly.PriceMult;
                else
                    info.PriceMult *= assembly.PriceMult;

                if (assembly.PartsMin >= 0)
                    info.MinParts = Mathf.CeilToInt(max_parts * assembly.PartsMin);
            }
            info.PrefabID = GetPrefabId(mech);
            return info;
        }

        public static string GetPrefabId(MechDef mech)
        {
            if (mech.Chassis.Is<AssemblyVariant>(out var a) && !String.IsNullOrEmpty(a.PrefabID))
                return a.PrefabID + mech.Chassis.Tonnage.ToString();
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
            Log.Main.Debug?.Log("================= CHASSIS TO MECH ===================");
            foreach (var mechDef in ChassisToMech)
                Log.Main.Debug?.Log($"{mechDef.Key} => {mechDef.Value.Description.Id}");

            Log.Main.Debug?.Log("================= EXCLUDED ===================");
            foreach (var info in Proccesed.Where(i => i.Value.Excluded))
                Log.Main.Debug?.Log($"{info.Key}");

            Log.Main.Debug?.Log("================= GROUPS ===================");
            foreach (var list in Compatible)
            {
                Log.Main.Debug?.Log($"{list.Key}");

                foreach (var item in list.Value)
                    Log.Main.Debug?.Log($"--- {item.Description.Id}");
            }
            Log.Main.Debug?.Log("================= INVENTORY ===================");
            foreach (var mechDef in PartCount)
                Log.Main.Debug?.Log($"{mechDef.Key}: [{mechDef.Value:00}]");
            Log.Main.Debug?.Log("============================================");
        }

        public class parts_info
        {
            public int count;
            public int used;
            public int spare;
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
                spare = 0;
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
                Log.Main.Debug?.Log($"-- remove parts");
                RemoveMechPart(mech.Description.Id, chassis.MechPartMax);
                infoWidget.SetData(mechBay, null);
                Log.Main.Debug?.Log($"-- making mech");
                MakeMech(mechBay.Sim, 0);
                Log.Main.Debug?.Log($"-- refresh mechlab");
                mechBay.RefreshData(false);
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log("Error in Complete Mech", e);
            }
        }

        private static void MakeMech(SimGameState sim, int other_parts)
        {
            Log.Main.Debug?.Log($"Mech Assembly started for {mech.Description.UIName}");
            MechDef new_mech = new MechDef(mech, mechBay.Sim.GenerateSimGameUID(), true);

            try
            {
                var clear = Control.Instance.Settings.UseGameSettingsUnequiped
                    ? !sim.Constants.Salvage.EquipMechOnSalvage
                    : Control.Instance.Settings.UnEquipedMech;

                if (clear)
                {
                    Log.Main.Debug?.Log($"-- Clear Inventory");
                    new_mech.SetInventory(DefaultHelper.ClearInventory(new_mech, mechBay.Sim));
                }
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log($"ERROR in ClearInventory", e);
            }

            switch (Control.Instance.Settings.MechBrokeType)
            {
                case BrokeType.Random:
                    RandomBroke.BrokeMech(new_mech, sim, other_parts);
                    break;
                case BrokeType.Normalized:
                    DiceBroke.BrokeMech(new_mech, sim, other_parts, used_parts.Sum(i => i.spare));
                    break;
            }

            try
            {
                Log.Main.Debug?.Log("-- Adding mech");
                mechBay.Sim.AddMech(0, new_mech, true, false, true, null);
                Log.Main.Debug?.Log("-- Posting Message");
                mechBay.Sim.MessageCenter.PublishMessage(new SimGameMechAddedMessage(new_mech, chassis.MechPartMax, true));
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log($"ERROR in MakeMech", e);
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

            try
            {
                _state = opt_state.Default;
                var list = GetCompatible(chassis.Description.Id);
                used_parts = new List<parts_info>();
                int c = GetCount(mech.Description.Id);

                used_parts.Add(new parts_info(c, c > chassis.MechPartMax ? chassis.MechPartMax : c, 0,
                    mech.Description.UIName, mech.Description.Id));
                var info = Proccesed[mech.Description.Id];

                var settings = Control.Instance.Settings;

                float cb = settings.AdaptPartBaseCost * mech.Description.Cost / chassis.MechPartMax;
                Log.Main.Debug?.Log($"base part price for {mech.Description.UIName}({mech.Description.Id}): {cb}. mechcost: {mech.Description.Cost} ");
                Log.Main.Debug?.Log($"-- setting:{settings.AdaptPartBaseCost}, maxparts:{chassis.MechPartMax}, minparts:{info.MinParts}, pricemult: {info.PriceMult}");


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



                        var price = (int)(cb * omnimod * mod * info.PriceMult *
                                           (settings.ApplyPartPriceMod ? info2.PriceMult : 1));

                        Log.Main.Debug?.Log($"-- price for {mechDef.Description.UIName}({mechDef.Description.Id}) mechcost: {mechDef.Description.Cost}. price mod: {mod:0.000}, tag mod:{info2.PriceMult:0.000} omnimod:{omnimod:0.000} adopt price: {price}");
                        used_parts.Add(
                            new parts_info(num, 0, price, mechDef.Description.UIName, mechDef.Description.Id));
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
                if (settings.MechBrokeType == BrokeType.Normalized)
                {
                    ConditionsHandler.Instance.PrepareCheck(mech, mechBay.Sim);
                    DiceBroke.PrepareTechKits(mech, mechBay.Sim);
                }

                var eventDef = new SimGameEventDef(
                    SimGameEventDef.EventPublishState.PUBLISHED,
                    SimGameEventDef.SimEventType.UNSELECTABLE,
                    EventScope.Company,
                    new DescriptionDef(
                        "CustomSalvageAssemblyEvent",
                        mech_type + " Assembly",
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
                    eventTracker.Init(new[] { EventScope.Company }, 0, 0, SimGameEventDef.SimEventType.NORMAL,
                        mechBay.Sim);
                    _hasInitEventTracker = true;
                }

                mechBay.Sim.InterruptQueue.QueueEventPopup(eventDef, EventScope.Company, eventTracker);
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log(e);
            }

        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetMechType(MechDef mech)
        {
            return "'Mech";
        }

        public static string GetCurrentDescription()
        {
            var strs = Control.Instance.Settings.Strings;
            var text = new Text(mech.Description.UIName);
            var result = $"Assembling <b><color=#20ff20>" + text.ToString() + $"</color></b> Using {mech_type} Parts:\n";

            foreach (var info in used_parts)
            {
                if (info.used > 0 || info.spare > 0)
                {
                    if (info.spare == 0)
                        result +=
                            $"\n  <b>{info.mechname}</b>: <color=#20ff20>{info.used}</color> {(info.used == 1 ? "part" : "parts")}";
                    else if (info.used == 0)
                        result +=
                            $"\n  <b>{info.mechname}</b>: <color=#ffff20>{info.spare}</color> {(info.spare == 1 ? "part" : "parts")}";
                    else
                        result +=
                            $"\n  <b>{info.mechname}</b>: <color=#20ff20>{info.used}</color><color=#ffff20>+{info.spare}</color> {(info.used + info.spare == 1 ? "part" : "parts")}";
                    if (info.cbills > 0 && info.used > 0)
                        result +=
                            $", <color=#ffff00>{SimGameState.GetCBillString(info.cbills * info.used)}</color>";
                }
            }

            if (Control.Instance.Settings.MechBrokeType == BrokeType.Normalized)
            {
                result += "\n\n";
                var parts = used_parts.Sum(i => i.used) - used_parts[0].used;
                var spare = used_parts.Sum(i => i.spare);

                //Control.Instance.Log("1");

                if (Control.Instance.Settings.ShowDetailBonuses)
                {
                    var bonuses = DiceBroke.GetBonusString(mech, UnityGameInstance.BattleTechGame.Simulation, parts, spare);
                    result += bonuses;
                }
                //Control.Instance.Log("2");
                int total = DiceBroke.GetBonus(mech, UnityGameInstance.BattleTechGame.Simulation, parts, spare);
                result += $"<b><color=#ffff00>{total,-4:+0;-#}" + new Text(strs.TotalBonusCatption) + "</color></b>";
                ////Control.Instance.Log("3");

                if (DiceBroke.SelectedTechKit != null)
                {
                    result += $"\n\nUsing <b>{DiceBroke.SelectedTechKit}</b>";
                }

                if (Control.Instance.Settings.ShowBrokeChances)
                {
                    result += "\n\n";
                    result += DiceBroke.GetResultString(total);
                    result += $"\nComponent damage chance: {Mathf.RoundToInt(100 * DiceBroke.GetComp(mech, UnityGameInstance.BattleTechGame.Simulation, parts, spare))}%";
                }
            }

            var cbills = GetCbills();

            result += $"\n\n  <b>Total:</b> <color=#ffff00>{SimGameState.GetCBillString(cbills)}</color>";
            int left = chassis.MechPartMax - used_parts.Sum(i => i.used);

            if (_state == opt_state.AddSpare)
                result += "\n\nSelect spare parts";
            else if (_state == opt_state.AddMechKit)
                result += "\n\nSelect TechKit";
            else if (left > 0)
                result += $"\n\nNeed <color=#ff2020>{left}</color> more {(left == 1 ? "part" : "parts")}";
            else
                result += $"\n\nPreparations complete. Proceed?";
            return result;
        }

        private static int GetCbills()
        {
            int cbills = used_parts.Sum(i => i.used * i.cbills);
            if (DiceBroke.SelectedTechKit != null)
            {
                var kit = DiceBroke.SelectedTechKit;
                cbills = (int) ((cbills + kit.CBill) * kit.CBIllMul);
            }

            return cbills;
        }

        private enum opt_state { Default, AddSpare, AddMechKit }

        private static opt_state _state = opt_state.Default;

        public static void MakeOptions(TextMeshProUGUI eventDescription, SGEventPanel sgEventPanel, DataManager dataManager, RectTransform optionParent, List<SGEventOption> optionsList)
        {
            var str = Control.Instance.Settings.Strings;

            void set_info(SGEventOption option, string text, UnityAction<SimGameEventOption> action)
            {
                option.description.SetText(text);
                option.OptionSelected.RemoveAllListeners();
                option.OptionSelected.AddListener(action);
            }

            void set_add_part(SGEventOption option, int num)
            {
                if (num < used_parts.Count)
                {
                    var info = used_parts[num];
                    var left = info.count - info.used - info.spare;

                    if (left > 0)
                    {
                        if (info.cbills > 0)
                            set_info(option, new Text(left == 1 ? str.ButtonAddPartMoney : str.ButtonAddPartsMoney,
                                info.mechname, SimGameState.GetCBillString(info.cbills), left).ToString(),
                                arg =>
                                {
                                    info.used += 1;
                                    MakeOptions(eventDescription, sgEventPanel, dataManager, optionParent, optionsList);
                                });
                        else
                            set_info(option, new Text(left == 1 ? str.ButtonAddPart : str.ButtonAddParts, info.mechname, left).ToString(),
                                arg =>
                                {
                                    info.used += 1;
                                    MakeOptions(eventDescription, sgEventPanel, dataManager, optionParent, optionsList);
                                });
                    }
                    else
                        set_info(option, new Text(str.ButtonAllPartsUsed, info.mechname).ToString(),
                            arg => { });
                }
                else
                {
                    set_info(option, "---", arg => { });
                }
            }

            void set_add_spare_part(SGEventOption option, int num)
            {
                if (num < used_parts.Count)
                {
                    var info = used_parts[num];
                    var left = info.count - info.used - info.spare;
                    if (left > 0)
                        set_info(option,
                            new Text( left == 1 ? str.ButtonAddPart : str.ButtonAddParts, info.mechname, left).ToString(),
                            arg =>
                            {
                                info.spare += 1;
                                if (used_parts.Sum(i => i.spare) >= Control.Instance.Settings.MaxSpareParts
                                    || used_parts.Sum(i => i.count - i.used - i.spare) <= 0)
                                    _state = opt_state.Default;
                                MakeOptions(eventDescription, sgEventPanel, dataManager, optionParent, optionsList);
                            });
                    else
                        set_info(option,
                            new Text(str.ButtonAllPartsUsed, info.mechname).ToString(),
                            arg => { });
                }
                else if (num == used_parts.Count)
                {
                    set_info(option, new Text(str.ButtonClearSpare).ToString(), arg =>
                    {
                        foreach (var partsInfo in used_parts)
                            partsInfo.spare = 0;
                        MakeOptions(eventDescription, sgEventPanel, dataManager, optionParent, optionsList);
                    });
                }
                else if (num == used_parts.Count + 1)
                {
                    set_info(option, new Text(str.ButtonApplySpare).ToString(), arg =>
                    {
                        _state = opt_state.Default;
                        MakeOptions(eventDescription, sgEventPanel, dataManager, optionParent, optionsList);
                    });
                }
                else
                    set_info(option, "---", arg => { });
            }

            void set_add_techkit(SGEventOption option, int num)
            {
                if (num < DiceBroke.CompatibleTechKits.Count)
                {
                    var info = DiceBroke.CompatibleTechKits[num];
                    set_info(option, new Text(info.Item1.ToString() + " " + info.Item2.ToString() + " left").ToString(),
                            arg => {
                                DiceBroke.SelectedTechKit = info.Item1;
                                _state = opt_state.Default;
                                MakeOptions(eventDescription, sgEventPanel, dataManager, optionParent, optionsList);
                            });
                }
                else
                {
                    set_info(option, "---", arg => { });
                }
            }

            int count = used_parts.Sum(i => i.used);

            eventDescription.SetText(GetCurrentDescription());

            if (_state == opt_state.AddMechKit)
            {
                if (DiceBroke.CompatibleTechKits.Count > 4)
                {
                    set_add_techkit(optionsList[0], 0 + page * 3);
                    set_add_techkit(optionsList[1], 1 + page * 3);
                    set_add_techkit(optionsList[2], 2 + page * 3);
                    set_info(optionsList[3], new Text(str.ButtonNextPage).ToString(), arg =>
                    {
                        page = (page + 1) % ((DiceBroke.CompatibleTechKits.Count + 2) / 3 + 1);
                        MakeOptions(eventDescription, sgEventPanel, dataManager, optionParent, optionsList);
                    });
                }
                else
                {
                    set_add_techkit(optionsList[0], 0);
                    set_add_techkit(optionsList[1], 1);
                    set_add_techkit(optionsList[2], 2);
                    set_add_techkit(optionsList[3], 3);
                }
            }
            else if (_state == opt_state.AddSpare)
            {
                if (used_parts.Count + 2 > 4)
                {
                    set_add_spare_part(optionsList[0], 0 + page * 3);
                    set_add_spare_part(optionsList[1], 1 + page * 3);
                    set_add_spare_part(optionsList[2], 2 + page * 3);
                    set_info(optionsList[3], new Text(str.ButtonNextPage).ToString(), arg =>
                    {
                        page = (page + 1) % ((used_parts.Count + 2) / 3 + 1);
                        MakeOptions(eventDescription, sgEventPanel, dataManager, optionParent, optionsList);
                    });

                }
                else
                {
                    set_add_spare_part(optionsList[0], 0);
                    set_add_spare_part(optionsList[1], 1);
                    set_add_spare_part(optionsList[2], 2);
                    set_add_spare_part(optionsList[3], 3);
                }
            }
            else if (count < mechBay.Sim.Constants.Story.DefaultMechPartMax)
            {
                if (used_parts.Count > 5)
                {
                    set_add_part(optionsList[0], 1 + page * 3);
                    set_add_part(optionsList[1], 2 + page * 3);
                    set_add_part(optionsList[2], 3 + page * 3);
                    set_info(optionsList[3], new Text(str.ButtonNextPage).ToString(), arg =>
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
                int total = GetCbills();
                if (funds >= total)
                    set_info(optionsList[0], new Text(str.ButtonConfirm).ToString(), arg => { CompeteMech(); sgEventPanel.Dismiss(); });
                else
                    set_info(optionsList[0], new Text(str.ButtonNoMoney).ToString(), arg => { sgEventPanel.Dismiss(); });
                if (Control.Instance.Settings.MechBrokeType == BrokeType.Normalized)
                {
                    if (used_parts.Sum(i => i.count - i.used) == 0)
                        set_info(optionsList[1], new Text(str.ButtonNoSpare).ToString(), arg => { });
                    else if (used_parts.Sum(i => i.spare) < Control.Instance.Settings.MaxSpareParts
                            && used_parts.Sum(i => i.count - i.used - i.spare) > 0)
                        set_info(optionsList[1], new Text(str.ButtonAddSpare).ToString(), arg =>
                        {
                            _state = opt_state.AddSpare;
                            MakeOptions(eventDescription, sgEventPanel, dataManager, optionParent, optionsList);
                        });
                    else
                        set_info(optionsList[1], new Text(str.ButtonClearSpare).ToString(), arg =>
                        {
                            foreach (var partsInfo in used_parts)
                                partsInfo.spare = 0;
                            MakeOptions(eventDescription, sgEventPanel, dataManager, optionParent, optionsList);
                        });

                    if (DiceBroke.CompatibleTechKits.Count > 0)
                        if (DiceBroke.SelectedTechKit == null)
                            set_info(optionsList[2], new Text(str.ButtonAddTechKit).ToString(), arg =>
                            {
                                _state = opt_state.AddMechKit;
                                MakeOptions(eventDescription, sgEventPanel, dataManager, optionParent, optionsList);
                            });
                        else
                            set_info(optionsList[2], new Text(str.ButtonClearTechKit).ToString(), arg =>
                            {
                                DiceBroke.SelectedTechKit = null;
                                _state = opt_state.Default;
                                MakeOptions(eventDescription, sgEventPanel, dataManager, optionParent, optionsList);
                            });
                    else
                        set_info(optionsList[2], new Text(str.ButtonNoTechKit).ToString(), arg => { });
                }
                else
                {
                    set_info(optionsList[1], "---", arg => { });
                    set_info(optionsList[2], "---", arg => { });
                }

                set_info(optionsList[3], new Text(str.ButtonCancel).ToString(), arg => { sgEventPanel.Dismiss(); });
            }
        }

        private static void CompeteMech()
        {

            Log.Main.Debug?.Log($"Compete mech {mech.Description.UIName}({mech.Description.Id})()");
            try
            {
                Log.Main.Debug?.Log($"-- remove parts");
                infoWidget.SetData(mechBay, null);
                foreach (var info in used_parts)
                {
                    if (info.used > 0)
                        RemoveMechPart(info.mechid, info.used + info.spare);
                }

                int total = GetCbills();
                if(DiceBroke.SelectedTechKit != null)
                    mechBay.Sim.RemoveItemStat(DiceBroke.SelectedTechKit.Def.Description.Id, typeof(UpgradeDef), false);

                Log.Main.Debug?.Log($"-- take money {total}");
                mechBay.Sim.AddFunds(-total);
                //foreach (var item in used_parts)
                //    Control.Instance.Log($"- {item.mechid}[{item.mechname}] {item.used}/{item.spare}/{item.count}");
                var op = used_parts.Where(i => i.mechid != mech.Description.Id).Sum(i => i.used);
                Log.Main.Debug?.Log($"-- making mech other_parts:{op}");
                MakeMech(mechBay.Sim, op);
                used_parts.Clear();
                Log.Main.Debug?.Log($"-- refresh mechlab");
                mechBay.RefreshData(false);
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log("Error in Complete Mech", e);
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

            if (mech.Chassis.Is<LootableMech>(out var lm))
            {
                Log.Main.Debug?.Log($"--- Mech Replacing with {lm.ReplaceID}");
                try
                {
                    result = UnityGameInstance.BattleTechGame.Simulation.DataManager.MechDefs.Get(lm.ReplaceID);
                    if (result == null)
                    {
                        Log.Main.Error?.Log($"---unknown mech {lm.ReplaceID}, rollback");
                    }
                }
                catch
                {
                    result = null;
                }

                if (result == null)
                {
                    Log.Main.Error?.Log($"---unknown mech {lm.ReplaceID}, rollback");
                    result = mech;
                }
            }

            var id = GetMDefFromCDef(mech.ChassisID);
            return UnityGameInstance.BattleTechGame.DataManager.MechDefs.TryGet(id, out mech) ? result : null;
        }

        private static Dictionary<string, HashSet<string>> mechtags = new Dictionary<string, HashSet<string>>();

        public static HashSet<string> GetMechTags(MechDef mech)
        {
            if (mech?.Chassis == null)
                return null;

            if (mechtags.TryGetValue(mech.ChassisID, out var res))
                return res;

            var new_tags = build_mech_tags(mech);
            mechtags[mech.ChassisID] = new_tags;
            return new_tags;
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
                            Log.Main.Debug?.Log(logstr);
#endif

                        }
                    }

                    if (numb > 0)
                        BaseTP = sumb / numb;
                    if (numl > 0)
                        LimbTP = suml / numl;
                    if (numc > 0)
                        CompTP = sumc / numc;

                    Log.Main.Debug?.Log($"totals: base:{BaseTP}, limb:{LimbTP:0.000}, component:{CompTP:0.000}");

                    var tp = sim.MechTechSkill - BaseTP;
                    var ltp = Mathf.Clamp(tp * LimbTP, -settings.RepairTPMaxEffect, settings.RepairTPMaxEffect);
                    var ctp = Mathf.Clamp(tp * CompTP, -settings.RepairTPMaxEffect, settings.RepairTPMaxEffect);

                    Log.Main.Debug?.Log($"LeftTP: {tp} limb_change = {ltp:0.000} comp_change = {ctp * CompTP:0.000}");
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
}