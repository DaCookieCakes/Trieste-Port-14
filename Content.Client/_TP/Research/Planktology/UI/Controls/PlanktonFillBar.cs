using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client._TP.Research.Planktology.UI.Controls;

/// <summary>
///     A vertical fill gauge with tick marks, like the cryo pod's beaker readout.
/// </summary>
public sealed class PlanktonFillBar : Control
{
    private static readonly Color Background = Color.FromHex("#1B1B1E");
    private static readonly Color Fill = Color.FromHex("#4FA3C4");

    /// <summary>
    ///     0 to 1.
    /// </summary>
    public float Value;

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        handle.DrawRect(new UIBox2(Vector2.Zero, size), Background);

        var filled = size.Y * Math.Clamp(Value, 0f, 1f);
        handle.DrawRect(new UIBox2(0, size.Y - filled, size.X, size.Y), Fill);

        for (var i = 1; i < 4; i++)
        {
            var y = size.Y * i / 4f;
            handle.DrawLine(new Vector2(0, y), new Vector2(size.X * 0.4f, y), Color.White.WithAlpha(0.6f));
        }

        handle.DrawRect(new UIBox2(Vector2.Zero, size), Color.Gray, filled: false);
    }
}
