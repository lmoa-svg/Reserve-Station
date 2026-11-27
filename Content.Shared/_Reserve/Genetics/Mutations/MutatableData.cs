

using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Mutations;

/// <summary>
/// Complete mutation data for a mob which can easily be applied to a mob.
/// </summary>
[DataRecord, Serializable, NetSerializable]
public partial record struct MutatableData(List<EntProtoId> Dormant, List<EntProtoId> Mutations);
