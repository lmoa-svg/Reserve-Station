
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Console;

/// <summary>
/// Genetics console/scanner component for scanning a mob.
/// The mob can come from either a linked medical scanner or clicked mobs for the handheld version.
/// Must be used with <see cref="GeneticsConsoleComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GeneticsConsoleSystem))]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class GeneticsScannerComponent : Component
{
    /// <summary>
    /// Used to prevent scanning/sequencing/etc at the same time.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Busy;

    /// <summary>
    /// Subjects with more than this number of genetic damage can't be scanned.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxGeneticDamage = 90;

    /// <summary>
    /// The linked medical scanner.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Scanner;

    /// <summary>
    /// The mob currently in a linked scanner.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ScannedMob;

    /// <summary>
    /// How long it takes to scan a mob's genome.
    /// </summary>
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Sound played after successfully scanning a mob.
    /// </summary>
    [DataField]
    public SoundSpecifier? ScanSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg");

    /// <summary>
    /// Device linking source port for connecting to a medical scanner.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> ScannerPort = "MedicalScannerSender";
}

/// <summary>
/// Message to start the scanning process for an unscanned mob in the scanner.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GeneticsConsoleScanMessage : BoundUserInterfaceMessage;
