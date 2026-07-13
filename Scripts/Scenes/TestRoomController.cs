using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TheThingImDoing.Actors;
using TheThingImDoing.Content;
using TheThingImDoing.Core;
using TheThingImDoing.Spells;
using TheThingImDoing.UI;
using TheThingImDoing.World;

public partial class TestRoomController : Node2D
{
    private static readonly GridPos[] DemoWalls =
    {
        new(3, 2),
        new(3, 3),
        new(4, 3)
    };

    private readonly Dictionary<int, CharacterBody2D> _actorVisuals = new();
    private readonly Working[] _preparedWorkings =
    {
        WorkingSamples.CreateMarkOrDamage(),
        WorkingSamples.CreateEmergencyWall()
    };

    private TacticalEncounter? _encounter;
    private Label? _statusLabel;
    private Label? _workingTitleLabel;
    private Label? _traceLabel;
    private Label? _quickTraceLabel;
    private Label? _codexDetailLabel;
    private PanelContainer? _spellPanel;
    private Button? _editorToggleButton;
    private SpellGraphEditorControl? _spellEditor;
    private Button[] _slotButtons = [];
    private WorkingPreview? _preview;
    private string _lastTraceActionLabel = "";
    private WorkingResult? _lastTraceResult;
    private int _visibleTraceSteps = int.MaxValue;
    private bool _isSpellEditorOpen;
    private int _selectedWorkingIndex;
    private GridPos _selectedTarget = new(5, 1);

    [Export(PropertyHint.Range, "3,32,1")] public int GridWidth { get; set; } = 8;
    [Export(PropertyHint.Range, "3,32,1")] public int GridHeight { get; set; } = 6;
    [Export(PropertyHint.Range, "16,96,1")] public int TileSize { get; set; } = 48;
    [Export] public Vector2 GridOrigin { get; set; } = new(64, 96);
    [Export] public Vector2I PlayerStart { get; set; } = new(1, 1);
    [Export] public Vector2I GlassHoundStart { get; set; } = new(6, 4);
    [Export] public Vector2I AshScribeStart { get; set; } = new(6, 1);
    [Export] public Vector2I RootSaintStart { get; set; } = new(1, 4);

