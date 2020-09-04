using System;
using BattleTech;
using BattleTech.UI;
using Harmony;
using HBS;
using Localize;
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
            var name = new Text(mech.Description.UIName).ToString();

            if (___selectedChassis.MechPartMax == 0)
            {

                int value = Mathf.RoundToInt((float) ___selectedChassis.Description.Cost *
                                             ___mechBay.Sim.Constants.Finances.MechScrapModifier);

                if (Control.Instance.Settings.AllowScrapToParts)
                {
                    int max = ___mechBay.Sim.Constants.Story.DefaultMechPartMax;
                    int n1 = Mathf.Clamp(Mathf.RoundToInt(max * Control.Instance.Settings.MinScrapParts), 1, max);
                    int n2 = Mathf.Clamp(Mathf.RoundToInt(max * Control.Instance.Settings.MaxScrapParts), 1, max);


                    GenericPopupBuilder.Create($"Scrap {name}?",
                            $"Do you want scrap this chassis and sale spare parts for <color=#F79B26FF>{SimGameState.GetCBillString(value)}</color> or scrap and keep parts ({n1}-{n2} parts)")
                        .AddButton("Cancel", null, true, null)
                        .AddButton("Keep Parts", () => SplitToParts(___selectedChassis, n1, n2, ___mechBay), true, null)
                        .AddButton("Sale", () => ScrapChassis(1, ___selectedChassis, __instance, ___mechBay), true,
                            null)
                        .CancelOnEscape()
                        .AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true)
                        .Render();
                }
                else
                {
                    GenericPopupBuilder.Create($"Scrap {name}?",
                            $"Are you sure you want to scrap this 'Mech Chassis? It will be removed permanently from your inventory.\n\nSCRAP VALUE: <color=#F79B26FF>{SimGameState.GetCBillString(value)}</color>")
                        .AddButton("Cancel", null, true, null)
                        .AddButton("scrap", () => ScrapChassis(1, ___selectedChassis, __instance, ___mechBay), true,
                            null)
                        .CancelOnEscape()
                        .AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true)
                        .Render();
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
                        .AddButton("Scrap", () => ScrapParts(1, ___selectedChassis, __instance, ___mechBay), true, null)
                        
                        .CancelOnEscape().AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).Render();
                }
                else
                {
 //                   var popup = LazySingletonBehavior<UIManager>.Instance.GetOrCreatePopupModule<SG_Stores_MultiPurchasePopup>(string.Empty);
                   
                    var shopdef = new ShopDefItem(mech.Description.Id, ShopItemType.MechPart, 1, num, true, false, value);

                    SG_Stores_MultiPurchasePopup_Handler.StartDialog("Scrap", shopdef, mech.Description.Name + " Parts", num, value,
                        (n) => ScrapParts(n, ___selectedChassis, __instance, ___mechBay),
                        () => { return; }, ___mechBay.Sim);

//                    popup.SetData(___mechBay.Sim, shopdef, name + " parts", num, value, (n) => ScrapParts(n, ___selectedChassis, __instance, ___mechBay));
                }
            }

            return false;

        }

        private static void SplitToParts(ChassisDef chassisDef, int min, int max, MechBayPanel mechBay)
        {
            int k = mechBay.Sim.NetworkRandom.Int(min, max+1);
            UnityGameInstance.BattleTechGame.Simulation.ScrapInactiveMech(chassisDef.Description.Id, false);
            var mech = ChassisHandler.GetMech(chassisDef.Description.Id);
            for (int i = 0;i < k;i++ )
                mechBay.Sim.AddMechPart(mech.Description.Id);
            mechBay.RefreshData(false);
            mechBay.SelectChassis(null);

            GenericPopupBuilder.Create($"Scraped {mech.Description.UIName}.",
                    $"We manage to get <color=#20ff20>{k}</color> parts from {mech.Description.UIName} chassis")
                .AddButton("Ok", null, true, null)
                .CancelOnEscape().AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).Render();
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
