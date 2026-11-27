using System.Numerics;
using Content.Shared._Reserve.Viewcone.Components;
using Robust.Client.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client._Reserve.Viewcone.ComponentTree;

/// <summary>
/// Handles gathering sprites to modify alpha in the viewcone overlays.
/// </summary>
public sealed partial class ViewconeOcclusionSystem : ComponentTreeSystem<ViewconeOccludableTreeComponent, ViewconeOccludableComponent>
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    protected override bool DoFrameUpdate => true;
    protected override bool DoTickUpdate => false;
    protected override bool Recursive => false;

    protected override Box2 ExtractAabb(in ComponentTreeEntry<ViewconeOccludableComponent> entry, Vector2 pos, Angle rot)
    {
        return _spriteQuery.TryComp(entry.Uid, out var sprite)
            ? _sprite.CalculateBounds((entry.Uid, sprite), pos, rot, default).CalcBoundingBox()
            : Box2.UnitCentered.Translated(pos);
    }
}
