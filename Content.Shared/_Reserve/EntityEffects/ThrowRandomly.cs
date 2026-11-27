using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Content.Shared.Throwing;
using Content.Shared._Reserve.EntityEffects.Throw;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Reserve.EntityEffects;

/// <summary>
/// Throws the target entity in a random direction, with a fixed speed.
/// </summary>
public sealed partial class ThrowRandomly : BaseThrowEntityEffect<ThrowRandomly>;

public sealed partial class ThrowRandomlyEffectSystem : EntityEffectSystem<MetaDataComponent, ThrowRandomly>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<ThrowRandomly> args)
    {
        var effect = args.Effect;
        if (effect.Speed <= 0f)
            return;

        var speed = Math.Clamp(effect.Speed, 0.1f, 50f);
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent, ent.Comp));
        var angle = rand.NextAngle();
        var direction = angle.ToVec();

        _throwing.TryThrow(
            ent,
            direction,
            baseThrowSpeed: speed,
            user: args.User);
    }
}
