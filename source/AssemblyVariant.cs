using CustomComponents;

namespace CustomSalvage
{
    public interface IAssemblyVariant
    {
        string PrefabID { get; }
        bool Exclude { get; }
        bool Include { get; }

        bool ReplacePriceMult { get; }
        float PriceMult { get; }
        float PartsMin { get; }
    }

    [CustomComponent("AssemblyVariant")]
    public class AssemblyVariant : SimpleCustomChassis, IAssemblyVariant
    {
        public string PrefabID { get; set; } = "";
        public bool Exclude { get; set; } = false;
        public bool Include { get; set; } = false;

        public bool ReplacePriceMult { get; set; } = false;
        public float PriceMult { get; set; } = 1f;
        public float PartsMin { get; set; } = -1;
    }
}
