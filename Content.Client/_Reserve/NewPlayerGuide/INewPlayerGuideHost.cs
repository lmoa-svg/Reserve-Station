namespace Content.Client._Reserve.NewPlayerGuide;

/// <summary>
///     Implemented by a host UI container to track when embedded guide tags modify settings.
/// </summary>
public interface INewPlayerGuideHost
{
    void NotifySettingChanged();
}
