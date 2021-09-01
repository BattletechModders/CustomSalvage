using System;
using BattleTech;
using BattleTech.DataObjects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CustomSalvage.MechBroke
{
    public static class BrokeTools
    {
        private static void BrokeLimb(LocationLoadoutDef loc, bool broke)
        {
            if (broke)
                loc.CurrentInternalStructure = 0;
            else if (Control.Instance.Settings.RandomStructureOnRepairedLimbs)
                loc.CurrentInternalStructure *= Mathf.Ceil(Math.Min(Control.Instance.Settings.MinStructure, UnityEngine.Random.Range(0f,1f)));
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
            foreach (var cref in mech.Inventory)
            {
                if (mech.IsLocationDestroyed(cref.MountedLocation))
                {
                    Control.Instance.LogDebug($"---- {cref.ComponentDefID} - location destroyed");
                    cref.DamageLevel = ComponentDamageLevel.Destroyed;
                }
                else if (Control.Instance.Settings.RepairMechComponents)
                {
                    var roll = Random.Range(0f, 1f);

                    if (roll < repaired)
                    {
                        Control.Instance.LogDebug(
                            $"---- {cref.ComponentDefID} - {roll} vs {repaired} - repaired ");
                        cref.DamageLevel = ComponentDamageLevel.Functional;
                    }
                    else if (roll < damaged)
                    {
                        Control.Instance.LogDebug(
                            $"---- {cref.ComponentDefID} - {roll} vs {damaged} - damaged ");
                        cref.DamageLevel = ComponentDamageLevel.NonFunctional;
                    }
                    else
                    {
                        Control.Instance.LogDebug(
                            $"---- {cref.ComponentDefID} - {roll} vs {damaged} - fubar ");
                        cref.DamageLevel = ComponentDamageLevel.Destroyed;
                    }
                }
            }
        }
    }
}