using System.Collections.Generic;
using System.Linq;

namespace CustomSalvage.MechBroke
{
    public class Tags 
    {
        private static Tags _instance;

        public static Tags Instance {
            get
            {
               if(_instance == null)
                    _instance = new Tags();

               return _instance;
            }
        }

        public Dictionary<string, CSTag> CSTags = new Dictionary<string, CSTag>();

        public List<CSTag> AllCSTags = new List<CSTag>();

        public void LoadTags(IEnumerable<CSTag> tags)
        {
            if(tags == null)
                return;
            
            foreach (var tag in tags)
            {
                CSTags[tag.ID] = tag;
                if(Control.Instance.Settings.DEBUG_ShowLoadingTags)
                    Log.Main.Info?.Log(tag.ToString());
            }

            AllCSTags = CSTags.Values.ToList();

            
        }
    }
}