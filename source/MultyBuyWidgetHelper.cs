using Harmony;
using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using HBS;
using UnityEngine.Events;

namespace CustomSalvage
{
    public static class SG_Stores_MultiPurchasePopup_Handler
    {
        public static bool Replace { get; private set; } = false;
        public static string Text { get; private set; } = "SELL";


        public static void StartDialog(string replace_title, ShopDefItem selected, string item_name, int max, int price,
            UnityAction<int> on_confirm, UnityAction on_cancel, SimGameState sim)
        {
            Replace = true;
            Text = replace_title;
            var popup = LazySingletonBehavior<UIManager>.Instance.GetOrCreatePopupModule<SG_Stores_MultiPurchasePopup>("", true);
            popup.SetData(sim, selected, item_name, max, price, on_confirm, on_cancel);
        }

        public static void Reset()
        {
            Replace = false;
        }
    }

    [HarmonyPatch(typeof(SG_Stores_MultiPurchasePopup))]
    [HarmonyPatch("Refresh")]
    public static class SG_Stores_MultiPurchasePopup_Refresh
    {
        [HarmonyPostfix]
        public static void ReplaceTitle(LocalizableText ___TitleText, LocalizableText ___DescriptionText,
            string ___itemName, int ___costPerUnit, int ___quantityBeingSold, HBSDOTweenButton ___ConfirmButton)
        {
            if (!SG_Stores_MultiPurchasePopup_Handler.Replace)
                return;

            ___TitleText.SetText($"{SG_Stores_MultiPurchasePopup_Handler.Text} {___itemName}");
            var value = SimGameState.GetCBillString(___costPerUnit * ___quantityBeingSold);
            ___DescriptionText.SetText($"{SG_Stores_MultiPurchasePopup_Handler.Text} FOR <color=#F79B26>{value}</color>");
            ___ConfirmButton.SetText(SG_Stores_MultiPurchasePopup_Handler.Text);
        }
    }

    [HarmonyPatch(typeof(SG_Stores_MultiPurchasePopup))]
    [HarmonyPatch("OnCancel")]
    public static class SG_Stores_MultiPurchasePopup_OnCancel
    {
        [HarmonyPostfix]
        public static void HandleExit()
        {
            if (!SG_Stores_MultiPurchasePopup_Handler.Replace)
                return;
            SG_Stores_MultiPurchasePopup_Handler.Reset();
        }
    }

    [HarmonyPatch(typeof(SG_Stores_MultiPurchasePopup))]
    [HarmonyPatch("OnConfirm")]
    public static class SG_Stores_MultiPurchasePopup_OnConfirm
    {
        [HarmonyPostfix]
        public static void HandleExit()
        {
            if (!SG_Stores_MultiPurchasePopup_Handler.Replace)
                return;
            SG_Stores_MultiPurchasePopup_Handler.Reset();
        }
    }
}
