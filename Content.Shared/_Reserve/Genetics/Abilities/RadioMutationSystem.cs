using Content.Shared.Radio.Components;
using Content.Shared._Reserve.Genetics.Mutations;

namespace Content.Shared._Reserve.Genetics.Abilities;

public sealed partial class RadioMutationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<RadioMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<RadioMutationComponent> ent, ref MutationAddedEvent args)
    {
        var mob = args.Target.Owner;

        if (!HasComp<IntrinsicRadioReceiverComponent>(mob))
        {
            AddComp<IntrinsicRadioReceiverComponent>(mob);
            ent.Comp.AddedReceiver = true;
        }

        if (!TryComp<ActiveRadioComponent>(mob, out var active))
        {
            active = AddComp<ActiveRadioComponent>(mob);
            ent.Comp.AddedActive = true;
        }

        if (!TryComp<IntrinsicRadioTransmitterComponent>(mob, out var transmitter))
        {
            transmitter = AddComp<IntrinsicRadioTransmitterComponent>(mob);
            ent.Comp.AddedTransmitter = true;
        }

        foreach (var channel in ent.Comp.Channels)
        {
            if (active.Channels.Add(channel))
                ent.Comp.AddedActiveChannels.Add(channel);

            if (transmitter.Channels.Add(channel))
                ent.Comp.AddedTransmitterChannels.Add(channel);
        }

        Dirty(mob, active);
        Dirty(mob, transmitter);
        Dirty(ent);
    }

    private void OnRemoved(Entity<RadioMutationComponent> ent, ref MutationRemovedEvent args)
    {
        var mob = args.Target.Owner;
        var hasActive = TryComp<ActiveRadioComponent>(mob, out var active);
        var hasTransmitter = TryComp<IntrinsicRadioTransmitterComponent>(mob, out var transmitter);

        if (hasActive && active != null)
        {
            foreach (var channel in ent.Comp.AddedActiveChannels)
            {
                active.Channels.Remove(channel);
            }
            Dirty(mob, active);
        }

        if (hasTransmitter && transmitter != null)
        {
            foreach (var channel in ent.Comp.AddedTransmitterChannels)
            {
                transmitter.Channels.Remove(channel);
            }
            Dirty(mob, transmitter);
        }

        ent.Comp.AddedActiveChannels.Clear();
        ent.Comp.AddedTransmitterChannels.Clear();

        if (ent.Comp.AddedReceiver)
        {
            RemComp<IntrinsicRadioReceiverComponent>(mob);
            ent.Comp.AddedReceiver = false;
        }

        if (ent.Comp.AddedActive && hasActive && active != null && active.Channels.Count == 0)
        {
            RemComp(mob, active);
            ent.Comp.AddedActive = false;
        }

        if (ent.Comp.AddedTransmitter && hasTransmitter && transmitter != null && transmitter.Channels.Count == 0)
        {
            RemComp(mob, transmitter);
            ent.Comp.AddedTransmitter = false;
        }

        Dirty(ent);
    }
}
