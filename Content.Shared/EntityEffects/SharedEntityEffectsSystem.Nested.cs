using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects;

/// <summary>
/// Entity effects API counterparts using <see cref="EntityEffectPrototype"/> instead of <see cref="EntityEffect"/>.
/// </summary>
public sealed partial class SharedEntityEffectsSystem
{
    // Reserve start: nested effect recursion guard
    private int _nestedEffectDepth;
    private const int MaxNestedEffectDepth = 16;
    // Reserve end

    /// <summary>
    /// <c>TryApplyEffect</c> overload using a <see cref="EntityEffectPrototype"/> instead of <see cref="EntityEffect"/>.
    /// </summary>
    public void TryApplyEffect(EntityUid target, [ForbidLiteral] ProtoId<EntityEffectPrototype> id, float scale = 1f, EntityUid? user = null)
    {
        // Reserve start: nested effect recursion guard
        if (_nestedEffectDepth >= MaxNestedEffectDepth)
        {
            Log.Error($"Max nested effect depth reached ({MaxNestedEffectDepth}) for prototype {id} on {ToPrettyString(target)}! Cycle detected?");
            return;
        }

        _nestedEffectDepth++;
        try
        {
            var proto = _proto.Index(id); // Goobstation - Change _proto to ProtoMan when engine update
            if (_condition.TryConditions(target, proto.Conditions))
                ApplyEffects(target, proto.Effects, scale, user); // Trauma
        }
        finally
        {
            _nestedEffectDepth--;
        }
        // Reserve end
    }
}
