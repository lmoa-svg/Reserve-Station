// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Reserve.Viewcone;
using Content.Shared._Goobstation.Wizard.Traps;
using Robust.Client.GameObjects;

namespace Content.Client._Shitcode.Wizard.Systems;

public sealed class WizardTrapsSystem : SharedWizardTrapsSystem
{
    [Dependency] private readonly SpriteVisibilitySystem _spriteVis = default!; // Reserve: sprite visibility

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WizardTrapComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<WizardTrapComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!args.AppearanceData.TryGetValue(TrapVisuals.Alpha, out var alpha))
            return;

        _spriteVis.UpdateVisibilityModifiers(ent, nameof(WizardTrapComponent), (float) alpha); // Reserve: sprite.Color -> _spriteVis
    }
}