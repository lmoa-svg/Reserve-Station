using Content.Shared._Reserve.Viewcone.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Configuration;

namespace Content.Shared._Reserve.Viewcone;

/// <summary>
/// Provides public API for getting the actual modified viewcone angle (including equipment etc) rather than just the base angle.
/// </summary>
public sealed partial class ViewconeAngleSystem : EntitySystem
{
    [Dependency] private SharedBodySystem _body = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private EntityQuery<ViewconeComponent> _query = default!;
    [Dependency] private EntityQuery<WieldableComponent> _wieldableQuery = default!;

    private bool _disabled;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, ModifyViewconeAngleEvent>(OnBodyModifyAngle);
        SubscribeLocalEvent<StatusEffectContainerComponent, ModifyViewconeAngleEvent>(_status.RelayEvent);

        Subs.SubscribeWithRelay<ViewconeModifierComponent, ModifyViewconeAngleEvent>(OnModifyAngle, held: false);

        SubscribeLocalEvent<ViewconeModifierComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ViewconeModifierComponent, StatusEffectRelayedEvent<ModifyViewconeAngleEvent>>(OnEffectModifyAngle);
        SubscribeLocalEvent<CursorOffsetRequiresWieldComponent, HeldRelayedEvent<ModifyViewconeAngleEvent>>(OnScopeModify);

        Subs.CVar(_cfg, CCVars.DisableVisionCones, x => _disabled = x, true);
    }

    private void OnExamined(Entity<ViewconeModifierComponent> ent, ref ExaminedEvent args)
    {
        if (_disabled)
            return;

        // 1.25 -> 25, 0.6 -> 40
        var percent = Math.Abs((int) MathF.Round(ent.Comp.AngleModifier * 100f) - 100);
        if (percent == 0)
            return;

        var dir = ent.Comp.AngleModifier < 1f ? "decrease" : "increase";
        var loc = "viewcone-modifier-examine-" + dir;
        args.PushMarkup(Loc.GetString(loc, ("percent", percent)));
    }

    private void OnModifyAngle(Entity<ViewconeModifierComponent> ent, ref ModifyViewconeAngleEvent args)
    {
        args.ModifyAngle(ent.Comp.AngleModifier);
    }

    private void OnEffectModifyAngle(Entity<ViewconeModifierComponent> ent, ref StatusEffectRelayedEvent<ModifyViewconeAngleEvent> args)
    {
        var ev = args.Args;
        ev.ModifyAngle(ent.Comp.AngleModifier);
        args.Args = ev;
    }

    private void OnBodyModifyAngle(Entity<BodyComponent> ent, ref ModifyViewconeAngleEvent args)
    {
        foreach (var organ in _body.GetBodyOrganEntityComps<ViewconeModifierComponent>(ent.AsNullable()))
        {
            if (organ.Comp2.Enabled)
                args.ModifyAngle(organ.Comp1.AngleModifier);
        }
    }

    private void OnScopeModify(Entity<CursorOffsetRequiresWieldComponent> ent, ref HeldRelayedEvent<ModifyViewconeAngleEvent> args)
    {
        if (_wieldableQuery.TryComp(ent, out var wieldable) && wieldable.Wielded)
            args.Args.ModifyAngle(ent.Comp.ViewAngleMultiplier);
    }

    /// <summary>
    /// Returns the modified viewcone angle for an entity, calculated from the base,
    /// taking into account equipment, status effects, organs, and mutations.
    /// </summary>
    public float GetAngle(Entity<ViewconeComponent?> ent)
    {
        if (!_query.Resolve(ent, ref ent.Comp))
            return 0f;

        var ev = new ModifyViewconeAngleEvent();
        RaiseLocalEvent(ent, ref ev);

        // clamps to 0, 360 since this could easily go over with stacking equipment items
        return Math.Clamp(ent.Comp.BaseConeAngle * ev.AngleModifier, 0f, 360f);
    }
}

/// <summary>
/// Raised clientside by-ref and broadcast on an entity with a viewcone, and relayed to inventory, body organs, hands, mutations, and status effects.
/// Modifies their viewcone angle multiplicatively.
/// </summary>
[ByRefEvent]
public record struct ModifyViewconeAngleEvent() : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.HEAD | SlotFlags.EYES | SlotFlags.MASK;

    private float _angleModifier = 1f;

    public float AngleModifier => _angleModifier;

    public void ModifyAngle(float angle)
    {
        _angleModifier *= angle;
    }
}
