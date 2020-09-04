#if USE_CC
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CustomComponents;

namespace CustomSalvage
{
    [CustomComponent("LootableMech")]

    public class LootableMech : SimpleCustomChassis
    {
        public string ReplaceID { get; set; }
    }
}
#endif