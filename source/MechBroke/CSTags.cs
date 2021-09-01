using System;
using System.Collections.Generic;
using System.Text;
using BattleTech;
using JetBrains.Annotations;
using Localize;
using UIWidgets;

namespace CustomSalvage.MechBroke
{

    public class Token
    {
        public string Caption;
        public int Value;
        public Condition[] Conditions;

        public override string ToString()
        {
            return $"{Value,-4:+0;-#}" + new Text(Caption).ToString();
        }
    }

    public class CSTag : CustomResource
    {
        public enum CType { Sum, Min, Max }
        public CType Type { get; set; }
        public List<Token> Tokens { get; set; }
        
        public int GetValue(MechDef mech, HashSet<string> tags, SimGameState sim)
        {
            var result = 0;
            if (Tokens != null)
                foreach (var token in Tokens)
                {
                    if (Conditions.Instatnce.CheckCondition(token.Conditions, mech, tags, sim))
                        switch (Type)
                        {
                            case CType.Sum:
                                result += token.Value;
                                break;
                            case CType.Min:
                                if (result > token.Value)
                                    result = token.Value;
                                break;
                            case CType.Max:
                                if (result < token.Value)
                                    result = token.Value;
                                break;
                        }
                }

            return result;
        }

        [CanBeNull]
        public string GetString(MechDef mech, HashSet<string> tags, SimGameState sim)
        {
            var curvalue = 0;
            string result = "";
            if (Tokens != null)
                foreach (var token in Tokens)
                {
                    if (Conditions.Instatnce.CheckCondition(token.Conditions, mech, tags, sim))
                        switch (Type)
                        {
                            case CType.Sum:
                                curvalue += token.Value;
                                result += token.ToString() + "\n";
                                break;
                            case CType.Min:
                                if (curvalue > token.Value)
                                {
                                    curvalue = token.Value;
                                    result = token.ToString() + "\n";
                                }

                                break;
                            case CType.Max:
                                if (curvalue < token.Value)
                                {
                                    curvalue = token.Value;
                                    result = token.ToString() + "\n";
                                }
                                break;
                        }
                }

            return curvalue == 0 ? null : result;
        }

        public override string ToString()
        {
            var sb = new StringBuilder($"CSTag: ({Type}){ID}\n");
            foreach (var token in Tokens)
            {
                sb.Append("- " + token.ToString() + "\n");
                sb.Append("-- C: [");
                if (token.Conditions != null && token.Conditions.Length > 0)
                {
                    Conditions.Instatnce.PrintConditions(sb, token.Conditions);
                    sb.Append("    ]\n");
                }
                else
                    sb.Append(" ]\n");
            }

            return sb.ToString();
        }
    }
}