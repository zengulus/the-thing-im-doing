using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TheThingImDoing.Glyphs;

public partial class GlyphRenderer : Control
{
    private static readonly Color CellFill = new(0.075f, 0.082f, 0.095f);
    private static readonly Color CellFrame = new(0.22f, 0.24f, 0.28f);
    private static readonly Color SizeGuide = new(0.16f, 0.18f, 0.21f);

    private GlyphSpec? _spec;
    private int _glyphSize = 64;

    [Export(PropertyHint.Range, "8,256,1")]
    public int GlyphSize
    {
        get => _glyphSize;
        set
        {
            _glyphSize = Math.Clamp(value, 8, 256);
            QueueRedraw();
        }
    }

    [Export] public Color StrokeColor { get; set; } = new(0.90f, 0.93f, 0.88f);
    [Export] public Color FillColor { get; set; } = new(0.96f, 0.78f, 0.40f);
    [Export] public bool ShowCellGuide { get; set; } = true;

    public GlyphSpec? Spec
    {
        get => _spec;
        set
        {
            _spec = value;
            QueueRedraw();
        }
    }

    public void Configure(GlyphSpec spec, int glyphSize)
    {
        Spec = spec;
        GlyphSize = glyphSize;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        if (ShowCellGuide)
        {
            DrawCellGuide();
        }

        if (_spec == null)
        {
            return;
        }

        foreach (GlyphPrimitive primitive in _spec.Primitives)
        {
            DrawPrimitive(primitive);
        }
    }

