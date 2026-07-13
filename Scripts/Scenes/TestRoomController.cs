using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TheThingImDoing.Actors;
using TheThingImDoing.Content;
using TheThingImDoing.Core;
using TheThingImDoing.Progression;
using TheThingImDoing.Spells;
using TheThingImDoing.UI;
using TheThingImDoing.World;

public partial class TestRoomController : Node2D
{
    private static readonly string[] StartingClauseIds =
    {
        "clause.aim_at_target",
        "clause.aim_at_nearest_foe",
        "clause.if_marked",
        "clause.if_occupied",
        "clause.if_clear",
        "clause.mark_them",
        "clause.damage_them",
        "clause.raise_stone",
        "clause.push_them"
    };

    private readonly Dictionary<int, CharacterBody2D> _actorVisuals = new();
    private readonly HashSet<GridPos> _discoveredTiles = new();
    private readonly Dictionary<string, Button> _codexAddButtons = new(StringComparer.Ordinal);
    private readonly Working[] _preparedWorkings =
    {
        WorkingSamples.CreateMarkOrDamage(),
        WorkingSamples.CreateEmergencyWall()
    };

    private TacticalEncounter? _encounter;
    private EncounterDefinition? _encounterDefinition;
    private EnvironmentDefinition? _environmentDefinition;
    private GameRunSession? _runSession;
    private Theme? _gameTheme;
    private Camera2D? _camera;
    private Label? _statusLabel;
    private Label? _arenaTitleLabel;
    private Label? _arenaSubtitleLabel;
    private Label? _combatWorkingLabel;
    private Label? _workingTitleLabel;
    private Label? _traceLabel;
    private Label? _quickTraceLabel;
    private Label? _codexDetailLabel;
    private Label? _rewardTitleLabel;
    private Label? _rewardDetailLabel;
    private PanelContainer? _spellPanel;
    private PanelContainer? _rewardPanel;
    private PanelContainer? _helpPanel;
    private ColorRect? _rewardBlocker;
    private ColorRect? _helpBlocker;
    private Button? _editorToggleButton;
    private Button? _combatCastButton;
    private Button? _editorCastButton;
    private SpellGraphEditorControl? _spellEditor;
    private Button[] _slotButtons = [];
    private Button[] _combatSlotButtons = [];
    private VBoxContainer? _rewardChoices;
    private WorkingPreview? _preview;
    private RunPlayerState? _playerState;
    private string _lastTraceActionLabel = "";
    private WorkingResult? _lastTraceResult;
    private int _visibleTraceSteps = int.MaxValue;
    private bool _isSpellEditorOpen;
    private bool _isHoverPreviewActive;
    private bool _encounterResolutionShown;
    private int _selectedWorkingIndex;
    private int _previewWorkingIndex;
    private GridPos _selectedTarget = new(5, 1);

    [Export(PropertyHint.Range, "16,96,1")] public int TileSize { get; set; } = 48;
    [Export] public Vector2 GridOrigin { get; set; } = new(64, 96);

