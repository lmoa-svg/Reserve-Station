using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Disable vision effect spawning like footsteps, used for integration tests.
    /// </summary>
    public static readonly CVarDef<bool> DisableVisionEffects =
        CVarDef.Create("vision.disable_effects", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Whether to disable vision cone overlays.
    /// </summary>
    public static readonly CVarDef<bool> DisableVisionCones =
        CVarDef.Create("vision.disable_cones", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Scale for how strong out-of-vision graininess is, 0 is just pure greyscale.
    /// </summary>
    public static readonly CVarDef<float> VisionGrainScale =
        CVarDef.Create("vision.grain_scale", 0.75f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