    public override void _Ready()
    {
        _statusLabel = GetNodeOrNull<Label>("CanvasLayer/StatusLabel");
        BuildSpellUi();
        ResetEncounter();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            GridPos? gridTarget = WorldToGrid(GetGlobalMousePosition());

            if (gridTarget.HasValue)
            {
                _selectedTarget = gridTarget.Value;
                PreviewSelectedWorking();
                UpdateStatus();
                QueueRedraw();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (inputEvent is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        Direction? direction = key.Keycode switch
        {
            Key.Left or Key.A => Direction.West,
            Key.Right or Key.D => Direction.East,
            Key.Up or Key.W => Direction.North,
            Key.Down or Key.S => Direction.South,
            _ => null
        };

        if (direction.HasValue)
        {
            TryPlayerStepOrAttack(direction.Value);
            GetViewport().SetInputAsHandled();
            return;
        }

        switch (key.Keycode)
        {
            case Key.Space:
                WaitPlayerTurn();
                GetViewport().SetInputAsHandled();
                break;
            case Key.F:
                CastSelectedWorking();
                GetViewport().SetInputAsHandled();
                break;
            case Key.P:
                PreviewSelectedWorking();
                GetViewport().SetInputAsHandled();
                break;
            case Key.E:
                SetSpellEditorOpen(!_isSpellEditorOpen);
                GetViewport().SetInputAsHandled();
                break;
            case Key.Escape:
                if (_isSpellEditorOpen)
                {
                    SetSpellEditorOpen(false);
                    GetViewport().SetInputAsHandled();
                }
                break;
            case Key.Tab:
                SelectWorking((_selectedWorkingIndex + 1) % _preparedWorkings.Length);
                GetViewport().SetInputAsHandled();
                break;
            case Key.R:
                ResetEncounter();
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    public override void _Draw()
    {
        if (_encounter == null)
        {
            return;
        }

        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                var position = new GridPos(x, y);
                Rect2 rect = GetTileRect(position);
                Color fill = _encounter.Grid.GetTile(position) switch
                {
                    TileState.Wall => new Color(0.24f, 0.24f, 0.28f),
                    TileState.RaisedStone => new Color(0.45f, 0.45f, 0.5f),
                    _ => new Color(0.10f, 0.11f, 0.13f)
                };

                DrawRect(rect, fill);
                DrawRect(rect, new Color(0.30f, 0.33f, 0.38f), filled: false, width: 1.0f);
            }
        }

        if (_encounter.Grid.IsInside(_selectedTarget))
        {
            DrawRect(GetTileRect(_selectedTarget).Grow(-3), new Color(1.0f, 0.84f, 0.30f), filled: false, width: 3.0f);
        }

        foreach (TileCondition condition in _encounter.TileConditions.Where(condition => condition.ConditionId == "condition.marked"))
        {
            Color color = condition.OwnerActorId == _encounter.Player.Id
                ? new Color(0.95f, 0.62f, 1.0f, 0.65f)
                : new Color(1.0f, 0.46f, 0.18f, 0.65f);
            DrawCircle(GridToWorldCenter(condition.Position), TileSize * 0.16f, color);
        }

        foreach (EncounterActor actor in _encounter.Actors.Where(actor => actor.IsAlive))
        {
            if (_encounter.HasActorCondition(actor.Id, "condition.marked", _encounter.Player.Id))
            {
                DrawRect(GetTileRect(actor.Position).Grow(-6), new Color(0.95f, 0.62f, 1.0f), filled: false, width: 2.0f);
            }
        }

        DrawPreviewOverlay();
    }

    private void ResetEncounter()
    {
        foreach (CharacterBody2D visual in _actorVisuals.Values)
        {
            visual.QueueFree();
        }

        _actorVisuals.Clear();

        _encounter = new TacticalEncounter(
            GridWidth,
            GridHeight,
            GridPos.FromVector2I(PlayerStart));

        foreach (GridPos wall in DemoWalls)
        {
            if (_encounter.Grid.IsInside(wall))
            {
                _encounter.Grid.SetTile(wall, TileState.Wall);
            }
        }

        _encounter.AddEnemy("enemy.glass_hound", GridPos.FromVector2I(GlassHoundStart));
        _encounter.AddEnemy("enemy.ash_scribe", GridPos.FromVector2I(AshScribeStart));
        _encounter.AddEnemy("enemy.root_saint", GridPos.FromVector2I(RootSaintStart));

        LogDebug($"Reset room with local rule: {_encounter.FloorRules.DisplayName}");
        SyncActorVisuals();
        UpdateStatus();
        PreviewSelectedWorking();
        QueueRedraw();
    }

    private void BuildSpellUi()
    {
        CanvasLayer? canvas = GetNodeOrNull<CanvasLayer>("CanvasLayer");

        if (canvas == null)
        {
            canvas = new CanvasLayer { Name = "CanvasLayer" };
            AddChild(canvas);
        }

        BuildCommandBar(canvas);

        _spellPanel = new PanelContainer
        {
            Name = "SpellPage",
            Position = Vector2.Zero,
            Size = new Vector2(1280, 720),
            CustomMinimumSize = new Vector2(1280, 720),
            Visible = false
        };
        canvas.AddChild(_spellPanel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        _spellPanel.AddChild(margin);

        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        margin.AddChild(root);

        var titleRow = new HBoxContainer();
        root.AddChild(titleRow);

        var pageTitle = new Label
        {
            Text = GameStrings.Get("ui.editor.page_title"),
            CustomMinimumSize = new Vector2(300, 0)
        };
        pageTitle.AddThemeFontSizeOverride("font_size", 22);
        titleRow.AddChild(pageTitle);

        _workingTitleLabel = new Label
        {
            Text = GameStrings.Get("ui.editor.working_title"),
            HorizontalAlignment = HorizontalAlignment.Left,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _workingTitleLabel.AddThemeFontSizeOverride("font_size", 18);
        titleRow.AddChild(_workingTitleLabel);

        var headerPreviewButton = new Button
        {
            Text = GameStrings.Get("ui.editor.preview"),
            CustomMinimumSize = new Vector2(92, 30)
        };
        headerPreviewButton.Pressed += PreviewSelectedWorking;
        titleRow.AddChild(headerPreviewButton);

        var headerCastButton = new Button
        {
            Text = GameStrings.Get("ui.editor.cast"),
            CustomMinimumSize = new Vector2(78, 30)
        };
        headerCastButton.Pressed += CastSelectedWorking;
        titleRow.AddChild(headerCastButton);

        var closeButton = new Button
        {
            Text = GameStrings.Get("ui.editor.hide"),
            CustomMinimumSize = new Vector2(72, 30)
        };
        closeButton.Pressed += () => SetSpellEditorOpen(false);
        titleRow.AddChild(closeButton);

        var body = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        root.AddChild(body);

        var codexColumn = BuildCodexColumn();
        body.AddChild(codexColumn);

        var editorColumn = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        body.AddChild(editorColumn);

        var slotRow = new HBoxContainer();
        editorColumn.AddChild(slotRow);

        _slotButtons = new Button[_preparedWorkings.Length];
        for (int i = 0; i < _preparedWorkings.Length; i++)
        {
            int slotIndex = i;
            var button = new Button
            {
                Text = $"{GameStrings.Get("ui.editor.slot")} {i + 1}",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            button.Pressed += () => SelectWorking(slotIndex);
            _slotButtons[i] = button;
            slotRow.AddChild(button);
        }

        var actionRow = new HBoxContainer();
        editorColumn.AddChild(actionRow);

        var previewButton = new Button
        {
            Text = GameStrings.Get("ui.editor.preview"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        previewButton.Pressed += PreviewSelectedWorking;
        actionRow.AddChild(previewButton);

        var castButton = new Button
        {
            Text = GameStrings.Get("ui.editor.cast"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        castButton.Pressed += CastSelectedWorking;
        actionRow.AddChild(castButton);

        var sampleButton = new Button
        {
            Text = GameStrings.Get("ui.editor.sample"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        sampleButton.Pressed += ResetSelectedWorkingToSample;
        actionRow.AddChild(sampleButton);

        AddTraceStepControls(actionRow);

        _spellEditor = new SpellGraphEditorControl
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(850, 500)
        };
        _spellEditor.GraphChanged += OnGraphChanged;
        editorColumn.AddChild(_spellEditor);

        _traceLabel = new Label
        {
            Text = "",
            CustomMinimumSize = new Vector2(0, 120),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _traceLabel.AddThemeFontSizeOverride("font_size", 12);
        editorColumn.AddChild(_traceLabel);

        SelectWorking(0);
        SetSpellEditorOpen(false);
    }

    private Control BuildCodexColumn()
    {
        var codexPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(340, 0),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        codexPanel.AddChild(margin);

        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        margin.AddChild(root);

        var heading = new Label
        {
            Text = GameStrings.Get("ui.codex.title")
        };
        heading.AddThemeFontSizeOverride("font_size", 18);
        root.AddChild(heading);

        var help = new Label
        {
            Text = GameStrings.Get("ui.codex.help"),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        help.AddThemeFontSizeOverride("font_size", 12);
        root.AddChild(help);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        root.AddChild(scroll);

        var list = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        scroll.AddChild(list);

        foreach (ClauseDefinition definition in ClauseDefinitionCatalog.All
                     .OrderBy(definition => definition.Family)
                     .ThenBy(definition => definition.DisplayName))
        {
            list.AddChild(CreateCodexEntry(definition));
        }

        _codexDetailLabel = new Label
        {
            Text = "",
            CustomMinimumSize = new Vector2(0, 96),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _codexDetailLabel.AddThemeFontSizeOverride("font_size", 12);
        root.AddChild(_codexDetailLabel);

        ShowClauseCodex(ClauseDefinitionCatalog.All.First());
        return codexPanel;
    }

    private Control CreateCodexEntry(ClauseDefinition definition)
    {
        var entry = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 116)
        };

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        entry.AddChild(margin);

        var root = new VBoxContainer();
        margin.AddChild(root);

        var topRow = new HBoxContainer();
        root.AddChild(topRow);

        var nameButton = new Button
        {
            Text = definition.DisplayName,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText = definition.Tooltip
        };
        nameButton.Pressed += () => ShowClauseCodex(definition);
        topRow.AddChild(nameButton);

        var addButton = new Button
        {
            Text = GameStrings.Get("ui.codex.add"),
            CustomMinimumSize = new Vector2(56, 28)
        };
        string clauseId = definition.Id;
        addButton.Pressed += () =>
        {
            _spellEditor?.AddClauseNode(clauseId);
            ShowClauseCodex(definition);
        };
        topRow.AddChild(addButton);

        var playerText = new Label
        {
            Text = definition.PlayerText,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        playerText.AddThemeFontSizeOverride("font_size", 12);
        root.AddChild(playerText);

        var detail = new Label
        {
            Text = $"{definition.Family} | counters {definition.CounterSummary}\n{definition.Tooltip}",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        detail.AddThemeFontSizeOverride("font_size", 11);
        root.AddChild(detail);

        return entry;
    }

    private void ShowClauseCodex(ClauseDefinition definition)
    {
        if (_codexDetailLabel == null)
        {
            return;
        }

        string flowText = definition.IsCondition
            ? GameStrings.Get("ui.codex.flow.condition")
            : GameStrings.Get("ui.codex.flow.next");

        _codexDetailLabel.Text =
            $"{definition.DisplayName}\n" +
            $"{definition.PlayerText}\n\n" +
            $"{definition.Tooltip}\n\n" +
            $"{flowText}";
    }

    private void BuildCommandBar(CanvasLayer canvas)
    {
        var commandPanel = new PanelContainer
        {
            Name = "CommandPanel",
            Position = new Vector2(16, 528),
            Size = new Vector2(488, 176),
            CustomMinimumSize = new Vector2(488, 176)
        };
        canvas.AddChild(commandPanel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        commandPanel.AddChild(margin);

        var root = new VBoxContainer();
        margin.AddChild(root);

        var row = new HBoxContainer();
        root.AddChild(row);

        _editorToggleButton = new Button
        {
            Text = GameStrings.Get("ui.editor.open"),
            CustomMinimumSize = new Vector2(116, 32)
        };
        _editorToggleButton.Pressed += () => SetSpellEditorOpen(!_isSpellEditorOpen);
        row.AddChild(_editorToggleButton);

        for (int i = 0; i < _preparedWorkings.Length; i++)
        {
            int slotIndex = i;
            var button = new Button
            {
                Text = $"{GameStrings.Get("ui.editor.slot")} {i + 1}",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            button.Pressed += () => SelectWorking(slotIndex);
            row.AddChild(button);
        }

        var previewButton = new Button
        {
            Text = GameStrings.Get("ui.editor.preview"),
            CustomMinimumSize = new Vector2(88, 32)
        };
        previewButton.Pressed += PreviewSelectedWorking;
        row.AddChild(previewButton);

        var castButton = new Button
        {
            Text = GameStrings.Get("ui.editor.cast"),
            CustomMinimumSize = new Vector2(72, 32)
        };
        castButton.Pressed += CastSelectedWorking;
        row.AddChild(castButton);

        AddTraceStepControls(row);

        var hintLabel = new Label
        {
            Text = GameStrings.Get("ui.play.hint"),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        hintLabel.AddThemeFontSizeOverride("font_size", 12);
        root.AddChild(hintLabel);

        _quickTraceLabel = new Label
        {
            Text = GameStrings.Get("ui.play.preview_empty"),
            CustomMinimumSize = new Vector2(0, 80),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _quickTraceLabel.AddThemeFontSizeOverride("font_size", 12);
        root.AddChild(_quickTraceLabel);
    }

    private void AddTraceStepControls(HBoxContainer row)
    {
        row.AddChild(CreateTraceButton("<", "Show the previous omen trace step.", () => StepTrace(-1)));
        row.AddChild(CreateTraceButton(">", "Show the next omen trace step.", () => StepTrace(1)));
        row.AddChild(CreateTraceButton("All", "Show the full omen trace.", ShowFullTrace));
    }

    private static Button CreateTraceButton(string text, string tooltip, Action pressed)
    {
        var button = new Button
        {
            Text = text,
            TooltipText = tooltip,
            CustomMinimumSize = new Vector2(36, 30)
        };
        button.Pressed += pressed;
        return button;
    }

    private void SetSpellEditorOpen(bool isOpen)
    {
        _isSpellEditorOpen = isOpen;

        if (_spellPanel != null)
        {
            _spellPanel.Visible = isOpen;
            _spellPanel.MouseFilter = isOpen
                ? Control.MouseFilterEnum.Stop
                : Control.MouseFilterEnum.Ignore;
        }

        if (_editorToggleButton != null)
        {
            _editorToggleButton.Text = isOpen
                ? GameStrings.Get("ui.editor.hide_editor")
                : GameStrings.Get("ui.editor.open");
        }
    }

    private void SelectWorking(int index)
    {
        _selectedWorkingIndex = Mathf.PosMod(index, _preparedWorkings.Length);
        _spellEditor?.SetWorking(_preparedWorkings[_selectedWorkingIndex]);
        RefreshWorkingUi();
        PreviewSelectedWorking();
    }

    private void ResetSelectedWorkingToSample()
    {
        _preparedWorkings[_selectedWorkingIndex] = _selectedWorkingIndex == 0
            ? WorkingSamples.CreateMarkOrDamage()
            : WorkingSamples.CreateEmergencyWall();

        _spellEditor?.SetWorking(_preparedWorkings[_selectedWorkingIndex]);
        RefreshWorkingUi();
        PreviewSelectedWorking();
    }

    private void OnGraphChanged()
    {
        RefreshWorkingUi();
        PreviewSelectedWorking();
    }

    private void RefreshWorkingUi()
    {
        Working working = _preparedWorkings[_selectedWorkingIndex];

        if (_workingTitleLabel != null)
        {
            _workingTitleLabel.Text = $"{working.DisplayName}    Counters {working.EstimatedCounterSummary}";
        }

        for (int i = 0; i < _slotButtons.Length; i++)
        {
            _slotButtons[i].Text = i == _selectedWorkingIndex
                ? $"> {GameStrings.Get("ui.editor.slot")} {i + 1}: {_preparedWorkings[i].DisplayName}"
                : $"{GameStrings.Get("ui.editor.slot")} {i + 1}: {_preparedWorkings[i].DisplayName}";
        }
    }

    private void PreviewSelectedWorking()
    {
        if (_encounter == null)
        {
            return;
        }

        _preview = _encounter.PreviewWorkingDetailed(_preparedWorkings[_selectedWorkingIndex], _selectedTarget);
        WorkingResult result = _preview.Result;
        LogDebug($"Previewed {_preparedWorkings[_selectedWorkingIndex].DisplayName} at {_selectedTarget}: {result.Succeeded}");
        ShowTrace(GameStrings.Get("ui.editor.preview"), result);
        QueueRedraw();
    }

    private void CastSelectedWorking()
    {
        if (_encounter == null)
        {
            return;
        }

        _preview = null;
        WorkingResult result = _encounter.TryCastWorking(_preparedWorkings[_selectedWorkingIndex], _selectedTarget);
        LogDebug($"Cast {_preparedWorkings[_selectedWorkingIndex].DisplayName} at {_selectedTarget}: {result.Succeeded}");
        ShowTrace(GameStrings.Get("ui.editor.cast"), result);

        if (_encounter.Turns.Phase == TurnPhase.EnemyTurn)
        {
            _encounter.RunEnemyTurn();
        }

        SyncActorVisuals();
        UpdateStatus();
        QueueRedraw();
    }

    private void ShowTrace(string label, WorkingResult result)
    {
        _lastTraceActionLabel = label;
        _lastTraceResult = result;
        _visibleTraceSteps = result.Trace.Events.Count;
        RenderTrace();
    }

    private void StepTrace(int delta)
    {
        if (_lastTraceResult == null || _lastTraceResult.Trace.Events.Count == 0)
        {
            return;
        }

        int total = _lastTraceResult.Trace.Events.Count;
        int current = _visibleTraceSteps == int.MaxValue ? total : _visibleTraceSteps;
        _visibleTraceSteps = Math.Clamp(current + delta, 1, total);
        RenderTrace();
    }

    private void ShowFullTrace()
    {
        if (_lastTraceResult == null)
        {
            return;
        }

        _visibleTraceSteps = _lastTraceResult.Trace.Events.Count;
        RenderTrace();
    }

    private void RenderTrace()
    {
        if (_lastTraceResult == null)
        {
            return;
        }

        WorkingResult result = _lastTraceResult;
        int totalSteps = result.Trace.Events.Count;
        int visibleSteps = totalSteps == 0
            ? 0
            : Math.Clamp(_visibleTraceSteps, 1, totalSteps);
        string status = result.Succeeded ? "ok" : $"failed: {result.FailureReason}";
        string traceText =
            $"{_lastTraceActionLabel}: {status} | trace {visibleSteps}/{totalSteps} | counters {result.CounterSummary} | cost {result.CostAdjustmentSummary}\n" +
            BuildTraceDisplayText(result.Trace, visibleSteps);

        if (_traceLabel != null)
        {
            _traceLabel.Text = traceText;
        }

        if (_quickTraceLabel != null)
        {
            _quickTraceLabel.Text = CollapseTraceForHud(traceText);
        }
    }

    private static string BuildTraceDisplayText(OmenTrace trace, int visibleSteps)
    {
        string traceText = string.Join(
            "\n",
            trace.Events
                .Take(visibleSteps)
                .Select(traceEvent => $"{traceEvent.Step}. {traceEvent.Text}"));

        if (visibleSteps < trace.Events.Count)
        {
            traceText += $"\n... {trace.Events.Count - visibleSteps} more";
        }

        return traceText;
    }

    private void DrawPreviewOverlay()
    {
        if (_encounter == null || _preview == null)
        {
            return;
        }

        TacticalEncounter forecast = _preview.Encounter;

        DrawPreviewTileChanges(forecast);
        DrawPreviewTileMarks(forecast);
        DrawPreviewActorChanges(forecast);

        if (!_preview.Result.Succeeded && _encounter.Grid.IsInside(_selectedTarget))
        {
            DrawRect(GetTileRect(_selectedTarget).Grow(-7), new Color(1.0f, 0.25f, 0.20f, 0.95f), filled: false, width: 4.0f);
        }
    }

    private void DrawPreviewTileChanges(TacticalEncounter forecast)
    {
        if (_encounter == null)
        {
            return;
        }

        int width = Math.Min(_encounter.Grid.Width, forecast.Grid.Width);
        int height = Math.Min(_encounter.Grid.Height, forecast.Grid.Height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var position = new GridPos(x, y);
                TileState current = _encounter.Grid.GetTile(position);
                TileState predicted = forecast.Grid.GetTile(position);

                if (current == predicted)
                {
                    continue;
                }

                Color color = predicted switch
                {
                    TileState.RaisedStone => new Color(0.62f, 0.80f, 1.0f, 0.95f),
                    TileState.Floor => new Color(1.0f, 0.62f, 0.30f, 0.95f),
                    _ => new Color(0.90f, 0.90f, 0.95f, 0.95f)
                };

                DrawRect(GetTileRect(position).Grow(-5), color, filled: false, width: 4.0f);
            }
        }
    }

    private void DrawPreviewTileMarks(TacticalEncounter forecast)
    {
        if (_encounter == null)
        {
            return;
        }

        foreach (TileCondition condition in forecast.TileConditions.Where(condition =>
                     condition.ConditionId == "condition.marked"
                     && !_encounter.HasTileCondition(condition.Position, condition.ConditionId, condition.OwnerActorId)))
        {
            DrawCircle(GridToWorldCenter(condition.Position), TileSize * 0.24f, new Color(0.95f, 0.62f, 1.0f, 0.36f));
            DrawRect(GetTileRect(condition.Position).Grow(-10), new Color(0.95f, 0.62f, 1.0f, 0.95f), filled: false, width: 3.0f);
        }
    }

    private void DrawPreviewActorChanges(TacticalEncounter forecast)
    {
        if (_encounter == null)
        {
            return;
        }

        foreach (EncounterActor actor in _encounter.Actors.Where(actor => actor.IsAlive))
        {
            EncounterActor? predicted = forecast.GetActor(actor.Id);

            if (predicted == null)
            {
                continue;
            }

            if (predicted.IsAlive && predicted.Position != actor.Position)
            {
                DrawLine(
                    GridToWorldCenter(actor.Position),
                    GridToWorldCenter(predicted.Position),
                    new Color(0.30f, 0.92f, 1.0f, 0.90f),
                    width: 4.0f);
                DrawRect(GetTileRect(predicted.Position).Grow(-8), new Color(0.30f, 0.92f, 1.0f, 0.95f), filled: false, width: 3.0f);
            }

            if (predicted.Health < actor.Health || !predicted.IsAlive)
            {
                GridPos damagePosition = predicted.IsAlive ? predicted.Position : actor.Position;
                DrawRect(GetTileRect(damagePosition).Grow(-4), new Color(1.0f, 0.20f, 0.26f, 0.95f), filled: false, width: 4.0f);
            }

            bool markedNow = _encounter.HasActorCondition(actor.Id, "condition.marked", _encounter.Player.Id);
            bool markedPredicted = forecast.HasActorCondition(actor.Id, "condition.marked", forecast.Player.Id);

            if (!markedNow && markedPredicted)
            {
                GridPos markPosition = predicted.IsAlive ? predicted.Position : actor.Position;
                DrawRect(GetTileRect(markPosition).Grow(-9), new Color(0.95f, 0.62f, 1.0f, 0.95f), filled: false, width: 3.0f);
            }
        }
    }

    private static string CollapseTraceForHud(string traceText)
    {
        string[] lines = traceText
            .Split('\n')
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Take(5)
            .ToArray();

        return string.Join("\n", lines);
    }

    private void TryPlayerStepOrAttack(Direction direction)
    {
        if (_encounter == null || !_encounter.TryPlayerStepOrAttack(direction))
        {
            return;
        }

        LogDebug($"Player moved or attacked {direction}.");
        ResolveEnemyTurn();
    }

    private void WaitPlayerTurn()
    {
        if (_encounter == null)
        {
            return;
        }

        _encounter.WaitPlayerTurn();
        LogDebug("Player waited.");
        ResolveEnemyTurn();
    }

    private void ResolveEnemyTurn()
    {
        if (_encounter == null)
        {
            return;
        }

        _encounter.RunEnemyTurn();
        SyncActorVisuals();
        UpdateStatus();
        PreviewSelectedWorking();
        QueueRedraw();
    }

    private void SyncActorVisuals()
    {
        if (_encounter == null)
        {
            return;
        }

        HashSet<int> activeActorIds = _encounter.Actors
            .Where(actor => actor.IsAlive)
            .Select(actor => actor.Id)
            .ToHashSet();

        foreach ((int actorId, CharacterBody2D visual) in _actorVisuals.ToArray())
        {
            if (!activeActorIds.Contains(actorId))
            {
                visual.QueueFree();
                _actorVisuals.Remove(actorId);
            }
        }

        foreach (EncounterActor actor in _encounter.Actors.Where(actor => actor.IsAlive))
        {
            if (!_actorVisuals.TryGetValue(actor.Id, out CharacterBody2D? visual))
            {
                visual = CreateActorVisual(actor);
                _actorVisuals.Add(actor.Id, visual);
            }

            visual.Position = GridToWorldCenter(actor.Position);
            UpdateActorVisual(actor, visual);
        }
    }

    private CharacterBody2D CreateActorVisual(EncounterActor actor)
    {
        var body = new CharacterBody2D
        {
            Name = actor.Faction == Faction.Player ? "Player" : $"Enemy{actor.Id}"
        };

        float halfSize = TileSize * 0.33f;
        var square = new Polygon2D
        {
            Name = "Body",
            Color = GetActorColor(actor),
            Polygon =
            [
                new Vector2(-halfSize, -halfSize),
                new Vector2(halfSize, -halfSize),
                new Vector2(halfSize, halfSize),
                new Vector2(-halfSize, halfSize)
            ]
        };

        var label = new Label
        {
            Name = "GlyphLabel",
            Size = new Vector2(TileSize, TileSize),
            Position = new Vector2(-TileSize / 2.0f, -TileSize / 2.0f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        body.AddChild(square);
        body.AddChild(label);
        AddChild(body);

        return body;
    }

    private void UpdateActorVisual(EncounterActor actor, CharacterBody2D visual)
    {
        Label? label = visual.GetNodeOrNull<Label>("GlyphLabel");

        if (label == null || _encounter == null)
        {
            return;
        }

        bool marked = _encounter.HasActorCondition(actor.Id, "condition.marked", _encounter.Player.Id);
        string sigil = GetActorSigil(actor);
        string countersText = string.Join(
            ", ",
            actor.Counters.All
                .Where(counter => !counter.Key.StartsWith("condition."))
                .OrderBy(counter => counter.Key)
                .Select(counter => $"{counter.Key} {counter.Value}"));
        string counters = countersText.Length == 0 ? "" : $" {countersText}";
        label.Text = marked ? $"{sigil}\nM{counters}" : $"{sigil}\n{actor.Health}{counters}";
    }

    private static Color GetActorColor(EncounterActor actor)
    {
        if (actor.Faction == Faction.Player)
        {
            return new Color(0.32f, 0.72f, 0.95f);
        }

        return actor.EnemyId != null && EnemyConfigCatalog.TryGet(actor.EnemyId, out EnemyConfig? config)
            ? config.Tint
            : new Color(0.90f, 0.35f, 0.32f);
    }

    private static string GetActorSigil(EncounterActor actor)
    {
        if (actor.Faction == Faction.Player)
        {
            return GameStrings.Get("actors.player.sigil");
        }

        return actor.EnemyId != null && EnemyConfigCatalog.TryGet(actor.EnemyId, out EnemyConfig? config)
            ? config.Sigil
            : GameStrings.Get("enemies.generic.sigil");
    }

    private void UpdateStatus()
    {
        if (_statusLabel == null || _encounter == null)
        {
            return;
        }

        string resultText = _encounter.Result switch
        {
            GameResult.PlayerWon => "Victory. Press R to restart.",
            GameResult.PlayerLost => "Defeat. Press R to restart.",
            _ => $"Round {_encounter.Turns.Round} - {_encounter.Turns.Phase}"
        };

        string enemyHealth = string.Join(
            ", ",
            _encounter.Enemies.Select(enemy => $"{GetActorSigil(enemy)}{enemy.Id}: {enemy.Health}"));

        string enemyIntents = string.Join(
            "\n",
            _encounter.Enemies.Select(enemy =>
                $"{GetActorSigil(enemy)}{enemy.Id} {_encounter.GetEnemyDisplayName(enemy)}: {_encounter.GetEnemyIntent(enemy)}"));

        if (string.IsNullOrWhiteSpace(enemyHealth))
        {
            enemyHealth = "none";
        }

        _statusLabel.Text =
            $"{resultText}\n" +
            $"Player HP: {_encounter.Player.Health}\n" +
            $"Enemy HP: {enemyHealth}\n" +
            $"Rule: {_encounter.FloorRules.DisplayName}\n" +
            $"Target: {_selectedTarget}\n" +
            $"{enemyIntents}\n" +
            "WASD/arrows move | click target | F cast | P preview | E editor | R restart";
    }

    private static void LogDebug(string message)
    {
        GD.Print($"[Prototype] {message}");
    }

    private Rect2 GetTileRect(GridPos position)
    {
        return new Rect2(
            GridOrigin + new Vector2(position.X * TileSize, position.Y * TileSize),
            new Vector2(TileSize, TileSize));
    }

    private Vector2 GridToWorldCenter(GridPos position)
    {
        return GridOrigin
            + new Vector2(position.X * TileSize, position.Y * TileSize)
            + new Vector2(TileSize / 2.0f, TileSize / 2.0f);
    }

    private GridPos? WorldToGrid(Vector2 worldPosition)
    {
        Vector2 local = worldPosition - GridOrigin;

        if (local.X < 0 || local.Y < 0)
        {
            return null;
        }

        var gridPosition = new GridPos(
            Mathf.FloorToInt(local.X / TileSize),
            Mathf.FloorToInt(local.Y / TileSize));

        return _encounter?.Grid.IsInside(gridPosition) == true ? gridPosition : null;
    }
}
