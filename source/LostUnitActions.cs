﻿using System.Linq;
using BattleTech;

namespace CustomSalvage
{
    internal delegate void LostUnitActionDelegate(UnitResult unit, ContractHelper contract);

    internal static class LostUnitActions
    {
        public static void SalvageItems(UnitResult unit, ContractHelper contract)
        {
            foreach (var item in unit.mech.Inventory.Where(i => i.DamageLevel != ComponentDamageLevel.Destroyed && !unit.mech.IsLocationDestroyed(i.MountedLocation)))
                contract.AddComponentToPotentialSalvage(item.Def, item.DamageLevel, false);
        }

        public static void SalvageItemsAndParts(UnitResult unit, ContractHelper contract)
        {
            int num_parts = Control.Instance.GetNumParts(unit.mech);

            if (!unit.mech.MechTags.Contains(Control.Instance.Settings.NoSalvageMechTag) &&
                !unit.mech.Chassis.ChassisTags.Contains(Control.Instance.Settings.NoSalvageMechTag))
            {
                contract.AddMechPartsToPotentialSalvage(UnityGameInstance.BattleTechGame.Simulation.Constants,
                    unit.mech, num_parts);
            }

            foreach (var item in unit.mech.Inventory.Where(i => i.DamageLevel != ComponentDamageLevel.Destroyed && !unit.mech.IsLocationDestroyed(i.MountedLocation)))
                contract.AddComponentToPotentialSalvage(item.Def, item.DamageLevel, false);
        }

        public static void ReturnItems(UnitResult unit, ContractHelper contract)
        {
            foreach (var item in unit.mech.Inventory.Where(i => i.DamageLevel != ComponentDamageLevel.Destroyed && !unit.mech.IsLocationDestroyed(i.MountedLocation)))
                contract.AddComponentToFinalSalvage(item.Def);
        }

        public static void ReturnItemsAndParts(UnitResult unit, ContractHelper contract)
        {
            foreach (var item in unit.mech.Inventory.Where(i => i.DamageLevel != ComponentDamageLevel.Destroyed && !unit.mech.IsLocationDestroyed(i.MountedLocation)))
                contract.AddComponentToFinalSalvage(item.Def);
            int num_parts = Control.Instance.GetNumParts(unit.mech);
            contract.AddMechPartsToFinalSalvage(UnityGameInstance.BattleTechGame.Simulation.Constants, unit.mech, num_parts);
        }
    }
}