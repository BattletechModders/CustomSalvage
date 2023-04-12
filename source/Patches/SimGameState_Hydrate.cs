using BattleTech.Save.Test;
using BattleTech.Save;
using BattleTech;
using CustomUnits;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using CustomSalvage.MechBroke;

namespace CustomSalvage
{
    [HarmonyPatch(typeof(SimGameState), "Dehydrate")]
    public static class SimGameState_Dehydrate
    {
        static void Prefix(SimGameState __instance, SerializableReferenceContainer references, ref List<string> ___LastUsedMechs, ref List<string> ___LastUsedPilots)
        {
            Log.Main.Debug?.Log($"SimGameState.Dehydrate");
            try
            {
                var broke_rnd = __instance.CompanyStats.GetStatistic(BrokeTools.BROKE_RANDOM_DATA_STAT_NAME);
                if (broke_rnd == null) { broke_rnd = __instance.CompanyStats.AddStatistic<string>(BrokeTools.BROKE_RANDOM_DATA_STAT_NAME, "{}"); }
                broke_rnd.SetValue<string>(JsonConvert.SerializeObject(BrokeTools.rnd));
                ChassisHandler.SaveEmptyPartsInfo(__instance);
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log(e.ToString());
            }
        }
    }
    [HarmonyPatch(typeof(SimGameState), "Rehydrate")]
    public static class SimGameState_Rehydrate
    {
        static void Postfix(SimGameState __instance, GameInstanceSave gameInstanceSave)
        {
            Log.Main.Debug?.Log($"SimGameState.Rehydrate");
            try
            {
                var broke_rnd = __instance.CompanyStats.GetStatistic(BrokeTools.BROKE_RANDOM_DATA_STAT_NAME);
                if (broke_rnd == null)
                {
                    broke_rnd = __instance.CompanyStats.AddStatistic<string>(BrokeTools.BROKE_RANDOM_DATA_STAT_NAME, "{}");
                    Log.Main.Debug?.Log($" init new random broke data");
                    BrokeTools.rnd.InitNew();
                }
                else
                {
                    BrokeTools.rnd = JsonConvert.DeserializeObject<BrokeRandimizeData>(broke_rnd.Value<string>());
                }
                ChassisHandler.LoadEmptyPartsInfo(__instance);
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log(e.ToString());
            }
        }
    }

}