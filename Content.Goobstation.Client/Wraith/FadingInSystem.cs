using Content.Client._Reserve.Viewcone;
using Content.Goobstation.Shared.Wraith.Components;
using Robust.Client.GameObjects;

namespace Content.Goobstation.Client.Wraith;

public sealed partial class FadingInSystem : EntitySystem
{
    [Dependency] private SpriteVisibilitySystem _spriteVis = default!; // Reserve: SpriteSystem -> SpriteVisibilitySystem

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FadingInComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, FadingInComponent fading, ComponentStartup args)
    {
        // Start fully transparent
        _spriteVis.UpdateVisibilityModifiers(uid, nameof(FadingInComponent), 0f); // Reserve: _sprites.SetColor -> _spriteVis
        fading.Elapsed = 0f;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<FadingInComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out var fading, out _)) // Reserve: var sprite -> _
        {
            if (fading.Finished)
                continue;

            fading.Elapsed += frameTime;

            var alpha = Math.Clamp(fading.Elapsed / fading.FadeInTime, 0f, 1f);
            _spriteVis.UpdateVisibilityModifiers(uid, nameof(FadingInComponent), alpha); // Reserve: _sprites.SetColor -> _spriteVis
        }
    }
}

