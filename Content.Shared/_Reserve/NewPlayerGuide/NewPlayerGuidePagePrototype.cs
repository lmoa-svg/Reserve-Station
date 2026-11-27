using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.NewPlayerGuide;

[Prototype]
public sealed partial class NewPlayerGuidePagePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public int Order { get; private set; }

    [DataField(required: true)]
    public ResPath Document { get; private set; } = ResPath.Empty;

    [DataField("localizedDocument")]
    public Dictionary<string, ResPath> LocalizedDocument { get; private set; } = new();

    /// <summary>
    ///     Returns the localized document path for the requested culture, falling back to English or the base document.
    /// </summary>
    public ResPath GetLocalizedDocumentPath(string? culture)
    {
        if (LocalizedDocument.Count > 0)
        {
            if (culture != null && LocalizedDocument.TryGetValue(culture, out var culturePath))
                return culturePath;

            if (LocalizedDocument.TryGetValue("en-US", out var enUsPath))
                return enUsPath;
        }

        return Document;
    }
}
