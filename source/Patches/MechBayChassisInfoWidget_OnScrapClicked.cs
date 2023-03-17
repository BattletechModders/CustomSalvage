using BattleTech;
using BattleTech.UI;
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

            //foreach (var item in ___mechBay.Sim.CompanyStats)
            //{
            //    Control.Instance.Log($"{item.Key} : {item.Value.CurrentValue.ToString()} ");
            //}

            var mech = ChassisHandler.GetMech(___selectedChassis.Description.Id);
            var name = new Text(mech.Description.UIName).ToString();
            var strs = Control.Instance.Settings.Strings;

            if (___selectedChassis.MechPartMax == 0)
            {

                int value = Mathf.RoundToInt((float) ___selectedChassis.Description.Cost *
                                             ___mechBay.Sim.Constants.Finances.MechScrapModifier);

                if (Control.Instance.Settings.AllowScrapToParts)
                {
                    int max = ___mechBay.Sim.Constants.Story.DefaultMechPartMax;
                    int n1 = Mathf.Clamp(Mathf.RoundToInt(max * Control.Instance.Settings.MinScrapParts), 1, max);
                    int n2 = Mathf.Clamp(Mathf.RoundToInt(max * Control.Instance.Settings.MaxScrapParts), 1, max);


                    GenericPopupBuilder.Create(new Text(strs.ScrapDialogTitle, name).ToString(),
                        new Text(strs.ScrapDialogTextWithParts, SimGameState.GetCBillString(value), n1, n2).ToString())
                        .AddButton(new Text(strs.ButtonCancel).ToString(), null, true, null)
                        .AddButton(new Text(strs.ButtonKeepParts).ToString(), () => SplitToParts(___selectedChassis, n1, n2, ___mechBay), true, null)
                        .AddButton(new Text(strs.ButtonSell).ToString(), () => ScrapChassis(1, ___selectedChassis, __instance, ___mechBay), true,
                            null)
                        .CancelOnEscape()
                        .AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true)
                        .Render();
                }
                else
                {
                    GenericPopupBuilder.Create(new Text(strs.ScrapDialogTitle, name).ToString(),
                            new Text(strs.ScrapDialogText, SimGameState.GetCBillString(value)).ToString())
                        .AddButton(new Text(strs.ButtonCancel).ToString(), null, true, null)
                        .AddButton(new Text(strs.ButtonScrap).ToString(), () => ScrapChassis(1, ___selectedChassis, __instance, ___mechBay), true,
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
                    GenericPopupBuilder.Create(new Text(strs.ScrapPartsDialogTitle, name).ToString(),
                            new Text(strs.ScrapPartsDialogText, SimGameState.GetCBillString(value)).ToString())
                        .AddButton(new Text(strs.ButtonCancel).ToString(), null, true, null)
                        .AddButton(new Text(strs.ButtonScrap).ToString(), () => ScrapParts(1, ___selectedChassis, __instance, ___mechBay), true, null)
                        
                        .CancelOnEscape().AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).Render();
                }
                else
                {
                    var shopdef = new ShopDefItem(mech.Description.Id, ShopItemType.MechPart, 1, num, true, false, value);
                    SG_Stores_MultiPurchasePopup_Handler.StartDialog(new Text(strs.ScrapMultyPartsDialogTitle).ToString(), shopdef,
                        new Text(strs.ScrapMultyPartsDialogText, mech.Description.Name).ToString(),
                        num, value,
                        (n) => ScrapParts(n, ___selectedChassis, __instance, ___mechBay),
                        () => { return; }, ___mechBay.Sim);

                }
            }

            return false;

        }

        private static void SplitToParts(ChassisDef chassisDef, int min, int max, MechBayPanel mechBay)
        {
            int k = mechBay.Sim.NetworkRandom.Int(min, max+1);
            if (Control.Instance.Settings.DEBUG_LOTOFPARTS)
                k = 20;
            UnityGameInstance.BattleTechGame.Simulation.ScrapInactiveMech(chassisDef.Description.Id, false);
            var mech = ChassisHandler.GetMech(chassisDef.Description.Id);
            for (int i = 0;i < k;i++ )
                mechBay.Sim.AddMechPart(mech.Description.Id);
            mechBay.RefreshData(false);
            mechBay.SelectChassis(null);
            var strs = Control.Instance.Settings.Strings;

            GenericPopupBuilder.Create(new Text(strs.ScrapResultTitle, mech.Description.UIName).ToString(),
                new Text(strs.ScrapResultTitle, k, mech.Description.UIName).ToString())
                .AddButton(new Text(strs.ButtonOk).ToString(), null, true, null)
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
