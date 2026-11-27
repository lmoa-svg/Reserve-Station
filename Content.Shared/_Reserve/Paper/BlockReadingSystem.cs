using Content.Shared.Paper;
using Content.Shared.UserInterface;

namespace Content.Shared._Reserve.Paper;

public sealed partial class BlockReadingSystem : EntitySystem
{
    [Dependency] private EntityQuery<BlockReadingComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<BlockReadingComponent, PaperReadAttemptEvent>(OnPaperReadAttempt);
    }

    private void OnOpenAttempt(Entity<PaperComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (_query.HasComp(args.User))
            args.Cancel();
    }

    private void OnPaperReadAttempt(Entity<BlockReadingComponent> ent, ref PaperReadAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
