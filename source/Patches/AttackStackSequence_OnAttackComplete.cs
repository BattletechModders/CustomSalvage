using BattleTech;
using BattleTech.BinkMedia;
using Org.BouncyCastle.Math.Raw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSalvage;

[HarmonyPatch(typeof(AttackStackSequence), nameof(AttackStackSequence.OnAttackComplete))]
public static class AttackStackSequence_OnAttackComplete
{
    public static bool Prepare() => Control.Instance.Settings.ShowSalvageFloaties;
    [HarmonyWrapSafe]
    [HarmonyPriority(Priority.Last)]
    public static void Prefix(AttackStackSequence __instance, MessageCenterMessage message)
    {
        List<AttackDirector.AttackSequence> directorSequences = __instance.directorSequences;
        AttackDirector.AttackSequence attackSequence = directorSequences?[0];
        if (attackSequence == null) return;

        if (attackSequence.chosenTarget is Mech mechTarget)
        {
            Helper.ProcessMech(mechTarget);
        }
        if (attackSequence.attacker is Mech attackerMech)
        {
            Helper.ProcessMech(attackerMech);
        }
    }
}

public class Helper
{
    public static void ProcessMech(Mech mech)
    {
        if (!Helper.IsDead(mech) || !Helper.CanSalvage(mech)) return;
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
        if (mech == null) return 0;
        MechDef mechDef = mech.ToMechDef();
        Log.Main.Info?.Log("BattlefieldCSMessage " + mech.DisplayName + " Part Check:");

        if (mech.MechDef.IsLocationDestroyed(ChassisLocations.LeftArm)) Log.Main.Info?.Log("   Left Arm Destroyed!");
        if (mech.MechDef.IsLocationDestroyed(ChassisLocations.RightArm)) Log.Main.Info?.Log("   Right Arm Destroyed!");
        if (mech.MechDef.IsLocationDestroyed(ChassisLocations.LeftLeg)) Log.Main.Info?.Log("   Left Leg Destroyed!");
        if (mech.MechDef.IsLocationDestroyed(ChassisLocations.RightLeg)) Log.Main.Info?.Log("   Right Leg Destroyed!");
        if (mech.MechDef.IsLocationDestroyed(ChassisLocations.LeftTorso)) Log.Main.Info?.Log("   Left Torso Destroyed!");
        if (mech.MechDef.IsLocationDestroyed(ChassisLocations.RightTorso)) Log.Main.Info?.Log("   Right Torso Destroyed!");
        if (mech.MechDef.IsLocationDestroyed(ChassisLocations.CenterTorso)) Log.Main.Info?.Log("   Center Torso Destroyed!");
        if (mech.MechDef.IsLocationDestroyed(ChassisLocations.Head)) Log.Main.Info?.Log("  Head Destroyed!");
        int num = Control.Instance.GetNumParts(mechDef);
        Log.Main.Info?.Log($"   {num} parts");
        return num;
    }
}

