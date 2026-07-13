using System.Collections.Generic;
using Godot;

namespace TheThingImDoing.Glyphs;

public sealed record GlyphSpec(
    string Id,
    string DisplayName,
    IReadOnlyList<GlyphPrimitive> Primitives,
    string ReadabilityNote = "")
{
    public static IReadOnlyList<GlyphSpec> CreateProvingCircleSamples()
    {
        // Prototype readability notes:
        // - Ring, dot, diamond, fork, and simple chevrons survive best at 16 px.
        // - Open arcs need generous gaps below 24 px or they collapse into rings/noise.
        // - Dense hatches, jagged rays, and knot-ish loops should not be the only identifier at tiny sizes.
        return new[]
        {
            new GlyphSpec(
                "spark.ray",
                "Spark / Ray",
                new GlyphPrimitive[]
                {
                    new LinePrimitive(new Vector2(-0.64f, 0.44f), new Vector2(0.64f, -0.44f), 1.45f),
                    new ChevronPrimitive(new Vector2(0.46f, -0.32f), new Vector2(0.82f, -0.57f), 0.26f, 0.24f, 1.1f),
                    new LinePrimitive(new Vector2(-0.30f, -0.56f), new Vector2(-0.08f, -0.24f), 0.95f),
                    new LinePrimitive(new Vector2(0.30f, 0.56f), new Vector2(0.08f, 0.24f), 0.95f),
                    new DotPrimitive(Vector2.Zero, 0.10f)
                },
                "Cleaner than jagged rays at small sizes; keep spark ticks sparse."),

            new GlyphSpec(
                "mark.ring_dot",
                "Mark / Ring Dot",
                new GlyphPrimitive[]
                {
                    new RingPrimitive(Vector2.Zero, 0.62f, 1.5f),
                    new DotPrimitive(Vector2.Zero, 0.15f)
                },
                "Best tiny-size candidate: simple silhouette and strong center."),

            new GlyphSpec(
                "condition.fork",
                "Condition / Fork",
                new GlyphPrimitive[]
                {
                    new ForkPrimitive(
                        new Vector2(0.0f, 0.72f),
                        new Vector2(0.0f, 0.08f),
                        new Vector2(-0.48f, -0.48f),
                        new Vector2(0.48f, -0.48f),
                        1.35f),
                    new DiamondPrimitive(new Vector2(0.0f, 0.08f), 0.11f, true, 1.0f)
                },
                "Fork reads well when branches stay broad and sparse."),

            new GlyphSpec(
                "target.converge",
                "Target / Converge",
                new GlyphPrimitive[]
                {
                    new ChevronPrimitive(new Vector2(0.0f, -0.56f), new Vector2(0.0f, 1.0f), 0.42f, 0.28f, 1.25f),
                    new ChevronPrimitive(new Vector2(0.0f, 0.56f), new Vector2(0.0f, -1.0f), 0.42f, 0.28f, 1.25f),
                    new ChevronPrimitive(new Vector2(-0.56f, 0.0f), new Vector2(1.0f, 0.0f), 0.42f, 0.28f, 1.25f),
                    new ChevronPrimitive(new Vector2(0.56f, 0.0f), new Vector2(-1.0f, 0.0f), 0.42f, 0.28f, 1.25f),
                    new RingPrimitive(Vector2.Zero, 0.20f, 1.0f),
                    new DotPrimitive(Vector2.Zero, 0.06f)
                },
                "Good at 32 px and up; crowded at 16 px."),

            new GlyphSpec(
                "terrain.strata",
                "Terrain / Strata",
                new GlyphPrimitive[]
                {
                    new PolylinePrimitive(new[]
                    {
                        new Vector2(-0.74f, -0.40f),
                        new Vector2(-0.36f, -0.48f),
                        new Vector2(0.05f, -0.35f),
                        new Vector2(0.44f, -0.44f),
                        new Vector2(0.76f, -0.32f)
                    }, false, 1.0f),
                    new PolylinePrimitive(new[]
                    {
                        new Vector2(-0.70f, -0.06f),
                        new Vector2(-0.22f, -0.14f),
                        new Vector2(0.22f, -0.02f),
                        new Vector2(0.70f, -0.10f)
                    }, false, 1.0f),
                    new PolylinePrimitive(new[]
                    {
                        new Vector2(-0.62f, 0.26f),
                        new Vector2(-0.28f, 0.18f),
                        new Vector2(0.16f, 0.30f),
                        new Vector2(0.64f, 0.20f)
                    }, false, 1.0f),
                    new HatchPrimitive(new Vector2(-0.48f, 0.56f), new Vector2(0.48f, 0.48f), 5, 0.20f, 0.9f)
                },
                "Strata are clear at 32 px; hatch marks blur below 24 px."),

            new GlyphSpec(
                "memory.knot_loop",
                "Memory / Knot Loop",
                new GlyphPrimitive[]
                {
                    new ArcPrimitive(new Vector2(-0.26f, 0.0f), 0.36f, 0.08f, 0.86f, 1.1f),
                    new ArcPrimitive(new Vector2(0.26f, 0.0f), 0.36f, 0.58f, 1.36f, 1.1f),
                    new PolylinePrimitive(new[]
                    {
                        new Vector2(-0.02f, -0.10f),
                        new Vector2(-0.26f, -0.36f),
                        new Vector2(-0.56f, -0.12f),
                        new Vector2(-0.36f, 0.22f),
                        new Vector2(-0.02f, 0.08f),
                        new Vector2(0.02f, -0.08f),
                        new Vector2(0.36f, -0.22f),
                        new Vector2(0.56f, 0.12f),
                        new Vector2(0.26f, 0.36f),
                        new Vector2(0.02f, 0.10f)
                    }, false, 0.95f),
                    new DiamondPrimitive(Vector2.Zero, 0.08f, false, 0.9f)
                },
                "Useful as a large glyph only; too knotty for tiny UI.")
        };
    }
}

