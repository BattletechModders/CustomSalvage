using System.Collections.Generic;
using System.Linq;
using System.Text;
using BattleTech;
using BattleTech.UI;
using FluffyUnderware.DevTools.Extensions;
using HBS.Logging;
using JetBrains.Annotations;
using Localize;
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

        private readonly static int[] partmilestone =
        {
            19,
            15,
            12,
            9,
            3,
            0,
            -3,
        };

        public static (int roll, int total) BrokeMech(MechDef mech, SimGameState sim, int other_parts)
        {
            var target = GetBonus(mech, sim, other_parts);
            var roll = Random.Range(1, 7) + Random.Range(1, 7);
            target += roll;

            var parts = locs.ToList();
            var remove = ChassisLocations.None;

            for (int i = 0; i < partmilestone.Length && target < partmilestone[i]; i++)
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

        private static int parts_bonus(int parts)
        {
            return Mathf.RoundToInt(parts * Control.Instance.Settings.PartPenalty[parts])
                + (parts > 0 ? 1 : 0);
        }

        public static int GetBonus(MechDef mech, SimGameState sim, int other_parts)
        {
            //penalties
            var tags = ChassisHandler.GetMechTags(mech);

            var result = Tags.Instatnce.AllCSTags.Sum(i => i.GetValue(mech, tags, sim));
            var parts = sim.Constants.Story.DefaultMechPartMax;
            result -= parts_bonus(other_parts);
            result += tp_bonus(sim);
            //bonuses
            return result;
        }

        public static string GetBonusString(MechDef mech, SimGameState sim, int other_parts)
        {
            StringBuilder sb = new StringBuilder();
            var tags = ChassisHandler.GetMechTags(mech);
            foreach (var tag in Tags.Instatnce.AllCSTags)
            {
                var str = tag.GetString(mech, tags, sim);
                if (str != null)
                    sb.Append(str);
            }

            if (other_parts > 0)
                sb.AppendLine($"{-parts_bonus(other_parts),-4:-0,+#}" +
                              new Text(Control.Instance.Settings.Strings.FrankenPenaltyCaption).ToString());

            var tp = tp_bonus(sim);
            if (tp != 0)
                sb.AppendLine($"{tp,-4:-0,+#}" +
                              new Text(Control.Instance.Settings.Strings.TPBonusCaption).ToString());
            return sb.ToString();
        }

        private static int tp_bonus(SimGameState sim)
        {
            return sim.MechTechSkill / Control.Instance.Settings.DiceBaseTP + Control.Instance.Settings.DiceTPStep;
        }
    }
}