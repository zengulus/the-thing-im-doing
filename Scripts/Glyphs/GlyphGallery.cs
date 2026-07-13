using Godot;

namespace TheThingImDoing.Glyphs;

public partial class GlyphGallery : Control
{
    private static readonly int[] PreviewSizes = { 16, 24, 32, 64, 128 };

    [Export] public Vector2 CellSize { get; set; } = new(150.0f, 142.0f);
    [Export] public float NameColumnWidth { get; set; } = 190.0f;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        BuildGallery();
    }

    private void BuildGallery()
    {
        foreach (Node child in GetChildren())
        {
            child.QueueFree();
        }

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        AddChild(margin);

        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        margin.AddChild(root);

        var title = new Label
        {
            Text = "Glyph Proving Circle",
            CustomMinimumSize = new Vector2(0.0f, 28.0f)
        };
        title.AddThemeFontSizeOverride("font_size", 21);
        root.AddChild(title);

        var notes = new Label
        {
            Text = "Small-size read: ring, dot, diamond, fork, simple chevrons. Rework for tiny UI: dense hatch, jagged ray, knot loop, tight open arcs.",
            CustomMinimumSize = new Vector2(0.0f, 26.0f)
        };
        notes.AddThemeFontSizeOverride("font_size", 12);
        root.AddChild(notes);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddChild(scroll);

        var grid = new GridContainer
        {
            Columns = PreviewSizes.Length + 1,
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };
        grid.AddThemeConstantOverride("h_separation", 8);
        grid.AddThemeConstantOverride("v_separation", 8);
        scroll.AddChild(grid);

        grid.AddChild(CreateHeaderLabel(""));
        foreach (int size in PreviewSizes)
        {
            grid.AddChild(CreateHeaderLabel($"{size} px"));
        }

        foreach (GlyphSpec spec in GlyphSpec.CreateProvingCircleSamples())
        {
            grid.AddChild(CreateSpecLabel(spec));

            foreach (int size in PreviewSizes)
            {
                var renderer = new GlyphRenderer
                {
                    CustomMinimumSize = CellSize,
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    TooltipText = $"{spec.DisplayName} at {size} px"
                };
                renderer.Configure(spec, size);
                grid.AddChild(renderer);
            }
        }
    }

    private Label CreateHeaderLabel(string text)
    {
        var label = new Label
        {
            Text = text,
            CustomMinimumSize = new Vector2(text.Length == 0 ? NameColumnWidth : CellSize.X, 30.0f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", 13);
        return label;
    }

    private Label CreateSpecLabel(GlyphSpec spec)
    {
        var label = new Label
        {
            Text = $"{spec.DisplayName}\n{spec.Id}\n{spec.ReadabilityNote}",
            CustomMinimumSize = new Vector2(NameColumnWidth, CellSize.Y),
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        label.AddThemeFontSizeOverride("font_size", 12);
        return label;
    }
}
