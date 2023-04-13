using BattleTech;
using BattleTech.BinkMedia;
using Org.BouncyCastle.Math.Raw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSalvage
{
    [HarmonyPatch(typeof(AttackStackSequence), "OnAttackComplete")]
    public static class AttackStackSequence_OnAttackComplete
    {
        public static bool Prepare() => Control.Instance.Settings.ShowSalvageFloaties;

        [HarmonyPriority(Priority.Last)]
        public static void Prefix(AttackStackSequence __instance, MessageCenterMessage message)
        {
            List<AttackDirector.AttackSequence> directorSequences = __instance.directorSequences;
            if (directorSequences != null)
            {
                try
                {
                    AttackDirector.AttackSequence attackSequence = directorSequences[0];
                    if (attackSequence == null) return;

                    if (attackSequence?.chosenTarget is Mech mechTarget)
                    {
                        Helper.ProcessMech(mechTarget);
                    }

                    if (attackSequence.attacker is Mech attackerMech)
                    {
                        Helper.ProcessMech(attackerMech);
                    }
                    
                }
                catch (Exception ex)
                {
                    Log.Main.Error?.Log($"BattlefieldCSMessage.Patches.AttackStackSequence_OnAttackComplete_Patch Prefix() {ex}");
                }
            }
        }

        public class Helper
        {
            public static void ProcessMech(Mech mech)
            {
                if (Helper.IsDead(mech) && Helper.CanSalvage(mech))
                {
                    int num = Helper.SalvageParts(mech);
                    string text;
                    if (mech.MechDef.MechTags.Contains(Control.Instance.Settings.NoSalvageMechTag) || mech.MechDef.MechTags.Contains(Control.Instance.Settings.NoSalvageVehicleTag))
                    {
                        text = "NOT SALVAGEABLE";
                    }
                    else
                    {
                        text = num is > 1 or 0 ? $"{num} SALVAGEABLE PARTS" : $"{num} SALVAGEABLE PART";
                    }
                    mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(mech, text, FloatieMessage.MessageNature.Inspiration, true)));
                }
            }

            public static bool IsDead(AbstractActor actor)
            {
                return actor != null && (actor.IsDead || actor.IsFlaggedForDeath || actor.HasHandledDeath);
            }

            public static bool CanSalvage(AbstractActor actor)
            {
                return actor != null && actor.Combat.HostilityMatrix.IsEnemy(actor.Combat.LocalPlayerTeam, actor.team);
            }

            public static int SalvageParts(Mech mech)
            {
                int num;
                if (mech == null)
                {
                    num = 0;
                }
                else
                {
                    MechDef mechDef = mech.ToMechDef();
                    Log.Main.Info?.Log("BattlefieldCSMessage " + mech.DisplayName + " Part Check:");
                    bool flag2 = mech.MechDef.IsLocationDestroyed(ChassisLocations.LeftArm);
                    if (flag2)
                    {
                        Log.Main.Info?.Log("   Left Arm Destroyed!");
                    }
                    bool flag3 = mech.MechDef.IsLocationDestroyed(ChassisLocations.RightArm);
                    if (flag3)
                    {
                        Log.Main.Info?.Log("   Right Arm Destroyed!");
                    }
                    bool flag4 = mech.MechDef.IsLocationDestroyed(ChassisLocations.LeftLeg);
                    if (flag4)
                    {
                        Log.Main.Info?.Log("   Left Leg Destroyed!");
                    }
                    bool flag5 = mech.MechDef.IsLocationDestroyed(ChassisLocations.RightLeg);
                    if (flag5)
                    {
                        Log.Main.Info?.Log("   Right Leg Destroyed!");
                    }
                    bool flag6 = mech.MechDef.IsLocationDestroyed(ChassisLocations.LeftTorso);
                    if (flag6)
                    {
                        Log.Main.Info?.Log("   Left Torso Destroyed!");
                    }
                    bool flag7 = mech.MechDef.IsLocationDestroyed(ChassisLocations.RightTorso);
                    if (flag7)
                    {
                        Log.Main.Info?.Log("   Right Torso Destroyed!");
                    }
                    bool flag8 = mech.MechDef.IsLocationDestroyed(ChassisLocations.CenterTorso);
                    if (flag8)
                    {
                        Log.Main.Info?.Log("   Center Torso Destroyed!");
                    }
                    bool flag9 = mech.MechDef.IsLocationDestroyed(ChassisLocations.Head);
                    if (flag9)
                    {
                        Log.Main.Info?.Log("  Head Destroyed!");
                    }
                    num = Control.Instance.GetNumParts(mechDef);
                    Log.Main.Info?.Log($"   {num} parts");
                }
                return num;
            }
        }
    }
}
