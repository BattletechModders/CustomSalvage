using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using Harmony;
using HBS.Logging;
using Newtonsoft.Json;
using UnityEngine;

namespace CustomSalvage
{
    public enum RecoveryCalculationType
    {
        Vanila,
        Custom,
        PartDestroyed,
        AlwaysRecover,
        NeverRecover
    }

    public enum PartCalculationType
    {
        Vanila,
        VanilaAdjusted,
        Custom,
        PartDestroyed,
        PartDestroyedIgnoreCT
    }

    public enum LostMechActionType
    {
        ReturnItemsToPlayer,
        ReturnItemsAndPartsToPlayer,

        MoveItemsToSalvage,
        MoveItemsAndPartsToSalvage,
        FullMechSalvage
    }

    public enum BrokeType
    {
        None,
        Random,
        Normalized
    }

    public class Strings
    {
        public string FrankenPenaltyCaption = "FrankenMech";
        public string TPBonusCaption = "Tech Points";
        public string SparePartsCaption = "Spare Parts";
        public string TotalBonusCatption = "Total";

        public string ScrapDialogTitle = "Scrap {0}?";
        public string ScrapPartsDialogTitle = "Scrap {0} parts?";

        public string ScrapMultyPartsDialogTitle = "Scrap";
        public string ScrapMultyPartsDialogText = "{0} parts?";

        public string ScrapDialogTextWithParts =
            "Do you want scrap this chassis and sale spare parts for <color=#F79B26FF>{0}</color> or scrap and keep parts ({1}-{2} parts)";

        public string ScrapDialogText =
            "Are you sure you want to scrap this Chassis? It will be removed permanently from your inventory.\n\nSCRAP VALUE: <color=#F79B26FF>{0}</color>";

        public string ScrapPartsDialogText =
            "Are you sure you want to scrap this part? It will be removed permanently from your inventory.\n\nSCRAP VALUE: <color=#F79B26FF>{0}</color>";

        public string ScrapResultTitle = "Scraped {0}";
        public string ScrapResultText = "We manage to get <color=#20ff20>{0}</color> parts from {1} chassis";


        public string ButtonOk = "OK";
        public string ButtonCancel = "Cancel";
        public string ButtonKeepParts = "Keep Parts";
        public string ButtonSell = "Sell";
        public string ButtonScrap = "Scrap";
        public string ButtonNextPage = "Next Page >>";
        public string ButtonConfirm = "Confirm";
        public string ButtonApplySpare = "Apply spare parts";
        public string ButtonClearSpare = "Clear spare parts";
        public string ButtonAddSpare = "Add spare parts";
        public string ButtonNoSpare = "<i>no spare parts left</i>";

        public string ButtonNoMoney = "<color=#ff2020><i>Not enough C-Bills</i></color>";
        public string ButtonAddTechKit = "Clear TechKit";
        public string ButtonClearTechKit = "Clear TechKit";
        public string ButtonNoTechKit = "<i>no compatible TechKits</i>";
        public string ButtonAllPartsUsed = "<i><color=#a0a0a0>{0}</color>: <color=#ff4040>All parts used</color></i>";

        public string ButtonAddPart = "Add <color=#20ff20>{0}</color> {1} part left";
        public string ButtonAddParts = "Add <color=#20ff20>{0}</color> {1} parts left";
        public string ButtonAddPartMoney = "Add <color=#20ff20>{0}</color> for <color=#ffff00>{1}</color> {2} part left";
        public string ButtonAddPartsMoney = "Add <color=#20ff20>{0}</color> for <color=#ffff00>{1}</color> {2} parts left";

    }

    public class Settings
    {


        public class broke_info
        {
            public string tag;
            public int BaseTp = -1;
            public float Limb = -1;
            public float Component = -1;

        }

        public class tagicon_def
        {
            public string Tag { get; set; }
            public string Value { get; set; }

            [JsonIgnore]
            public Sprite Sprite
            {
                get;
                private set;
            }

            public void Complete(SimGameState sim)
            {
                if (Sprite == null)
                {
                    Control.Instance.LogDebug($"Request icon [{Value}] for [{Tag}]");
                    sim.RequestItem<Sprite>(
                        Tag,
                        (sprite) =>
                        {
                            Control.Instance.LogDebug($"sprite [{Value}] loaded");
                            Sprite = sprite;
                        },
                        BattleTechResourceType.Sprite
                    );
                }
            }
        }

