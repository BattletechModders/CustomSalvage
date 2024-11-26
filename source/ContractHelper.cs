using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using CustomComponents;

namespace CustomSalvage;

public static class FullUnitSalvageHelper
{
    public class SlotsCountDelegates
    {
        Func<MechDef, int> chassisSlotsCountDelegate { get; set; } = null;
        Func<List<MechComponentDef>, int> inventorySlotsCountDelegate { get; set; } = null;
        public SlotsCountDelegates(Func<MechDef, int> chassis, Func<List<MechComponentDef>, int> inventory)
        {
            chassisSlotsCountDelegate = chassis;
            inventorySlotsCountDelegate = inventory;
        }
        public int count(MechDef mech, List<MechComponentDef> inventory)
        {
            return (chassisSlotsCountDelegate == null ? 0 : chassisSlotsCountDelegate.Invoke(mech)) + (inventorySlotsCountDelegate == null ? 0 : inventorySlotsCountDelegate.Invoke(inventory));
        }
    }
    internal static Dictionary<string, SlotsCountDelegates> salvageSlotsDelegates = new Dictionary<string, SlotsCountDelegates>();
    public static void RegisterDelegates(string name, Func<MechDef, int> chassis, Func<List<MechComponentDef>, int> inventory)
    {
        salvageSlotsDelegates[name] = new SlotsCountDelegates(chassis, inventory);
    }
    public static int count(MechDef mech, List<MechComponentDef> inventory, int internalSlotsCount)
    {
        int result = internalSlotsCount;
        foreach(var dl in salvageSlotsDelegates)
        {
            int r = dl.Value.count(mech, inventory);
            if (r < result) { result = r; }
        }
        return result;
    }
}

public class ContractHelper
{
    public Contract Contract { get; }
    public List<SalvageDef> FinalPotentialSalvage { get; }

    public ContractHelper(Contract contract, bool reset)
    {
        Contract = contract;
        FinalPotentialSalvage = contract.finalPotentialSalvage;
        if (reset)
        {
            Contract.LostMechs = new();
            Contract.SalvageResults = new();
            Contract.SalvagedChassis = new();
        }
    }

    private MechComponentDef CheckDefaults(MechComponentDef def)
    {
        if (!def.CCFlags().NoSalvage)
        {
            return def;
        }

        MechComponentDef lootableDef = null;
        
        if (def.Is<LootableRandom>(out var lootableR))
        {
            string itemID = lootableR.GetChoice();
            if(itemID != null) lootableDef = UnityGameInstance.BattleTechGame.DataManager.GetMechComponentDef(def.ComponentType, itemID);
        }

        if (lootableDef == null && def.Is<LootableDefault>(out var lootable)) {
            lootableDef = UnityGameInstance.BattleTechGame.DataManager.GetMechComponentDef(def.ComponentType, lootable.ItemID);
        }

        // if (lootableDef == null) return null;
        //if (lootableDef.CCFlags().NoSalvage) return null;
        
        return lootableDef;
    }

    public static bool isSalvagable(MechComponentDef def)
    {
        if (def.CCFlags().NoSalvage == false) { return true; }
        if (def.Is<LootableDefault>(out var lootable) == false) { return false; }
        var lootableDef = UnityGameInstance.BattleTechGame.DataManager.GetMechComponentDef(def.ComponentType, lootable.ItemID);
        if (lootableDef == null) { return false; }
        if (lootableDef.CCFlags().NoSalvage) { return false; }
        return true;
    }

