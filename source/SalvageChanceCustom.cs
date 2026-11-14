using CustomComponents;

namespace CustomSalvage;

[CustomComponent("SalvageChance")]
public class SalvageChanceCustom : SimpleCustomComponent
{
    public float Chance = 1f;
    public bool DropLootableDefault = false;
}