        public class colordef
        {
            public string Tag { get; set; }
            public string Color { get; set; }
            [JsonIgnore]
            public Color color { get; set; }

            public void Complete()
            {
                color = ColorUtility.TryParseHtmlString(Color, out var c) ? c : UnityEngine.Color.magenta;
            }


        }

        public class special
        {
            public string Tag;
            public float Mod = 1f;
        }

        public Strings Strings;

        public LogLevel LogLevel = LogLevel.Debug;
        public bool DEBUG_ShowLoadingTags = false;
        public bool DEBUG_LOTOFPARTS = false;
        public bool DEBUG_ShowConfig = true;

        public RecoveryCalculationType RecoveryType = RecoveryCalculationType.PartDestroyed;
        public PartCalculationType PartCountType = PartCalculationType.PartDestroyedIgnoreCT;
        public LostMechActionType LostMechAction = LostMechActionType.ReturnItemsToPlayer;
        public BrokeType MechBrokeType = BrokeType.Random;

        public bool AllowDropBlackListed = false;
        public string NoSalvageMechTag = "NOSALVAGE";
        public string NoSalvageVehicleTag = "NOSALVAGE";

        public float RecoveryMod = 1;
        public float LimbRecoveryPenalty = 0.05f;
        public float TorsoRecoveryPenalty = 0.1f;
        public float HeadRecoveryPenaly = 0;
        public float EjectRecoveryBonus = 0.25f;


        public int CenterTorsoDestroyedParts = 1;
        public float SalvageArmWeight = 0.75f;
        public float SalvageLegWeight = 0.75f;
        public float SalvageTorsoWeight = 1f;
        public float SalvageHeadWeight = 0.25f;
        public float SalvageCTWeight = 1.5f;


        public float VACTDestroyedMod = 0.35f;
        public float VABLDestroyedMod = 0.68f;


        public bool SalvageTurrets = true;
        public float SalvageTurretsComponentChance = 0.33f;
        public bool UpgradeSalvage = false;

        public colordef[] BGColors =
            {
                new colordef { Tag = "unit_assault", Color = "#b2babb10" },
                new colordef { Tag = "unit_heavy", Color = "#f0b27a10" },
                new colordef { Tag = "unit_medium", Color = "#82e0aa10" },
                new colordef { Tag = "unit_light", Color = "#85c1e910" }
            };

        public string StoredMechColor = "#7FFFD4";
        public string ReadyColor = "#32CD32";
        public string VariantsColor = "yellow";
        public string NotReadyColor = "white";
        public string ExcludedColor = "magenta";


        [JsonIgnore] public Color color_ready;
        [JsonIgnore] public Color color_variant;
        [JsonIgnore] public Color color_stored;
        [JsonIgnore] public Color color_notready;
        [JsonIgnore] public Color color_exclude;

        public bool AssemblyVariants = true;
        public float MinPartsToAssembly = 0.33f;
        public float MinPartsToAssemblySpecial = 0.5f;
        public string[] ExcludeTags = { "BLACKLISTED" };
        public string[] ExcludeVariants = { };
        public special[] SpecialTags = null;

        public bool AllowScrapToParts = true;
        public float MinScrapParts = 0.51f;
        public float MaxScrapParts = 0.91f;

        public int MaxVariantsInDescription = 5;

        public bool UseGameSettingsUnequiped = false;
        public bool UnEquipedMech = false;
        public bool ShowDEBUGChances = true;

        public bool RepairChanceByTP = false;
        public int BaseTP = 10;
        public float LimbChancePerTp = 0.01f;
        public float ComponentChancePerTp = 0.01f;
        public float RepairTPMaxEffect = 0.33f;
        public float LimbMinChance = 0.1f;
        public float LimbMaxChance = 0.95f;
        public float ComponentMinChance = 0.1f;
        public float ComponentMaxChance = 0.95f;

        public broke_info[] BrokeByTag = null;

        public bool HeadRepaired = false;
        public bool LeftArmRepaired = false;
        public bool RightArmRepaired = false;
        public bool CentralTorsoRepaired = false;
        public bool LeftTorsoRepaired = false;
        public bool RightTorsoRepaired = false;
        public bool LeftLegRepaired = false;
        public bool RightLegRepaired = false;

