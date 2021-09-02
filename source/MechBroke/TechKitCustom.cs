using System.Collections.Generic;
using BattleTech.UI;
using CustomComponents;

namespace CustomSalvage.MechBroke
{
    public class TechKitCustom : SimpleCustomComponent, IMechLabFilter, IAfterLoad
    {
        public Token Info;
        public bool CheckFilter(MechLabPanel panel)
        {
            return false;
        }

        public void OnLoaded(Dictionary<string, object> values)
        {
            DiceBroke.AddKit(this);
        }
    }
}