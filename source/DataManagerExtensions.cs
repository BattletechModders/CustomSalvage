using System;
using BattleTech;
using BattleTech.Data;

namespace CustomSalvage;

internal static class DataManagerExtensions
{
    internal static MechComponentDef GetMechComponentDef(this DataManager dataManager, ComponentType componentType, string id)
    {
        var resourceType = ComponentTypeToBattleTechResourceType(componentType);
        return (MechComponentDef)dataManager.Get(resourceType, id);
    }

    private static BattleTechResourceType ComponentTypeToBattleTechResourceType(ComponentType componentType)
    {
        switch (componentType)
        {
            case ComponentType.AmmunitionBox:
                return BattleTechResourceType.AmmunitionBoxDef;
            case ComponentType.HeatSink:
                return BattleTechResourceType.HeatSinkDef;
            case ComponentType.JumpJet:
                return BattleTechResourceType.JumpJetDef;
            case ComponentType.Upgrade:
                return BattleTechResourceType.UpgradeDef;
            case ComponentType.Weapon:
                return BattleTechResourceType.WeaponDef;
            case ComponentType.NotSet:
            case ComponentType.Special:
            case ComponentType.MechPart:
            default:
                throw new ArgumentOutOfRangeException(nameof(componentType), componentType, null);
        }
    }
}