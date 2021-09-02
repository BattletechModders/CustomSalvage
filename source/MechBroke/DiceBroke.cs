using System.Collections.Generic;
using System.Linq;
using System.Text;
using BattleTech;
using BattleTech.UI;
using FluffyUnderware.DevTools.Extensions;
using HBS.Logging;
using JetBrains.Annotations;
using Localize;
using Steamworks;
using UnityEngine;

namespace CustomSalvage.MechBroke
{
    public static class DiceBroke
    {
        readonly static ChassisLocations[] locs = new ChassisLocations[]
        {
            ChassisLocations.Head,
            ChassisLocations.RightTorso,ChassisLocations.LeftTorso,
            ChassisLocations.RightArm,ChassisLocations.LeftArm,
            ChassisLocations.RightLeg,ChassisLocations.LeftLeg,
        };
        readonly static ChassisLocations[] all_parts = new ChassisLocations[]
        {
            ChassisLocations.Head, ChassisLocations.CenterTorso,
            ChassisLocations.RightTorso,ChassisLocations.LeftTorso,
            ChassisLocations.RightArm,ChassisLocations.LeftArm,
            ChassisLocations.RightLeg,ChassisLocations.LeftLeg,
        };

        private static List<TechKitCustom> tech_kits = new List<TechKitCustom>();


        public static TechKitCustom SelectedTechKit { get; set; }
        public static List<TechKitCustom> CompatibleTechKits { get; set; }
        public static List<ChassisHandler.parts_info> SpareParts { get; set; }

        private static readonly float[] probs =
        {
            2.77f,
            5.55f,
            8.33f,
            11.11f,
            13.88f,
            16.66f,
            13.88f,
            11.11f,
            8.33f,
            5.55f,
            2.77f
        };

        public static (int roll, int total) BrokeMech(MechDef mech, SimGameState sim, int other_parts, int spare_parts)
        {
            var target = GetBonus(mech, sim, other_parts, spare_parts);
            var roll = Random.Range(1, 7) + Random.Range(1, 7);
            target += roll;

            var parts = locs.ToList();
            var remove = ChassisLocations.None;

            (bool ct_remove, int parts_to_remove) = get_parts_to_remove(target);
            if (ct_remove)
            {
                remove.Set(ChassisLocations.CenterTorso);
                parts_to_remove -= 1;
            }


            for (int i = 0; i < parts_to_remove; i++)
            {
                int n = Random.Range(0, parts.Count);
                remove.Set(parts[n]);
                parts.RemoveAt(n);
            }

            if (target < 6)
                remove.Set(ChassisLocations.CenterTorso);

            foreach (var to_remove in all_parts)
                BrokeTools.BrokeLocation(mech, to_remove, remove.HasFlag(to_remove));

            BrokeTools.BrokeEquipment(mech, 0.25f, 0.75f);

            return (roll, target);
        }

        private static (bool, int) get_parts_to_remove(int target)
        {
            var s = Control.Instance.Settings;
            int n = Mathf.Clamp(target, s.MinRoll, s.MaxRoll);
            return (n >= s.CTRoll, s.partresults[n]);
        }

        private static int parts_bonus(int parts)
        {
            return -Mathf.RoundToInt(parts * Control.Instance.Settings.PartPenalty[UnityGameInstance.BattleTechGame.Simulation.Constants.Story.DefaultMechPartMax])
                - (parts > 0 ? 1 : 0);
        }

        public static int GetBonus(MechDef mech, SimGameState sim, int other_parts, int spare_parts)
        {
            //penalties
            //Control.Instance.LogError("1-1");
            var tags = ChassisHandler.GetMechTags(mech);
            //Control.Instance.LogError("1-2");

            var result = Tags.Instance.AllCSTags.Sum(i => i.GetValue(mech, tags, sim));
            //Control.Instance.LogError("1-3");
            var parts = sim.Constants.Story.DefaultMechPartMax;
            result += parts_bonus(other_parts);
            result += tp_bonus(sim);
            result += spare_parts;
            //bonuses
            return result;
        }

        public static string GetResultString(int bonus)
        {
            int[] a = new int[11];
            int ct = 0;

            string result = "Location Repaired: ";


            float ct_chance = 0;
            for (int i = 0; i < 11; i++)
            {
                (var ct_alive, int parts) = get_parts_to_remove(i + 2 + bonus);
                a[i] = parts;
                if (ct_alive)
                {
                    ct_chance += probs[i];
                    if (ct == 0)
                        ct = i + 2;
                }

            }

            void add_to_result(float pr, int pt)
            {
                pr = Mathf.Round(pr * 10f) / 10f;

                result += $"{8 - pt} - {Mathf.RoundToInt(pr)}%, ";
            }

            int last = a[0];
            float prob = probs[0];
            for (int i = 1; i < 11; i++)
            {
                if (a[i] == last)
                    prob += probs[i];
                else
                {
                    add_to_result(prob, last);
                    last = a[i];
                    prob = probs[i];
                }
            }

            add_to_result(prob, last);
            if (ct_chance == 0)
                result += "\n CT will not be repaired";
            else
                result += $"\nCT Repaired at {ct} roll - { Mathf.RoundToInt(ct_chance)}%";


            return result;
        }

        public static string GetBonusString(MechDef mech, SimGameState sim, int other_parts, int spare_parts)
        {
            StringBuilder sb = new StringBuilder();
            var tags = ChassisHandler.GetMechTags(mech);
            foreach (var tag in Tags.Instance.AllCSTags)
            {
                var str = tag.GetString(mech, tags, sim);
                if (str != null)
                    sb.Append(str);
            }

            if (other_parts > 0)
                sb.AppendLine($"{parts_bonus(other_parts),-4:+0;-#}" +
                              new Text(Control.Instance.Settings.Strings.FrankenPenaltyCaption).ToString());

            var tp = tp_bonus(sim);
            if (tp != 0)
                sb.AppendLine($"{tp,-4:+0;-#}" +
                              new Text(Control.Instance.Settings.Strings.TPBonusCaption).ToString());

            if( spare_parts > 0)
                sb.AppendLine($"{spare_parts,-4:+0;-#}" +
                              new Text(Control.Instance.Settings.Strings.SparePartsCaption).ToString());

            return sb.ToString();
        }

        private static int tp_bonus(SimGameState sim)
        {
            return sim.MechTechSkill / Control.Instance.Settings.DiceTPStep + Control.Instance.Settings.DiceBaseTP;
        }

        public static void AddKit(TechKitCustom techKitCustom)
        {
            tech_kits.Add(techKitCustom);
        }
        public static void PrepareTechKits(MechDef mech, SimGameState mechBaySim)
        {
            SelectedTechKit = null;
            CompatibleTechKits = new List<TechKitCustom>();
            SpareParts = new List<ChassisHandler.parts_info>();
            foreach (var kit in tech_kits)
            {
                if (ConditionsHandler.Instance.CheckCondition(kit.Info.Conditions, mech))
                    CompatibleTechKits.Add(kit);
            }
        }
    }
}