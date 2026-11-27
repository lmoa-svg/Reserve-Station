
using Content.Shared.Actions;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Abilities;

/// <summary>
/// Component for the chemspike transfer action.
/// Removes itself and transfers the user's bloodstream chemicals to the mob the spike is embedded in.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChemTransferActionComponent : Component;

public sealed partial class ChemTransferActionEvent : InstantActionEvent;
