using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TheThingImDoing.Content;
using TheThingImDoing.Spells;

namespace TheThingImDoing.UI;

public partial class SpellGraphEditorControl : Control
{
    private const float NodeWidth = 170.0f;
    private const float NodeHeight = 102.0f;

    private readonly Dictionary<int, PanelContainer> _nodeCards = new();
    private readonly Dictionary<int, bool> _draggingByNode = new();
    private readonly Dictionary<int, Vector2> _dragOffsetsByNode = new();
    private Working? _working;
    private PendingConnection? _pendingConnection;

    [Export(PropertyHint.Range, "1,24,1")] public int MaxNodeCount { get; set; } = 7;

    public event Action? GraphChanged;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(700, 430);
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Pass;
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.07f, 0.075f, 0.09f));

        if (_working == null)
        {
            return;
        }

        DrawGrid();

        foreach (WorkingNode node in _working.Nodes)
        {
            DrawConnection(node, WorkingOutputPort.Next, new Color(0.48f, 0.68f, 0.95f));
            DrawConnection(node, WorkingOutputPort.True, new Color(0.36f, 0.82f, 0.50f));
            DrawConnection(node, WorkingOutputPort.False, new Color(0.92f, 0.42f, 0.38f));
        }

        if (_pendingConnection.HasValue)
        {
            WorkingNode? node = _working.GetNode(_pendingConnection.Value.NodeId);
            if (node != null)
            {
                DrawCircle(GetOutputPosition(node, _pendingConnection.Value.Port), 5.0f, new Color(1.0f, 0.85f, 0.32f));
            }
        }
    }

    public void SetWorking(Working working)
    {
        _working = working;
        _pendingConnection = null;
        RebuildNodeCards();
        QueueRedraw();
    }

    public void AddClauseNode(string clauseId)
    {
        if (_working == null)
        {
            return;
        }

        if (!ClauseDefinitionCatalog.TryGet(clauseId, out _))
        {
            return;
        }

        if (_working.Nodes.Count >= MaxNodeCount)
        {
            return;
        }

        int nodeId = _working.GetNextAvailableNodeId();
        int column = (_working.Nodes.Count % 3);
        int row = _working.Nodes.Count / 3;
        var position = new Vector2(18 + column * 210, 22 + row * 135);
        var node = new WorkingNode(nodeId, clauseId, position);

        WorkingNode? previous = _working.Nodes.LastOrDefault();
        _working.AddNode(node);

        if (previous != null
            && previous.NextNodeId == null
            && ClauseDefinitionCatalog.TryGet(previous.ClauseId, out ClauseDefinition? previousDefinition)
            && !previousDefinition.IsCondition)
        {
            previous.NextNodeId = node.Id;
        }

        RebuildNodeCards();
        NotifyChanged();
    }

    public void ClearPendingConnection()
    {
        _pendingConnection = null;
        QueueRedraw();
    }

    private void RebuildNodeCards()
    {
        foreach (PanelContainer card in _nodeCards.Values)
        {
            card.QueueFree();
        }

        _nodeCards.Clear();
        _draggingByNode.Clear();
        _dragOffsetsByNode.Clear();

        if (_working == null)
        {
            return;
        }

        foreach (WorkingNode node in _working.Nodes)
        {
            PanelContainer card = CreateNodeCard(node);
            _nodeCards.Add(node.Id, card);
            AddChild(card);
        }
    }

    private PanelContainer CreateNodeCard(WorkingNode node)
    {
        ClauseDefinitionCatalog.TryGet(node.ClauseId, out ClauseDefinition? definition);
        var card = new PanelContainer
        {
            Position = node.BoardPosition,
            Size = new Vector2(NodeWidth, NodeHeight),
            CustomMinimumSize = new Vector2(NodeWidth, NodeHeight),
            MouseFilter = MouseFilterEnum.Stop
        };

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        card.AddChild(margin);

        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        margin.AddChild(root);

        var header = new HBoxContainer();
        root.AddChild(header);

        var inputButton = new Button
        {
            Text = GameStrings.Get("ui.graph.input"),
            CustomMinimumSize = new Vector2(36, 24),
            TooltipText = GameStrings.Get("ui.graph.input_tooltip")
        };
        inputButton.Pressed += () => ConnectPendingTo(node.Id);
        header.AddChild(inputButton);

        var title = new Label
        {
            Text = definition?.DisplayName ?? node.ClauseId,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 13);
        header.AddChild(title);

        var deleteButton = new Button
        {
            Text = GameStrings.Get("ui.graph.delete"),
            CustomMinimumSize = new Vector2(28, 24),
            TooltipText = GameStrings.Get("ui.graph.delete_tooltip")
        };
        deleteButton.Pressed += () => RemoveNode(node.Id);
        header.AddChild(deleteButton);

        var text = new Label
        {
            Text = definition?.PlayerText ?? GameStrings.Get("ui.graph.missing_clause"),
            CustomMinimumSize = new Vector2(NodeWidth - 20, 28),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        text.AddThemeFontSizeOverride("font_size", 12);
        root.AddChild(text);

        var outputs = new HBoxContainer();
        root.AddChild(outputs);

        if (definition?.IsCondition == true)
        {
            outputs.AddChild(CreateOutputButton(node.Id, WorkingOutputPort.True, GameStrings.Get("ui.graph.true")));
            outputs.AddChild(CreateOutputButton(node.Id, WorkingOutputPort.False, GameStrings.Get("ui.graph.false")));
        }
        else
        {
            outputs.AddChild(CreateOutputButton(node.Id, WorkingOutputPort.Next, GameStrings.Get("ui.graph.next")));
        }

        card.GuiInput += inputEvent => HandleCardInput(node, card, inputEvent);

        return card;
    }

    private Button CreateOutputButton(int nodeId, WorkingOutputPort port, string label)
    {
        var button = new Button
        {
            Text = label,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = GameStrings.Get("ui.graph.output_tooltip")
        };

        button.Pressed += () =>
        {
            _pendingConnection = new PendingConnection(nodeId, port);
            QueueRedraw();
        };

        return button;
    }

    private void HandleCardInput(WorkingNode node, PanelContainer card, InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton)
        {
            _draggingByNode[node.Id] = mouseButton.Pressed;
            _dragOffsetsByNode[node.Id] = mouseButton.Position;
            return;
        }

        if (inputEvent is InputEventMouseMotion mouseMotion
            && _draggingByNode.TryGetValue(node.Id, out bool isDragging)
            && isDragging)
        {
            node.BoardPosition = ClampToBoard(card.Position + mouseMotion.Relative);
            card.Position = node.BoardPosition;
            NotifyChanged(redrawOnly: true);
        }
    }

    private Vector2 ClampToBoard(Vector2 position)
    {
        return new Vector2(
            Mathf.Clamp(position.X, 0.0f, Mathf.Max(0.0f, Size.X - NodeWidth)),
            Mathf.Clamp(position.Y, 0.0f, Mathf.Max(0.0f, Size.Y - NodeHeight)));
    }

    private void ConnectPendingTo(int targetNodeId)
    {
        if (_working == null || !_pendingConnection.HasValue)
        {
            return;
        }

        PendingConnection pending = _pendingConnection.Value;
        WorkingNode? source = _working.GetNode(pending.NodeId);

        if (source == null || source.Id == targetNodeId)
        {
            _pendingConnection = null;
            QueueRedraw();
            return;
        }

        source.SetOutput(pending.Port, targetNodeId);
        _pendingConnection = null;
        NotifyChanged();
    }

    private void RemoveNode(int nodeId)
    {
        if (_working == null)
        {
            return;
        }

        _working.RemoveNode(nodeId);
        RebuildNodeCards();
        NotifyChanged();
    }

    private void DrawGrid()
    {
        Color lineColor = new(0.11f, 0.12f, 0.15f);

        for (float x = 0.0f; x < Size.X; x += 24.0f)
        {
            DrawLine(new Vector2(x, 0), new Vector2(x, Size.Y), lineColor);
        }

        for (float y = 0.0f; y < Size.Y; y += 24.0f)
        {
            DrawLine(new Vector2(0, y), new Vector2(Size.X, y), lineColor);
        }
    }

    private void DrawConnection(WorkingNode source, WorkingOutputPort port, Color color)
    {
        if (_working == null)
        {
            return;
        }

        int? targetNodeId = source.GetOutput(port);

        if (!targetNodeId.HasValue)
        {
            return;
        }

        WorkingNode? target = _working.GetNode(targetNodeId.Value);

        if (target == null)
        {
            return;
        }

        Vector2 start = GetOutputPosition(source, port);
        Vector2 end = GetInputPosition(target);
        Vector2 controlA = start + new Vector2(44, 0);
        Vector2 controlB = end - new Vector2(44, 0);
        DrawPolyline(GetBezierPoints(start, controlA, controlB, end), color, width: 3.0f);
    }

    private static Vector2 GetInputPosition(WorkingNode node)
    {
        return node.BoardPosition + new Vector2(0, 28);
    }

    private static Vector2 GetOutputPosition(WorkingNode node, WorkingOutputPort port)
    {
        float y = port switch
        {
            WorkingOutputPort.True => 72,
            WorkingOutputPort.False => 93,
            _ => 82
        };

        return node.BoardPosition + new Vector2(NodeWidth, y);
    }

    private static Vector2[] GetBezierPoints(Vector2 start, Vector2 controlA, Vector2 controlB, Vector2 end)
    {
        Vector2[] points = new Vector2[18];

        for (int i = 0; i < points.Length; i++)
        {
            float t = i / (float)(points.Length - 1);
            points[i] = start.BezierInterpolate(controlA, controlB, end, t);
        }

        return points;
    }

    private void NotifyChanged(bool redrawOnly = false)
    {
        QueueRedraw();

        if (!redrawOnly)
        {
            GraphChanged?.Invoke();
        }
    }

    private readonly record struct PendingConnection(int NodeId, WorkingOutputPort Port);
}
