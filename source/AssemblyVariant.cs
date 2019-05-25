#if USE_CC
using System;

using CustomComponents;

namespace CustomSalvage
{
    [CustomComponent("AssemblyVariant")]
    public class AssemblyVariant : SimpleCustomChassis
    {
        public string PrefabID = "";
        public bool Exclude = false;
        public bool Include = false;

        //public bool Special = false;
        //public bool CanUseSpecial = false;
        //public bool CanUseOnNormal = false;

        public bool ReplacePriceMult = false;
        public float PriceMult = 1f;
        public float PartsMin = -1;
    }
}

#endif