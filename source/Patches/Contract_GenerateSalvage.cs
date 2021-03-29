using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using Harmony;
using UnityEngine;
#if USE_CC
using CustomComponents;
#endif


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
                Control.Instance.LogDebug($"Start GenerateSalvage for {__instance.Name}");

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
                    Control.Instance.LogError("No simgame - cancel salvage");
                    return false;
                }

                var Constants = simgame.Constants;

                Control.Instance.LogDebug("- Lost Units");
                foreach (var unitResult in lostUnits)
                {
                    ProccessPlayerMech(unitResult, Contract);
                }
                Control.Instance.LogDebug($"- Enemy Mechs {__instance.Name}");

                foreach (var unit in enemyMechs)
                {
                    if (Control.Instance.IsDestroyed(unit) || unit.pilot.IsIncapacitated || unit.pilot.HasEjected)
                        AddMechToSalvage(unit.mech, Contract, simgame, Constants, true);
                    else
                    {
                        Control.Instance.LogDebug($"-- Salvaging {unit.mech.Name}");
                        Control.Instance.LogDebug($"--- not destroyed, skipping");
                    }
                }

                Control.Instance.LogDebug($"- Enemy Vechicle {__instance.Name}");
                var vehicles = Contract.Contract.BattleTechGame.Combat.AllEnemies.OfType<Vehicle>()
                    .Where(i => i.IsDead);

                foreach (var vehicle in vehicles)
                {
                    Control.Instance.LogDebug($"-- Salvaging {vehicle?.VehicleDef?.Chassis?.Description?.Name}");
                    var tag = Control.Instance.Settings.NoSalvageMechTag;
                    if (!string.IsNullOrEmpty(tag) &&
                        (vehicle.VehicleDef.VehicleTags != null &&
                         vehicle.VehicleDef.VehicleTags.Contains(tag)))
                    {
                        Control.Instance.LogDebug($"--- NOSALVAGE by tag, skipped");
                    }
                    else
                        AddVechicleToSalvage(vehicle, Contract, simgame);
                }
                //foreach (var vechicle in enemyVehicles)
                //{
                //    Control.Instance.LogDebug($"-- Salvaging {vechicle?.Chassis?.Description?.Name}");
                //    AddVechicleToSalvage(vechicle, Contract, simgame);
                //}

                if (Control.Instance.Settings.SalvageTurrets)
                {
                    Control.Instance.LogDebug($"- Enemy Turret {__instance.Name}");
                    var turrets = Contract.Contract.BattleTechGame.Combat.AllEnemies.OfType<Turret>()
                        .Where(t => t.IsDead);

                    foreach (var turret in turrets)
                    {
                        Control.Instance.LogDebug($"-- Salvaging {turret?.TurretDef?.Description?.Name}");
                        AddTurretToSalvage(turret, Contract, simgame);
                    }
                }

                Control.Instance.CallForAdditionalSavage(Contract);

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
                Control.Instance.LogError("Unhandled error in salvage", e);
            }

            return false;
        }

        public static void ProccessPlayerMech(UnitResult unitResult, ContractHelper Contract)
        {
            var mech = unitResult.mech;
            Control.Instance.LogDebug($"-- Salvaging {mech.Name}");

            if (!Control.Instance.IsDestroyed(unitResult))
            {
                Control.Instance.LogDebug("--- not destroyed - skipped");
                unitResult.mechLost = false;
                return;
            }

            unitResult.mechLost = !Control.Instance.NeedRecovery(unitResult, Contract);

            if(unitResult.mechLost)
                Control.Instance.LostUnitAction(unitResult, Contract);
        }

        private static void AddTurretToSalvage(Turret turret, ContractHelper contract, SimGameState simgame)
        {
            if (turret == null || turret.allComponents != null)
                foreach (var component in turret.allComponents)
                {
                    var chance = simgame.NetworkRandom.Float();
                    if (component.DamageLevel != ComponentDamageLevel.Destroyed && chance < Control.Instance.Settings.SalvageTurretsComponentChance)
                    {
                        Control.Instance.LogDebug($"--- {chance:0.000} < {Control.Instance.Settings.SalvageTurretsComponentChance:0.00}");
                        contract.AddComponentToPotentialSalvage(component.componentDef, component.DamageLevel, true);
                    }
                    else
                        Control.Instance.LogDebug($"--- {chance:0.000} > {Control.Instance.Settings.SalvageTurretsComponentChance:0.00} - {component.defId} skipped");
                }
        }

        public static void AddVechicleToSalvage(Vehicle vechicle, ContractHelper contract, SimGameState simgame)
        {
            if (!string.IsNullOrEmpty(Control.Instance.Settings.NoSalvageVehicleTag) &&
                vechicle.VehicleDef.VehicleTags.Contains(Control.Instance.Settings.NoSalvageVehicleTag))
            {
                Control.Instance.LogDebug($"-- NoSalvage - skipped");
                return;
            }
            
            foreach (var component in vechicle.VehicleDef.Inventory)
            {
                if (component.DamageLevel != ComponentDamageLevel.Destroyed)
                {
                    contract.AddComponentToPotentialSalvage(component.Def, component.DamageLevel, true);
                }
            }
        }

        private static void AddMechToSalvage(MechDef mech, ContractHelper contract, SimGameState simgame, SimGameConstants constants, bool can_upgrade)
        {
            Control.Instance.LogDebug($"-- Salvaging {mech.Name}");

            int numparts = Control.Instance.GetNumParts(mech);

            try
            {
                var mech_to_add = mech;

#if USE_CC
                if (mech.Chassis.Is<LootableMech>(out var lm))
                {
                    Control.Instance.LogDebug($"--- Mech Replacing with {lm.ReplaceID}");
                    try
                    {
                        mech_to_add = UnityGameInstance.BattleTechGame.Simulation.DataManager.MechDefs.Get(lm.ReplaceID);
                        if (mech_to_add == null)
                        {
                            Control.Instance.LogError($"---unknown mech {lm.ReplaceID}, rollback");
                        }
                    }
                    catch
                    {
                        mech_to_add = null;
                    }

                    if (mech_to_add == null)
                    {
                        Control.Instance.LogError($"---unknown mech {lm.ReplaceID}, rollback");
                        mech_to_add = mech;
                    }

                }
#endif

                if (mech.MechTags.Contains(Control.Instance.Settings.NoSalvageMechTag) ||
                    mech.Chassis.ChassisTags.Contains(Control.Instance.Settings.NoSalvageMechTag))
                {
                    Control.Instance.LogDebug($"--- {Control.Instance.Settings.NoSalvageMechTag} mech, no parts");
                }
                else
                {
                    Control.Instance.LogDebug($"--- Adding {numparts} parts");
                    contract.AddMechPartsToPotentialSalvage(constants, mech, numparts);
                }
            }

            catch (Exception e)
            {
                Control.Instance.LogError("Error in adding parts", e);
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
                Control.Instance.LogError("Error in adding component", e);
            }
        }
    }
}