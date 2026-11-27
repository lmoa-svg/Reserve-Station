using Content.Shared.DetailExaminable;
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Content.Shared._Reserve.Genetics.Mutations;
using System.Linq;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics;

/// <summary>
/// Simple API for getting and changing <see cref="UniqueEnzymes"/> for mobs.
/// </summary>
public sealed partial class UniqueEnzymesSystem : EntitySystem
{
    [Dependency] private readonly MarkingManager _markingManager = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private MutationSystem _mutation = default!;
    [Dependency] private SharedHumanoidAppearanceSystem _humanoid = default!;

    [Dependency] private EntityQuery<FingerprintComponent> _printsQuery = default!;
    [Dependency] private EntityQuery<HumanoidAppearanceComponent> _humanoidQuery = default!;

    /// <summary>
    /// Change a mob's unique enzymes, if it is mutatable (i.e. no renaming mice and shit).
    /// </summary>
    public void ChangeEnzymes(EntityUid mob, UniqueEnzymes enzymes)
    {
        if (!_mutation.CanMutate(mob))
            return;

        var name = enzymes.Name.Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            if (name.Length > 64)
                name = name[..64];

            _meta.SetEntityName(mob, name);
        }

        if (enzymes.Prints is { } print && _printsQuery.TryComp(mob, out var prints))
        {
            prints.Fingerprint = print;
            Dirty(mob, prints);
        }

        if (!_humanoidQuery.TryComp(mob, out var humanoid))
            return;

        if (enzymes.EyeColor is { } eyeColor)
        {
            humanoid.EyeColor = eyeColor;
            if (humanoid.CustomBaseLayers.ContainsKey(HumanoidVisualLayers.Eyes))
                _humanoid.SetBaseLayerColor(mob, HumanoidVisualLayers.Eyes, eyeColor, true, humanoid);

            humanoid.MarkingSet.EnsureDefault(humanoid.SkinColor, humanoid.EyeColor, _markingManager);
            var ev = new HumanoidMarkingsChangedEvent();
            RaiseLocalEvent(mob, ref ev);
            Dirty(mob, humanoid);
        }

        if (enzymes.SkinColor is { } skinColor)
            _humanoid.SetSkinColor(mob, skinColor, humanoid: humanoid);

        // no age because peter thiel
        // no species because lol
        if (enzymes.Sex is { } sex)
            _humanoid.SetSex(mob, sex, humanoid: humanoid);
        if (enzymes.Gender is { } gender)
            _humanoid.SetGender(mob, gender, humanoid: humanoid);
    }

    /// <summary>
    /// Get the unique enzymes for a mob.
    /// </summary>
    public UniqueEnzymes GetEnzymes(EntityUid mob)
    {
        var humanoid = _humanoidQuery.CompOrNull(mob);
        return new UniqueEnzymes(
            Name(mob),
            _printsQuery.CompOrNull(mob)?.Fingerprint,
            humanoid?.Sex,
            humanoid?.Gender,
            humanoid?.EyeColor,
            humanoid?.SkinColor
        );
    }
}