public abstract record GlyphPrimitive(float Stroke);

public sealed record RingPrimitive(Vector2 Center, float Radius, float StrokeWidth = 1.4f)
    : GlyphPrimitive(StrokeWidth);

public sealed record ArcPrimitive(Vector2 Center, float Radius, float StartTurns, float EndTurns, float StrokeWidth = 1.2f)
    : GlyphPrimitive(StrokeWidth);

public sealed record LinePrimitive(Vector2 Start, Vector2 End, float StrokeWidth = 1.2f)
    : GlyphPrimitive(StrokeWidth);

public sealed record PolylinePrimitive(IReadOnlyList<Vector2> Points, bool Closed = false, float StrokeWidth = 1.2f)
    : GlyphPrimitive(StrokeWidth);

public sealed record DotPrimitive(Vector2 Center, float Radius)
    : GlyphPrimitive(0.0f);

public sealed record DiamondPrimitive(Vector2 Center, float Radius, bool Filled = false, float StrokeWidth = 1.1f)
    : GlyphPrimitive(StrokeWidth);

public sealed record ChevronPrimitive(Vector2 Center, Vector2 Direction, float Width, float Depth, float StrokeWidth = 1.2f)
    : GlyphPrimitive(StrokeWidth);

public sealed record ForkPrimitive(Vector2 Root, Vector2 Junction, Vector2 LeftBranch, Vector2 RightBranch, float StrokeWidth = 1.2f)
    : GlyphPrimitive(StrokeWidth);

public sealed record HatchPrimitive(Vector2 Start, Vector2 End, int Count, float TickLength, float StrokeWidth = 1.0f)
    : GlyphPrimitive(StrokeWidth);

public sealed record JaggedRayPrimitive(Vector2 Start, Vector2 End, int JagCount, float Amplitude, float StrokeWidth = 1.2f)
    : GlyphPrimitive(StrokeWidth);
