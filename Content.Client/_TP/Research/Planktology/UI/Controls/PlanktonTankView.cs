using System.Numerics;
using Content.Client.Resources;
using Content.Shared._TP.Research.Planktology;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client._TP.Research.Planktology.UI.Controls;

/// <summary>
///     Draws the inside of the culture tank: water tinted by temperature, 
///         the lamp when it's on,
///         and a numbered bubble per colony. 
/// </summary>
public sealed partial class PlanktonTankView : Control
{
    [Dependency] private IGameTiming _timing = default!;

    private const float NeutralTemperature = 293.15f;

    private static readonly Color WaterColor = Color.FromHex("#4E4E4E");
    private static readonly Color ColdColor = Color.FromHex("#647CDA");
    private static readonly Color HotColor = Color.FromHex("#FF0000");
    private static readonly Color LightColor = Color.FromHex("#FFE45C");

    private readonly Font _font;
    private readonly List<(Vector2 Position, float Radius)> _bubbles = new();

    private List<PlanktonSpecies> _species = new();
    private bool _lit;
    private float _temperature = NeutralTemperature;
    private float _minTemperature = NeutralTemperature - 70f;
    private float _maxTemperature = NeutralTemperature + 80f;

    public int? Selected;

    /// <summary>
    ///     Raised with the index of the clicked colony.
    /// </summary>
    public event Action<int>? OnSpeciesClicked;

    public PlanktonTankView()
    {
        IoCManager.InjectDependencies(this);
        _font = IoCManager.Resolve<IResourceCache>().GetFont("/Fonts/NotoSans/NotoSans-Bold.ttf", 11);
        MouseFilter = MouseFilterMode.Stop;
        RectClipContent = true;
    }

    public void UpdateState(PlanktonTankBoundUserInterfaceState state)
    {
        _species = state.Species;
        _lit = state.LightsOn && state.Powered;
        _temperature = state.Temperature;
        _minTemperature = state.MinTemperature;
        _maxTemperature = state.MaxTemperature;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        handle.DrawRect(new UIBox2(Vector2.Zero, size), GetWaterColor());

        if (_lit)
            DrawLight(handle, size);

        _bubbles.Clear();
        var time = (float) _timing.RealTime.TotalSeconds;
        var count = _species.Count;

        for (var i = 0; i < count; i++)
        {
            var species = _species[i];

            // Spread each colonies across the tank and let them each bob at their own pace.
            var phase = species.Number * 0.37f;
            var x = size.X * (i + 1) / (count + 1);
            var y = size.Y * 0.5f + MathF.Sin(time * (0.6f + (species.Number % 7) * 0.05f) + phase) * size.Y * 0.22f;
            var radius = (14f + MathF.Min(18f, 3f * MathF.Log2(species.Size + 1f))) * UIScale;
            var position = new Vector2(x, y);
            _bubbles.Add((position, radius));

            var fill = species.Alive ? Color.White.WithAlpha(0.85f) : Color.Gray.WithAlpha(0.4f);
            handle.DrawCircle(position, radius, fill);
            handle.DrawCircle(position, radius, i == Selected ? LightColor : Color.Black, filled: false);

            var text = species.Number.ToString();
            var textSize = handle.GetDimensions(_font, text, UIScale);
            handle.DrawString(_font, position - textSize / 2f, text, UIScale, species.Alive ? Color.Black : Color.DimGray);
        }
    }

    /// <summary>
    ///     Neutral water, tinting blue as it gets colder and red as it gets hotter.
    /// </summary>
    private Color GetWaterColor()
    {
        if (_temperature < NeutralTemperature)
        {
            var cold = Math.Clamp((NeutralTemperature - _temperature) / (NeutralTemperature - _minTemperature), 0f, 1f);
            return Color.InterpolateBetween(WaterColor, ColdColor, cold);
        }

        var hot = Math.Clamp((_temperature - NeutralTemperature) / (_maxTemperature - NeutralTemperature), 0f, 1f);
        return Color.InterpolateBetween(WaterColor, HotColor, hot);
    }

    /// <summary>
    ///     A soft yellow glow from the lamp at the top, fading out as it spreads.
    ///     The circle is centered on the top edge, with the view clipping it.
    /// </summary>
    private static void DrawLight(DrawingHandleScreen handle, Vector2 size)
    {
        var center = new Vector2(size.X / 2f, 0f);
        var maxRadius = size.Y * 0.65f;

        // Stack faint circles that shrink towards the lamp, building up a smooth radial falloff.
        const int steps = 6;
        for (var i = 0; i < steps; i++)
        {
            var t = (float) i / steps;
            var radius = maxRadius * (1f - t);
            handle.DrawCircle(center, radius, LightColor.WithAlpha(0.035f + 0.02f * t));
        }

        // A bright core where the lamp is.
        handle.DrawCircle(center, maxRadius * 0.08f, LightColor.WithAlpha(0.9f));
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        for (var i = 0; i < _bubbles.Count; i++)
        {
            var (position, radius) = _bubbles[i];
            if (Vector2.Distance(args.RelativePixelPosition, position) > radius)
                continue;

            OnSpeciesClicked?.Invoke(i);
            args.Handle();
            return;
        }
    }
}
