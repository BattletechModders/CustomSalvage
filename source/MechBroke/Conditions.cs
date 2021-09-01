using System.Collections.Generic;
using System.Linq;
using System.Text;
using BattleTech;
using JetBrains.Annotations;
using Harmony;

namespace CustomSalvage.MechBroke
{
    public delegate bool ConditionDelegate(Condition condition, MechDef mech, HashSet<string> mechtags, SimGameState sim);

    public class Condition
    {
        public string Type { get; set; }
        public string[] Strings { get; set; }
        public int[] Ints { get; set; }
        public float[] Floats { get; set; }

        public Condition[] Subs { get; set; }
    }

    public class Conditions
    {
        private static Conditions _instance;

        public static Conditions Instatnce
        {
            get
            {
                if (_instance == null)
                    _instance = new Conditions();

                return _instance;
            }
        }

        private Dictionary<string, ConditionDelegate> handlers;


        private Conditions()
        {
            handlers = new Dictionary<string, ConditionDelegate>();

            handlers["not"] = handler_not;
            handlers["and"] = handler_and;
            handlers["or"] = handler_or;
            handlers["companystat"] = handler_companystat;
            handlers["mechtag"] = handler_mechtag;

        }

        public void RegisterHandler(string keyword, ConditionDelegate handler)
        {
            handlers[keyword] = handler;
        }

        public bool CheckCondition(IEnumerable<Condition> conditions, MechDef mech, HashSet<string> mechtags,
            SimGameState sim)
        {
            if (conditions != null)
                foreach (var condition in conditions)
                {
                    if (condition != null
                        && handlers.TryGetValue(condition.Type, out var handler)
                        && !handler(condition, mech, mechtags, sim))
                        return false;
                }

            return true;
        }

        private bool handler_and(Condition condition, MechDef mech, HashSet<string> mechtags, SimGameState sim)
        {
            return CheckCondition(condition.Subs, mech, mechtags, sim);
        }

        private bool handler_not(Condition condition, MechDef mech, HashSet<string> mechtags, SimGameState sim)
        {
            return !CheckCondition(condition.Subs, mech, mechtags, sim);
        }

        private bool handler_or(Condition condition, MechDef mech, HashSet<string> mechtags, SimGameState sim)
        {
            if (condition.Subs != null)
                foreach (var c in condition.Subs)
                {
                    if (c == null
                        || !handlers.TryGetValue(condition.Type, out var handler)
                        || handler(condition, mech, mechtags, sim))
                        return true;
                }

            return false;
        }

        private bool handler_mechtag(Condition condition, MechDef mech, HashSet<string> mechtags, SimGameState sim)
        {
            if (mechtags == null || condition.Strings == null || condition.Strings.Length < 1)
                return false;

            return condition.Strings.All(tag => mechtags.Contains(tag));
        }

        private bool handler_companystat(Condition condition, MechDef mech, HashSet<string> mechtags, SimGameState sim)
        {
            if (mechtags == null || condition.Strings == null || condition.Strings.Length < 1)
                return false;

            return sim.CompanyStats.GetStatistic(condition.Strings[0]) == null;
        }

        public void PrintConditions(StringBuilder sb, Condition[] tokenConditions)
        {
            PrintConditions(sb, tokenConditions, "     ");
        }


        private void PrintConditions(StringBuilder sb, Condition[] tokenConditions, string prefix)
        {
            foreach (var c in tokenConditions)
            {
                if (c == null)
                    continue;
                sb.Append(prefix);
                sb.Append(c.Type);
                if (c.Strings != null && c.Strings.Length > 0)
                    sb.Append(" [" + c.Strings.Join(delimiter: ",") + "]");
                if (c.Ints != null && c.Ints.Length > 0)
                    sb.Append(" [" + c.Ints.Join(delimiter: ",") + "]");
                if (c.Floats != null && c.Floats.Length > 0)
                    sb.Append(" [" + c.Floats.Join(delimiter: ",") + "]");
                if (c.Subs != null && c.Subs.Length > 0)
                {
                    sb.Append(" Subs: [\n");
                    PrintConditions(sb, c.Subs, prefix + "  ");
                    sb.Append(prefix + "]");
                }

                sb.Append("\n");
            }
        }
    }
}