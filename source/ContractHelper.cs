using System;
using System.Collections.Generic;
using System.Reflection;
using BattleTech;
using BattleTech.Data;
using CustomComponents;
using Harmony;

namespace CustomSalvage
{

    public class ContractHelper
    {
        private readonly Traverse main;

        private readonly Traverse<List<SalvageDef>> salvaged_chassis;
        private readonly Traverse<List<MechDef>> lost_mechs;
        private readonly Traverse<List<SalvageDef>> salvage_results;
        private readonly Traverse<int> salvage_count;
        private readonly Traverse<int> prio_salvage_count;

        private readonly MethodInfo filter_salvage;


        public List<SalvageDef> finalPotentialSalvage { get; private set; }

        public List<SalvageDef> SalvagedChassis
        {
            get => salvaged_chassis.Value;
            set => salvaged_chassis.Value = value;
        }

        public List<SalvageDef> SalvageResults
        {
            get => salvage_results.Value;
            set => salvage_results.Value = value;
        }

        public List<MechDef> LostMechs
        {
            get => lost_mechs.Value;
            set => lost_mechs.Value = value;
        }



        public int FinalSalvageCount { get => salvage_count.Value; set => salvage_count.Value = value; }
        public int FinalPrioritySalvageCount { get => prio_salvage_count.Value; set => prio_salvage_count.Value = value; }

        public Contract Contract { get; private set; }

        public ContractHelper(Contract contract, List<SalvageDef> finalPotentialSalvage)
        {

            main = new Traverse(contract);
            this.Contract = contract;

            this.finalPotentialSalvage = finalPotentialSalvage;
            salvage_results = main.Property<List<SalvageDef>>("SalvageResults");
            salvaged_chassis = main.Property<List<SalvageDef>>("SalvagedChassis");
            lost_mechs = main.Property<List<MechDef>>("LostMechs");
            salvage_count = main.Property<int>("FinalSalvageCount");
            prio_salvage_count = main.Property<int>("FinalPrioritySalvageCount");

            filter_salvage = contract.GetType().GetMethod("FilterPotentialSalvage", BindingFlags.NonPublic | BindingFlags.Instance);

            if (filter_salvage == null)
                Control.LogError("filter_salvage = null");

        }

        internal MechComponentDef CheckDefaults(MechComponentDef def)
        {
            if (!def.Is<Flags>(out var f) || !f.NotSalvagable) return def;

            if (!def.Is<LootableDefault>(out var lootable)) return null;

            MechComponentDef component = null;

            switch (def.ComponentType)
            {
                case ComponentType.AmmunitionBox:
                    if (UnityGameInstance.BattleTechGame.DataManager.AmmoBoxDefs.Exists(lootable.ItemID))
                        component = UnityGameInstance.BattleTechGame.DataManager.AmmoBoxDefs.Get(lootable.ItemID);
                    break;

                case ComponentType.Weapon:
                    if (UnityGameInstance.BattleTechGame.DataManager.WeaponDefs.Exists(lootable.ItemID))
                        component = UnityGameInstance.BattleTechGame.DataManager.WeaponDefs.Get(lootable.ItemID);
                    break;

                case ComponentType.Upgrade:
                    if (UnityGameInstance.BattleTechGame.DataManager.UpgradeDefs.Exists(lootable.ItemID))
                        component = UnityGameInstance.BattleTechGame.DataManager.UpgradeDefs.Get(lootable.ItemID);
                    break;

                case ComponentType.HeatSink:
                    if (UnityGameInstance.BattleTechGame.DataManager.HeatSinkDefs.Exists(lootable.ItemID))
                        component = UnityGameInstance.BattleTechGame.DataManager.HeatSinkDefs.Get(lootable.ItemID);
                    break;
                case ComponentType.JumpJet:
                    if (UnityGameInstance.BattleTechGame.DataManager.JumpJetDefs.Exists(lootable.ItemID))
                        component = UnityGameInstance.BattleTechGame.DataManager.JumpJetDefs.Get(lootable.ItemID);
                    break;
            }

            if (component == null || (component.Is<Flags>(out f) && f.NotSalvagable))
                return null;

            return component;

        }

