using Content.Goobstation.Common.Religion;
using Content.Server.Chat.Managers;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared._Reserve.Genetics.Abilities;
using Content.Shared._Reserve.Genetics.Mutations;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._Reserve.Genetics.Abilities;

public sealed partial class TelepathyActionSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityQuery<ActorComponent> _actorQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MutationTelepathyActionComponent, TelepathyActionEvent>(OnTelepathyPrompt);
        SubscribeLocalEvent<MutationTelepathyActionComponent, ComponentShutdown>(OnShutdown);

        Subs.BuiEvents<MutationTelepathyActionComponent>(TelepathyUiKey.Key, subs =>
        {
            subs.Event<TelepathyChosenMessage>(OnTelepathyChosen);
        });
    }

    private void OnShutdown(Entity<MutationTelepathyActionComponent> ent, ref ComponentShutdown args)
    {
        _ui.CloseUi(ent.Owner, TelepathyUiKey.Key);
    }

    private void OnTelepathyPrompt(Entity<MutationTelepathyActionComponent> ent, ref TelepathyActionEvent args)
    {
        var user = args.Performer;
        var target = args.Target;
        ent.Comp.Target = target;

        if (!_ui.TryOpenUi(ent.Owner, TelepathyUiKey.Key, user))
            Log.Error($"Failed to open UI for {ToPrettyString(ent)} of {ToPrettyString(user)}");
    }

    private void OnTelepathyChosen(Entity<MutationTelepathyActionComponent> ent, ref TelepathyChosenMessage args)
    {
        var user = args.Actor;
        var msg = args.Message.Trim();

        if (string.IsNullOrWhiteSpace(msg) || msg.Length > ent.Comp.MaxLength)
            return;

        if (ent.Comp.Target is not {} target)
            return;

        ent.Comp.Target = null;

        var mutation = _mutation.GetActionMutation(ent.Owner);
        if (mutation == null || mutation.Value.Comp.Target != user)
            return;

        if (!_mutation.HasMutation(user, mutation.Value.Owner))
            return;

        if (!_blocker.CanInteract(user, user) || !_mob.IsAlive(user))
            return;

        var xformUser = Transform(user);
        var xformTarget = Transform(target);
        if (xformUser.MapID != xformTarget.MapID)
            return;

        var range = 10f;
        if (_actions.GetTargetRange(ent.Owner) is { } targetRange && targetRange > 0f)
            range = targetRange;

        if (!_transform.InRange(user, target, range))
            return;

        var ident = Identity.Entity(target, EntityManager);
        if (!_actorQuery.TryComp(target, out var actor))
        {
            _popup.PopupEntity(Loc.GetString("MutationTelepathy-popup-mindless", ("target", ident)), user, user);
            return;
        }

        if (HasComp<MindShieldComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("MutationTelepathy-popup-blocked"), user, user);
            return;
        }

        var ev = new BeforeCastTouchSpellEvent(target);
        RaiseLocalEvent(target, ev);
        if (ev.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("MutationTelepathy-popup-blocked"), user, user);
            return;
        }

        var channel = actor.PlayerSession.Channel;

        _actions.StartUseDelay(ent.Owner);

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{user:user} sent a telepathic message to {target:target}: {msg}");

        Tell(channel, msg);

        var ghostFilter = Filter.Empty().AddWhereAttachedEntity(HasComp<GhostComponent>);
        var wrapped = Loc.GetString("MutationTelepathy-message-wrap", ("message", FormattedMessage.EscapeText(msg)));
        _chatMan.ChatMessageToManyFiltered(ghostFilter,
            ChatChannel.Dead,
            msg,
            wrapped,
            source: EntityUid.Invalid,
            hideChat: false,
            recordReplay: false,
            colorOverride: null);
    }

    private void Tell(INetChannel client, string message)
    {
        _chatMan.ChatMessageToOne(ChatChannel.Local,
            message,
            Loc.GetString("MutationTelepathy-message-wrap", ("message", FormattedMessage.EscapeText(message))),
            source: EntityUid.Invalid,
            hideChat: false,
            client: client,
            recordReplay: true);
    }
}
