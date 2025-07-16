using System.ComponentModel;
using DArvis.Models;

namespace DArvis.Types;

public enum SyncOperation
{
    [Description("Op.Walk North")]
    WalkNorth = Direction.North,
    [Description("Op.Walk East")]
    WalkEast = Direction.East,
    [Description("Op.Walk South")]
    WalkSouth = Direction.South,
    [Description("Op.Walk West")]
    WalkWest = Direction.West,
}