using BattleTech;
using BattleTech.BinkMedia;
using BattleTech.Data;
using BattleTech.Save.SaveGameStructure;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using IRBTModUtils;
using SVGImporter;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UIWidgets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using static CustomComponents.WorkOrderCosts;
using static RootMotion.FinalIK.Grounding;

namespace CustomSalvage
{
    public static class MechBayMechInfoWidgetHelper
    {
        public static void SetData(this MechBayMechInfoWidget info, SalvageDef salageDef, SimGameState sim)
        {
            info.sim = sim;
            info.mechBay = null;
            info.dataManager = sim.DataManager;
            info.selectedMechElement = null;
            info.selectedMech = salageDef.mechDef;
            info.rootInfoObj.SetActive(true);
            info.noInfoObj.SetActive(false);
            if (info.repairBtnObj != null) { info.repairBtnObj.SetActive(false); }
            info.workInProgressObj.SetActive(false);
            info.lcWorkInProgressObj?.SetActive(false);
            info.SetDescriptions();
            info.SetStats();
            info.SetPaperdoll();
            info.SetLoadout();
            info.SetHardpoints();
            info.SetInitiative();
        }
        public static void SanitizeLootMagnetStacking(this SimGameState sim, List<SalvageDef> salvageList)
        {
            List<SalvageDef> list = new List<SalvageDef>(salvageList);
            Log.Main.Debug?.Log($"SanitizeLootMagnetStacking");
            foreach (SalvageDef salvageDef in list)
            {
                string displayName = salvageDef.Description.UIName;
                int qty_pos = displayName.IndexOf(" <lowercase>[QTY:");
                if (qty_pos >= 0)
                {
                    displayName = displayName.Remove(qty_pos);
                }
                salvageDef.Description.UIName = displayName;
                string rewardId = salvageDef.RewardID;
                qty_pos = rewardId.IndexOf("_LootMagnetInfo:c");
                if (qty_pos >= 0)
                {
                    int qtyIdx = rewardId.IndexOf("_qty");
                    string countS = rewardId.Substring(qtyIdx + "_qty".Length);
                    salvageDef.RewardID = rewardId.Remove(qty_pos);
                    salvageDef.Count = 1;
                    int count = int.Parse(countS);
                    Log.Main.Debug?.Log($" -- Salvage {salvageDef.Description.Id}:{salvageDef.RewardID} expanding to: {countS}");
                    for (int t = 1; t < count; ++t)
                    {
                        SalvageDef newSalvage = new SalvageDef(salvageDef);
                        newSalvage.RewardID = sim.GenerateSimGameUID();
                        newSalvage.Count = 1;
                        salvageList.Add(newSalvage);
                    }
                }
            }
            foreach(SalvageDef salvageDef in list)
            {
                if(salvageDef.Count > 1)
                {
                    for (int t = 1; t < salvageDef.Count; ++t)
                    {
                        SalvageDef newSalvage = new SalvageDef(salvageDef);
                        newSalvage.RewardID = sim.GenerateSimGameUID();
                        newSalvage.Count = 1;
                        salvageList.Add(newSalvage);
                    }
                    salvageDef.Count = 1;
                }
            }
            foreach (SalvageDef salvageDef in list)
            {
                switch (salvageDef.Type)
                {
                    case SalvageDef.SalvageType.COMPONENT: {
                            if (salvageDef.MechComponentDef == null) {
                                Log.Main.Debug?.Log($" -- WARNING {salvageDef.Description.Id}:{salvageDef.RewardID} without component definition");
                                continue; 
                            }
                            if(salvageDef.MechComponentDef.Description.Cost != salvageDef.Description.Cost)
                            {
                                Log.Main.Debug?.Log($" -- Salvage component {salvageDef.Description.Id}:{salvageDef.RewardID} set cost to {salvageDef.MechComponentDef.Description.Cost}");
                                salvageDef.Description.Cost = salvageDef.MechComponentDef.Description.Cost;
                            }
                        }; break;
                    case SalvageDef.SalvageType.MECH_PART:
                        {
                            MechDef mechDef = sim.DataManager.mechDefs.Get(salvageDef.Description.Id);
                            if (mechDef == null) {
                                Log.Main.Debug?.Log($" -- WARNING {salvageDef.Description.Id}:{salvageDef.RewardID} can't find mechdef");
                                continue;
                            }
                            if (mechDef.Description.Cost != salvageDef.Description.Cost)
                            {
                                Log.Main.Debug?.Log($" -- Salvage mech part {salvageDef.Description.Id}:{salvageDef.RewardID} set cost to {mechDef.Description.Cost}");
                                salvageDef.Description.Cost = mechDef.Description.Cost;
                            }
                        }; break;
                }
            }
        }
    }
    public class FullMechSalvageInfo : EventTrigger
    {
        public static readonly string FULL_MECH_SUFFIX = "_FULL_MECH";
        public ListElementController_SalvageFullMech_NotListView owner = null;
        public AAR_SalvageScreen salvageScreen = null;
        public GenericPopup popup = null;
        public MechBayMechInfoWidget info = null;
        public GameObject info_paintSelector = null;
        public GameObject info_refitBtn = null;
        public GameObject info_optionBtn = null;
        public bool allowDisassemble { get; set; } = true;
        public void Init(ListElementController_SalvageFullMech_NotListView owner, AAR_SalvageScreen salvageScreen)
        {
            this.owner = owner;
            this.salvageScreen = salvageScreen;
        }
        public void Pool()
        {
            this.OnInfoClose();
            salvageScreen = null;
            this.owner = null;
        }
        public void IconLoaded(string id, SVGAsset icon)
        {
            if (this.owner != null)
            {
                this.owner.ItemWidget.icon.vectorGraphics = icon;
            }
        }
        public static FullMechSalvageInfo Instantine(InventoryItemElement_NotListView parent)
        {
            var TOOLTIP = parent.SalvageTooltip.gameObject;
            var TYPE_ICON_TR = parent.icon.transform.parent.parent.gameObject.GetComponent<RectTransform>();
            var TOOLTIP2 = GameObject.Instantiate(TOOLTIP);
            TOOLTIP2.name = "TOOLTIP2";
            var TOOLTIP_TR = TOOLTIP.GetComponent<RectTransform>();
            var TOOLTIP2_TR = TOOLTIP2.GetComponent<RectTransform>();
            TOOLTIP2.transform.SetParent(TOOLTIP.transform.parent);
            var main_tr = parent.gameObject.GetComponent<RectTransform>();
            TOOLTIP2_TR.pivot = new Vector2(0f, 0.5f);
            Log.Main.Debug?.Log("full mech. InitAndCreate");
            TOOLTIP2_TR.sizeDelta = new Vector2(TYPE_ICON_TR.sizeDelta.x - main_tr.sizeDelta.x, 0f);
            TOOLTIP_TR.sizeDelta = new Vector2(-TYPE_ICON_TR.sizeDelta.x, 0f);
            TOOLTIP_TR.anchoredPosition = new Vector2(TYPE_ICON_TR.sizeDelta.x / 2f, 1);
            Log.Main.Debug?.Log($" TYPE_ICON_TR:{TYPE_ICON_TR.sizeDelta} main_tr:{main_tr.sizeDelta} TOOLTIP_TR:{TOOLTIP_TR.sizeDelta},{TOOLTIP_TR.anchoredPosition}");
            var tooltip2 = TOOLTIP2.GetComponent<HBSTooltip>();
            if (tooltip2 != null) { GameObject.Destroy(tooltip2); }
            parent.gameObject.name = ListElementController_BASE_NotListView.INVENTORY_ELEMENT_PREFAB_NotListView + FullMechSalvageInfo.FULL_MECH_SUFFIX + "(Clone)";
            var result = TOOLTIP2.AddComponent<FullMechSalvageInfo>();
            if (UIManager.Instance.dataManager.Exists(BattleTechResourceType.SVGAsset, Control.Instance.Settings.FullUnitInfoIcon))
            {
                parent.icon.vectorGraphics = UIManager.Instance.dataManager.GetObjectOfType<SVGAsset>(Control.Instance.Settings.FullUnitInfoIcon, BattleTechResourceType.SVGAsset);
            }
            else
            {
                if (UIManager.Instance.dataManager.ResourceLocator.EntryByID(Control.Instance.Settings.FullUnitInfoIcon, BattleTechResourceType.SVGAsset) != null)
                {
                    LoadRequest loadRequest = UIManager.Instance.dataManager.CreateLoadRequest();
                    loadRequest.AddLoadRequest<SVGAsset>(BattleTechResourceType.SVGAsset, Control.Instance.Settings.FullUnitInfoIcon, result.IconLoaded);
                    loadRequest.ProcessRequests();
                }
            }
            return result;
        }
        public override void OnPointerClick(PointerEventData eventData)
        {
            Log.Main.Debug?.Log($"FullMechSalvageInfo.OnPointerClick");
            try
            {
                GenericPopupBuilder builder = GenericPopupBuilder.Create("UNIT INFO", "PLACEHOLDER");
                builder.SetAlwaysOnTop();
                builder.AddButton("Close", new Action(this.OnInfoClose), false);
                AAR_SalvageChosen choosen = this.owner.ItemWidget.gameObject.GetComponentInParent<AAR_SalvageChosen>();
                if(choosen == null) builder.AddButton("Disassemble", new Action(this.OnDisassemble), false);
                this.popup = builder.CancelOnEscape().Render();
                popup._contentText.gameObject.SetActive(false);
                this.popup.gameObject.name = "uixPrfPanl_SIM_salvageUnitInfo-Widget";
                this.info = UIManager.Instance.dataManager.PooledInstantiate("uixPrfPanl_SIM_mechBayUnitInfo-Widget", BattleTech.BattleTechResourceType.UIModulePrefabs).GetComponent<MechBayMechInfoWidget>();
                this.info.gameObject.transform.SetParent(popup._contentText.transform.parent);
                this.info.gameObject.transform.SetSiblingIndex(popup._contentText.transform.GetSiblingIndex() + 1);
                this.info.SetData(this.owner.salvageDef, this.owner.simState);
                Transform[] trs = this.info.gameObject.GetComponentsInChildren<Transform>(false);
                this.info_paintSelector = this.info.gameObject.GetComponentInChildren<MechPaintPatternSelectorWidget>().gameObject;
                this.info_paintSelector.SetActive(false);
                foreach(Transform tr in trs)
                {
                    if (tr.parent != this.info.rootInfoObj.transform) { continue; }
                    if (tr.name == "layout_actionRefit-Repair") { this.info_refitBtn = tr.gameObject; }else
                    if (tr.name == "layout_optionButtons") { this.info_optionBtn = tr.gameObject; }
                }
                this.info_refitBtn.SetActive(false);
                this.info_optionBtn.SetActive(false);
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }

        public void OnInfoClose()
        {
            if(this.info != null)
            {
                this.info_refitBtn?.SetActive(true);
                this.info_optionBtn?.SetActive(true);
                this.info_paintSelector?.SetActive(true);
                this.info_refitBtn = null;
                this.info_optionBtn = null;
                this.info_paintSelector = null;
                UIManager.Instance.dataManager.PoolGameObject("uixPrfPanl_SIM_mechBayUnitInfo-Widget", this.info.gameObject);
                this.info = null;
            }
            if (this.popup != null)
            {
                this.popup._contentText.gameObject.SetActive(true);
                this.popup.Pool();
                this.popup = null;
            }
        }

        public void OnDisassemble()
        {
            try
            {
                this.OnInfoClose();
                Log.Main.Debug?.Log($"FullMechSalvageInfo.OnDisassemble {this.owner.mechDef.Description.Id}");
                AAR_SalvageScreen i_salvageScreen = this.salvageScreen;
                AAR_SalvageSelection i_salvageSelection = this.salvageScreen.salvageSelection;
                List<ListElementController_BASE_NotListView> notselectedSalvage = new List<ListElementController_BASE_NotListView>();
                foreach (var pSalvage in i_salvageSelection.GetSalvageInventory())
                {
                    notselectedSalvage.Add(pSalvage.controller);
                }
                foreach(var salvage in notselectedSalvage)
                {
                    i_salvageScreen.RemoveFromInventoryList(salvage.ItemWidget);
                    i_salvageScreen.AllSalvageControllers.Remove(salvage);
                }
                AAR_ScreenFullMechHelper helper = this.salvageScreen.gameObject.GetComponent<AAR_ScreenFullMechHelper>();
                //helper?.WriteSalvage(i_salvageScreen.contract.finalPotentialSalvage);
                var salvageDef = i_salvageScreen.contract.finalPotentialSalvage.Find((x)=>x.mechDef == this.owner.mechDef);
                if(salvageDef != null)
                {
                    Log.Main.Debug?.Log($" -- {this.owner.mechDef.Description.Id} found in finalPotentialSalvage. Disassembling");
                    ContractHelper contract = new ContractHelper(i_salvageScreen.contract, false);
                    i_salvageScreen.contract.finalPotentialSalvage.Remove(salvageDef);
                    Contract_GenerateSalvage.AddMechToSalvage(salvageDef.mechDef, contract, i_salvageScreen.simState, i_salvageScreen.simState.Constants, true, true);
                }
                foreach (var pSalvage in notselectedSalvage)
                {
                    pSalvage.Pool();
                }
                List<InventoryItemElement_NotListView> PriorityInventory = new List<InventoryItemElement_NotListView>(i_salvageScreen.salvageChosen.PriorityInventory);
                //bool has_full_mech_in_nonselected = false;
                i_salvageScreen.simState.SanitizeLootMagnetStacking(i_salvageScreen.contract.finalPotentialSalvage);
                Log.Main.Debug?.Log($" -- salvage after disassemble {i_salvageScreen.contract.finalPotentialSalvage.Count}");
                foreach (var salvage in i_salvageScreen.contract.finalPotentialSalvage)
                {
                    Log.Main.Debug?.Log($"   -- {salvage.Description.Id}:{salvage.Type}:{salvage.ComponentType}:{salvage.RewardID} count:{salvage.Count}");
                    //if (salvage.Type != SalvageDef.SalvageType.MECH) { continue; }
                    //if (salvage.ComponentType != ComponentType.MechFull) { continue; }
                    //InventoryItemElement_NotListView item = PriorityInventory.Find((x) => x.controller.salvageDef.Type == salvage.Type && x.controller.salvageDef.ComponentType == salvage.ComponentType && x.controller.salvageDef.mechDef == salvage.mechDef && x.controller.salvageDef.Description.Id == salvage.Description.Id);
                    //if (item != null) {
                    //    Log.Main.Debug?.Log($"     -- found in priority items");
                    //    continue; 
                    //}
                    //has_full_mech_in_nonselected = true;
                }

                List<SalvageDef> chosenSalvage = new List<SalvageDef>();
                foreach (var salvage in PriorityInventory)
                {
                    chosenSalvage.Add(salvage.controller.salvageDef);
                    i_salvageScreen.salvageChosen.OnRemoveItem(salvage, true);
                    i_salvageScreen.AllSalvageControllers.Remove(salvage.controller);
                    var controller = salvage.controller;
                    controller.Pool();
                }
                Thread.CurrentThread.SetFlag("LootMagnet_supress_dialog");
                try
                {
                    i_salvageScreen.CalculateAndAddAvailableSalvage();
                }catch(Exception e)
                {
                    UIManager.logger.LogException(e);
                }
                if (Thread.CurrentThread.isFlagSet("LootMagnet_supress_dialog")) { Thread.CurrentThread.ClearFlag("LootMagnet_supress_dialog"); }
                foreach (var salvage in chosenSalvage)
                {
                    InventoryItemElement_NotListView item = i_salvageSelection.GetSalvageInventory().Find((x) => x.controller.salvageDef.Type == salvage.Type && x.controller.salvageDef.ComponentType == salvage.ComponentType && x.controller.salvageDef.mechDef == salvage.mechDef && x.controller.salvageDef.Description.Id == salvage.Description.Id);
                    if (item != null) {
                        i_salvageScreen.salvageChosen.OnAddItem(item, true);
                    }
                }
                helper?.CheckDisassemble();
                AAR_SalvageChosenCustom choosenCustom = helper.parent.salvageChosen.gameObject.GetComponent<AAR_SalvageChosenCustom>();
                if (choosenCustom != null)
                {
                    choosenCustom.Refresh();
                }
                else{
                    Log.Main.Warning?.Log($"can't find AAR_SalvageChosenCustom component on {helper.parent.salvageChosen.gameObject.name}");
                }
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            //Log.Main.Debug?.Log($"FullMechSalvageInfo.OnPointerEnter");
            this.owner?.ItemWidget?.tooltip?.OnPointerEnter(eventData);
            if(this.owner != null)
            {
                this.owner.ItemWidget.icon.gameObject.SetActive(true);
                this.owner.ItemWidget.iconMech.gameObject.SetActive(false);
            }
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            //Log.Main.Debug?.Log($"FullMechSalvageInfo.OnPointerExit");
            this.owner?.ItemWidget?.tooltip?.OnPointerExit(eventData);
            if (this.owner != null)
            {
                this.owner.ItemWidget.icon.gameObject.SetActive(false);
                this.owner.ItemWidget.iconMech.gameObject.SetActive(true);
            }
        }
    }
    public class AAR_ScreenFullMechHelper: MonoBehaviour
    {
        public AAR_SalvageScreen parent;
        public GameObject disassembleAllBtnContainer;
        public HBSDOTweenButton disassembleAllBtn;
        //public List<SalvageDef> finalSalvage = new List<SalvageDef>();
        public void Init(AAR_SalvageScreen parent)
        {
            this.parent = parent;
            //this.ReadSalvage(parent.contract.finalPotentialSalvage);
        }
        //public void ReadSalvage(List<SalvageDef> salvage)
        //{
        //    finalSalvage.Clear();
        //    finalSalvage.AddRange(this.parent.contract.finalPotentialSalvage);
        //}
        //public void WriteSalvage(List<SalvageDef> salvage)
        //{
        //    salvage.Clear();
        //    salvage.AddRange(this.finalSalvage);
        //}
        public void OnClicked()
        {
            try
            {
                List<InventoryItemElement_NotListView> PriorityInventory = new List<InventoryItemElement_NotListView>(parent.salvageChosen.PriorityInventory);
                List<SalvageDef> chosenSalvage = new List<SalvageDef>();
                foreach (var salvage in PriorityInventory)
                {
                    chosenSalvage.Add(salvage.controller.salvageDef);
                    parent.salvageChosen.OnRemoveItem(salvage, true);
                    parent.AllSalvageControllers.Remove(salvage.controller);
                    var controller = salvage.controller;
                    controller.Pool();
                }
                List<ListElementController_BASE_NotListView> notselectedSalvage = new List<ListElementController_BASE_NotListView>();
                foreach (var pSalvage in parent.salvageSelection.GetSalvageInventory())
                {
                    notselectedSalvage.Add(pSalvage.controller);
                }
                foreach (var salvage in notselectedSalvage)
                {
                    parent.RemoveFromInventoryList(salvage.ItemWidget);
                    parent.AllSalvageControllers.Remove(salvage);
                    salvage.Pool();
                }
                //this.WriteSalvage(parent.contract.finalPotentialSalvage);
                List<SalvageDef> toDisassemble = new List<SalvageDef>();
                for (int t = 0; t < parent.contract.finalPotentialSalvage.Count;)
                {
                    SalvageDef salvageDef = parent.contract.finalPotentialSalvage[t];
                    if (salvageDef.Type != SalvageDef.SalvageType.MECH) { ++t; continue; }
                    if (salvageDef.ComponentType != ComponentType.MechFull) { ++t; continue; }
                    SalvageDef chosen = chosenSalvage.Find((x) => x.Type == SalvageDef.SalvageType.MECH && x.ComponentType == ComponentType.MechFull && x.mechDef == salvageDef.mechDef);
                    if (chosen != null) { ++t; continue; }
                    toDisassemble.Add(salvageDef);
                    parent.contract.finalPotentialSalvage.RemoveAt(t);
                }
                ContractHelper contract = new ContractHelper(parent.contract, false);
                foreach (var salvageDef in toDisassemble)
                {
                    Contract_GenerateSalvage.AddMechToSalvage(salvageDef.mechDef, contract, parent.simState, parent.simState.Constants, true, true);
                }
                Thread.CurrentThread.SetFlag("LootMagnet_supress_dialog");
                try
                {
                    parent.simState.SanitizeLootMagnetStacking(parent.contract.finalPotentialSalvage);
                    parent.CalculateAndAddAvailableSalvage();
                }
                catch (Exception e)
                {
                    UIManager.logger.LogException(e);
                }
                if (Thread.CurrentThread.isFlagSet("LootMagnet_supress_dialog")) { Thread.CurrentThread.ClearFlag("LootMagnet_supress_dialog"); }
                foreach (var salvage in chosenSalvage)
                {
                    InventoryItemElement_NotListView item = parent.salvageSelection.GetSalvageInventory().Find((x) => x.controller.salvageDef.Type == salvage.Type && x.controller.salvageDef.ComponentType == salvage.ComponentType && x.controller.salvageDef.mechDef == salvage.mechDef && x.controller.salvageDef.Description.Id == salvage.Description.Id);
                    if (item != null)
                    {
                        parent.salvageChosen.OnAddItem(item, true);
                    }
                }
                this.CheckDisassemble();
            }catch(Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }
        public bool CheckDisassemble()
        {
            bool has_full_mechs = false;
            Log.Main.Debug?.Log("CheckDisassemble");
            foreach(var salvage in parent.salvageSelection.GetSalvageInventory())
            {
                if (salvage.controller.salvageDef.Type != SalvageDef.SalvageType.MECH) { continue; }
                if (salvage.controller.salvageDef.ComponentType != ComponentType.MechFull) { continue; }
                has_full_mechs = true;
                Log.Main.Debug?.Log($" -- found full mech {salvage.controller.salvageDef.Description.Id}:{salvage.controller.salvageDef.Type}:{salvage.controller.salvageDef.ComponentType}");
                break;
            }
            this.disassembleAllBtn.SetState(has_full_mechs?ButtonState.Enabled:ButtonState.Disabled);
            return has_full_mechs;
        }
        public static AAR_ScreenFullMechHelper Instantine(AAR_SalvageScreen parent)
        {
            AAR_ScreenFullMechHelper result = parent.gameObject.AddComponent<AAR_ScreenFullMechHelper>();
            result.parent = parent;
            RectTransform posTr = null;
            foreach(var tr in parent.gameObject.GetComponentsInChildren<RectTransform>(true))
            {
                if (tr.name != "randomRewards_details-HideAfterConfirm") { continue; }
                posTr = tr;
            }
            if (posTr != null) {
                result.disassembleAllBtnContainer = GameObject.Instantiate(parent.CompleteButton.gameObject.transform.parent.gameObject);
                result.disassembleAllBtnContainer.transform.SetParent(posTr.parent);
                RectTransform btnTr = result.disassembleAllBtnContainer.GetComponent<RectTransform>();
                btnTr.sizeDelta = posTr.sizeDelta;
                btnTr.localPosition = posTr.localPosition + Vector3.down * posTr.sizeDelta.y;
                btnTr.pivot = new Vector2(0.5f, 0.5f);
                result.disassembleAllBtn = result.disassembleAllBtnContainer.GetComponentInChildren<HBSDOTweenButton>();
                RectTransform btnTR2 = result.disassembleAllBtn.gameObject.GetComponent<RectTransform>();
                btnTR2.anchoredPosition = new Vector2(btnTr.sizeDelta.x / 2f, btnTr.sizeDelta.y / 2f);
                btnTR2.pivot = new Vector2(0.5f,2f);
                result.disassembleAllBtn.OnClicked = new UnityEngine.Events.UnityEvent();
                result.disassembleAllBtn.OnClicked.AddListener(new UnityAction(result.OnClicked));
                LocalizableText caption = result.disassembleAllBtnContainer.GetComponentInChildren<LocalizableText>();
                caption.SetText("Disassemble all");
            }
            return result;
        }
    }
}