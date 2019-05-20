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
    }
}