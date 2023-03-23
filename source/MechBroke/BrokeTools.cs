using System;
using System.Collections.Generic;
using BattleTech;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CustomSalvage.MechBroke;

public static class BrokeTools
{
    private static void BrokeLimb(LocationLoadoutDef loc, bool broke)
    {
        if (broke)
        {
            loc.CurrentInternalStructure = 0;
        }
        else if (Control.Instance.Settings.RandomStructureOnRepairedLimbs)
        {
            loc.CurrentInternalStructure *= Mathf.Ceil(Math.Min(Control.Instance.Settings.MinStructure, Random.Range(0f,1f)));
        }
    }

    public static void BrokeLocation(MechDef mech, ChassisLocations location, bool broke)
    {
        var settings = Control.Instance.Settings;
        switch (location)
        {
            case ChassisLocations.Head:
                BrokeLimb(mech.Head, !settings.HeadRepaired && (!settings.RepairMechLimbs || broke));
                break;
            case ChassisLocations.LeftArm:
                BrokeLimb(mech.LeftArm, !settings.LeftArmRepaired && (!settings.RepairMechLimbs || broke));
                break;
            case ChassisLocations.LeftTorso:
                BrokeLimb(mech.LeftTorso, !settings.LeftTorsoRepaired && (!settings.RepairMechLimbs || broke));
                break;
            case ChassisLocations.CenterTorso:
                BrokeLimb(mech.CenterTorso, !settings.CentralTorsoRepaired && (!settings.RepairMechLimbs || broke));
                break;
            case ChassisLocations.RightTorso:
                BrokeLimb(mech.RightTorso, !settings.RightTorsoRepaired && (!settings.RepairMechLimbs || broke));
                break;
            case ChassisLocations.RightArm:
                BrokeLimb(mech.RightArm, !settings.RightArmRepaired && (!settings.RepairMechLimbs || broke));
                break;
            case ChassisLocations.LeftLeg:
                BrokeLimb(mech.LeftLeg, !settings.LeftLegRepaired && (!settings.RepairMechLimbs || broke));
                break;
            case ChassisLocations.RightLeg:
                BrokeLimb(mech.RightLeg, !settings.RightLegRepaired && (!settings.RepairMechLimbs || broke));
                break;
        }
    }

    public static void BrokeEquipment(MechDef mech, float repaired, float damaged)
    {
        if(Control.Instance.Settings.ComponentDamageRandom)
        {
            BrokeEquipmentRandom(mech, repaired, damaged);
        }
        else
        {
            BrokeEquipmentProportional(mech, repaired, damaged);
        }
    }

    public static void BrokeEquipmentRandom(MechDef mech, float repaired, float damaged)
    {
        foreach (var cref in mech.Inventory)
        {
            if (mech.IsLocationDestroyed(cref.MountedLocation))
            {
                Log.Main.Debug?.Log($"---- {cref.ComponentDefID} - location destroyed");
                cref.DamageLevel = ComponentDamageLevel.Destroyed;
            }
            else if (Control.Instance.Settings.RepairMechComponents)
            {
                var roll = Random.Range(0f, 1f);

                if (roll < repaired)
                {
                    Log.Main.Debug?.Log($"---- {cref.ComponentDefID} - {roll} vs {repaired} - repaired ");
                    cref.DamageLevel = ComponentDamageLevel.Functional;
                }
                else if (roll < damaged)
                {
                    Log.Main.Debug?.Log($"---- {cref.ComponentDefID} - {roll} vs {damaged} - damaged ");
                    cref.DamageLevel = ComponentDamageLevel.NonFunctional;
                }
                else
                {
                    Log.Main.Debug?.Log($"---- {cref.ComponentDefID} - {roll} vs {damaged} - fubar ");
                    cref.DamageLevel = ComponentDamageLevel.Destroyed;
                }
            }
        }
    }

    public static void BrokeEquipmentProportional(MechDef mech, float repaired, float damaged)
    {
        Log.Main.Debug?.Log($"BrokeEquipmentProportional: broke:{damaged:0.00} repair:{repaired:0.00}");

        var list = new List<MechComponentRef>();
        foreach (var cref in mech.Inventory)
        {
            if (mech.IsLocationDestroyed(cref.MountedLocation))
            {
                Log.Main.Debug?.Log($"---- {cref.ComponentDefID} - location destroyed");
                cref.DamageLevel = ComponentDamageLevel.Destroyed;
            }
            else if(!cref.IsFixed)
            {
                list.Add(cref);
            }
        }

        if (list.Count <= 1)
        {
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            var n1 = Random.Range(0, list.Count);
            var n2 = Random.Range(0, list.Count);

            var t = list[n1];
            list[n1] = list[n2];
            list[n2] = t;
        }

        float frep = list.Count * repaired;
        float fdam = list.Count * damaged;
        int nrep = (int) frep;
        int ndam = (int) fdam;
        float prep = frep - nrep;
        float pdam = fdam - ndam;


        for (int i = 0; i < list.Count; i++)
        {
            var cref = list[i];
            if (i < nrep)
            {
                Log.Main.Debug?.Log($"---- {cref.ComponentDefID} - repaired");
                cref.DamageLevel = ComponentDamageLevel.Functional;
            }
            else if(i == nrep)
            {
                if (Random.Range(0f, 1f) < prep)
                {
                    Log.Main.Debug?.Log($"---- {cref.ComponentDefID} - repaired");
                    cref.DamageLevel = ComponentDamageLevel.Functional;
                }
                else if (damaged == repaired)
                {
                    Log.Main.Debug?.Log($"---- {cref.ComponentDefID} - fubar");
                    cref.DamageLevel = ComponentDamageLevel.Destroyed;
                }
                else
                {
                    Log.Main.Debug?.Log($"---- {cref.ComponentDefID} - damaged");
                    cref.DamageLevel = ComponentDamageLevel.NonFunctional;
                }
            }
            else if (i < ndam)
            {
                Log.Main.Debug?.Log($"---- {cref.ComponentDefID} - damaged");
                cref.DamageLevel = ComponentDamageLevel.NonFunctional;
            }
            else if (i == ndam)
            {
                if (Random.Range(0f, 1f) < pdam)
                {
                    Log.Main.Debug?.Log($"---- {cref.ComponentDefID} - damaged");
                    cref.DamageLevel = ComponentDamageLevel.NonFunctional;
                }
                else
                {
                    Log.Main.Debug?.Log($"---- {cref.ComponentDefID} - fubar");
                    cref.DamageLevel = ComponentDamageLevel.Destroyed;
                }
            }
            else
            {
                Log.Main.Debug?.Log($"---- {cref.ComponentDefID} - fubar");
                cref.DamageLevel = ComponentDamageLevel.Destroyed;
            }
        }
    }


}