        public void AddComponentToFinalSalvage(MechComponentDef def)
        {
            if (def != null)
            {
                if(def.ComponentTags.Contains("BLACKLISTED"))
                {
                    Control.LogDebug($"--- {def.Description.Id} is BLACKLISTED. skipped");
                    return;
                }

                def = CheckDefaults(def);
                if (def == null)
                    return;

                var salvage = new SalvageDef()
                {
                    MechComponentDef = def,
                    Description = new DescriptionDef(def.Description),
                    RewardID = Contract.GenerateRewardUID(),
                    Type = SalvageDef.SalvageType.COMPONENT,
                    ComponentType = def.ComponentType,
                    Damaged = false,
                    Count = 1
                };

                SalvageResults.Add(salvage);
            }
        }
        public void AddComponentToPotentialSalvage(MechComponentDef def, ComponentDamageLevel damageLevel,
             bool can_upgrade)
        {
            if (def != null)
            {
                if (def.ComponentTags.Contains("BLACKLISTED"))
                {
                    Control.LogDebug($"--- {def.Description.Id} is BLACKLISTED. skipped");
                    return;
                }

                var sc = Contract.BattleTechGame.Simulation.Constants;

                var replace = CheckDefaults(def);

                if (replace == null)
                {
                    Control.LogDebug($"--- {def.Description.Id} is not lootable. skipped");
                    return;
                }

                if (replace != def)
                {
                    Control.LogDebug($"--- {def.Description.Id} replaced with {replace.Description.Id}");
                    def = replace;
                }

                if (can_upgrade & Control.Settings.UpgradeSalvage)
                {
                    float chance = Contract.BattleTechGame.Simulation.NetworkRandom.Float(0, 1f);

                    if (def.ComponentType == ComponentType.Weapon)
                    {
                        float num2 = ((float)Contract.Override.finalDifficulty + sc.Salvage.VeryRareWeaponChance) / sc.Salvage.WeaponChanceDivisor;
                        float num3 = ((float)Contract.Override.finalDifficulty + sc.Salvage.RareWeaponChance) / sc.Salvage.WeaponChanceDivisor;

                        float[] array = null;
                        if (chance < num2)
                        {
                            array = sc.Salvage.VeryRareWeaponLevel;
                        }
                        else if (chance < num3)
                        {
                            array = sc.Salvage.RareWeaponLevel;
                        }
                        WeaponDef weaponDef = def as WeaponDef;
                        if (array != null)
                        {
                            List<WeaponDef_MDD> weaponsByTypeAndRarityAndOwnership = MetadataDatabase.Instance.GetWeaponsByTypeAndRarityAndOwnership(weaponDef.WeaponSubType, array);
                            if (weaponsByTypeAndRarityAndOwnership != null && weaponsByTypeAndRarityAndOwnership.Count > 0)
                            {
                                weaponsByTypeAndRarityAndOwnership.Shuffle<WeaponDef_MDD>();
                                WeaponDef_MDD weaponDef_MDD = weaponsByTypeAndRarityAndOwnership[0];
                                weaponDef = UnityGameInstance.BattleTechGame.DataManager.WeaponDefs.Get(weaponDef_MDD.WeaponDefID);
                                Control.LogDebug($"--- {def.Description.Id} upgraded to {weaponDef.Description.Id}");
                                def = weaponDef;
                            }
                        }


                    }
                    else
                    {
                        float num2 = ((float)Contract.Override.finalDifficulty + sc.Salvage.VeryRareUpgradeChance) / sc.Salvage.UpgradeChanceDivisor;
                        float num3 = ((float)Contract.Override.finalDifficulty + sc.Salvage.RareUpgradeChance) / sc.Salvage.UpgradeChanceDivisor;
                        float[] array = null;
                        var mechComponentDef = def;
                        if (chance < num2)
                        {
                            array = sc.Salvage.VeryRareUpgradeLevel;
                        }
                        else if (chance < num3)
                        {
                            array = sc.Salvage.RareUpgradeLevel;
                        }
                        if (array != null)
                        {
                            List<UpgradeDef_MDD> upgradesByRarityAndOwnership = MetadataDatabase.Instance.GetUpgradesByRarityAndOwnership(array);
                            if (upgradesByRarityAndOwnership != null && upgradesByRarityAndOwnership.Count > 0)
                            {
                                upgradesByRarityAndOwnership.Shuffle<UpgradeDef_MDD>();
                                UpgradeDef_MDD upgradeDef_MDD = upgradesByRarityAndOwnership[0];
                                mechComponentDef = UnityGameInstance.BattleTechGame.DataManager.UpgradeDefs.Get(upgradeDef_MDD.UpgradeDefID);
                                Control.LogDebug($"--- {def.Description.Id} upgraded to {mechComponentDef.Description.Id}");
                                def = mechComponentDef;
                            }
                        }

                    }
                }

                SalvageDef salvageDef = new SalvageDef();
                salvageDef.MechComponentDef = def;
                salvageDef.Description = new DescriptionDef(def.Description);
                salvageDef.RewardID = Contract.GenerateRewardUID();
                salvageDef.Type = SalvageDef.SalvageType.COMPONENT;
                salvageDef.ComponentType = def.ComponentType;
                salvageDef.Damaged = false;
                salvageDef.Weight = sc.Salvage.DefaultComponentWeight;
                salvageDef.Count = 1;
                this.finalPotentialSalvage.Add(salvageDef);
                Control.LogDebug($"---- {def.Description.Id} added");
            }
        }

        private void add_parts(SimGameConstants sc, MechDef mech, int numparts, List<SalvageDef> salvagelist)
        {
            for (int i = 0; i < numparts; i++)
            {
                var salvageDef = new SalvageDef();
                salvageDef.Type = SalvageDef.SalvageType.MECH_PART;
                salvageDef.ComponentType = ComponentType.MechPart;
                salvageDef.Count = 1;
                salvageDef.Weight = sc.Salvage.DefaultMechPartWeight;
                DescriptionDef description = mech.Description;
                DescriptionDef description2 = new DescriptionDef(description.Id,
                    $"{description.Name} {sc.Story.DefaultMechPartName}", description.Details, description.Icon, description.Cost, description.Rarity, description.Purchasable, description.Manufacturer, description.Model, description.UIName);
                salvageDef.Description = description2;
                salvageDef.RewardID = Contract.GenerateRewardUID();

                salvagelist.Add(salvageDef);
            }
        }

        public void AddMechPartsToPotentialSalvage(SimGameConstants constants, MechDef mech, int numparts)
        {
            if (mech.MechTags.Contains("BLACKLISTED"))
            {
                Control.LogDebug($"--- {mech.Description.Id} is BLACKLISTED. skipped");
                return;
            }

            add_parts(constants, mech, numparts, finalPotentialSalvage);
        }

        public void AddMechPartsToFinalSalvage(SimGameConstants constants, MechDef mech, int numparts)
        {
            if (mech.MechTags.Contains("BLACKLISTED"))
            {
                Control.LogDebug($"--- {mech.Description.Id} is BLACKLISTED. skipped");
                return;
            }
            add_parts(constants, mech, numparts, SalvageResults);
        }


        internal void FilterPotentialSalvage(List<SalvageDef> salvageDefs)
        {
            filter_salvage.Invoke(Contract, new Object[] { salvageDefs });
        }
    }
}