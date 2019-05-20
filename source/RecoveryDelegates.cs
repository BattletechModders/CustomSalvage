using BattleTech;

namespace CustomSalvage
{
    public delegate bool RecoveryDelegate(UnitResult result, ContractHelper contract);

    public static class RecoveryDelegates
    {
        public static bool PartDestroyed(UnitResult result, ContractHelper contract)
        {
            var simgame = contract.Contract.BattleTechGame.Simulation;
            var chance = simgame.Constants.Salvage.DestroyedMechRecoveryChance;
            var mech = result.mech;


            chance -= mech.IsLocationDamaged(ChassisLocations.Head)
                ? Control.Settings.HeadRecoveryPenaly
                : 0;

            chance -= mech.IsLocationDestroyed(ChassisLocations.LeftTorso)
                ? Control.Settings.TorsoRecoveryPenalty
                : 0;
            chance -= mech.IsLocationDestroyed(ChassisLocations.CenterTorso)
                ? Control.Settings.TorsoRecoveryPenalty
                : 0;
            chance -= mech.IsLocationDestroyed(ChassisLocations.RightTorso)
                ? Control.Settings.TorsoRecoveryPenalty
                : 0;

            chance -= mech.IsLocationDestroyed(ChassisLocations.RightArm)
                ? Control.Settings.LimbRecoveryPenalty
                : 0;
            chance -= mech.IsLocationDestroyed(ChassisLocations.RightLeg)
                ? Control.Settings.LimbRecoveryPenalty
                : 0;
            chance -= mech.IsLocationDestroyed(ChassisLocations.LeftArm)
                ? Control.Settings.LimbRecoveryPenalty
                : 0;
            chance -= mech.IsLocationDestroyed(ChassisLocations.LeftLeg)
                ? Control.Settings.LimbRecoveryPenalty
                : 0;

            chance += result.pilot.HasEjected
                ? Control.Settings.EjectRecoveryBonus
                : 0;


            var num = simgame.NetworkRandom.Float(0f, 1f);
            var recover = chance > num;

            Control.LogDebug(recover
                ? $"--- {num:0.00} vs {chance:0.00} - roll success, recovery"
                : $"--- {num:0.00} vs {chance:0.00} - roll failed, no recovery");

            return recover;
        }

        public static bool VanilaRecovery(UnitResult result, ContractHelper contract)
        {
            var mech = result.mech;

            Control.LogDebug($"-- Recovery {mech.Name} vanila method");
            var simgame = contract.Contract.BattleTechGame.Simulation;
            float num = simgame.NetworkRandom.Float(0f, 1f);

            if (mech.IsLocationDestroyed(ChassisLocations.CenterTorso))
            {
                Control.LogDebug($"--- CenterTorso Destroyed - no recovery");
                return false;
            }
            else
            {
                var chance = simgame.Constants.Salvage.DestroyedMechRecoveryChance;
                var recover = chance > num;

                Control.LogDebug(recover
                    ? $"--- {num:0.00} vs {chance:0.00} - roll success, recovery"
                    : $"--- {num:0.00} vs {chance:0.00} - roll failed, no recovery");

                return recover;
            }

        }
    }
}