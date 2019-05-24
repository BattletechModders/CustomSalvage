using System;
using BattleTech;
using BattleTech.UI;
using Harmony;
using HBS;
using UnityEngine;

namespace CustomSalvage
{
    [HarmonyPatch(typeof(MechBayChassisInfoWidget))]
    [HarmonyPatch("OnScrapClicked")]
    public static class MechBayChassisInfoWidget_OnScrapClicked
    {

        [HarmonyPrefix]
        public static bool OnReadyClicked(ChassisDef ___selectedChassis, MechBayPanel ___mechBay
            , MechBayChassisUnitElement ___chassisElement, MechBayChassisInfoWidget __instance)
        {
            if (___selectedChassis == null)
                return false;

            var mech = ChassisHandler.GetMech(___selectedChassis.Description.Id);
            var name = mech.Description.UIName;

            if (___selectedChassis.MechPartMax == 0)
            {

                var chassisQuantity = ___mechBay.Sim.GetItemCount(___selectedChassis.Description.Id, 
                    typeof(MechDef), SimGameState.ItemCountType.UNDAMAGED_ONLY);
                int value = Mathf.RoundToInt((float)___selectedChassis.Description.Cost * ___mechBay.Sim.Constants.Finances.MechScrapModifier);

                if (chassisQuantity == 1)
                {
                    GenericPopupBuilder.Create($"Scrap {name}?",
                        $"Are you sure you want to scrap this 'Mech Chassis? It will be removed permanently from your inventory.\n\nSCRAP VALUE: <color=#F79B26FF>{SimGameState.GetCBillString(value)}</color>")
                        .AddButton("Cancel", null, true, null)
                        .AddButton("Scrap", () => ScrapChassis(1, ___selectedChassis, __instance, ___mechBay), true, null).CancelOnEscape().AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).Render();
                }
                else
                {
                    var popup = LazySingletonBehavior<UIManager>.Instance.GetOrCreatePopupModule<SG_Stores_MultiPurchasePopup>(string.Empty);
                    var shopdef = new ShopDefItem(___selectedChassis.Description.Id, ShopItemType.Mech, 1, chassisQuantity, true, false, value);
                    popup.SetData(___mechBay.Sim, shopdef, name, chassisQuantity, value, (n) => ScrapChassis(n, ___selectedChassis, __instance, ___mechBay));
                }
            }
            else
            {
                int num = ___selectedChassis.MechPartCount;
                int value = Mathf.RoundToInt((float)___selectedChassis.Description.Cost * ___mechBay.Sim.Constants.Finances.MechScrapModifier) / ___selectedChassis.MechPartMax;

                if (num == 1)
                {
                    GenericPopupBuilder.Create($"Scrap {name} part?",
                            $"Are you sure you want to scrap this 'Mech part? It will be removed permanently from your inventory.\n\nSCRAP VALUE: <color=#F79B26FF>{SimGameState.GetCBillString(value)}</color>")
                        .AddButton("Cancel", null, true, null)
                        .AddButton("Scrap", () => ScrapParts(1, ___selectedChassis, __instance, ___mechBay), true, null).CancelOnEscape().AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).Render();
                }
                else
                {
                    var popup = LazySingletonBehavior<UIManager>.Instance.GetOrCreatePopupModule<SG_Stores_MultiPurchasePopup>(string.Empty);
                    var shopdef = new ShopDefItem(mech.Description.Id, ShopItemType.MechPart, 1, num, true, false, value);
                    popup.SetData(___mechBay.Sim, shopdef, name + " parts", num, value, (n) => ScrapParts(n, ___selectedChassis, __instance, ___mechBay));
                }
            }

            return false;

        }


        private static void ScrapParts(int num, ChassisDef mech, MechBayChassisInfoWidget widget, MechBayPanel mechBay)
        {
            for (int i = 0; i < num; i++)
                UnityGameInstance.BattleTechGame.Simulation.ScrapMechPart(mech.Description.Id, 1,
                    UnityGameInstance.BattleTechGame.Simulation.Constants.Story.DefaultMechPartMax, true);
            widget.SetData(mechBay, null);
            mechBay.RefreshData(false);
            mechBay.SelectChassis(null);
        }

        private static void ScrapChassis(int num, ChassisDef p1, MechBayChassisInfoWidget widget, MechBayPanel mechBay)
        {
            for(int i =0;i < num;i ++)
                UnityGameInstance.BattleTechGame.Simulation.ScrapInactiveMech(p1.Description.Id, true);
            mechBay.RefreshData(false);
            mechBay.SelectChassis(null);
        }
    }
}
