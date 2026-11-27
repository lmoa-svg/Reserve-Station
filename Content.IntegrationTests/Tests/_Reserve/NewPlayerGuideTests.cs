using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client._Reserve.Chat.UI;
using Content.Client._Reserve.NewPlayerGuide;
using Content.Client._Reserve.NewPlayerGuide.Tags;
using Content.Client.Guidebook;
using Content.Client.Guidebook.Controls;
using Content.Client.Guidebook.Richtext;
using Content.Goobstation.Common.CCVar;
using Content.Shared._Reserve.NewPlayerGuide;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Input;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Reserve;

[TestFixture]
[TestOf(typeof(NewPlayerGuidePopup))]
public sealed class NewPlayerGuideTests
{
    [Test]
    public async Task ValidatePagesParseCleanly()
    {
        await using var pair = await PoolManager.GetServerClient();
        var client = pair.Client;
        await client.WaitIdleAsync();

        var protoMan = client.ResolveDependency<IPrototypeManager>();
        var resMan = client.ResolveDependency<IResourceManager>();
        var parser = client.ResolveDependency<DocumentParsingManager>();

        var pages = protoMan.EnumeratePrototypes<NewPlayerGuidePagePrototype>()
            .OrderBy(p => p.Order)
            .ThenBy(p => p.ID, StringComparer.Ordinal)
            .ToList();

        Assert.That(pages, Has.Count.EqualTo(6));

        foreach (var culture in new[] { "en-US", "ru-RU" })
        {
            foreach (var page in pages)
            {
                await client.WaitAssertion(() =>
                {
                    var path = page.GetLocalizedDocumentPath(culture);
                    Assert.That(resMan.ContentFileExists(path), Is.True, $"Missing guide page {path}");

                    using var reader = resMan.ContentFileReadText(path);
                    var text = reader.ReadToEnd();

                    var host = new Document();
                    Assert.That(parser.TryAddMarkup(host, text), Is.True, $"Failed to parse {page.ID} ({path})");

                    var errors = FindControls<GuidebookError>(host);
                    Assert.That(errors, Is.Empty,
                        $"Unexpected GuidebookError in {page.ID} ({path}): {errors.FirstOrDefault()?.GetType().Name}");
                });

                await client.WaitRunTicks(1);
            }
        }

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task StepThroughNewPlayerGuidePopup()
    {
        await using var pair = await PoolManager.GetServerClient();
        var client = pair.Client;
        await client.WaitIdleAsync();

        var uiMan = client.ResolveDependency<IUserInterfaceManager>();

        NewPlayerGuidePopup popup = default!;
        await client.WaitPost(() =>
        {
            popup = new NewPlayerGuidePopup { ShowRulesAfter = false };
            uiMan.WindowRoot.AddChild(popup);
            LayoutContainer.SetAnchorPreset(popup, LayoutContainer.LayoutPreset.Wide);
        });

        await client.WaitRunTicks(2);

        await client.WaitAssertion(() =>
        {
            Assert.That(popup.PageCount, Is.EqualTo(6));
            Assert.That(popup.CurrentPageIndex, Is.EqualTo(0));
            Assert.That(popup.BackButton.Disabled, Is.True);
            Assert.That(popup.SkipButton.Visible, Is.True);
            Assert.That(FindControls<GuidebookError>(popup.PageContainer), Is.Empty);
        });

        for (var expectedPage = 1; expectedPage < 6; expectedPage++)
        {
            await client.WaitPost(() => popup.ShowPage(expectedPage));
            await client.WaitRunTicks(2);

            await client.WaitAssertion(() =>
            {
                Assert.That(popup.CurrentPageIndex, Is.EqualTo(expectedPage));
                Assert.That(popup.BackButton.Disabled, Is.False);
                Assert.That(popup.SkipButton.Visible, Is.EqualTo(expectedPage != 5));
                Assert.That(FindControls<GuidebookError>(popup.PageContainer), Is.Empty,
                    $"Unexpected GuidebookError on page {expectedPage}");
            });
        }

        await client.WaitPost(() => popup.Orphan());
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SpeechPreviewUpdatesFromSliders()
    {
        await using var pair = await PoolManager.GetServerClient();
        var client = pair.Client;
        await client.WaitIdleAsync();

        var uiMan = client.ResolveDependency<IUserInterfaceManager>();
        var cfg = client.ResolveDependency<IConfigurationManager>();

        var prevRunechat = cfg.GetCVar(GoobCVars.ChatRunechat);
        var prevFontSize = cfg.GetCVar(GoobCVars.ChatRunechatFontSize);
        var prevOutline = cfg.GetCVar(GoobCVars.ChatRunechatOutlineScale);

        NewPlayerGuidePopup popup = default!;
        await client.WaitPost(() =>
        {
            cfg.SetCVar(GoobCVars.ChatRunechat, true);
            cfg.SetCVar(GoobCVars.ChatRunechatFontSize, 12);
            cfg.SetCVar(GoobCVars.ChatRunechatOutlineScale, 1f);

            popup = new NewPlayerGuidePopup { ShowRulesAfter = false };
            uiMan.WindowRoot.AddChild(popup);
            LayoutContainer.SetAnchorPreset(popup, LayoutContainer.LayoutPreset.Wide);
            popup.ShowPage(1);
        });

        await client.WaitRunTicks(3);

        var sliders = FindControls<GuideCVarSlider>(popup.PageContainer);
        var previews = FindControls<SpeechPreviewControl>(popup.PageContainer);
        Assert.That(sliders, Has.Count.EqualTo(2));
        Assert.That(previews, Has.Count.EqualTo(1));

        var preview = previews[0];
        var fontSliderContainer = sliders[0];
        var outlineSliderContainer = sliders[1];
        var fontSlider = FindControls<Slider>(fontSliderContainer).Single();
        var outlineSlider = FindControls<Slider>(outlineSliderContainer).Single();

        await client.WaitAssertion(() =>
        {
            Assert.That(fontSlider.VisibleInTree, Is.True);
            Assert.That(fontSlider.Size.X, Is.GreaterThan(10f));
            Assert.That(fontSlider.Size.Y, Is.GreaterThan(5f));
            Assert.That(outlineSlider.Size.X, Is.GreaterThan(10f));
            Assert.That(outlineSlider.Size.Y, Is.GreaterThan(5f));
        });

        var initialRowCount = preview.ChildCount;
        var initialFirstLabel = FindControls<RichTextLabel>(preview).First();
        var initialStylesheet = initialFirstLabel.Stylesheet;
        var initialPopupLabel = FindControls<Label>(preview).First(l => l.OutlineThicknessOverride.HasValue);
        var initialPopupFont = initialPopupLabel.FontOverride;

        // 1) Simulate UI click near the right end of the font size slider.
        await ClickControlAtRatio(client, uiMan, fontSlider, 0.85f);
        await client.WaitRunTicks(3);

        await client.WaitAssertion(() =>
        {
            var newFontSize = cfg.GetCVar(GoobCVars.ChatRunechatFontSize);
            Assert.That(newFontSize, Is.GreaterThan(12), "Clicking font size slider did not change CVar.");
            Assert.That(popup.SettingsChanged, Is.True);
            Assert.That(preview.ChildCount, Is.EqualTo(initialRowCount));

            var updatedLabel = FindControls<RichTextLabel>(preview).First();
            Assert.That(updatedLabel, Is.Not.SameAs(initialFirstLabel),
                "SpeechPreviewControl did not rebuild children when font size changed.");
            Assert.That(updatedLabel.Stylesheet, Is.Not.SameAs(initialStylesheet),
                "Runechat preview RichTextLabel stylesheet did not update when font size changed.");

            var updatedPopupLabel = FindControls<Label>(preview).First(l => l.OutlineThicknessOverride.HasValue);
            Assert.That(updatedPopupLabel.FontOverride, Is.Not.SameAs(initialPopupFont),
                "Popup preview FontOverride did not update when font size changed.");
        });

        // 2) Exercise outline scale slider via simulated UI click.
        var outlineBeforeClick = cfg.GetCVar(GoobCVars.ChatRunechatOutlineScale);
        var labelBeforeOutline = FindControls<RichTextLabel>(preview).First();
        var outlineBefore = labelBeforeOutline.OutlineThicknessOverride;
        var popupOutlineBefore = FindControls<Label>(preview).First(l => l.OutlineThicknessOverride.HasValue).OutlineThicknessOverride;

        await ClickControlAtRatio(client, uiMan, outlineSlider, 0.1f);
        await client.WaitRunTicks(3);

        await client.WaitAssertion(() =>
        {
            var newOutline = cfg.GetCVar(GoobCVars.ChatRunechatOutlineScale);
            Assert.That(newOutline, Is.Not.EqualTo(outlineBeforeClick).Within(0.01f),
                "Clicking outline slider did not change CVar.");

            var updatedLabel = FindControls<RichTextLabel>(preview).First();
            Assert.That(updatedLabel, Is.Not.SameAs(labelBeforeOutline),
                "SpeechPreviewControl did not rebuild children when outline scale changed.");
            Assert.That(updatedLabel.OutlineThicknessOverride, Is.Not.EqualTo(outlineBefore),
                "Runechat preview outline thickness did not update.");

            var updatedPopupLabel = FindControls<Label>(preview).First(l => l.OutlineThicknessOverride.HasValue);
            Assert.That(updatedPopupLabel.OutlineThicknessOverride, Is.Not.EqualTo(popupOutlineBefore),
                "Popup preview outline thickness did not update.");
        });

        // 3) Also exercise Slider.Value directly.
        var labelBeforeDirectSet = FindControls<RichTextLabel>(preview).First();
        await client.WaitPost(() => fontSlider.Value = 8);
        await client.WaitRunTicks(3);

        await client.WaitAssertion(() =>
        {
            Assert.That(cfg.GetCVar(GoobCVars.ChatRunechatFontSize), Is.EqualTo(8));
            var updatedLabel = FindControls<RichTextLabel>(preview).First();
            Assert.That(updatedLabel, Is.Not.SameAs(labelBeforeDirectSet));
        });

        // 4) Switch to Speech Bubbles and back to Runechat, then verify sliders still update preview.
        var choiceRow = FindControls<GuideChoiceRow>(popup.PageContainer).Single();
        var choiceButtons = FindControls<GuideChoice>(choiceRow).Select(c => c.ChoiceButton).ToList();
        Assert.That(choiceButtons, Has.Count.EqualTo(2));

        await ClickControlAtRatio(client, uiMan, choiceButtons[1], 0.5f);
        await client.WaitRunTicks(3);

        await client.WaitAssertion(() =>
        {
            Assert.That(cfg.GetCVar(GoobCVars.ChatRunechat), Is.False);
            Assert.That(FindControls<PanelContainer>(preview).Any(p => p.StyleClasses.Contains("speechBox")), Is.True,
                "SpeechPreviewControl did not switch to speech bubble panels.");
        });

        await ClickControlAtRatio(client, uiMan, choiceButtons[0], 0.5f);
        await client.WaitRunTicks(3);

        var labelBeforePostSwitch = FindControls<RichTextLabel>(preview).First();
        await client.WaitPost(() => fontSlider.Value = 20);
        await client.WaitRunTicks(3);

        await client.WaitAssertion(() =>
        {
            Assert.That(cfg.GetCVar(GoobCVars.ChatRunechat), Is.True);
            Assert.That(cfg.GetCVar(GoobCVars.ChatRunechatFontSize), Is.EqualTo(20));
            var updatedLabel = FindControls<RichTextLabel>(preview).First();
            Assert.That(updatedLabel, Is.Not.SameAs(labelBeforePostSwitch));
        });

        // 5) Verify Viewcone and ScreenShake GuideCVarSliders also work when interacted with.
        var prevGrain = cfg.GetCVar(CCVars.VisionGrainScale);
        var prevShake = cfg.GetCVar(CCVars.ScreenShakeIntensity);

        await client.WaitPost(() => popup.ShowPage(4));
        await client.WaitRunTicks(3);
        var viewconeSlider = FindControls<Slider>(FindControls<GuideCVarSlider>(popup.PageContainer).Single()).Single();
        await ClickControlAtRatio(client, uiMan, viewconeSlider, 0.2f);
        await client.WaitRunTicks(2);
        await client.WaitAssertion(() =>
        {
            Assert.That(cfg.GetCVar(CCVars.VisionGrainScale), Is.Not.EqualTo(prevGrain).Within(0.01f));
        });

        await client.WaitPost(() => popup.ShowPage(5));
        await client.WaitRunTicks(3);
        var shakeSlider = FindControls<Slider>(FindControls<GuideCVarSlider>(popup.PageContainer).Single()).Single();
        await ClickControlAtRatio(client, uiMan, shakeSlider, 0.2f);
        await client.WaitRunTicks(2);
        await client.WaitAssertion(() =>
        {
            Assert.That(cfg.GetCVar(CCVars.ScreenShakeIntensity), Is.Not.EqualTo(prevShake).Within(0.01f));
        });

        await client.WaitPost(() =>
        {
            popup.Orphan();
            cfg.SetCVar(GoobCVars.ChatRunechat, prevRunechat);
            cfg.SetCVar(GoobCVars.ChatRunechatFontSize, prevFontSize);
            cfg.SetCVar(GoobCVars.ChatRunechatOutlineScale, prevOutline);
            cfg.SetCVar(CCVars.VisionGrainScale, prevGrain);
            cfg.SetCVar(CCVars.ScreenShakeIntensity, prevShake);
        });

        await pair.CleanReturnAsync();
    }

    private static async Task ClickControlAtRatio(
        Robust.UnitTesting.RobustIntegrationTest.ClientIntegrationInstance client,
        IUserInterfaceManager uiMan,
        Control control,
        float xRatio)
    {
        GUIBoundKeyEventArgs downArgs = default!;
        GUIBoundKeyEventArgs upArgs = default!;
        Control? hit = null;

        await client.WaitPost(() =>
        {
            var pixelSize = control.PixelSize;
            var pos = (Vector2) control.GlobalPixelPosition + new Vector2(pixelSize.X * xRatio, pixelSize.Y * 0.5f);
            var coords = new ScreenCoordinates(pos, control.Window?.Id ?? default);

            hit = uiMan.MouseGetControl(coords);
            Assert.That(hit, Is.SameAs(control),
                $"Expected MouseGetControl at {coords} to hit {control} (GlobalPixelPosition={control.GlobalPixelPosition}, PixelSize={pixelSize}), but hit {hit}.");

            var relativePos = coords.Position / control.UIScale - control.GlobalPosition;
            var relativePixelPos = coords.Position - control.GlobalPixelPosition;

            downArgs = new GUIBoundKeyEventArgs(
                EngineKeyFunctions.UIClick,
                BoundKeyState.Down,
                coords,
                canFocus: true,
                relativePos,
                relativePixelPos);

            upArgs = new GUIBoundKeyEventArgs(
                EngineKeyFunctions.UIClick,
                BoundKeyState.Up,
                coords,
                canFocus: false,
                relativePos,
                relativePixelPos);
        });

        await client.DoGuiEvent(hit!, downArgs);
        await client.WaitRunTicks(1);
        await client.DoGuiEvent(hit!, upArgs);
    }

    private static List<T> FindControls<T>(Control root) where T : Control
    {
        var result = new List<T>();
        Collect(root, result);
        return result;

        static void Collect(Control current, List<T> list)
        {
            if (current is T match)
                list.Add(match);

            foreach (var child in current.Children)
            {
                Collect(child, list);
            }
        }
    }
}
