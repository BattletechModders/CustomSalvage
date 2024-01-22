using CustomComponents;

namespace CustomSalvage;

[CustomComponent("LootableMech")]

public class LootableMech : SimpleCustomChassis
{
    public string ReplaceID { get; set; }
}

[CustomComponent("LootableUniqueMech")]

public class LootableUniqueMech : SimpleCustomChassis
{
    public string ReplaceID { get; set; }
}