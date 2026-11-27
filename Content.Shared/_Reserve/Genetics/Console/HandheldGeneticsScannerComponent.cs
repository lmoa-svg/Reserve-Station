

using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Console;

/// <summary>
/// Links a clicked mob to a <see cref="GeneticsScannerComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GeneticsConsoleSystem))]
public sealed partial class HandheldGeneticsScannerComponent : Component
{
    /// <summary>
    /// How long the mob linking doafter is.
    /// </summary>
    [DataField]
    public TimeSpan LinkTime = TimeSpan.FromSeconds(0.5);
}
