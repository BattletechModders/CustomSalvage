using System.Linq;
using CustomComponents;

namespace CustomSalvage; 

[CustomComponent("LootableRandom")]
public class LootableRandom : SimpleCustomComponent {
    public class option {
        public string ItemID { get; set; }
        public float Weight { get; set; }
    }
    
    public option[] Options { get; set; }

    public string GetChoice() {
        if (Options == null || Options.Length == 0)
        {
            Log.Main.Warning?.Log("invalid LootableRandom tag");
            return null;
        }
        float total = Options.Sum(o => o.Weight);
        if (total < 0)
        {
            Log.Main.Warning?.Log("invalid LootableRandom tag");
            return null;
        }
        float choice = UnityEngine.Random.Range(0.0f, total);
        Log.Main.Trace?.Log($"GetChoice: total {total}, choice {choice}");
        foreach (var o in Options)
        {
            Log.Main.Trace?.Log($"item {o.ItemID}, weight {o.Weight}, choice {choice}");
            if (choice <= o.Weight) return o.ItemID;
            choice -= o.Weight;
        }

        return null; // should never actually be reached
    }
    
}