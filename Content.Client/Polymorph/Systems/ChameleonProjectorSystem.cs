// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Reserve.Viewcone;
using Content.Client.Effects;
using Content.Client.Smoking;
using Content.Shared.Chemistry.Components;
using Content.Shared.Polymorph.Components;
using Content.Shared.Polymorph.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Polymorph.Systems;

public sealed class ChameleonProjectorSystem : SharedChameleonProjectorSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteVisibilitySystem _spriteVis = default!; // Reserve: SpriteSystem -> SpriteVisibilitySystem

    private EntityQuery<AppearanceComponent> _appearanceQuery;

    public override void Initialize()
    {
        base.Initialize();

        _appearanceQuery = GetEntityQuery<AppearanceComponent>();

        SubscribeLocalEvent<ChameleonDisguiseComponent, AfterAutoHandleStateEvent>(OnHandleState);

        SubscribeLocalEvent<ChameleonDisguisedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChameleonDisguisedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ChameleonDisguisedComponent, GetFlashEffectTargetEvent>(OnGetFlashEffectTargetEvent);
    }

    private void OnHandleState(Entity<ChameleonDisguiseComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        CopyComp<SpriteComponent>(ent);
        CopyComp<GenericVisualizerComponent>(ent);
        CopyComp<SolutionContainerVisualsComponent>(ent);
        CopyComp<BurnStateVisualsComponent>(ent);

        // reload appearance to hopefully prevent any invisible layers
        if (_appearanceQuery.TryComp(ent, out var appearance))
            _appearance.QueueUpdate(ent, appearance);
    }

    private void OnStartup(Entity<ChameleonDisguisedComponent> ent, ref ComponentStartup args)
    {
        _spriteVis.UpdateVisibilityModifiers(ent, nameof(ChameleonDisguisedComponent), 0f); // Reserve: _sprite.SetVisible -> _spriteVis
    }

    private void OnShutdown(Entity<ChameleonDisguisedComponent> ent, ref ComponentShutdown args)
    {
        _spriteVis.UpdateVisibilityModifiers(ent, nameof(ChameleonDisguisedComponent), 1f); // Reserve: _sprite.SetVisible -> _spriteVis
    }

    private void OnGetFlashEffectTargetEvent(Entity<ChameleonDisguisedComponent> ent, ref GetFlashEffectTargetEvent args)
    {
        args.Target = ent.Comp.Disguise;
    }
}