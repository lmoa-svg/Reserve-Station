// based on Trauma-Station: Content.Trauma.Shared/EntityEffects/WeightedRandomEffect.cs

using System.Text;
using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;

namespace Content.Shared._Reserve.EntityEffects;

/// <summary>
/// Like <c>WeightedRandomPrototype</c> but for <see cref="EntityEffect"/>.
/// When ran it will activate a random effect.
/// </summary>
public sealed partial class WeightedRandomEffect : EntityEffectBase<WeightedRandomEffect>
{
    [DataField(required: true)]
    public List<WeightedEffect> Children = new();

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var totalWeights = GetTotalWeights();
        if (totalWeights <= 0f)
            return "Randomly chooses 1 of the following effects (none): 0%";

        var builder = new StringBuilder("Randomly chooses 1 of the following effects:");
        var totalPercent = 100f / totalWeights;
        foreach (var child in Children)
        {
            var percent = child.Weight * totalPercent;
            builder.Append("- ");
            builder.Append((int) percent);
            builder.Append("%: ");
            if (child.Effect.EntityEffectGuidebookText(prototype, entSys) is not { } text)
            {
                builder.Append("???,");
                continue;
            }

            builder.Append(text);
            builder.Append(',');
        }

        return builder.ToString();
    }

    public float GetTotalWeights()
    {
        var total = 0f;
        foreach (var child in Children)
        {
            if (child.Weight > 0f)
                total += child.Weight;
        }

        return total;
    }
}

public sealed partial class WeightedRandomEffectSystem : EntityEffectSystem<MetaDataComponent, WeightedRandomEffect>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<WeightedRandomEffect> args)
    {
        var effect = args.Effect;
        var totalWeights = effect.GetTotalWeights();
        if (totalWeights <= 0f)
            return;

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent, ent.Comp));
        var target = rand.NextFloat() * totalWeights;
        var total = 0f;
        EntityEffect? fallback = null;
        foreach (var child in effect.Children)
        {
            if (child.Weight <= 0f)
                continue;

            fallback = child.Effect;
            total += child.Weight;
            if (total >= target)
            {
                _effects.TryApplyEffect(ent, child.Effect, args.Scale, args.User);
                return;
            }
        }

        if (fallback != null)
            _effects.TryApplyEffect(ent, fallback, args.Scale, args.User);
    }
}

[DataDefinition]
public partial record struct WeightedEffect()
{
    [DataField(required: true)]
    public EntityEffect Effect = default!;

    [DataField]
    public float Weight = 1f;
}