    private void DrawCellGuide()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), CellFill);
        DrawRect(new Rect2(Vector2.Zero, Size).Grow(-0.5f), CellFrame, filled: false, width: 1.0f);

        var glyphRect = new Rect2(
            (Size - new Vector2(GlyphSize, GlyphSize)) * 0.5f,
            new Vector2(GlyphSize, GlyphSize));
        DrawRect(glyphRect, SizeGuide, filled: false, width: 1.0f);
    }

    private void DrawPrimitive(GlyphPrimitive primitive)
    {
        switch (primitive)
        {
            case RingPrimitive ring:
                DrawCircle(ToCanvas(ring.Center), ToCanvasLength(ring.Radius), StrokeColor, filled: false, ToStroke(ring.Stroke), antialiased: true);
                break;

            case ArcPrimitive arc:
                DrawArcPrimitive(arc);
                break;

            case LinePrimitive line:
                DrawLine(ToCanvas(line.Start), ToCanvas(line.End), StrokeColor, ToStroke(line.Stroke), antialiased: true);
                break;

            case PolylinePrimitive polyline:
                DrawPolylinePrimitive(polyline);
                break;

            case DotPrimitive dot:
                DrawCircle(ToCanvas(dot.Center), ToDotRadius(dot.Radius), FillColor, filled: true, antialiased: true);
                break;

            case DiamondPrimitive diamond:
                DrawDiamond(diamond);
                break;

            case ChevronPrimitive chevron:
                DrawChevron(chevron);
                break;

            case ForkPrimitive fork:
                DrawFork(fork);
                break;

            case HatchPrimitive hatch:
                DrawHatch(hatch);
                break;

            case JaggedRayPrimitive jaggedRay:
                DrawJaggedRay(jaggedRay);
                break;
        }
    }

    private void DrawArcPrimitive(ArcPrimitive arc)
    {
        const float tau = Mathf.Pi * 2.0f;
        float start = arc.StartTurns * tau;
        float end = arc.EndTurns * tau;

        while (end < start)
        {
            end += tau;
        }

        int pointCount = Math.Clamp((int)MathF.Ceiling(Math.Abs(end - start) * GlyphSize / 7.0f), 8, 96);
        DrawArc(ToCanvas(arc.Center), ToCanvasLength(arc.Radius), start, end, pointCount, StrokeColor, ToStroke(arc.Stroke), antialiased: true);
    }

    private void DrawPolylinePrimitive(PolylinePrimitive polyline)
    {
        if (polyline.Points.Count < 2)
        {
            return;
        }

        Vector2[] points = polyline.Points.Select(ToCanvas).ToArray();
        if (polyline.Closed)
        {
            points = Close(points);
        }

        DrawPolyline(points, StrokeColor, ToStroke(polyline.Stroke), antialiased: true);
    }

    private void DrawDiamond(DiamondPrimitive diamond)
    {
        Vector2 center = ToCanvas(diamond.Center);
        float radius = ToCanvasLength(diamond.Radius);
        Vector2[] points =
        {
            center + new Vector2(0.0f, -radius),
            center + new Vector2(radius, 0.0f),
            center + new Vector2(0.0f, radius),
            center + new Vector2(-radius, 0.0f)
        };

        if (diamond.Filled)
        {
            DrawColoredPolygon(points, FillColor);
        }

        DrawPolyline(Close(points), StrokeColor, ToStroke(diamond.Stroke), antialiased: true);
    }

    private void DrawChevron(ChevronPrimitive chevron)
    {
        Vector2 direction = SafeNormalized(chevron.Direction, new Vector2(0.0f, -1.0f));
        Vector2 perpendicular = new(-direction.Y, direction.X);
        Vector2 center = chevron.Center;
        Vector2 tip = center + direction * chevron.Depth * 0.5f;
        Vector2 wingCenter = center - direction * chevron.Depth * 0.5f;
        Vector2 leftWing = wingCenter - perpendicular * chevron.Width * 0.5f;
        Vector2 rightWing = wingCenter + perpendicular * chevron.Width * 0.5f;
        float width = ToStroke(chevron.Stroke);

        DrawLine(ToCanvas(tip), ToCanvas(leftWing), StrokeColor, width, antialiased: true);
        DrawLine(ToCanvas(tip), ToCanvas(rightWing), StrokeColor, width, antialiased: true);
    }

    private void DrawFork(ForkPrimitive fork)
    {
        float width = ToStroke(fork.Stroke);
        Vector2 junction = ToCanvas(fork.Junction);

        DrawLine(ToCanvas(fork.Root), junction, StrokeColor, width, antialiased: true);
        DrawLine(junction, ToCanvas(fork.LeftBranch), StrokeColor, width, antialiased: true);
        DrawLine(junction, ToCanvas(fork.RightBranch), StrokeColor, width, antialiased: true);
    }

    private void DrawHatch(HatchPrimitive hatch)
    {
        Vector2 direction = SafeNormalized(hatch.End - hatch.Start, new Vector2(1.0f, 0.0f));
        Vector2 perpendicular = new(-direction.Y, direction.X);
        int count = Math.Clamp(hatch.Count, 1, 24);
        float halfLength = hatch.TickLength * 0.5f;
        float width = ToStroke(hatch.Stroke);

        for (int index = 0; index < count; index++)
        {
            float t = (index + 1.0f) / (count + 1.0f);
            Vector2 center = hatch.Start.Lerp(hatch.End, t);
            DrawLine(
                ToCanvas(center - perpendicular * halfLength),
                ToCanvas(center + perpendicular * halfLength),
                StrokeColor,
                width,
                antialiased: true);
        }
    }

    private void DrawJaggedRay(JaggedRayPrimitive ray)
    {
        Vector2 direction = SafeNormalized(ray.End - ray.Start, new Vector2(1.0f, 0.0f));
        Vector2 perpendicular = new(-direction.Y, direction.X);
        int segmentCount = Math.Clamp(ray.JagCount * 2 + 1, 3, 25);
        var points = new List<Vector2>(segmentCount + 1);

        for (int index = 0; index <= segmentCount; index++)
        {
            float t = index / (float)segmentCount;
            Vector2 point = ray.Start.Lerp(ray.End, t);

            if (index > 0 && index < segmentCount)
            {
                float sign = index % 2 == 0 ? -1.0f : 1.0f;
                point += perpendicular * ray.Amplitude * sign;
            }

            points.Add(ToCanvas(point));
        }

        DrawPolyline(points.ToArray(), StrokeColor, ToStroke(ray.Stroke), antialiased: true);
    }

    private Vector2 ToCanvas(Vector2 point)
    {
        return Size * 0.5f + point * GlyphSize * 0.5f;
    }

    private float ToCanvasLength(float normalizedLength)
    {
        return normalizedLength * GlyphSize * 0.5f;
    }

    private float ToStroke(float stroke)
    {
        float scaled = stroke * Math.Max(0.7f, GlyphSize / 32.0f);
        return Math.Clamp(scaled, 1.0f, GlyphSize * 0.14f);
    }

    private float ToDotRadius(float radius)
    {
        return Math.Max(ToCanvasLength(radius), GlyphSize <= 16 ? 1.4f : 1.0f);
    }

    private static Vector2 SafeNormalized(Vector2 value, Vector2 fallback)
    {
        return value.LengthSquared() < 0.0001f ? fallback : value.Normalized();
    }

    private static Vector2[] Close(IReadOnlyList<Vector2> points)
    {
        var closed = new Vector2[points.Count + 1];
        for (int index = 0; index < points.Count; index++)
        {
            closed[index] = points[index];
        }

        closed[^1] = points[0];
        return closed;
    }
}
