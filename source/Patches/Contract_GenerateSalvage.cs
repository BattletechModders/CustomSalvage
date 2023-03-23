using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using UnityEngine;


namespace CustomSalvage
{
    [HarmonyPatch(typeof(Contract), "GenerateSalvage")]
    [HarmonyPriority(Priority.HigherThanNormal)]
    internal static class Contract_GenerateSalvage
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPriority(Priority.HigherThanNormal)]
        public static void Prefix(ref bool __runOriginal, List<UnitResult> enemyMechs, List<VehicleDef> enemyVehicles,
            List<UnitResult> lostUnits, bool logResults,
            Contract __instance, ref List<SalvageDef> ___finalPotentialSalvage)
        {
            if (!__runOriginal)
            {
                return;
            }

            __runOriginal = false;

            Log.Main.Debug?.Log($"Start GenerateSalvage for {__instance.Name}");

            ___finalPotentialSalvage = new List<SalvageDef>();
            var Contract = new ContractHelper(__instance, ___finalPotentialSalvage);


            var simgame = __instance.BattleTechGame.Simulation;
            if (simgame == null)
            {
                Log.Main.Error?.Log("No simgame - cancel salvage");
                __runOriginal = false;
                return;
            }

            var Constants = simgame.Constants;

            Log.Main.Debug?.Log("- Lost Units");
            foreach (var unitResult in lostUnits)
            {
                ProccessPlayerMech(unitResult, Contract);
            }
            Log.Main.Debug?.Log($"- Enemy Mechs {__instance.Name}");

            foreach (var unit in enemyMechs)
            {
                if (Control.Instance.IsDestroyed(unit) || unit.pilot.IsIncapacitated || unit.pilot.HasEjected)
                    AddMechToSalvage(unit.mech, Contract, simgame, Constants, true);
                else
                {
                    Log.Main.Debug?.Log($"-- Salvaging {unit.mech.Name}");
                    Log.Main.Debug?.Log($"--- not destroyed, skipping");
                }
            }

            Log.Main.Debug?.Log($"- Enemy Vechicle {__instance.Name}");
            var vehicles = Contract.Contract.BattleTechGame.Combat.AllEnemies.OfType<Vehicle>()
                .Where(i => i.IsDead);

            foreach (var vehicle in vehicles)
            {
                Log.Main.Debug?.Log($"-- Salvaging {vehicle?.VehicleDef?.Chassis?.Description?.Name}");
                var tag = Control.Instance.Settings.NoSalvageMechTag;
                if (!string.IsNullOrEmpty(tag) &&
                    (vehicle.VehicleDef.VehicleTags != null &&
                     vehicle.VehicleDef.VehicleTags.Contains(tag)))
                {
                    Log.Main.Debug?.Log($"--- NOSALVAGE by tag, skipped");
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
                Log.Main.Debug?.Log($"- Enemy Turret {__instance.Name}");
                var turrets = Contract.Contract.BattleTechGame.Combat.AllEnemies.OfType<Turret>()
                    .Where(t => t.IsDead);

                foreach (var turret in turrets)
                {
                    Log.Main.Debug?.Log($"-- Salvaging {turret?.TurretDef?.Description?.Name}");
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

            Contract.Contract.FinalSalvageCount = num7;
            Contract.Contract.FinalPrioritySalvageCount = Math.Min(7, Mathf.FloorToInt((float)num7 * Constants.Salvage.PrioritySalvageModifier));
        }

        public static void ProccessPlayerMech(UnitResult unitResult, ContractHelper Contract)
        {
            var mech = unitResult.mech;
            Log.Main.Debug?.Log($"-- Salvaging {mech.Name}");

            if (!Control.Instance.IsDestroyed(unitResult))
            {
                Log.Main.Debug?.Log("--- not destroyed - skipped");
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
                        Log.Main.Debug?.Log($"--- {chance:0.000} < {Control.Instance.Settings.SalvageTurretsComponentChance:0.00}");
                        contract.AddComponentToPotentialSalvage(component.componentDef, component.DamageLevel, true);
                    }
                    else
                        Log.Main.Debug?.Log($"--- {chance:0.000} > {Control.Instance.Settings.SalvageTurretsComponentChance:0.00} - {component.defId} skipped");
                }
        }

        public static void AddVechicleToSalvage(Vehicle vechicle, ContractHelper contract, SimGameState simgame)
        {
            if (!string.IsNullOrEmpty(Control.Instance.Settings.NoSalvageVehicleTag) &&
                vechicle.VehicleDef.VehicleTags.Contains(Control.Instance.Settings.NoSalvageVehicleTag))
            {
                Log.Main.Debug?.Log($"-- NoSalvage - skipped");
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
            Log.Main.Debug?.Log($"-- Salvaging {mech.Name}");

            int numparts = Control.Instance.GetNumParts(mech);

            try
            {
                var mech_to_salvage = ChassisHandler.FindMechReplace(mech);

                if (mech_to_salvage == null || mech_to_salvage.MechTags.Contains(Control.Instance.Settings.NoSalvageMechTag) ||
                    mech_to_salvage.Chassis.ChassisTags.Contains(Control.Instance.Settings.NoSalvageMechTag))
                {
                    Log.Main.Debug?.Log($"--- {Control.Instance.Settings.NoSalvageMechTag} mech, no parts");
                }
                else
                {
                    Log.Main.Debug?.Log($"--- Adding {numparts} parts");
                    contract.AddMechPartsToPotentialSalvage(constants, mech_to_salvage, numparts);
                }
            }

            catch (Exception e)
            {
                Log.Main.Error?.Log("Error in adding parts", e);
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
                Log.Main.Error?.Log("Error in adding component", e);
            }
        }


    }
}