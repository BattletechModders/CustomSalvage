using System.Collections.Generic;
using System.IO;
using BattleTech;
using BattleTech.UI;
using CustomComponents;

namespace CustomSalvage.MechBroke
{
    public class TechKitCustom : SimpleCustomComponent, IMechLabFilter, IAfterLoad
    {
        public Condition[] Condition;
        public int Value = 0;
        public float CompRepairAddBonus = 0;
        public float CompRepairMulBonus = 1;
        public int CBill = 0;
        public float CBIllMul = 1;

        public bool CheckFilter(MechLabPanel panel)
        {
            return false;
        }

        public void OnLoaded(Dictionary<string, object> values)
        {
            DiceBroke.AddKit(this);
        }

        public override string ToString()
        {
            var result = Def.Description.UIName;

            if (Value != 0)
                result += "  Tech " + Value.ToString("-0;+#");
            if (CompRepairAddBonus != 0)
                result += "  Comp " + ((int) CompRepairAddBonus * 100).ToString("-0;+#") + "%";
            if(CompRepairMulBonus != 1)
                result += "  Comp x" + CompRepairMulBonus.ToString();

            if (CBill != 0)
                result += "  Cost " + SimGameState.GetCBillString(CBill);

            if (CBIllMul != 1)
                result += "  Cost x" + CBIllMul.ToString();

            return result;
        }
    }
}