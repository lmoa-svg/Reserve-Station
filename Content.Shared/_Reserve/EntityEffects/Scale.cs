using System.Numerics;
using Content.Shared.EntityEffects;
using Content.Shared.Sprite;
using Robust.Shared.Prototypes;

namespace Content.Shared._Reserve.EntityEffects;

/// <summary>
/// Multiplies the target entity's sprite scale.
/// </summary>
public sealed partial class Scale : EntityEffectBase<Scale>
{
    /// <summary>
    /// What to multiply scale by, componentwise.
    /// Using 1 for an axis means it is left alone.
    /// </summary>
    [DataField(required: true)]
    public Vector2 Multiplier;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-scale-entity",
            ("chance", Probability),
            ("x", Multiplier.X),
            ("y", Multiplier.Y));
}

public sealed partial class ScaleEffectSystem : EntityEffectSystem<TransformComponent, Scale>
{
    [Dependency] private readonly SharedScaleVisualsSystem _scale = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<Scale> args)
    {
        var factors = args.Effect.Multiplier;
        if (factors.X <= 0f || factors.Y <= 0f)
            return;

        var scale = _scale.GetSpriteScale(ent);
        var newScale = Vector2.Multiply(scale, factors);
        newScale.X = Math.Clamp(newScale.X, 0.25f, 3.0f);
        newScale.Y = Math.Clamp(newScale.Y, 0.25f, 3.0f);
        _scale.SetSpriteScale(ent, newScale);
    }
}
