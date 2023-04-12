using System.Collections.Generic;
using System.Linq;
using System.Text;
using BattleTech;
using FluffyUnderware.DevTools.Extensions;
using Localize;
using UnityEngine;

namespace CustomSalvage.MechBroke;

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

    private static Dictionary<string, TechKitCustom> tech_kits = new Dictionary<string, TechKitCustom>();

    public static TechKitCustom SelectedTechKit { get; set; }
    public static List<System.Tuple<TechKitCustom, int>> CompatibleTechKits { get; set; }
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

    public static (int roll, int total) BrokeMech(MechDef mech, SimGameState sim, int other_parts, int spare_parts, int all_used_parts, int used_empty_parts)
    {
        var target = GetBonus(mech, sim, other_parts, spare_parts);
        var roll = BrokeTools.rnd.Next(1, 7) + BrokeTools.rnd.Next(1, 7);
        target += roll;

        var parts = locs.ToList();
        var remove = ChassisLocations.None;

        (bool ct_remove, int parts_to_remove) = get_parts_to_remove(target);

        if (ct_remove)
        {
            remove = remove.Set(ChassisLocations.CenterTorso);
            parts_to_remove -= 1;
        }

        if (parts_to_remove >= 7)
        {
            remove = ChassisLocations.All;
        }
        else if (parts_to_remove == 0)
        {
            remove = ChassisLocations.None;
        }
        else
        {
            for (int i = 0; i < parts_to_remove && parts.Count > 0; i++)
            {
                int n = BrokeTools.rnd.Next(0, parts.Count);
                remove = remove.Set(parts[n]);
                parts.RemoveAt(n);
            }
        }


        Log.Main.Debug?.Log($"Breaking {mech.Description.Id}");
        Log.Main.Debug?.Log($"- roll: {roll} + {target - roll} = {target}");
        Log.Main.Debug?.Log($"- parts: {parts_to_remove} CT:{ct_remove}");
        Log.Main.Debug?.Log($"- break: {remove}");


        foreach (var to_remove in all_parts)
        {
            BrokeTools.BrokeLocation(mech, to_remove, remove.HasFlag(to_remove));
            Log.Main.Debug?.Log($"-- {to_remove}: {remove.HasFlag(to_remove)}");
        }

        var compchance = 1 - GetComp(mech, sim, other_parts, spare_parts, all_used_parts, used_empty_parts);

        BrokeTools.BrokeEquipment(mech, compchance, compchance);

        return (roll, target);
    }

    private static (bool, int) get_parts_to_remove(int target)
    {
        var s = Control.Instance.Settings;
        int n = Mathf.Clamp(target, s.MinRoll, s.MaxRoll);
        return (n < s.CTRoll, s.partresults[n]);
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
        //Control.Instance.Log("bonus:");
        var result = Tags.Instance.AllCSTags.Sum(i => i.GetValue(mech, tags, sim));
        //Control.Instance.Log($" {result}: tags");

        //var parts = sim.Constants.Story.DefaultMechPartMax;
        result += parts_bonus(other_parts);
        //Control.Instance.Log($" {result}: +parts {other_parts}");
        result += tp_bonus(sim);
        //Control.Instance.Log($" {result}: +tp");
        result += spare_parts;
        //Control.Instance.Log($" {result}: +spare");
        if (SelectedTechKit != null)
        {
            result += SelectedTechKit.Value;
        }

        //Control.Instance.Log($" {result}: +kit");
        //bonuses
        return result;
    }

    public static float GetComp(MechDef mech, SimGameState sim, int other_parts, int spare_parts)
    {
        var s = Control.Instance.Settings;

        float result = s.ComponentDamageBase;
        result += sim.MechTechSkill * s.ComponentChancePerTp;
        result += other_parts * s.ComponentDamageFranken;
        result += spare_parts * s.ComponentDamageSpare;

        if (SelectedTechKit != null)
        {
            result -= SelectedTechKit.CompRepairAddBonus;
        }


        return result;
    }
    public static float GetComp(MechDef mech, SimGameState sim, int other_parts, int spare_parts, int all_used_parts, int used_empty_parts)
    {
        var s = Control.Instance.Settings;

        float result = s.ComponentDamageBase;
        result += sim.MechTechSkill * s.ComponentChancePerTp;
        Log.Main.Debug?.Log($"GetComp {mech.Description.Id} other:{other_parts} spare:{spare_parts} used:{all_used_parts} empty:{used_empty_parts}");
        result += other_parts * s.ComponentDamageFranken;
        result += spare_parts * s.ComponentDamageSpare;

        if (SelectedTechKit != null)
        {
            result -= SelectedTechKit.CompRepairAddBonus;
        }
        if (all_used_parts > 0) {
          result = 1f - ((all_used_parts - used_empty_parts) / (all_used_parts)) * (1f - result);
        }
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
            (var ct_destroyed, int parts) = get_parts_to_remove(i + 2 + bonus);
            a[i] = parts;
            if (!ct_destroyed)
            {
                ct_chance += probs[i];
                if (ct == 0)
                {
                    ct = i + 2;
                }
            }

        }
        Log.Main.Debug?.Log("--");

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
            {
                prob += probs[i];
            }
            else
            {
                add_to_result(prob, last);
                last = a[i];
                prob = probs[i];
            }
        }

        add_to_result(prob, last);
        if (ct_chance < 0.01)
        {
            result += "\n CT will not be repaired";
        }
        else
        {
            result += $"\nCT Repaired at {ct} roll - { Mathf.RoundToInt(ct_chance)}%";
        }

        //var c = GetComp(mech, )

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
            {
                sb.Append(str);
            }
        }

        if (other_parts > 0)
        {
            sb.AppendLine($"{parts_bonus(other_parts),-4:+0;-#}" +
                          new Text(Control.Instance.Settings.Strings.FrankenPenaltyCaption).ToString());
        }

        var tp = tp_bonus(sim);
        if (tp != 0)
        {
            sb.AppendLine($"{tp,-4:+0;-#}" +
                          new Text(Control.Instance.Settings.Strings.TPBonusCaption).ToString());
        }

        if (spare_parts > 0)
        {
            sb.AppendLine($"{spare_parts,-4:+0;-#}" +
                          new Text(Control.Instance.Settings.Strings.SparePartsCaption).ToString());
        }

        if (SelectedTechKit != null && SelectedTechKit.Value != 0)
        {
            sb.AppendLine($"{SelectedTechKit.Value,-4:+0;-#}" +
                          SelectedTechKit.Def.Description.UIName);
        }


        return sb.ToString();
    }

    private static int tp_bonus(SimGameState sim)
    {
        return sim.MechTechSkill / Control.Instance.Settings.DiceTPStep + Control.Instance.Settings.DiceBaseTP;
    }

    public static void AddKit(TechKitCustom techKitCustom)
    {
        tech_kits.Add(techKitCustom.Def.Description.Id, techKitCustom);
    }
    public static void PrepareTechKits(MechDef mech, SimGameState sim)
    {
        SelectedTechKit = null;
        CompatibleTechKits = new ();
        SpareParts = new List<ChassisHandler.parts_info>();

        var items = sim.GetAllInventoryStrings();

        Log.Main.Debug?.Log("TechKits:");

        if (items != null)
        {
            foreach (var item in items)
            {
                if (item.StartsWith("Item.UpgradeDef"))
                {
                    var id = item.Substring(16);
                    var num = sim.CompanyStats.GetValue<int>(item);
                    bool comp = tech_kits.TryGetValue(id, out var kit) && num >= 1;
                    Log.Main.Debug?.Log($"- {id}: {num} {comp}");
                    if (comp)
                    {
                        CompatibleTechKits.Add(new(kit, num));
                    }
                }
            }
        }
    }
}