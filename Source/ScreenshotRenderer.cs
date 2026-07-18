using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.ScreenshotTool;

public static class ScreenshotRenderer
{
    public enum Pass
    {
        White,
        Black,
    };
    private static int _screenshotIndex;
    private static bool _savingAllowed;
    private static bool _isStandalone;
    private static Vector2? _relPos;

    private static Color[] _blackScreenshot;
    private static Color[] _whiteScreenshot;

    public static bool SavingFinished { get; private set; }

    public static void AllowSaving(int index, Vector2? relPos, bool isStandalone)
    {
        _savingAllowed = true;
        _screenshotIndex = index;
        _relPos = relPos;
        _isStandalone = isStandalone;
    }

    public static void TakeScreenshot(Player self, Pass pass)
    {
        int w = Engine.ViewWidth;
        int h = Engine.ViewHeight;
        Color[] pixels = new Color[w * h];
        Engine.Graphics.GraphicsDevice.GetBackBufferData(pixels);

        if (pass == Pass.Black)
            _blackScreenshot = pixels;
        else
            _whiteScreenshot = pixels;
    }

    // warning: use cached _relPos, don't compute it from the player which might have moved
    private static void SaveScreenshot(Player self) {
        int w = Engine.ViewWidth;
        int h = Engine.ViewHeight;
        Color[] pixels = new Color[w * h];

        // compute transparency if needed by using the two cached screenshots
        if (ScreenshotToolModule.Settings.RemoveBackground)
        {
            for (int i = 0; i < w * h; i++)
            {
                Color colW = _whiteScreenshot[i];
                Color colB = _blackScreenshot[i];

                // need r=g=b for the background color for this to work
                int alpha = (int)MathHelper.Clamp(255 - (colW.R - colB.R), 0, 255);
                if (alpha < 1)
                    pixels[i] = Color.Transparent;
                else
                {
                    float mul = 255f / alpha;
                    pixels[i] = new Color(
                        (int)MathHelper.Clamp(colB.R * mul, 0, 255),
                        (int)MathHelper.Clamp(colB.G * mul, 0, 255),
                        (int)MathHelper.Clamp(colB.B * mul, 0, 255),
                        alpha
                    );
                }
            }
        }
        else
            for (int i = 0; i < w * h; i++)
                pixels[i] = _blackScreenshot[i];

        string fileName;
        if (_isStandalone)
            fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        else
        {
            Vector2 relPos = _relPos ?? new Vector2();
            fileName = $"{_screenshotIndex:0000},dx={(int)relPos.X},dy={(int)relPos.Y}.png";
        }

        string filePath = FileHelper.GetExistingPath(self, fileName);
        Logger.Info("ScreenshotTool", $"Saving screenshot to \"{filePath}\"");

        using RenderTarget2D target = new RenderTarget2D(Engine.Graphics.GraphicsDevice, w, h, false,
            SurfaceFormat.Color, DepthFormat.None);
        target.SetData(pixels);

        using Stream stream = File.OpenWrite(filePath);
        target.SaveAsPng(stream, w, h);
    }

    public static void OnPlayerUpdate(Player self)
    {
        if (!_savingAllowed)
            return;
        
        SaveScreenshot(self);

        _savingAllowed = false;
        SavingFinished = true;
    }
}
