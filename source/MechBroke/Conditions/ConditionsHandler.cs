using System.Collections.Generic;
using System.Linq;
using System.Text;
using BattleTech;
using JetBrains.Annotations;
using Harmony;

namespace CustomSalvage.MechBroke
{
    public delegate bool ConditionDelegate(MechDef mech, Condition condition);

    public delegate void PrepareDelegate(MechDef mech, SimGameState sim);


    public class Condition
    {
        public string Type { get; set; }
        public string[] Strings { get; set; }
        public int[] Ints { get; set; }
        public float[] Floats { get; set; }

        public Condition[] Subs { get; set; }
    }

    public class ConditionsHandler
    {
        private static ConditionsHandler _instance;

        public static ConditionsHandler Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ConditionsHandler();

                return _instance;
            }
        }

        private Dictionary<string, ConditionDelegate> handlers;
        private List<PrepareDelegate> prepare_delegates;


        private ConditionsHandler()
        {
            handlers = new Dictionary<string, ConditionDelegate>();
            prepare_delegates = new List<PrepareDelegate>();
            RegisterHandler("mechtag", Conditions.MechTags.Handler, Conditions.MechTags.Prepare);
            RegisterHandler("mechtagany", Conditions.AnyMechTags.Handler, Conditions.AnyMechTags.Prepare);
            RegisterHandler("systemtag", Conditions.PlanetTags.Handler, Conditions.PlanetTags.Prepare);
            RegisterHandler("systemtagany", Conditions.AnyPlanetTags.Handler, Conditions.AnyPlanetTags.Prepare);
            RegisterHandler("pilottag", Conditions.PilotTag.Handler, Conditions.PilotTag.Prepare);
            RegisterHandler("pilottagany", Conditions.AnyPilotTag.Handler, Conditions.AnyPilotTag.Prepare);
            RegisterHandler("companystat", Conditions.CompanyStat.Handler, Conditions.CompanyStat.Prepare);

            RegisterHandler("true", Conditions.Boolean.True);
            RegisterHandler("false", Conditions.Boolean.False);
            RegisterHandler("and", Conditions.Boolean.And);
            RegisterHandler("not", Conditions.Boolean.Not);
            RegisterHandler("or", Conditions.Boolean.Or);
        }

        public void RegisterHandler(string keyword, ConditionDelegate handler, PrepareDelegate prepare = null)
        {
            handlers[keyword] = handler;
            if (prepare != null)
                prepare_delegates.Add(prepare);
        }

        public void PrepareCheck(MechDef mech, SimGameState sim)
        {
            foreach (var prepareDelegate in prepare_delegates)
            {
                prepareDelegate(mech, sim);
            }
        }

        public bool CheckCondition(IEnumerable<Condition> conditions, MechDef mech)
        {
            if (conditions != null)
                foreach (var condition in conditions)
                {
                    if (condition != null)
                        if (handlers.TryGetValue(condition.Type, out var handler))
                        {
                            if (!handler(mech, condition))
                                return false;
                        }
                        else
                        {
                            Log.Main.Error?.Log($"Not found handler for {condition.Type} condition");
                        }
                }

            return true;
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