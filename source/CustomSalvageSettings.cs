using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.UI;
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
        AllwaysRecover,
        NeverRecover
    }

    public enum PartCalculationType
    {
        Vanila,
        VanilaAdjusted,
        Custom,
        PartDestroyed
    }

    public enum LostMechActionType
    {
        ReturnItemsToPlayer,
        ReturnItemsAndPartsToPlayer,

        MoveItemsToSalvage,
        MoveItemsAndPartsToSalvage,
        FullMechSalvage
    }

    public class CustomSalvageSettings
    {
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

        public LogLevel LogLevel = LogLevel.Debug;


        public RecoveryCalculationType RecoveryType = RecoveryCalculationType.PartDestroyed;
        public PartCalculationType PartCountType = PartCalculationType.PartDestroyed ;
        public LostMechActionType LostMechAction = LostMechActionType.ReturnItemsToPlayer;

        public float RecoveryMod = 1;
        public float LimbRecoveryPenalty = 0.05f;
        public float TorsoRecoveryPenalty = 0.1f;
        public float HeadRecoveryPenaly = 0;
        public float EjectRecoveryBonus = 0.25f;


        public int CenterTorsoDestroyedParts = 1;
        public float SalvageArmWeight = 0.75f;
        public float SalvageLegWeight = 0.75f;
        public float SalvageTorsoWeight = 1f;
        public float SalvageHeadWeight = 0.5f;


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
        public string[] ExclideVariants = { };
        public special[] SpecialTags = null;

        public int MaxVariantsInDescription = 5;

        public bool UnEquipedMech = false;
        public bool BrokenMech = true;


        public bool HeadRepaired = false;
        public bool LeftArmRepaired = false;
        public bool RightArmRepaired = false;
        public bool CentralTorsoRepaired = false;
        public bool LeftTorsoRepaired = false;
        public bool RightTorsoRepaired = false;
        public bool LeftLegRepaired = false;
        public bool RightLegRepaired = false;

        public bool RepairMechLimbs = true;
        public float RepairMechLimbsChance = 0.75f;
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
        }
    }
}
