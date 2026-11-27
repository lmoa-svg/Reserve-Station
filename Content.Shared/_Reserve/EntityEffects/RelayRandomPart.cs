// based on Trauma-Station: Content.Medical.Shared/EntityEffects/RelayRandomPart.cs

using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;

namespace Content.Shared._Reserve.EntityEffects;

/// <summary>
/// Relays entity effects to a single random body part picked from allowed types.
/// </summary>
public sealed partial class RelayRandomPart : EntityEffectBase<RelayRandomPart>
{
    /// <summary>
    /// The body part types to pick from.
    /// </summary>
    [DataField(required: true)]
    public BodyPartType[] Types = default!;

    /// <summary>
    /// Optional part symmetry to require.
    /// </summary>
    [DataField]
    public BodyPartSymmetry? PartSymmetry;

    /// <summary>
    /// Effect to apply to a random part.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect Effect = default!;

    /// <summary>
    /// Effect to apply to the target body if no valid bodyparts were found.
    /// </summary>
    [DataField]
    public EntityEffect? FailEffect;
}

public sealed partial class RelayRandomPartEffectSystem : EntityEffectSystem<BodyComponent, RelayRandomPart>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    private readonly List<EntityUid> _parts = new();

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<RelayRandomPart> args)
    {
        var effect = args.Effect;
        var symmetry = effect.PartSymmetry;
        _parts.Clear();

        foreach (var (uid, part) in _body.GetBodyChildren(ent.Owner, ent.Comp))
        {
            if (symmetry is { } required && part.Symmetry != required)
                continue;

            if (Array.IndexOf(effect.Types, part.PartType) < 0)
                continue;

            _parts.Add(uid);
        }

        if (_parts.Count == 0) // no parts found
        {
            if (effect.FailEffect is { } fail)
                _effects.TryApplyEffect(ent, fail, args.Scale, args.User);
            return;
        }

        _parts.Sort((a, b) => GetNetEntity(a).Id.CompareTo(GetNetEntity(b).Id));

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var picked = rand.Pick(_parts);
        _effects.TryApplyEffect(picked, effect.Effect, args.Scale, args.User);
    }
}