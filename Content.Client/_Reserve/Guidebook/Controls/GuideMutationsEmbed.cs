using System.Diagnostics.CodeAnalysis;
using Content.Client.Guidebook.Richtext;
using Content.Shared._Reserve.Genetics.Mutations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._Reserve.Guidebook.Controls;

/// <summary>
/// Lists all mutations and how to get them.
/// </summary>
public sealed partial class GuideMutationsEmbed : BoxContainer, IDocumentTag
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public GuideMutationsEmbed()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Vertical;

        var mutation = _entMan.System<MutationSystem>();

        var names = new List<string>(); // to reuse allocation of 2 strings instead of recreating it for every recipe
        var ids = new List<string>(mutation.AllMutations.Count);
        foreach (var id in mutation.AllMutations.Keys)
        {
            ids.Add(id);
        }
        ids.Sort(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            var comp = mutation.AllMutations[id];
            AddChild(new MutationInfoControl(_proto, mutation, id, comp, names));
        }
    }

    bool IDocumentTag.TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = this;
        return true;
    }
}
