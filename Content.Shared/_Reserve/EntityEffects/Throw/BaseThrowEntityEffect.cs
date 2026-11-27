
using Content.Shared.EntityEffects;

namespace Content.Shared._Reserve.EntityEffects.Throw;

/// <summary>
/// Entity effect that specifically deals with throwing entities.
/// </summary>
public abstract partial class BaseThrowEntityEffect<T> : EntityEffectBase<T> where T : BaseThrowEntityEffect<T>
{
    /// <summary>
    /// The speed at which the thrown entity will be thrown.
    /// </summary>
    [DataField]
    public float Speed = 10f;
}
