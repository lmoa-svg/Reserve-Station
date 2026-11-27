using Content.Shared.CombatMode;
using Content.Shared.EntityEffects;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Reserve.EntityEffects;

/// <summary>
/// Makes the target entity attack a random mob nearby.
/// </summary>
public sealed partial class AttackOthers : EntityEffectBase<AttackOthers>
{
    /// <summary>
    /// Try to use the held item instead of a punch attack.
    /// </summary>
    [DataField]
    public bool UseHeld = true;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-attack-others", ("chance", Probability), ("useHeld", UseHeld));
}

public sealed partial class AttackOthersEntityEvent : EntityEffectSystem<CombatModeComponent, AttackOthers>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityQuery<MeleeWeaponComponent> _query = default!;

    protected override void Effect(Entity<CombatModeComponent> ent, ref EntityEffectEvent<AttackOthers> args)
    {
        if (_net.IsClient)
            return;

        var user = ent.Owner;
        var weapon = user;
        if (args.Effect.UseHeld)
            weapon = _hands.GetActiveItemOrSelf(user);

        if (!_query.TryComp(weapon, out var weaponComp))
            return;

        var range = weaponComp.Range > 0f ? weaponComp.Range : SharedInteractionSystem.InteractionRange;
        var coords = Transform(user).Coordinates;
        var candidates = new List<EntityUid>();

        foreach (var mob in _lookup.GetEntitiesInRange<MobStateComponent>(coords, range))
        {
            if (mob.Owner == user)
                continue;

            if (_mobState.IsDead(mob.Owner, mob.Comp))
                continue;

            if (!_interaction.InRangeUnobstructed(user, mob.Owner, range: range))
                continue;

            candidates.Add(mob.Owner);
        }

        if (candidates.Count == 0)
            return;

        var target = _random.Pick(candidates);
        var wasOn = ent.Comp.IsInCombatMode;
        _combatMode.SetInCombatMode(ent, true, ent.Comp);
        _melee.AttemptLightAttack(user, weapon, weaponComp, target);
        _combatMode.SetInCombatMode(ent, wasOn, ent.Comp);
    }
}
