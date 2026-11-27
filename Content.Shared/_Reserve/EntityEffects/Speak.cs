using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.EntityEffects;
using Content.Shared.Speech;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Reserve.EntityEffects;

/// <summary>
/// Makes the target entity say a random line from a localized dataset.
/// It can also have a string prefixed.
/// </summary>
public sealed partial class Speak : EntityEffectBase<Speak>
{
    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> Id;

    [DataField]
    public LocId? Prefix;

    [DataField]
    public bool HideChat;

    [DataField]
    public LocId GuidebookText = "entity-effect-guidebook-speak";

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString(GuidebookText, ("chance", Probability));
}

public sealed partial class SpeakEffectSystem : EntityEffectSystem<SpeechComponent, Speak>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;

    protected override void Effect(Entity<SpeechComponent> ent, ref EntityEffectEvent<Speak> args)
    {
        if (_net.IsClient)
            return;

        if (!_proto.TryIndex(args.Effect.Id, out var proto) || proto.Values.Count == 0)
            return;

        var picked = _random.Pick(proto.Values);

        // prepend the prefix
        if (args.Effect.Prefix is {} prefix)
            picked = Loc.GetString(prefix) + picked;

        // this is still logged so admins can know e.g. what started a dispute
        _chat.TrySendInGameICMessage(
            ent,
            picked,
            InGameICChatType.Speak,
            hideChat: args.Effect.HideChat,
            checkRadioPrefix: false);
    }
}
