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

## PartCountType

How to calculate number of Parts you get from mech

### Vanila - base 1 for destroyed ct, 2 for legs, 3 for head/eject
### VanilaAdjusted - same, but proportional to needed parts for assembly option
- `float VACTDestroyedMod = 0.35f` - num of parts you get when ct destroyed
- `float VABLDestroyedMod = 0.68f` - num of parts you get when legs destroyed

## LostMechActionType 
Defines what to do with your lost mech

### ReturnItemsToPlayer - undestroyed items will return to player(Vanila way)
### ReturnItemsAndPartsToPlayer - MechParts and items will return to player
### MoveItemsToSalvage - items put on salvage list and can be looted
### MoveItemsAndPartsToSalvage - items and mechparts put on salvage list

## `bool SalvageTurrets = true` - add turrets to salvage
## `bool UpgradeSalvage = false' - salavaged items have chance to upgrade to "+" variants(not span into player lost mech items if they go to salvage)


