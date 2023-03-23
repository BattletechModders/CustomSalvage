using System.Collections.Generic;
using BattleTech;

namespace CustomSalvage.MechBroke.Conditions;

public static class Boolean
{
    public static bool True(MechDef mech, Condition condition)
    {
        return true;
    }
    public static bool False(MechDef mech, Condition condition)
    {
        return false;
    }

    public static bool Not(MechDef mech, Condition condition)
    {
        return !ConditionsHandler.Instance.CheckCondition(condition.Subs, mech);
    }
    public static bool And(MechDef mech, Condition condition)
    {
        return ConditionsHandler.Instance.CheckCondition(condition.Subs, mech);
    }


    public static bool Or(MechDef mech, Condition condition)
    {
        IEnumerable<Condition> to_e(Condition c)
        {
            yield return c;
        }

        if (condition.Subs != null)
        {
            foreach (var c in condition.Subs)
            {
                if (c != null && ConditionsHandler.Instance.CheckCondition(to_e(c), mech))
                {
                    return true;
                }
            }
        }

        return false;
    }
}