# CusomSalvage

CustomSalvage - mod that allow customize salvage and assembly operations

# Options

## RecoveryType

How to define if you lost mech will return to you or need to recovered. Options

### `Vanila` - Base Game variant. Roll vs fixed chance
### `PartDestroyed` - Custom components based. 
Additional options
- `float RecoveryMod = 1` - multiplier to base game recovery chance
- `float LimbRecoveryPenalty = 0.05f` - penalty for lost leg or arm(each lost limb applied)
- `float TorsoRecoveryPenalty = 0.1f` - penalty for lost torso port(include CT)
- `float HeadRecoveryPenaly = 0` - for lost head
- `float EjectRecoveryBonus = 0.25f` - additional bonus if pilot ejected

### `AllwaysRecover` - lost mech allways return t0 player
### `NeverRecover` - lost mech allways lost

