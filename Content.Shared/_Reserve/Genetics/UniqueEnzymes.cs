
using Content.Shared.Humanoid;
using Robust.Shared.Enums;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics;

/// <summary>
/// Basic appearance features that can be applied to a Mutatable mob.
/// Hair isn't included since you can do that with barber scissors / mirror.
/// </summary>
[DataRecord, Serializable, NetSerializable]
public partial record struct UniqueEnzymes(string Name, string? Prints, Sex? Sex, Gender? Gender, Color? EyeColor, Color? SkinColor);
