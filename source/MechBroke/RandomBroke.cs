using System;
using BattleTech;

namespace CustomSalvage.MechBroke;

public static class RandomBroke
{

    public static void BrokeMech(MechDef new_mech, SimGameState sim, int other_parts, int used_parts, int used_empty_parts)
    {
        try
        {

            Log.Main.Debug?.Log($"-- broke parts");
            //var rnd = new Random();
            var chances = new AssemblyChancesResult(new_mech, sim, other_parts, used_parts, used_empty_parts);
            var settings = Control.Instance.Settings;

            Log.Main.Debug?.Log($"--- RepairMechLimbsChance: {chances.LimbChance}, RepairMechLimbs: {settings.RepairMechLimbs} ");
            float roll = 0;
            //hd
            roll = BrokeTools.rnd.Next();
            Log.Main.Debug?.Log($"--- HeadRepaired: {settings.HeadRepaired}, roll: {roll} ");
            BrokeTools.BrokeLocation(new_mech, ChassisLocations.Head, roll > chances.LimbChance);

            //ct
            roll = BrokeTools.rnd.Next();
            Log.Main.Debug?.Log($"--- CentralTorsoRepaired: {settings.CentralTorsoRepaired}, roll: {roll} ");
            BrokeTools.BrokeLocation(new_mech, ChassisLocations.CenterTorso, roll > chances.LimbChance);

            //rt
            roll = BrokeTools.rnd.Next();
            Log.Main.Debug?.Log($"--- RightTorsoRepaired: {settings.RightTorsoRepaired}, roll: {roll} ");
            BrokeTools.BrokeLocation(new_mech, ChassisLocations.RightTorso, roll > chances.LimbChance);

            //lt
            roll = BrokeTools.rnd.Next();
            Log.Main.Debug?.Log($"--- LeftTorsoRepaired: {settings.LeftTorsoRepaired}, roll: {roll} ");
            BrokeTools.BrokeLocation(new_mech, ChassisLocations.LeftTorso, roll > chances.LimbChance);

            //ra
            roll = BrokeTools.rnd.Next();
            Log.Main.Debug?.Log($"--- RightArmRepaired: {settings.RightArmRepaired}, roll: {roll} ");
            BrokeTools.BrokeLocation(new_mech, ChassisLocations.RightArm, roll > chances.LimbChance);
            //la
            roll = BrokeTools.rnd.Next();
            Log.Main.Debug?.Log($"--- LeftArmRepaired: {settings.LeftArmRepaired}, roll: {roll} ");
            BrokeTools.BrokeLocation(new_mech, ChassisLocations.LeftArm, roll > chances.LimbChance);

            //rl
            roll = BrokeTools.rnd.Next();
            Log.Main.Debug?.Log($"--- RightLegRepaired: {settings.RightLegRepaired}, roll: {roll} ");
            BrokeTools.BrokeLocation(new_mech, ChassisLocations.RightLeg, roll > chances.LimbChance);

            //ll
            roll = BrokeTools.rnd.Next();
            Log.Main.Debug?.Log($"--- LeftLegRepaired: {settings.LeftLegRepaired}, roll: {roll} ");
            BrokeTools.BrokeLocation(new_mech, ChassisLocations.LeftLeg, roll > chances.LimbChance);

            Log.Main.Debug?.Log($"-- broke equipment");

            BrokeTools.BrokeEquipment(new_mech, chances.CompFChance, chances.CompNFChance);
        }
        catch (Exception e)
        {
            Log.Main.Error?.Log($"ERROR in BrokeParts", e);
            throw;
        }
    }
}