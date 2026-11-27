using Content.Goobstation.Common.Religion;
using Content.Server.Chat.Managers;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.CombatMode;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared._Reserve.Genetics.Abilities;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Reserve.Genetics.Abilities;

public sealed partial class MindReadActionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityQuery<ActorComponent> _actorQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindReadActionComponent, MindReadActionEvent>(OnMindRead);
    }

    private void OnMindRead(Entity<MindReadActionComponent> ent, ref MindReadActionEvent args)
    {
        var user = args.Performer;
        var target = args.Target;

        if (!_actorQuery.TryComp(user, out var actor))
            return;

        if (!_blocker.CanInteract(user, target))
            return;

        var channel = actor.PlayerSession.Channel;

        args.Handled = true;

        var identity = Identity.Name(target, EntityManager);
        if (!_mind.TryGetMind(target, out _, out var mind))
        {
            _popup.PopupEntity(Loc.GetString("MutationMindReader-popup-target-mindless", ("target", identity)), user, user);
            return;
        }

        if (_mob.IsDead(target))
        {
            _popup.PopupEntity(Loc.GetString("MutationMindReader-popup-target-dead", ("target", identity)), user, user);
            return;
        }

        if (HasComp<MindShieldComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("MutationMindReader-popup-mind-protected", ("target", identity)), user, user);
            return;
        }

        var ev = new BeforeCastTouchSpellEvent(target);
        RaiseLocalEvent(target, ev);
        if (ev.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("MutationMindReader-popup-mind-protected", ("target", identity)), user, user);
            return;
        }

        if (user == target)
        {
            _popup.PopupEntity(Loc.GetString("MutationMindReader-popup-self"), user, user);
            return;
        }

        _adminLogger.Add(LogType.Genetics, LogImpact.Low, $"{user:user} read the mind of {target:target}");

        _popup.PopupEntity(Loc.GetString("MutationMindReader-popup-plunge", ("target", identity)), user, user);

        if (_random.Prob(ent.Comp.AlertProb))
            _popup.PopupEntity(Loc.GetString("MutationMindReader-popup-alert"), target, target, PopupType.MediumCaution);

        var combat = _combatMode.IsInCombatMode(target);
        Tell(channel, Loc.GetString("MutationMindReader-popup-combat-mode", ("target", identity), ("combat", combat)));

        if (mind.CharacterName is {} name && name != identity)
            Tell(channel, Loc.GetString("MutationMindReader-popup-true-identity", ("target", identity), ("name", name)), Color.Red);
    }

    private void Tell(INetChannel client, string message, Color? color = null)
    {
        _chatMan.ChatMessageToOne(ChatChannel.Local,
            message,
            message,
            source: EntityUid.Invalid,
            hideChat: false,
            client: client,
            colorOverride: color,
            recordReplay: true);
    }
}
