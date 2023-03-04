using System;
using BattleTech;

namespace CustomSalvage
{
    public delegate int PartsNumDelegeate(MechDef mech);

    public static class PartsNumCalculations
    {
        internal static int VanilaAdjusted(MechDef mech)
        {
            int n =
                UnityGameInstance.BattleTechGame.Simulation.Constants.Story.DefaultMechPartMax;
            {
                if (mech.IsLocationDestroyed(ChassisLocations.CenterTorso))
                    return (int)(n * Control.Instance.Settings.VACTDestroyedMod);
                if (mech.IsLocationDestroyed(ChassisLocations.LeftLeg) &&
                    mech.IsLocationDestroyed(ChassisLocations.RightLeg))
                    return (int)(n * Control.Instance.Settings.VABLDestroyedMod);
                return n;
            }
        }

        internal static int PartDestroyed(MechDef mech)
        {
            var settings = Control.Instance.Settings;
            if (mech.IsLocationDestroyed(ChassisLocations.CenterTorso))
                return settings.CenterTorsoDestroyedParts;


            float total = settings.SalvageArmWeight * 2 + settings.SalvageHeadWeight +
                          settings.SalvageLegWeight * 2 + settings.SalvageTorsoWeight * 2 + 1;

            float val = total;

            val -= mech.IsLocationDestroyed(ChassisLocations.Head) ? settings.SalvageHeadWeight : 0;

            val -= mech.IsLocationDestroyed(ChassisLocations.LeftTorso)
                ? settings.SalvageTorsoWeight
                : 0;
            val -= mech.IsLocationDestroyed(ChassisLocations.RightTorso)
                ? settings.SalvageTorsoWeight
                : 0;

            val -= mech.IsLocationDestroyed(ChassisLocations.LeftLeg) ? settings.SalvageLegWeight : 0;
            val -= mech.IsLocationDestroyed(ChassisLocations.RightLeg) ? settings.SalvageLegWeight : 0;

            val -= mech.IsLocationDestroyed(ChassisLocations.LeftArm) ? settings.SalvageArmWeight : 0;
            val -= mech.IsLocationDestroyed(ChassisLocations.LeftLeg) ? settings.SalvageArmWeight : 0;

            var constants = UnityGameInstance.BattleTechGame.Simulation.Constants;

            int numparts = (int)(constants.Story.DefaultMechPartMax * val / total + 0.5f);
            if (numparts <= 0)
                numparts = 1;
            if (numparts > constants.Story.DefaultMechPartMax)
                numparts = constants.Story.DefaultMechPartMax;

            Log.Main.Debug?.Log($"Parts {val}/{total} = {val/total:0.00} =  {numparts}");

            return numparts;
        }

        internal static int PartDestroyedNoCT(MechDef mech)
        {
            var settings = Control.Instance.Settings;
            float total = settings.SalvageArmWeight * 2 + settings.SalvageHeadWeight +
                          settings.SalvageLegWeight * 2 + settings.SalvageTorsoWeight * 2 + 1 + 
                          settings.SalvageCTWeight;

            float val = total;

            val -= mech.IsLocationDestroyed(ChassisLocations.Head) ? settings.SalvageHeadWeight : 0;

            val -= mech.IsLocationDestroyed(ChassisLocations.LeftTorso)
                ? settings.SalvageTorsoWeight
                : 0;
            val -= mech.IsLocationDestroyed(ChassisLocations.RightTorso)
                ? settings.SalvageTorsoWeight
                : 0;

            val -= mech.IsLocationDestroyed(ChassisLocations.LeftLeg) ? settings.SalvageLegWeight : 0;
            val -= mech.IsLocationDestroyed(ChassisLocations.RightLeg) ? settings.SalvageLegWeight : 0;

            val -= mech.IsLocationDestroyed(ChassisLocations.LeftArm) ? settings.SalvageArmWeight : 0;
            val -= mech.IsLocationDestroyed(ChassisLocations.LeftLeg) ? settings.SalvageArmWeight : 0;

            val -= mech.IsLocationDestroyed(ChassisLocations.CenterTorso) ? settings.SalvageCTWeight : 0;

            var constants = UnityGameInstance.BattleTechGame.Simulation.Constants;

            int numparts = (int)(constants.Story.DefaultMechPartMax * val / total + 0.5f);
            if (numparts <= 0)
                numparts = 1;
            if (numparts > constants.Story.DefaultMechPartMax)
                numparts = constants.Story.DefaultMechPartMax;

            Log.Main.Debug?.Log($"Parts {val}/{total} = {val / total:0.00} =  {numparts}");
            return numparts;
        }

        internal static int Vanila(MechDef mech)
        {
            if (mech.IsLocationDestroyed(ChassisLocations.CenterTorso))
                return 1;
            if (mech.IsLocationDestroyed(ChassisLocations.LeftLeg) &&
                mech.IsLocationDestroyed(ChassisLocations.RightLeg))
                return 2;
            return 3;
        }
    }
}