    public override void _Ready()
    {
        _gameTheme = GameTheme.Create();
        _camera = GetNodeOrNull<Camera2D>("Camera2D");
        if (_camera != null)
        {
            _camera.PositionSmoothingEnabled = true;
            _camera.PositionSmoothingSpeed = 10.0f;
        }
        _playerState = new RunPlayerState(maxHealth: 5, StartingClauseIds);
        _statusLabel = GetNodeOrNull<Label>("CanvasLayer/StatusLabel");

        if (_statusLabel != null)
        {
            _statusLabel.Theme = _gameTheme;
            _statusLabel.Position = new Vector2(16, 16);
            _statusLabel.Size = new Vector2(416, 244);
            _statusLabel.MouseFilter = Control.MouseFilterEnum.Stop;
            _statusLabel.AddThemeFontSizeOverride("font_size", 13);
            _statusLabel.AddThemeStyleboxOverride(
                "normal",
                GameTheme.Box(new Color(GameTheme.Surface, 0.96f), GameTheme.Border, 1, 7));
        }

        BuildSpellUi();
        StartNewRun();
        ShowHelp();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (_rewardBlocker?.Visible == true)
        {
            if (inputEvent is InputEventKey { Keycode: Key.R, Pressed: true, Echo: false })
            {
                StartNewRun();
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (_helpBlocker?.Visible == true)
        {
            if (inputEvent is InputEventKey
                {
                    Pressed: true,
                    Echo: false,
                    Keycode: Key.Enter or Key.KpEnter or Key.Escape
                })
            {
                HideHelp();
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (_isSpellEditorOpen)
        {
            if (inputEvent is InputEventKey
                {
                    Pressed: true,
                    Echo: false,
                    Keycode: Key.E or Key.Escape
                })
            {
                SetSpellEditorOpen(false);
                GetViewport().SetInputAsHandled();
            }

            return;
        }

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
                StartNewRun();
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

        Color floorColor = _environmentDefinition != null
            ? Color.FromHtml(_environmentDefinition.BackgroundColorHex)
            : new Color(0.10f, 0.11f, 0.13f);
        Color gridColor = _environmentDefinition != null
            ? Color.FromHtml(_environmentDefinition.GridColorHex)
            : new Color(0.30f, 0.33f, 0.38f);
        Vector2 gridSize = new(_encounter.Grid.Width * TileSize, _encounter.Grid.Height * TileSize);
        DrawRect(
            new Rect2(GridOrigin - new Vector2(2048, 2048), gridSize + new Vector2(4096, 4096)),
            floorColor.Darkened(0.64f));
        DrawRect(
            new Rect2(
                GridOrigin - new Vector2(10, 10),
                new Vector2(_encounter.Grid.Width * TileSize + 20, _encounter.Grid.Height * TileSize + 20)),
            new Color(0, 0, 0, 0.58f));

        for (int y = 0; y < _encounter.Grid.Height; y++)
        {
            for (int x = 0; x < _encounter.Grid.Width; x++)
            {
                var position = new GridPos(x, y);
                Rect2 rect = GetTileRect(position);
                bool visible = IsTileVisible(position);
                bool discovered = _discoveredTiles.Contains(position);
                Color fill = _encounter.Grid.GetTile(position) switch
                {
                    TileState.Wall => floorColor.Lightened(0.18f),
                    TileState.RaisedStone => gridColor.Lightened(0.12f),
                    _ => floorColor
                };

                if (!discovered)
                {
                    fill = new Color(0.015f, 0.018f, 0.025f);
                }
                else if (!visible)
                {
                    fill = fill.Darkened(0.62f);
                }

                DrawRect(rect, fill);
                DrawRect(
                    rect,
                    discovered ? gridColor.Darkened(visible ? 0.0f : 0.65f) : fill,
                    filled: false,
                    width: 1.0f);
            }
        }

        DrawTargetingOverlay();

        foreach (TileCondition condition in _encounter.TileConditions.Where(condition =>
                     condition.ConditionId == "condition.marked" && IsTileVisible(condition.Position)))
        {
            Color color = condition.OwnerActorId == _encounter.Player.Id
                ? new Color(0.95f, 0.62f, 1.0f, 0.65f)
                : new Color(1.0f, 0.46f, 0.18f, 0.65f);
            DrawCircle(GridToWorldCenter(condition.Position), TileSize * 0.16f, color);
        }

        foreach (EncounterActor actor in _encounter.Actors.Where(actor =>
                     actor.IsAlive && (actor.Faction == Faction.Player || IsTileVisible(actor.Position))))
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
        if (_runSession?.CurrentEncounter == null || _playerState == null)
        {
            return;
        }

        foreach (CharacterBody2D visual in _actorVisuals.Values)
        {
            visual.QueueFree();
        }

        _actorVisuals.Clear();
        _encounterDefinition = _runSession.CurrentEncounter;
        _environmentDefinition = EnvironmentDefinitionCatalog.Get(_encounterDefinition.EnvironmentId);
        _encounter = TacticalEncounterFactory.Create(
            _encounterDefinition,
            _playerState.CurrentHealth,
            _playerState.MaxHealth,
            _runSession.CurrentEncounterSeed);

        foreach (string relicId in _playerState.RelicIds)
        {
            _encounter.AddRelic(relicId);
        }

        _encounterResolutionShown = false;
        _discoveredTiles.Clear();
        EncounterActor? initiallyVisibleEnemy = _encounter.Enemies
            .FirstOrDefault(enemy => IsTileVisible(enemy.Position));
        _selectedTarget = initiallyVisibleEnemy?.Position ?? _encounter.Player.Position;
        _previewWorkingIndex = _selectedWorkingIndex;
        LogDebug(
            $"Entered {_encounterDefinition.DisplayName} in {_environmentDefinition.DisplayName} " +
            $"with local rule {_encounter.FloorRules.DisplayName}.");
        SyncActorVisuals();
        _camera?.ResetSmoothing();
        UpdateStatus();
        RefreshWorkingUi();
        PreviewSelectedWorking();
        QueueRedraw();
    }

    private void StartNewRun()
    {
        _playerState = new RunPlayerState(
            maxHealth: 5,
            StartingClauseIds);
        _runSession = new GameRunSession("run.ashen_archive", CreateRunSeed());
        _preparedWorkings[0] = WorkingSamples.CreateMarkOrDamage();
        _preparedWorkings[1] = WorkingSamples.CreateEmergencyWall();
        _selectedWorkingIndex = 0;
        _spellEditor?.SetWorking(_preparedWorkings[0]);

        if (_rewardPanel != null)
        {
            _rewardPanel.Visible = false;
        }

        if (_rewardBlocker != null)
        {
            _rewardBlocker.Visible = false;
        }

        RefreshCodexAvailability();
        ResetEncounter();
    }

    private void BuildSpellUi()
    {
        CanvasLayer? canvas = GetNodeOrNull<CanvasLayer>("CanvasLayer");

        if (canvas == null)
        {
            canvas = new CanvasLayer { Name = "CanvasLayer" };
            AddChild(canvas);
        }

        // Keep the fixed HUD above the world while camera movement rebuilds the world draw list.
        canvas.Layer = 100;

        BuildArenaHeader(canvas);
        BuildCommandBar(canvas);

        _spellPanel = new PanelContainer
        {
            Name = "SpellPage",
            Position = Vector2.Zero,
            Size = new Vector2(1280, 720),
            CustomMinimumSize = new Vector2(1280, 720),
            Visible = false
        };
        _spellPanel.Theme = _gameTheme;
        _spellPanel.AddThemeStyleboxOverride(
            "panel",
            GameTheme.Box(new Color(GameTheme.Ink, 1.0f), GameTheme.Border, 0, 0));
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

        _editorCastButton = new Button
        {
            Text = GameStrings.Get("ui.editor.cast"),
            CustomMinimumSize = new Vector2(78, 30)
        };
        _editorCastButton.Pressed += CastSelectedWorking;
        titleRow.AddChild(_editorCastButton);

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

        var sampleButton = new Button
        {
            Text = GameStrings.Get("ui.editor.sample"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        sampleButton.Pressed += ResetSelectedWorkingToSample;
        actionRow.AddChild(sampleButton);

        var cancelConnectionButton = new Button
        {
            Text = GameStrings.Get("ui.editor.cancel_connection"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText = GameStrings.Get("ui.editor.cancel_connection_tooltip")
        };
        cancelConnectionButton.Pressed += () => _spellEditor?.ClearPendingConnection();
        actionRow.AddChild(cancelConnectionButton);

        AddTraceStepControls(actionRow);

        _spellEditor = new SpellGraphEditorControl
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(760, 380)
        };
        _spellEditor.GraphChanged += OnGraphChanged;
        editorColumn.AddChild(_spellEditor);

        _traceLabel = new Label
        {
            Text = "",
            CustomMinimumSize = new Vector2(0, 96),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _traceLabel.AddThemeFontSizeOverride("font_size", 12);
        editorColumn.AddChild(_traceLabel);

        SelectWorking(0);
        SetSpellEditorOpen(false);
        BuildRewardUi(canvas);
        BuildHelpUi(canvas);
    }

    private void BuildArenaHeader(CanvasLayer canvas)
    {
        var panel = new PanelContainer
        {
            Name = "ArenaHeader",
            Position = new Vector2(532, 28),
            Size = new Vector2(680, 104),
            CustomMinimumSize = new Vector2(680, 104),
            Theme = _gameTheme
        };
        canvas.AddChild(panel);

        var root = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        panel.AddChild(root);

        _arenaTitleLabel = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _arenaTitleLabel.AddThemeFontSizeOverride("font_size", 22);
        _arenaTitleLabel.AddThemeColorOverride("font_color", GameTheme.Gold);
        root.AddChild(_arenaTitleLabel);

        _arenaSubtitleLabel = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _arenaSubtitleLabel.AddThemeFontSizeOverride("font_size", 12);
        _arenaSubtitleLabel.AddThemeColorOverride("font_color", GameTheme.MutedText);
        root.AddChild(_arenaSubtitleLabel);
    }

    private void BuildRewardUi(CanvasLayer canvas)
    {
        _rewardBlocker = CreateModalBlocker("RewardBlocker");
        canvas.AddChild(_rewardBlocker);

        _rewardPanel = new PanelContainer
        {
            Name = "RewardPanel",
            Position = new Vector2(416, 142),
            Size = new Vector2(448, 436),
            CustomMinimumSize = new Vector2(448, 436),
            Visible = false
        };
        _rewardPanel.Theme = _gameTheme;
        canvas.AddChild(_rewardPanel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        _rewardPanel.AddChild(margin);

        var root = new VBoxContainer();
        margin.AddChild(root);

        _rewardTitleLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = GameStrings.Get("ui.reward.title")
        };
        _rewardTitleLabel.AddThemeFontSizeOverride("font_size", 24);
        root.AddChild(_rewardTitleLabel);

        _rewardDetailLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(0, 64),
            Text = GameStrings.Get("ui.reward.help")
        };
        root.AddChild(_rewardDetailLabel);

        _rewardChoices = new VBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        root.AddChild(_rewardChoices);
    }

    private void BuildHelpUi(CanvasLayer canvas)
    {
        _helpBlocker = CreateModalBlocker("HelpBlocker");
        canvas.AddChild(_helpBlocker);

        _helpPanel = new PanelContainer
        {
            Name = "HelpPanel",
            Position = new Vector2(360, 92),
            Size = new Vector2(560, 536),
            CustomMinimumSize = new Vector2(560, 536),
            Visible = false,
            Theme = _gameTheme
        };
        _helpPanel.AddThemeStyleboxOverride(
            "panel",
            GameTheme.Box(new Color(GameTheme.Ink, 0.995f), GameTheme.Gold, 2, 9));
        canvas.AddChild(_helpPanel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 28);
        margin.AddThemeConstantOverride("margin_top", 22);
        margin.AddThemeConstantOverride("margin_right", 28);
        margin.AddThemeConstantOverride("margin_bottom", 22);
        _helpPanel.AddChild(margin);

        var root = new VBoxContainer();
        margin.AddChild(root);

        var title = new Label
        {
            Text = GameStrings.Get("ui.help.title"),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 26);
        title.AddThemeColorOverride("font_color", GameTheme.Gold);
        root.AddChild(title);

        var subtitle = new Label
        {
            Text = GameStrings.Get("ui.help.subtitle"),
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        subtitle.AddThemeColorOverride("font_color", GameTheme.MutedText);
        root.AddChild(subtitle);

        var instructions = new Label
        {
            Text = GameStrings.Get("ui.help.instructions"),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        instructions.AddThemeFontSizeOverride("font_size", 15);
        root.AddChild(instructions);

        var begin = new Button
        {
            Text = GameStrings.Get("ui.help.begin"),
            CustomMinimumSize = new Vector2(0, 48)
        };
        begin.Pressed += HideHelp;
        root.AddChild(begin);
    }

    private void ShowHelp()
    {
        if (_helpPanel == null || _helpBlocker == null)
        {
            return;
        }

        SetSpellEditorOpen(false);
        GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
        _helpBlocker.Visible = true;
        _helpPanel.Visible = true;
        _helpBlocker.MoveToFront();
        _helpPanel.MoveToFront();
    }

    private void HideHelp()
    {
        if (_helpPanel != null)
        {
            _helpPanel.Visible = false;
        }
        if (_helpBlocker != null)
        {
            _helpBlocker.Visible = false;
        }
        GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
    }

    private static ColorRect CreateModalBlocker(string name)
    {
        return new ColorRect
        {
            Name = name,
            Position = Vector2.Zero,
            Size = new Vector2(1280, 720),
            CustomMinimumSize = new Vector2(1280, 720),
            Color = new Color(0.015f, 0.018f, 0.027f, 0.88f),
            MouseFilter = Control.MouseFilterEnum.Stop,
            Visible = false
        };
    }

    private void ShowRewardChoices(
        string title,
        string detail,
        IEnumerable<RewardDefinition> rewards,
        Action<RewardDefinition> chooseReward,
        Action? continueWithoutReward = null)
    {
        if (_rewardPanel == null || _rewardChoices == null)
        {
            return;
        }

        foreach (Node child in _rewardChoices.GetChildren())
        {
            child.QueueFree();
        }

        if (_rewardTitleLabel != null)
        {
            _rewardTitleLabel.Text = title;
        }

        if (_rewardDetailLabel != null)
        {
            _rewardDetailLabel.Text = detail;
        }

        RewardDefinition[] rewardArray = rewards.ToArray();

        if (rewardArray.Length == 0)
        {
            var continueButton = new Button
            {
                Text = GameStrings.Get("ui.reward.continue"),
                CustomMinimumSize = new Vector2(0, 56)
            };
            continueButton.Pressed += () =>
            {
                _rewardPanel.Visible = false;
                if (_rewardBlocker != null)
                {
                    _rewardBlocker.Visible = false;
                }
                continueWithoutReward?.Invoke();
            };
            _rewardChoices.AddChild(continueButton);
        }

        foreach (RewardDefinition reward in rewardArray)
        {
            var button = new Button
            {
                Text = $"{reward.DisplayName}\n{reward.Description}",
                CustomMinimumSize = new Vector2(0, 72),
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            button.Pressed += () =>
            {
                _rewardPanel.Visible = false;
                if (_rewardBlocker != null)
                {
                    _rewardBlocker.Visible = false;
                }
                GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
                chooseReward(reward);
            };
            _rewardChoices.AddChild(button);
        }

        if (_rewardBlocker != null)
        {
            _rewardBlocker.Visible = true;
            _rewardBlocker.MoveToFront();
        }
        _rewardPanel.Visible = true;
        _rewardPanel.MoveToFront();
        SetSpellEditorOpen(false);
    }

    private void ShowRunComplete(string title, string detail)
    {
        if (_rewardPanel == null || _rewardChoices == null)
        {
            return;
        }

        foreach (Node child in _rewardChoices.GetChildren())
        {
            child.QueueFree();
        }

        if (_rewardTitleLabel != null)
        {
            _rewardTitleLabel.Text = title;
        }

        if (_rewardDetailLabel != null)
        {
            _rewardDetailLabel.Text = detail;
        }

        var restart = new Button
        {
            Text = GameStrings.Get("ui.play.restart"),
            CustomMinimumSize = new Vector2(0, 56)
        };
        restart.Pressed += StartNewRun;
        _rewardChoices.AddChild(restart);
        if (_rewardBlocker != null)
        {
            _rewardBlocker.Visible = true;
            _rewardBlocker.MoveToFront();
        }
        _rewardPanel.Visible = true;
        _rewardPanel.MoveToFront();
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
            Text = _playerState?.UnlockedClauseIds.Contains(definition.Id) == true
                ? GameStrings.Get("ui.codex.add")
                : GameStrings.Get("ui.codex.sealed"),
            CustomMinimumSize = new Vector2(56, 28),
            Disabled = _playerState?.UnlockedClauseIds.Contains(definition.Id) != true,
            TooltipText = _playerState?.UnlockedClauseIds.Contains(definition.Id) == true
                ? definition.Tooltip
                : GameStrings.Get("ui.codex.sealed_tooltip")
        };
        string clauseId = definition.Id;
        addButton.Pressed += () =>
        {
            _spellEditor?.AddClauseNode(clauseId);
            ShowClauseCodex(definition);
        };
        _codexAddButtons[clauseId] = addButton;
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

    private void RefreshCodexAvailability()
    {
        if (_playerState == null)
        {
            return;
        }

        foreach ((string clauseId, Button button) in _codexAddButtons)
        {
            bool unlocked = _playerState.UnlockedClauseIds.Contains(clauseId);
            Working working = _preparedWorkings[_selectedWorkingIndex];
            bool alreadyUsed = working.Nodes.Any(node => node.ClauseId == clauseId);
            bool isFull = working.Nodes.Count >= WorkingValidator.MaxNodeCount;
            button.Disabled = !unlocked || alreadyUsed || isFull;
            button.Text = !unlocked
                ? GameStrings.Get("ui.codex.sealed")
                : alreadyUsed
                    ? GameStrings.Get("ui.codex.used")
                    : isFull
                        ? GameStrings.Get("ui.codex.full")
                        : GameStrings.Get("ui.codex.add");
            button.TooltipText = !unlocked
                ? GameStrings.Get("ui.codex.sealed_tooltip")
                : alreadyUsed
                    ? GameStrings.Get("ui.codex.used_tooltip")
                    : isFull
                        ? GameStrings.Get("ui.codex.full_tooltip")
                        : ClauseDefinitionCatalog.Get(clauseId).Tooltip;
        }
    }

    private void BuildCommandBar(CanvasLayer canvas)
    {
        var commandPanel = new PanelContainer
        {
            Name = "CommandPanel",
            Position = new Vector2(16, 276),
            Size = new Vector2(416, 428),
            CustomMinimumSize = new Vector2(416, 428)
        };
        commandPanel.Theme = _gameTheme;
        canvas.AddChild(commandPanel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        commandPanel.AddChild(margin);

        var root = new VBoxContainer();
        margin.AddChild(root);

        var utilityRow = new HBoxContainer();
        root.AddChild(utilityRow);

        _editorToggleButton = new Button
        {
            Text = GameStrings.Get("ui.editor.open"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FocusMode = Control.FocusModeEnum.None
        };
        _editorToggleButton.Pressed += () => SetSpellEditorOpen(!_isSpellEditorOpen);
        utilityRow.AddChild(_editorToggleButton);

        var waitButton = new Button
        {
            Text = GameStrings.Get("ui.play.wait"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FocusMode = Control.FocusModeEnum.None,
            TooltipText = GameStrings.Get("ui.play.wait_tooltip")
        };
        waitButton.Pressed += WaitPlayerTurn;
        utilityRow.AddChild(waitButton);

        var restartButton = new Button
        {
            Text = GameStrings.Get("ui.play.restart"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FocusMode = Control.FocusModeEnum.None,
            TooltipText = GameStrings.Get("ui.play.restart_tooltip")
        };
        restartButton.Pressed += StartNewRun;
        utilityRow.AddChild(restartButton);

        var helpButton = new Button
        {
            Text = GameStrings.Get("ui.help.button"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FocusMode = Control.FocusModeEnum.None,
            TooltipText = GameStrings.Get("ui.help.button_tooltip")
        };
        helpButton.Pressed += ShowHelp;
        utilityRow.AddChild(helpButton);

        _combatWorkingLabel = new Label
        {
            Text = "",
            CustomMinimumSize = new Vector2(0, 38),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _combatWorkingLabel.AddThemeFontSizeOverride("font_size", 14);
        root.AddChild(_combatWorkingLabel);

        var slotRow = new HBoxContainer();
        root.AddChild(slotRow);

        _combatSlotButtons = new Button[_preparedWorkings.Length];

        for (int i = 0; i < _preparedWorkings.Length; i++)
        {
            int slotIndex = i;
            var button = new Button
            {
                Text = "",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                FocusMode = Control.FocusModeEnum.None
            };
            button.Pressed += () => SelectWorking(slotIndex);
            button.MouseEntered += () =>
            {
                _isHoverPreviewActive = true;
                PreviewWorking(slotIndex, startAtFirstStep: true);
            };
            button.MouseExited += () =>
            {
                if (_isHoverPreviewActive)
                {
                    _isHoverPreviewActive = false;
                    PreviewSelectedWorking();
                }
            };
            _combatSlotButtons[i] = button;
            slotRow.AddChild(button);
        }

        var actionRow = new HBoxContainer();
        root.AddChild(actionRow);

        var previewButton = new Button
        {
            Text = GameStrings.Get("ui.editor.preview"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FocusMode = Control.FocusModeEnum.None
        };
        previewButton.Pressed += PreviewSelectedWorking;
        actionRow.AddChild(previewButton);

        _combatCastButton = new Button
        {
            Text = GameStrings.Get("ui.editor.cast"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FocusMode = Control.FocusModeEnum.None
        };
        _combatCastButton.Pressed += CastSelectedWorking;
        actionRow.AddChild(_combatCastButton);

        AddTraceStepControls(actionRow);

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
            CustomMinimumSize = new Vector2(0, 104),
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
            CustomMinimumSize = new Vector2(36, 30),
            FocusMode = Control.FocusModeEnum.None
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
        _isHoverPreviewActive = false;
        _selectedWorkingIndex = Mathf.PosMod(index, _preparedWorkings.Length);
        _spellEditor?.SetWorking(_preparedWorkings[_selectedWorkingIndex]);
        RefreshCodexAvailability();
        RefreshWorkingUi();
        PreviewSelectedWorking();
    }

    private void ResetSelectedWorkingToSample()
    {
        _preparedWorkings[_selectedWorkingIndex] = _selectedWorkingIndex == 0
            ? WorkingSamples.CreateMarkOrDamage()
            : WorkingSamples.CreateEmergencyWall();

        _spellEditor?.SetWorking(_preparedWorkings[_selectedWorkingIndex]);
        RefreshCodexAvailability();
        RefreshWorkingUi();
        PreviewSelectedWorking();
    }

    private void OnGraphChanged()
    {
        RefreshCodexAvailability();
        RefreshWorkingUi();
        PreviewSelectedWorking();
    }

    private void RefreshWorkingUi()
    {
        Working working = _preparedWorkings[_selectedWorkingIndex];
        IReadOnlyList<string> validationIssues = WorkingValidator.Validate(working);
        bool canCast = validationIssues.Count == 0
            && _encounter?.Turns.Phase == TurnPhase.PlayerTurn
            && _encounter.Result == GameResult.InProgress;
        string readiness = validationIssues.Count == 0
            ? GameStrings.Get("ui.play.ready")
            : $"{GameStrings.Get("ui.play.needs_repair")}: {validationIssues[0]}";

        if (_workingTitleLabel != null)
        {
            _workingTitleLabel.Text = $"{working.DisplayName}    {readiness}    {working.EstimatedCounterSummary}";
        }

        if (_combatWorkingLabel != null)
        {
            _combatWorkingLabel.Text =
                $"{GameStrings.Get("ui.play.prepared_working")}: {working.DisplayName}\n" +
                $"{readiness} · {GameStrings.Get("ui.play.ritual_cost")}: {working.EstimatedCounterSummary}";
        }

        for (int i = 0; i < _slotButtons.Length; i++)
        {
            _slotButtons[i].Text = i == _selectedWorkingIndex
                ? $"> {GameStrings.Get("ui.editor.slot")} {i + 1}: {_preparedWorkings[i].DisplayName}"
                : $"{GameStrings.Get("ui.editor.slot")} {i + 1}: {_preparedWorkings[i].DisplayName}";
        }

        for (int i = 0; i < _combatSlotButtons.Length; i++)
        {
            Working slotWorking = _preparedWorkings[i];
            string marker = i == _selectedWorkingIndex ? "◆ " : "";
            _combatSlotButtons[i].Text =
                $"{marker}{i + 1}. {slotWorking.DisplayName}\n{slotWorking.EstimatedCounterSummary}";
            _combatSlotButtons[i].TooltipText =
                $"{slotWorking.DisplayName}\n{GameStrings.Get("ui.play.hover_preview")}";
        }

        if (_combatCastButton != null)
        {
            _combatCastButton.Disabled = !canCast;
        }

        if (_editorCastButton != null)
        {
            _editorCastButton.Disabled = !canCast;
        }
    }

    private void PreviewSelectedWorking()
    {
        PreviewWorking(_selectedWorkingIndex, startAtFirstStep: true);
    }

    private void PreviewWorking(int workingIndex, bool startAtFirstStep)
    {
        if (_encounter == null)
        {
            return;
        }

        Working working = _preparedWorkings[Mathf.PosMod(workingIndex, _preparedWorkings.Length)];
        _previewWorkingIndex = Mathf.PosMod(workingIndex, _preparedWorkings.Length);
        _preview = _encounter.PreviewWorkingDetailed(working, _selectedTarget);
        WorkingResult result = _preview.Result;
        LogDebug($"Previewed {working.DisplayName} at {_selectedTarget}: {result.Succeeded}");

        if (workingIndex == _selectedWorkingIndex)
        {
            ShowTrace($"{GameStrings.Get("ui.editor.preview")} · {working.DisplayName}", result, startAtFirstStep);
        }
        else if (_quickTraceLabel != null)
        {
            string status = result.Succeeded
                ? GameStrings.Get("ui.play.ready")
                : result.FailureReason ?? GameStrings.Get("ui.play.needs_repair");
            _quickTraceLabel.Text =
                $"{GameStrings.Get("ui.play.hovering")}: {working.DisplayName} · {status}\n" +
                CollapseTraceForHud(result.Trace.ToDisplayText());
        }

        QueueRedraw();
    }

    private void RefreshPreviewForecast()
    {
        if (_encounter == null)
        {
            return;
        }

        _previewWorkingIndex = _selectedWorkingIndex;
        _preview = _encounter.PreviewWorkingDetailed(_preparedWorkings[_selectedWorkingIndex], _selectedTarget);
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
        RefreshWorkingUi();
        HandleEncounterResult();

        if (_encounter.Result == GameResult.InProgress)
        {
            RefreshPreviewForecast();
        }

        QueueRedraw();
    }

    private void ShowTrace(string label, WorkingResult result, bool startAtFirstStep = false)
    {
        _lastTraceActionLabel = label;
        _lastTraceResult = result;
        _visibleTraceSteps = startAtFirstStep && result.Trace.Events.Count > 0
            ? 1
            : result.Trace.Events.Count;
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


        _spellEditor?.SetTraceProgress(result.Trace, visibleSteps);
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

        Working previewWorking = _preparedWorkings[
            Mathf.PosMod(_previewWorkingIndex, _preparedWorkings.Length)];
        if (!_preview.Result.Succeeded
            && UsesSelectedTarget(previewWorking)
            && _encounter.Grid.IsInside(_selectedTarget))
        {
            DrawRect(GetTileRect(_selectedTarget).Grow(-7), new Color(1.0f, 0.25f, 0.20f, 0.95f), filled: false, width: 4.0f);
        }
    }

    private void DrawTargetingOverlay()
    {
        if (_encounter == null
            || !_encounter.Grid.IsInside(_selectedTarget)
            || !_discoveredTiles.Contains(_selectedTarget))
        {
            return;
        }

        Working working = _preparedWorkings[Mathf.PosMod(_previewWorkingIndex, _preparedWorkings.Length)];
        bool usesSelectedTarget = UsesSelectedTarget(working);

        if (usesSelectedTarget)
        {
            DrawRect(
                GetTileRect(_selectedTarget).Grow(-3),
                new Color(1.0f, 0.84f, 0.30f),
                filled: false,
                width: 3.0f);
            return;
        }

        DrawRect(
            GetTileRect(_selectedTarget).Grow(-7),
            new Color(0.52f, 0.55f, 0.62f, 0.75f),
            filled: false,
            width: 1.0f);

        if (!WorkingValidator.UsesNearestFoeTarget(working))
        {
            return;
        }

        EncounterActor? automaticFocus = _encounter.Enemies
            .Where(enemy => IsTileVisible(enemy.Position))
            .OrderBy(enemy => enemy.Position.ManhattanDistanceTo(_encounter.Player.Position))
            .ThenBy(enemy => enemy.Id)
            .FirstOrDefault();

        if (automaticFocus != null)
        {
            DrawRect(
                GetTileRect(automaticFocus.Position).Grow(-3),
                new Color(0.78f, 0.52f, 1.0f),
                filled: false,
                width: 3.0f);
            DrawCircle(
                GridToWorldCenter(automaticFocus.Position),
                TileSize * 0.13f,
                new Color(0.78f, 0.52f, 1.0f),
                filled: false,
                width: 2.0f);
        }
    }

    private static bool UsesSelectedTarget(Working working)
    {
        return WorkingValidator.UsesSelectedTarget(working);
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

                if (current == predicted || !IsTileVisible(position))
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
                     && IsTileVisible(condition.Position)
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

        foreach (EncounterActor actor in _encounter.Actors.Where(actor =>
                     actor.IsAlive && IsTileVisible(actor.Position)))
        {
            EncounterActor? predicted = forecast.GetActor(actor.Id);

            if (predicted == null)
            {
                continue;
            }

            if (predicted.IsAlive
                && predicted.Position != actor.Position
                && IsTileVisible(predicted.Position))
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
                if (IsTileVisible(damagePosition))
                {
                    DrawRect(GetTileRect(damagePosition).Grow(-4), new Color(1.0f, 0.20f, 0.26f, 0.95f), filled: false, width: 4.0f);
                }
            }

            bool markedNow = _encounter.HasActorCondition(actor.Id, "condition.marked", _encounter.Player.Id);
            bool markedPredicted = forecast.HasActorCondition(actor.Id, "condition.marked", forecast.Player.Id);

            if (!markedNow && markedPredicted)
            {
                GridPos markPosition = predicted.IsAlive ? predicted.Position : actor.Position;
                if (IsTileVisible(markPosition))
                {
                    DrawRect(GetTileRect(markPosition).Grow(-9), new Color(0.95f, 0.62f, 1.0f, 0.95f), filled: false, width: 3.0f);
                }
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
        RefreshWorkingUi();
        HandleEncounterResult();

        if (_encounter.Result == GameResult.InProgress)
        {
            PreviewSelectedWorking();
        }

        QueueRedraw();
    }

    private void HandleEncounterResult()
    {
        if (_encounter == null
            || _playerState == null
            || _runSession == null
            || _encounterResolutionShown
            || _encounter.Result == GameResult.InProgress)
        {
            return;
        }

        _encounterResolutionShown = true;
        _playerState.CaptureEncounterResult(_encounter);

        if (_encounter.Result == GameResult.PlayerLost)
        {
            ShowRunComplete(
                GameStrings.Get("ui.run.defeat_title"),
                GameStrings.Get("ui.run.defeat_detail"));
            return;
        }

        _runSession.TryAdvance(GameResult.PlayerWon);

        if (_runSession.IsComplete)
        {
            ShowRunComplete(
                GameStrings.Get("ui.run.victory_title"),
                GameStrings.Get("ui.run.victory_detail"));
            return;
        }

        IReadOnlyList<RewardDefinition> rewards = BuildRewardOffer();
        ShowRewardChoices(
            GameStrings.Get("ui.reward.title"),
            $"{GameStrings.Get("ui.reward.room_cleared")} {_runSession.Victories}/{_runSession.Definition.EncounterIds.Count}. " +
            GameStrings.Get("ui.reward.help"),
            rewards,
            reward =>
            {
                _playerState.ApplyReward(reward);
                RefreshCodexAvailability();
                ResetEncounter();
            },
            ResetEncounter);
    }

    private IReadOnlyList<RewardDefinition> BuildRewardOffer()
    {
        if (_playerState == null || _runSession == null)
        {
            return [];
        }

        return RewardOfferPolicy.BuildOffer(
            RewardDefinitionCatalog.All,
            _playerState,
            _runSession.Victories,
            Math.Max(1, _encounterDefinition?.RewardAmount ?? 1));
    }

    private void SyncActorVisuals()
    {
        if (_encounter == null)
        {
            return;
        }

        UpdateExploration();
        FollowPlayer();

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
            visual.Visible = actor.Faction == Faction.Player || IsTileVisible(actor.Position);
            UpdateActorVisual(actor, visual);
        }
    }

    private void UpdateExploration()
    {
        if (_encounter == null)
        {
            return;
        }

        for (int y = 0; y < _encounter.Grid.Height; y++)
        {
            for (int x = 0; x < _encounter.Grid.Width; x++)
            {
                var position = new GridPos(x, y);
                if (IsTileVisible(position))
                {
                    _discoveredTiles.Add(position);
                }
            }
        }
    }

    private bool IsTileVisible(GridPos position)
    {
        if (_encounter == null || !_encounter.Grid.IsInside(position))
        {
            return false;
        }

        GridPos playerPosition = _encounter.Player.Position;
        int chebyshevDistance = Math.Max(
            Math.Abs(position.X - playerPosition.X),
            Math.Abs(position.Y - playerPosition.Y));
        return chebyshevDistance <= TacticalEncounter.EnemyAwarenessRadius
            && _encounter.Grid.HasLineOfSight(playerPosition, position);
    }

    private void FollowPlayer()
    {
        if (_encounter == null || _camera == null)
        {
            return;
        }

        // Keep the player in the open play area to the right of the fixed command panels.
        _camera.Position = GridToWorldCenter(_encounter.Player.Position) - new Vector2(180, 42);
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
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Pass
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
        string markSigil = marked ? " ✦" : "";
        string actorName = actor.Faction == Faction.Player
            ? GameStrings.Get("actors.player.name")
            : _encounter.GetEnemyDisplayName(actor);
        string counters = StatusTextFormatter.FormatActorCounters(actor);
        string effects = StatusTextFormatter.FormatActorEffects(actor.Effects);

        string awarenessSigil = actor.Faction == Faction.Enemy
            ? actor.IsAlerted ? "!" : "?"
            : "";
        label.Text = $"{awarenessSigil}{sigil}\n{actor.Health}♥{markSigil}";
        label.TooltipText =
            $"{actorName}\n{GameStrings.Get("ui.play.health")}: {actor.Health}/{actor.MaxHealth}" +
            (actor.Faction == Faction.Enemy
                ? $"\n{GameStrings.Get("ui.play.awareness")}: " +
                  GameStrings.Get(actor.IsAlerted ? "ui.play.alerted" : "ui.play.unaware")
                : "") +
            (counters.Length > 0 ? $"\n{GameStrings.Get("ui.play.counters")}: {counters}" : "") +
            (effects.Length > 0 ? $"\n{GameStrings.Get("ui.play.effects")}: {effects}" : "");
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
            GameResult.PlayerWon when _encounterDefinition?.IsFinal == true => "The Crown is broken.",
            GameResult.PlayerWon => "Chamber clear. Choose what endures.",
            GameResult.PlayerLost => "Defeat. Press R to restart.",
            _ => $"Round {_encounter.Turns.Round} - {_encounter.Turns.Phase}"
        };

        string[] visibleEnemyIntents = _encounter.Enemies
            .Where(enemy => IsTileVisible(enemy.Position))
            .Select(enemy =>
                $"{(enemy.IsAlerted ? "!" : "?")}{GetActorSigil(enemy)} {_encounter.GetEnemyDisplayName(enemy)} " +
                $"({enemy.Health}♥): {_encounter.GetEnemyIntent(enemy)}")
            .ToArray();
        string enemyIntents = StatusTextFormatter.SummarizeEnemyIntents(visibleEnemyIntents);
        int livingEnemies = _encounter.Enemies.Count();
        int visibleEnemies = _encounter.Enemies.Count(enemy => IsTileVisible(enemy.Position));
        string exploration =
            $"Explored {_discoveredTiles.Count}/{_encounter.Grid.Width * _encounter.Grid.Height} tiles" +
            $" · Threats in sight {visibleEnemies}" +
            (visibleEnemies < livingEnemies ? " · Others unknown" : "");
        string runProgress = _runSession == null
            ? ""
            : $"{_runSession.Definition.DisplayName} · " +
              $"{GameStrings.Get("ui.run.chamber")} " +
              $"{Math.Min(_runSession.CurrentEncounterIndex + 1, _runSession.Definition.EncounterIds.Count)}/" +
              $"{_runSession.Definition.EncounterIds.Count} · Seed {_runSession.Seed}";
        string place = _encounterDefinition == null || _environmentDefinition == null
            ? ""
            : $"{_encounterDefinition.DisplayName} · {_environmentDefinition.DisplayName}";

        if (_arenaTitleLabel != null)
        {
            _arenaTitleLabel.Text = place;
        }
        if (_arenaSubtitleLabel != null)
        {
            _arenaSubtitleLabel.Text = _environmentDefinition == null
                ? _encounter.FloorRules.Description
                : $"{_environmentDefinition.Description}  •  {_encounter.FloorRules.DisplayName}: " +
                  _encounter.FloorRules.Description;
        }

        _statusLabel.Text =
            $"{runProgress}\n" +
            $"{place}\n" +
            $"{resultText} · {GameStrings.Get("ui.play.health")} {_encounter.Player.Health}/{_encounter.Player.MaxHealth}\n" +
            $"{exploration}\n" +
            $"{GameStrings.Get("ui.run.local_rule")}: {_encounter.FloorRules.DisplayName}\n" +
            $"{GameStrings.Get("ui.run.target")}: ({_selectedTarget.X}, {_selectedTarget.Y})\n" +
            $"{enemyIntents}";
        _statusLabel.TooltipText = _environmentDefinition == null
            ? _encounter.FloorRules.Description
            : $"{_environmentDefinition.Description}\n\n" +
              $"{_encounter.FloorRules.DisplayName}: {_encounter.FloorRules.Description}";
    }

    private static void LogDebug(string message)
    {
        GD.Print($"[Prototype] {message}");
    }

    private static int CreateRunSeed()
    {
        string configuredSeed = OS.GetEnvironment("TTID_RUN_SEED");
        if (int.TryParse(configuredSeed, out int parsedSeed))
        {
            return parsedSeed;
        }

        return (int)(Time.GetTicksUsec() & 0x7FFFFFFF);
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