    public void AddComponentToFinalSalvage(MechComponentDef def)
    {
        if (def != null)
        {
            if(!Control.Instance.Settings.AllowDropBlackListed && def.ComponentTags.Contains("BLACKLISTED"))
            {
                Log.Main.Debug?.Log($"--- {def.Description.Id} is BLACKLISTED. skipped");
                return;
            }

            def = CheckDefaults(def);
            if (def == null)
            {
                return;
            }

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

            Contract.SalvageResults.Add(salvage);
        }
    }
    public void AddComponentToPotentialSalvage(MechComponentDef def, ComponentDamageLevel damageLevel,
        bool can_upgrade)
    {
        if (def != null)
        {
            if (!Control.Instance.Settings.AllowDropBlackListed && def.ComponentTags.Contains("BLACKLISTED"))
            {
                Log.Main.Debug?.Log($"--- {def.Description.Id} is BLACKLISTED. skipped");
                return;
            }

            var sc = Contract.BattleTechGame.Simulation.Constants;

            var replace = CheckDefaults(def);

            if (replace == null)
            {
                Log.Main.Debug?.Log($"--- {def.Description.Id} is not lootable. skipped");
                return;
            }

            if (replace != def)
            {
                Log.Main.Debug?.Log($"--- {def.Description.Id} replaced with {replace.Description.Id}");
                def = replace;
            }

            if (can_upgrade & Control.Instance.Settings.UpgradeSalvage)
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
                            Log.Main.Debug?.Log($"--- {def.Description.Id} upgraded to {weaponDef.Description.Id}");
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
                            Log.Main.Debug?.Log($"--- {def.Description.Id} upgraded to {mechComponentDef.Description.Id}");
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
            FinalPotentialSalvage.Add(salvageDef);
            Log.Main.Debug?.Log($"---- {def.Description.Id} added");
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
    private void add_mech(SimGameConstants sc, MechDef mech, List<SalvageDef> salvagelist)
    {
        var salvageDef = new SalvageDef();
        salvageDef.Type = SalvageDef.SalvageType.MECH;
        salvageDef.ComponentType = ComponentType.MechFull;
        salvageDef.Count = 1;
        salvageDef.Weight = sc.Salvage.DefaultChassisWeight;
        salvageDef.Description = new DescriptionDef(mech.Description);
        salvageDef.RewardID = Contract.GenerateRewardUID();
        salvageDef.mechDef = mech;
        salvageDef.MechComponentDef = new MechComponentDef(ComponentType.MechFull, MechComponentType.NotSet, new EffectData[] { }, mech.MechTags, mech.Description);
        salvagelist.Add(salvageDef);
    }

    public void AddMechPartsToPotentialSalvage(SimGameConstants constants, MechDef mech, int numparts)
    {


        if (!Control.Instance.Settings.AllowDropBlackListed &&  mech.MechTags.Contains("BLACKLISTED"))
        {
            Log.Main.Debug?.Log($"--- {mech.Description.Id} is BLACKLISTED. skipped");
            return;
        }

        add_parts(constants, mech, numparts, FinalPotentialSalvage);
    }
    public bool IsChassisExistsFinalPotentialSalvage(string chassisId)
    {
        foreach (var salvageDef in FinalPotentialSalvage)
        {
            if (salvageDef.Type != SalvageDef.SalvageType.MECH) { continue; }
            if (salvageDef.ComponentType != ComponentType.MechFull) { continue; }
            if (salvageDef.mechDef == null) { continue; }
            if (salvageDef.mechDef.chassisID != chassisId) { continue; }
            return true;
        }
        return false;
    }
    public void AddMechToPotentialSalvage(SimGameConstants constants, MechDef mech)
    {

        if (!Control.Instance.Settings.AllowDropBlackListed && mech.MechTags.Contains("BLACKLISTED"))
        {
            Log.Main.Debug?.Log($"--- {mech.Description.Id} is BLACKLISTED. skipped");
            return;
        }

        add_mech(constants, mech, FinalPotentialSalvage);
    }

    public void AddMechPartsToFinalSalvage(SimGameConstants constants, MechDef mech, int numparts)
    {
        if (!Control.Instance.Settings.AllowDropBlackListed &&  mech.MechTags.Contains("BLACKLISTED"))
        {
            Log.Main.Debug?.Log($"--- {mech.Description.Id} is BLACKLISTED. skipped");
            return;
        }
        add_parts(constants, mech, numparts, Contract.SalvageResults);
    }


    internal void FilterPotentialSalvage(List<SalvageDef> salvageDefs)
    {
        Contract.FilterPotentialSalvage(salvageDefs);
    }
}