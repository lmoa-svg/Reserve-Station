// based on Trauma-Station: Content.Medical.Shared/EntityEffects/RelayBodyParts.cs

using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Reserve.EntityEffects;

/// <summary>
/// Relays entity effects to all body parts of a given type, or all parts.
/// Target must be a body.
/// </summary>
public sealed partial class RelayBodyParts : EntityEffectBase<RelayBodyParts>
{
    /// <summary>
    /// The body part type to run effects on.
    /// It will run on all of them if there are multiple.
    /// If this is null it will run on all body parts.
    /// </summary>
    [DataField]
    public BodyPartType? PartType;

    /// <summary>
    /// Optional part symmetry to require.
    /// </summary>
    [DataField]
    public BodyPartSymmetry? Symmetry;

    /// <summary>
    /// Text to use for the guidebook entry for reagents.
    /// </summary>
    [DataField]
    public LocId? GuidebookText;

    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => GuidebookText is { } key ? Loc.GetString(key, ("chance", Probability)) : null;
}

public sealed partial class RelayBodyPartsEffectSystem : EntityEffectSystem<BodyComponent, RelayBodyParts>
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<RelayBodyParts> args)
    {
        var effect = args.Effect;
        var partType = effect.PartType;
        var symmetry = effect.Symmetry;

        foreach (var (uid, part) in _body.GetBodyChildren(ent.Owner, ent.Comp))
        {
            if (partType is { } requiredType && part.PartType != requiredType)
                continue;

            if (symmetry is { } requiredSymmetry && part.Symmetry != requiredSymmetry)
                continue;

            _effects.ApplyEffects(uid, effect.Effects, args.Scale, args.User);
        }
    }
}