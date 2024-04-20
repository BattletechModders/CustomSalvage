using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BattleTech;
using CustomUnits;
using UnityEngine;

namespace CustomSalvage;

[HarmonyPatch(typeof(Contract), "GenerateSalvage")]
[HarmonyPriority(Priority.HigherThanNormal)]
internal static class Contract_GenerateSalvage
{
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.HigherThanNormal)]
    public static void Prefix(ref bool __runOriginal, List<UnitResult> enemyMechs, List<VehicleDef> enemyVehicles,
        List<UnitResult> lostUnits, bool logResults,
        Contract __instance)
    {
        if (!__runOriginal)
        {
            return;
        }

        __runOriginal = false;

        Log.Main.Debug?.Log($"Start GenerateSalvage for {__instance.Name}");
        if (Control.Instance.Settings.DEBUG_AllMechsSalvage)
        {
            CombatGameState combat = __instance.BattleTechGame.Combat;
            List<Mech> allMechs = combat.AllMechs;
            for (int index = 0; index < allMechs.Count; ++index)
            {
                Mech mech = allMechs[index];
                if (combat.HostilityMatrix.IsEnemy(combat.LocalPlayerTeam, mech.team))
                {
                    if(mech.IsDead == false)
                    {
                        UnitResult unitResult = new UnitResult(mech.ToMechDef(), Pilot.MakeDumbClone(mech.GetPilot()));
                        enemyMechs.Add(unitResult);
                    }
                }
            }
        }
        __instance.finalPotentialSalvage = new List<SalvageDef>();
        var Contract = new ContractHelper(__instance, true);


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
        Log.Main.Debug?.Log($"- Enemy Mechs {__instance.contractTypeID}:{__instance.OverrideID}");

        foreach (var unit in enemyMechs)
        {
            if(unit.mech == null)
            {
                Log.Main.Debug?.Log($"-- warning null mech in salvage");
                continue;
            }
            if (Control.Instance.IsDestroyed(unit) || unit.pilot.IsIncapacitated || unit.pilot.HasEjected || Control.Instance.Settings.DEBUG_AllMechsSalvage)
            {
                try
                {
                    AddMechToSalvage(unit.mech, Contract, simgame, Constants, true, false);
                }catch(Exception e)
                {
                    Log.Main.Error?.Log(e.ToString());
                }
            }
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
            {
                AddVechicleToSalvage(vehicle, Contract, simgame);
            }
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

        Contract.FilterPotentialSalvage(__instance.finalPotentialSalvage);
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

        if (unitResult.mechLost)
        {
            Control.Instance.LostUnitAction(unitResult, Contract);
        }
    }

    private static void AddTurretToSalvage(Turret turret, ContractHelper contract, SimGameState simgame)
    {
        if (turret == null || turret.allComponents != null)
        {
            foreach (var component in turret.allComponents)
            {
                var chance = simgame.NetworkRandom.Float();
                if (component.DamageLevel != ComponentDamageLevel.Destroyed && chance < Control.Instance.Settings.SalvageTurretsComponentChance)
                {
                    Log.Main.Debug?.Log($"--- {chance:0.000} < {Control.Instance.Settings.SalvageTurretsComponentChance:0.00}");
                    contract.AddComponentToPotentialSalvage(component.componentDef, component.DamageLevel, true);
                }
                else
                {
                    Log.Main.Debug?.Log($"--- {chance:0.000} > {Control.Instance.Settings.SalvageTurretsComponentChance:0.00} - {component.defId} skipped");
                }
            }
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
    public static float GetStructurePersantage(this MechDef mech)
    {
        float allStructure = 0f;
        float availStructure = 0f;
        foreach(var locationDef in mech.Chassis.Locations)
        {
            if ((locationDef.InternalStructure <= 1f) && (locationDef.MaxArmor <= 0f)) { continue; }
            allStructure += locationDef.InternalStructure;
            var location = mech.GetLocationLoadoutDef(locationDef.Location);
            if (location.DamageLevel >= LocationDamageLevel.Destroyed) { continue; }
            availStructure += location.CurrentInternalStructure;
        }
        return allStructure > 0.01f ? (availStructure / allStructure) : 0f;
    }
    public static float GetStructureBorder(MechDef mech, SimGameState sim)
    {
        float result = Control.Instance.Settings.FullUnitStructurePersentage;
        if (mech.IsVehicle() && (Control.Instance.Settings.FullVehicleStructurePersentage > 0f))
        {
            result = Control.Instance.Settings.FullVehicleStructurePersentage;
        }else if((mech.IsSquad() == false)&&(Control.Instance.Settings.FullMechStructurePersentage > 0f))
        {
            result = Control.Instance.Settings.FullMechStructurePersentage;
        }
        if(result > 0f)
        {
            result += Control.Instance.Settings.AdditionalStructurePercentagePerPart * sim.Constants.Story.DefaultMechPartMax;
        }
        return result;
    }
    public static float GetFullRecoveryChance(MechDef mech)
    {
        float result = Control.Instance.Settings.FullUnitRecoveryChance;
        if (mech.IsVehicle() && (Control.Instance.Settings.FullVehicleRecoveryChance > 0f))
        {
            result = Control.Instance.Settings.FullVehicleRecoveryChance;
        }
        else if ((mech.IsSquad() == false) && (Control.Instance.Settings.FullMechRecoveryChance > 0f))
        {
            result = Control.Instance.Settings.FullMechRecoveryChance;
        }
        return result > 0f ? result : 1f;
    }
    public static void AddMechToSalvage(MechDef mech, ContractHelper contract, SimGameState simgame, SimGameConstants constants, bool can_upgrade, bool force_disassemble)
    {
        Log.Main.Debug?.Log($"--- Salvaging mech {mech.Description.Id}");
        int numparts = Control.Instance.GetNumParts(mech);
        bool full_mech_salvage = Control.Instance.Settings.FullEnemyUnitSalvage;
        if (force_disassemble) { full_mech_salvage = false; }
        if ((full_mech_salvage) && (mech.IsVehicle() == false) && (mech.IsSquad() == false)) {
            if (mech.IsLocationDestroyed(ChassisLocations.CenterTorso)) {
                Log.Main.Debug?.Log($" unit is regular mech and CT is destroyed");
                full_mech_salvage = false;
            }
        }
        if ((full_mech_salvage) && mech.IsSquad())
        {
            Log.Main.Debug?.Log($" unit is squad");
            full_mech_salvage = false;
        }
        if((full_mech_salvage) && mech.IsVehicle() && Control.Instance.Settings.VehicleAlwaysDisassembled)
        {
            Log.Main.Debug?.Log($" unit is vehcile and VehicleAlwaysDisassembled is set");
            full_mech_salvage = false;
        }
        float structAvail = GetStructurePersantage(mech);
        float structBorder = GetStructureBorder(mech, simgame);
        Log.Main.Debug?.Log($" unit rest structure persentage {structAvail} border:{structBorder}");
        if (full_mech_salvage && (structBorder > 0f) && (structAvail < structBorder))
        {
            Log.Main.Debug?.Log($"  wasted");
            full_mech_salvage = false;
        }
        float recoveryRoll = UnityEngine.Random.Range(0f, 1f);
        float rollBorder = GetFullRecoveryChance(mech);
        Log.Main.Debug?.Log($" unit full recovery roll {recoveryRoll} border:{rollBorder}");
        if(full_mech_salvage && (recoveryRoll > rollBorder))
        {
            Log.Main.Debug?.Log($"  wasted");
            full_mech_salvage = false;
        }
        try
        {
            var mech_to_salvage = ChassisHandler.FindMechReplace(simgame, contract, mech);
            if (mech != mech_to_salvage) {
                Log.Main.Debug?.Log($"--- mech has salvage replacement {mech.Description.Id} -> {mech_to_salvage.Description.Id}");
                full_mech_salvage = false; 
            }
            if (mech_to_salvage == null || mech_to_salvage.MechTags.Contains(Control.Instance.Settings.NoSalvageMechTag) ||
                mech_to_salvage.Chassis.ChassisTags.Contains(Control.Instance.Settings.NoSalvageMechTag))
            {
                Log.Main.Debug?.Log($"--- {Control.Instance.Settings.NoSalvageMechTag} mech, no parts");
                full_mech_salvage = false;
            }
            else
            {
                if (full_mech_salvage == false)
                {
                    Log.Main.Debug?.Log($"--- Adding {numparts} parts");
                    contract.AddMechPartsToPotentialSalvage(constants, mech_to_salvage, numparts);
                }
                else
                {
                    Log.Main.Debug?.Log($"--- Adding full");
                    contract.AddMechToPotentialSalvage(constants, mech_to_salvage);
                }
            }
        }
        catch (Exception e)
        {
            Log.Main.Error?.Log("Error in adding parts", e);
        }

        try
        {

            if (full_mech_salvage == false)
            {
                if (((mech.IsVehicle() == false) && (mech.IsSquad() == false))||(mech.IsVehicle() && Control.Instance.Settings.VehicleDisassembleComponents) || (mech.IsSquad() && Control.Instance.Settings.SquadDisassembleComponents))
                {
                    bool isVehcile = mech.IsVehicle();
                    Log.Main.Debug?.Log($"--- Adding components isVehicle:{isVehcile} partedit:{CustomUnits.Core.Settings.VehcilesPartialEditable}");
                    foreach (var component in mech.Inventory.Where(item =>
                                 !mech.IsLocationDestroyed(item.MountedLocation) &&
                                 item.DamageLevel != ComponentDamageLevel.Destroyed))
                    {
                        if (CustomUnits.Core.Settings.VehcilesPartialEditable && isVehcile && Control.Instance.Settings.VehicleDisassembleEditableComponentsOnly)
                        {
                            Log.Main.Debug?.Log($"  {component.ComponentDefID}:{component.ComponentDefType}");
                            if (component.isEditable())
                            {
                                contract.AddComponentToPotentialSalvage(component.Def, ComponentDamageLevel.Functional, can_upgrade);
                            }
                        }
                        else
                        {
                            contract.AddComponentToPotentialSalvage(component.Def, ComponentDamageLevel.Functional, can_upgrade);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Main.Error?.Log("Error in adding component", e);
        }

    }


}