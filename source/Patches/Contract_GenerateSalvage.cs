using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using Harmony;
using UnityEngine;

namespace CustomSalvage
{
    [HarmonyPatch(typeof(Contract), "GenerateSalvage")]
    [HarmonyPriority(Priority.HigherThanNormal)]
    public static class Contract_GenerateSalvage
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.HigherThanNormal)]
        public static bool GenerateSalvage(List<UnitResult> enemyMechs, List<VehicleDef> enemyVehicles,
            List<UnitResult> lostUnits, bool logResults,
            Contract __instance, ref List<SalvageDef> ___finalPotentialSalvage)
        {
            try
            {
                Control.LogDebug($"Start GenerateSalvage for {__instance.Name}");

                ___finalPotentialSalvage = new List<SalvageDef>();
                var Contract = new ContractHelper(__instance, ___finalPotentialSalvage)
                {
                    LostMechs = new List<MechDef>(),
                    SalvageResults = new List<SalvageDef>(),
                    SalvagedChassis = new List<SalvageDef>()
                };


                var simgame = __instance.BattleTechGame.Simulation;
                if (simgame == null)
                {
                    Control.LogError("No simgame - cancel salvage");
                    return false;
                }

                var Constants = simgame.Constants;

                Control.LogDebug("- Lost Units");
                for (int i = 0; i < lostUnits.Count; i++)
                {
                    var mech = lostUnits[i].mech;
                    Control.LogDebug($"-- Salvaging {mech.Name}");

                    if (!Control.IsDestroyed(lostUnits[i]))
                    {
                        Control.LogDebug("--- not destroyed - skipped");
                        lostUnits[i].mechLost = false;
                        continue;
                    }

                    lostUnits[i].mechLost = !Control.NeedRecovery(lostUnits[i], Contract);

                    if (lostUnits[i].mechLost)
                    {
                        Control.LostUnitAction(lostUnits[i], Contract);
                    }
                }
                Control.LogDebug($"- Enemy Mechs {__instance.Name}");

                foreach (var unit in enemyMechs)
                {
                    if(Control.IsDestroyed(unit) || unit.pilot.IsIncapacitated || unit.pilot.HasEjected)
                        AddMechToSalvage(unit.mech, Contract, simgame, Constants, true);
                    else
                    {
                        Control.LogDebug($"-- Salvaging {unit.mech.Name}");
                        Control.LogDebug($"--- not destroyed, skipping");
                    }
                }

                Control.LogDebug($"- Enemy Vechicle {__instance.Name}");
                foreach (var vechicle in enemyVehicles)
                {
                    Control.LogDebug($"-- Salvaging {vechicle?.Chassis?.Description?.Name}");
                    AddVechicleToSalvage(vechicle, Contract, simgame);
                }

                if (Control.Settings.SalvageTurrets)
                {
                    Control.LogDebug($"- Enemy Turret {__instance.Name}");
                    var turrets = Contract.Contract.BattleTechGame.Combat.AllEnemies.OfType<Turret>()
                        .Where(t => t.IsDead);

                    foreach (var turret in turrets)
                    {
                        Control.LogDebug($"-- Salvaging {turret?.TurretDef?.Description?.Name}");
                        AddTurretToSalvage(turret, Contract, simgame);
                    }
                }


                Contract.FilterPotentialSalvage(___finalPotentialSalvage);
                int num2 = __instance.SalvagePotential;
                float num3 = Constants.Salvage.VictorySalvageChance;
                float num4 = Constants.Salvage.VictorySalvageLostPerMechDestroyed;
                if (__instance.State == BattleTech.Contract.ContractState.Failed)
                {
                    num3 = Constants.Salvage.DefeatSalvageChance;
                    num4 = Constants.Salvage.DefeatSalvageLostPerMechDestroyed;
                }
                else if (__instance.State == BattleTech.Contract.ContractState.Retreated)
                {
                    num3 = Constants.Salvage.RetreatSalvageChance;
                    num4 = Constants.Salvage.RetreatSalvageLostPerMechDestroyed;
                }
                float num5 = num3;
                float num6 = (float)num2 * __instance.PercentageContractSalvage;
                if (num2 > 0)
                {
                    num6 += (float)Constants.Finances.ContractFloorSalvageBonus;
                }
                num3 = Mathf.Max(0f, num5 - num4 * (float)lostUnits.Count);
                int num7 = Mathf.FloorToInt(num6 * num3);
                if (num2 > 0)
                {
                    num2 += Constants.Finances.ContractFloorSalvageBonus;
                }

                Contract.FinalSalvageCount = num7;
                Contract.FinalPrioritySalvageCount = Math.Min(7, Mathf.FloorToInt((float)num7 * Constants.Salvage.PrioritySalvageModifier));

            }
            catch (Exception e)
            {
                Control.LogError("Unhandled error in salvage", e);
            }

            return false;
        }

        private static void AddTurretToSalvage(Turret turret, ContractHelper contract, SimGameState simgame)
        {
            if (turret == null || turret.allComponents != null)
                foreach (var component in turret.allComponents)
                {
                    var chance = simgame.NetworkRandom.Float();
                    if (component.DamageLevel != ComponentDamageLevel.Destroyed && chance < Control.Settings.SalvageTurretsComponentChance)
                    {
                        Control.LogDebug($"--- {chance:0.000} < {Control.Settings.SalvageTurretsComponentChance:0.00}");
                        contract.AddComponentToPotentialSalvage(component.componentDef, component.DamageLevel, true);
                    }
                    else
                        Control.LogDebug($"--- {chance:0.000} > {Control.Settings.SalvageTurretsComponentChance:0.00} - {component.defId} skipped");
                }
        }

        private static void AddVechicleToSalvage(VehicleDef vechicle, ContractHelper contract, SimGameState simgame)
        {
            foreach (var component in vechicle.Inventory)
            {
                if (component.DamageLevel != ComponentDamageLevel.Destroyed)
                {
                    contract.AddComponentToPotentialSalvage(component.Def, component.DamageLevel, true);
                }
            }
        }

        private static void AddMechToSalvage(MechDef mech, ContractHelper contract, SimGameState simgame, SimGameConstants constants, bool can_upgrade)
        {
            Control.LogDebug($"-- Salvaging {mech.Name}");

            int numparts = Control.GetNumParts(mech);

            try
            {
                Control.LogDebug($"--- Adding {numparts} parts");
                contract.AddMechPartsToPotentialSalvage(constants, mech, numparts);
            }
            catch (Exception e)
            {
                Control.LogError("Error in adding parts", e);
            }

            try
            {

                foreach (var component in mech.Inventory.Where(item =>
                    !mech.IsLocationDestroyed(item.MountedLocation) &&
                    item.DamageLevel != ComponentDamageLevel.Destroyed))
                {
                    contract.AddComponentToPotentialSalvage(component.Def, ComponentDamageLevel.Functional, can_upgrade);
                }
            }
            catch (Exception e)
            {
                Control.LogError("Error in adding component", e);
            }
        }
    }
}