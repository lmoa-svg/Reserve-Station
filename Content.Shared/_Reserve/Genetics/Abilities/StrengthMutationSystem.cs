using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._Reserve.Genetics.Abilities;

public sealed partial class StrengthMutationSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnGetMeleeDamage(Entity<StrengthMutationComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        args.Damage *= MathF.Max(0f, ent.Comp.MeleeModifier);
    }
}