        public bool RepairMechLimbs = true;
        public float RepairMechLimbsChance = 0.33f;
        public bool RandomStructureOnRepairedLimbs = true;
        public float MinStructure = 0.25f;

        public bool RepairMechComponents = true;
        public float RepairComponentsFunctionalThreshold = 0.25f;
        public float RepairComponentsNonFunctionalThreshold = 0.5f;

        public float AdaptPartBaseCost = 0.015f;
        public float MaxAdaptMod = 5f;
        public float AdaptModWeight = 2f;
        public bool ApplyPartPriceMod = false;

        public string OmniTechTag = null;
        public float OmniSpecialtoSpecialMod = 0.1f;
        public float OmniSpecialtoNormalMod = 0.25f;
        public float OmniNormalMod = 0f;
        public bool ShowLogPrefix { get; set; } = true;
        public tagicon_def[] IconTags { get; set; } = null;

        public int MaxSpareParts = 5;
        public bool ShowDetailBonuses = true;
        public bool ShowBrokeChances = true;
        public bool ShowBrokeChancesFirst = true;

        public float[,] PartCountPenalty = new float[,]
        {
            { 3, 2 },
            { 4, 1.5f },
            { 5, 1.2f },
            { 6, 1f },
            { 7, 0.75f },
            { 8, 0.5f },
        };

        public int DiceBaseTP = -8;
        public int DiceTPStep = 4;
        public int MinRoll = -5;
        public int MaxRoll = 20;
        public int CTRoll = 6;
        private int[,] PartResults = new int[,]
        {
            {-5, 8},
            {-4, 7},
            {-3, 7},
            {-2, 7},
            {-1, 7},
            {0, 6},
            {1, 6},
            {2, 6},
            {3, 5},
            {4, 5},
            {5, 5},
            {6, 4},
            {7, 4},
            {8, 4},
            {9, 3},
            {10, 3},
            {11, 3},
            {12, 2},
            {13, 2},
            {14, 2},
            {15, 1},
            {16, 1},
            {17, 1},
            {19, 1},
            {20, 0}
        };

        [JsonIgnore]
        internal Dictionary<int, int> partresults;
        [JsonIgnore]
        internal Dictionary<int, float> PartPenalty;

        public void Complete()
        {
            if (BGColors != null && BGColors.Length > 1)
                foreach (var colordef in BGColors)
                    colordef.Complete();

            color_ready = ColorUtility.TryParseHtmlString(ReadyColor, out var c) ? c : new Color(50, 205, 50);
            color_variant = ColorUtility.TryParseHtmlString(VariantsColor, out c) ? c : UnityEngine.Color.yellow;
            color_stored = ColorUtility.TryParseHtmlString(StoredMechColor, out c) ? c : UnityEngine.Color.white;
            color_notready = ColorUtility.TryParseHtmlString(NotReadyColor, out c) ? c : UnityEngine.Color.grey;
            color_exclude = ColorUtility.TryParseHtmlString(ExcludedColor, out c) ? c : new Color(127, 255, 212);

            PartPenalty = new Dictionary<int, float>()
            {
                { 3, 2 },
                { 4, 1.5f },
                { 5, 1.2f },
                { 6, 1f },
                { 7, 0.75f },
                { 8, 0.5f },
            };
            if (PartCountPenalty == null || PartCountPenalty.Length == 0)
                for (int i = 0; i < PartCountPenalty.GetLength(0); i++)
                    PartPenalty[(int)PartCountPenalty[i, 0]] = PartCountPenalty[i, 1];
            if (Strings == null) Strings = new Strings();

            partresults = new Dictionary<int, int>();
            partresults[MinRoll] = 8;
            partresults[MaxRoll] = 0;
            int t = MaxRoll - MinRoll;
            for (int i = MinRoll + 1; i < MaxRoll; i++)
            {
                int a = (int)(((float)i - MinRoll) / t * 6 + 1);
                partresults[i] = a;
            }

            if (PartResults != null)
                for (int i = 0; i < PartResults.GetLength(0); i++)
                    if (PartResults[i, 0] > MinRoll 
                        && PartResults[i, 0] < MaxRoll 
                        && PartResults[i, 1] >= 0 
                        && PartResults[i, 1] <= 8)

                        partresults[PartResults[i, 0]] = PartResults[i, 1];
        }
    }
}
