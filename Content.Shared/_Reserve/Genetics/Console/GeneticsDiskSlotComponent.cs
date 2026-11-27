

using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Console;

[RegisterComponent, NetworkedComponent, Access(typeof(GeneticsDiskSystem))]
public sealed partial class GeneticsDiskSlotComponent : Component
{
    /// <summary>
    /// Name of the item slot that holds a genetics disk.
    /// </summary>
    [DataField]
    public string DiskSlot = "genetics_disk";
}
