using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Reserve.EntityEffects;

/// <summary>
/// Makes the target entity melee attack itself.
/// </summary>
public sealed partial class AttackSelf : EntityEffectBase<AttackSelf>
{
    /// <summary>
    /// Try to use the held item instead of a punch attack.
    /// </summary>
    [DataField]
    public bool UseHeld = true;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-attack-self", ("chance", Probability), ("useHeld", UseHeld));
}

public sealed partial class AttackSelfEntityEvent : EntityEffectSystem<CombatModeComponent, AttackSelf>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityQuery<MeleeWeaponComponent> _query = default!;

    protected override void Effect(Entity<CombatModeComponent> ent, ref EntityEffectEvent<AttackSelf> args)
    {
        var user = ent.Owner;
        var weapon = user;
        if (args.Effect.UseHeld)
            weapon = _hands.GetActiveItemOrSelf(user);

        if (!_query.TryComp(weapon, out var weaponComp))
            return;

        var target = ent.Owner;
        // SharedMeleeWeaponSystem.cs:493 prevents self-attack if weaponUid == target (unarmed self-punch)
        if (weapon == target)
        {
            if (_net.IsClient)
                return;

            if (weaponComp.Damage.GetTotal() > 0)
            {
                _damageable.TryChangeDamage(user, weaponComp.Damage, origin: user);
                _audio.PlayPvs(weaponComp.HitSound, user);
            }
            return;
        }

        var wasOn = ent.Comp.IsInCombatMode;
        _combatMode.SetInCombatMode(ent, true, ent.Comp);
        _melee.AttemptLightAttack(user, weapon, weaponComp, target);
        _combatMode.SetInCombatMode(ent, wasOn, ent.Comp);
    }
}
