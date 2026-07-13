using Godot;

namespace TheThingImDoing.UI;

public static class GameTheme
{
    public static readonly Color Ink = new("#0b0d13");
    public static readonly Color Surface = new("#151923");
    public static readonly Color RaisedSurface = new("#202634");
    public static readonly Color Border = new("#3b455b");
    public static readonly Color Text = new("#edf0f7");
    public static readonly Color MutedText = new("#aab2c4");
    public static readonly Color Gold = new("#e8bd55");
    public static readonly Color Violet = new("#b487ee");
    public static readonly Color Cyan = new("#63c5da");
    public static readonly Color Danger = new("#e65d68");

    public static Theme Create()
    {
        var theme = new Theme
        {
            DefaultFontSize = 14
        };

        theme.SetColor("font_color", "Label", Text);
        theme.SetColor("font_shadow_color", "Label", new Color(0, 0, 0, 0.72f));
        theme.SetConstant("shadow_offset_x", "Label", 1);
        theme.SetConstant("shadow_offset_y", "Label", 2);

        theme.SetColor("font_color", "Button", Text);
        theme.SetColor("font_hover_color", "Button", Colors.White);
        theme.SetColor("font_pressed_color", "Button", Colors.White);
        theme.SetColor("font_disabled_color", "Button", new Color(MutedText, 0.5f));
        theme.SetStylebox("normal", "Button", Box(RaisedSurface, Border, 1, 5));
        theme.SetStylebox("hover", "Button", Box(new Color("#2b3344"), Violet, 2, 5));
        theme.SetStylebox("pressed", "Button", Box(new Color("#343044"), Gold, 2, 5));
        theme.SetStylebox("disabled", "Button", Box(new Color("#11141c"), new Color(Border, 0.45f), 1, 5));
        theme.SetStylebox("focus", "Button", Box(new Color(0, 0, 0, 0), Gold, 2, 5));

        theme.SetStylebox("panel", "PanelContainer", Box(new Color(Surface, 0.985f), Border, 1, 7));
        theme.SetStylebox("panel", "TooltipPanel", Box(new Color(Ink, 0.98f), Violet, 1, 5));
        theme.SetColor("font_color", "TooltipLabel", Text);

        theme.SetConstant("separation", "HBoxContainer", 6);
        theme.SetConstant("separation", "VBoxContainer", 6);
        theme.SetStylebox("focus", "LineEdit", Box(RaisedSurface, Gold, 2, 4));
        return theme;
    }

    public static StyleBoxFlat Box(Color background, Color border, int borderWidth, int radius)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomRight = radius,
            CornerRadiusBottomLeft = radius,
            ContentMarginLeft = 8,
            ContentMarginTop = 6,
            ContentMarginRight = 8,
            ContentMarginBottom = 6
        };
    }
}
