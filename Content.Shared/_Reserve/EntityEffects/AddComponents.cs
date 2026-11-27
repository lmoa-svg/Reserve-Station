using Content.Shared.EntityEffects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Reserve.EntityEffects;

/// <summary>
/// Adds a set of components to this entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class AddComponentsEntityEffectSystem : EntityEffectSystem<MetaDataComponent, AddComponents>
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly INetManager _net = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<AddComponents> args)
    {
        if (_net.IsClient)
            return;
        var components = args.Effect.Components;
        if (args.Effect.ReplaceExisting)
        {
            var filtered = new ComponentRegistry();
            var transformName = _factory.GetComponentName(typeof(TransformComponent));
            var metaDataName = _factory.GetComponentName(typeof(MetaDataComponent));

            foreach (var (name, entry) in components)
            {
                if (name == transformName || name == metaDataName ||
                    name == "Transform" || name == "MetaData" ||
                    entry.Component is TransformComponent or MetaDataComponent)
                {
                    continue;
                }

                filtered[name] = entry;
            }

            EntityManager.AddComponents(entity, filtered, true);
        }
        else
        {
            EntityManager.AddComponents(entity, components, false);
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AddComponents : EntityEffectBase<AddComponents>
{
    /// <summary>
    /// Components to add to this entity.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Replace existing components instead of keeping their current configuration.
    /// </summary>
    [DataField]
    public bool ReplaceExisting;
}
