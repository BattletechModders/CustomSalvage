using System;
using BattleTech;

namespace CustomSalvage.MechBroke
{
    public static class RandomBroke
    {

        public static void BrokeMech(MechDef new_mech, SimGameState sim, int other_parts)
        {
            try
            {

                Control.Instance.LogDebug($"-- broke parts");
                var rnd = new Random();
                var chances = new AssemblyChancesResult(new_mech, sim, other_parts);
                var settings = Control.Instance.Settings;

                Control.Instance.LogDebug($"--- RepairMechLimbsChance: {chances.LimbChance}, RepairMechLimbs: {settings.RepairMechLimbs} ");
                float roll = 0;
                //hd
                roll = (float)rnd.NextDouble();
                Control.Instance.LogDebug($"--- HeadRepaired: {settings.HeadRepaired}, roll: {roll} ");
                BrokeTools.BrokeLocation(new_mech, ChassisLocations.Head, roll > chances.LimbChance);

                //ct
                roll = (float)rnd.NextDouble();
                Control.Instance.LogDebug($"--- CentralTorsoRepaired: {settings.CentralTorsoRepaired}, roll: {roll} ");
                BrokeTools.BrokeLocation(new_mech, ChassisLocations.CenterTorso, roll > chances.LimbChance);

                //rt
                roll = (float)rnd.NextDouble();
                Control.Instance.LogDebug($"--- RightTorsoRepaired: {settings.RightTorsoRepaired}, roll: {roll} ");
                BrokeTools.BrokeLocation(new_mech, ChassisLocations.RightTorso, roll > chances.LimbChance);

                //lt
                roll = (float)rnd.NextDouble();
                Control.Instance.LogDebug($"--- LeftTorsoRepaired: {settings.LeftTorsoRepaired}, roll: {roll} ");
                BrokeTools.BrokeLocation(new_mech, ChassisLocations.LeftTorso, roll > chances.LimbChance);

                //ra
                roll = (float)rnd.NextDouble();
                Control.Instance.LogDebug($"--- RightArmRepaired: {settings.RightArmRepaired}, roll: {roll} ");
                BrokeTools.BrokeLocation(new_mech, ChassisLocations.RightArm, roll > chances.LimbChance);
                //la
                roll = (float)rnd.NextDouble();
                Control.Instance.LogDebug($"--- LeftArmRepaired: {settings.LeftArmRepaired}, roll: {roll} ");
                BrokeTools.BrokeLocation(new_mech, ChassisLocations.LeftArm, roll > chances.LimbChance);

                //rl
                roll = (float)rnd.NextDouble();
                Control.Instance.LogDebug($"--- RightLegRepaired: {settings.RightLegRepaired}, roll: {roll} ");
                BrokeTools.BrokeLocation(new_mech, ChassisLocations.RightLeg, roll > chances.LimbChance);

                //ll
                roll = (float)rnd.NextDouble();
                Control.Instance.LogDebug($"--- LeftLegRepaired: {settings.LeftLegRepaired}, roll: {roll} ");
                BrokeTools.BrokeLocation(new_mech, ChassisLocations.LeftLeg, roll > chances.LimbChance);

                Control.Instance.LogDebug($"-- broke equipment");

                BrokeTools.BrokeEquipment(new_mech, chances.CompFChance, chances.CompNFChance);
            }
            catch (Exception e)
            {
                Control.Instance.LogError($"ERROR in BrokeParts", e);
                throw;
            }
        }
    }
}