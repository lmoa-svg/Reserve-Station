using Content.Shared.EntityEffects;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Reserve.EntityEffects;

/// <summary>
/// Multiplies the target entity's fixture radii.
/// Density is unchanged so mass will increase/decrease.
/// </summary>
public sealed partial class ScaleFixtures : EntityEffectBase<ScaleFixtures>
{
    /// <summary>
    /// What to scale fixtures by.
    /// </summary>
    [DataField(required: true)]
    public float Multiplier;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class ScaleFixturesEffectSystem : EntityEffectSystem<FixturesComponent, ScaleFixtures>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    protected override void Effect(Entity<FixturesComponent> ent, ref EntityEffectEvent<ScaleFixtures> args)
    {
        if (_net.IsClient)
            return;
        var multiplier = args.Effect.Multiplier;
        if (multiplier <= 0f)
            return;

        multiplier = Math.Clamp(multiplier, 0.1f, 10f);

        foreach (var (_, fixture) in ent.Comp.Fixtures)
        {
            switch (fixture.Shape)
            {
                case PhysShapeCircle circle:
                    var newRadius = circle.Radius * multiplier;
                    if (newRadius < 0.05f || newRadius > 5.0f)
                        return;
                    break;
                case PolygonShape poly:
                    for (var i = 0; i < poly.VertexCount; i++)
                    {
                        var newDist = (poly.Vertices[i] * multiplier).Length();
                        if (newDist > 10.0f)
                            return;
                    }
                    break;
            }
        }

        _physics.ScaleFixtures(ent.AsNullable(), multiplier);
    }
}
