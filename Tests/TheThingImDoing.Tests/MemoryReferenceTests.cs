using TheThingImDoing.Core;
using TheThingImDoing.Spells;
using TheThingImDoing.World;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class MemoryReferenceTests
{
    [Fact]
    public void StoredActorReference_PersistsAcrossCastsAndFollowsLivingActorById()
    {
        var encounter = new TacticalEncounter(7, 3, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(5, 1), health: 3);
        Working store = CreateStoreWorking();

        WorkingPreview preview = encounter.PreviewWorkingDetailed(store, enemy.Position);

        Assert.True(preview.Result.Succeeded);
        Assert.True(preview.Result.ChangedWorld);
        Assert.True(preview.Encounter.Player.TryGetWorkingReference(
            "ref.memory.primary",
            out WorkingReference previewReference));
        Assert.Equal(enemy.Id, previewReference.ActorId);
        Assert.False(encounter.Player.TryGetWorkingReference("ref.memory.primary", out _));

        WorkingResult stored = encounter.TryCastWorking(store, enemy.Position);

        Assert.True(stored.Succeeded);
        Assert.True(encounter.Player.TryGetWorkingReference(
            "ref.memory.primary",
            out WorkingReference storedReference));
        Assert.Equal(enemy.Id, storedReference.ActorId);

        GridPos originalPosition = enemy.Position;
        encounter.RunEnemyTurn();
        Assert.NotEqual(originalPosition, enemy.Position);

        WorkingResult recalled = encounter.TryCastWorking(CreateRecallDamageWorking(), encounter.Player.Position);

        Assert.True(recalled.Succeeded);
        Assert.Equal(2, enemy.Health);
        Assert.Contains(recalled.Trace.Events, item =>
            item.Text == $"Focused ref.memory.primary: actor {enemy.Id}.");
    }

    [Fact]
    public void StoredTileReference_BehindWallFailsWithoutSpendingTurn()
    {
        var encounter = new TacticalEncounter(8, 3, new GridPos(1, 1));
        encounter.AddDummyEnemy(new GridPos(6, 1), health: 3);
        var rememberedTile = new GridPos(4, 1);

        Assert.True(encounter.TryCastWorking(CreateStoreWorking(), rememberedTile).Succeeded);
        encounter.Grid.SetTile(new GridPos(3, 1), TileState.Wall);
        encounter.RunEnemyTurn();

        WorkingResult recalled = encounter.TryCastWorking(
            CreateRecallRaiseWorking(),
            encounter.Player.Position);

        Assert.False(recalled.Succeeded);
        Assert.Equal("Recalled target is outside line of sight.", recalled.FailureReason);
        Assert.Equal(TurnPhase.PlayerTurn, encounter.Turns.Phase);
        Assert.Equal(TileState.Floor, encounter.Grid.GetTile(rememberedTile));
        Assert.Contains(recalled.Trace.Events, item =>
            item.Text == "The recalled target is outside line of sight.");
    }

    [Fact]
    public void StoredActorReference_BeyondRangeFailsWithoutSpendingTurn()
    {
        var encounter = new TacticalEncounter(30, 3, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddEnemy("enemy.root_saint", new GridPos(12, 1));

        Assert.True(encounter.TryCastWorking(CreateStoreWorking(), enemy.Position).Succeeded);
        encounter.RunEnemyTurn();

        for (int step = 0; step < 8; step++)
        {
            Assert.True(encounter.TryMoveActor(enemy, Direction.East));
        }

        WorkingResult recalled = encounter.TryCastWorking(
            CreateRecallDamageWorking(),
            encounter.Player.Position);

        Assert.False(recalled.Succeeded);
        Assert.Equal("Recalled target is beyond perception range.", recalled.FailureReason);
        Assert.Equal(TurnPhase.PlayerTurn, encounter.Turns.Phase);
        Assert.Equal(enemy.MaxHealth, enemy.Health);
    }

    [Fact]
    public void EnemyStoredReference_RemainsInternalAndIgnoresPlayerPerceptionRules()
    {
        var encounter = new TacticalEncounter(8, 3, new GridPos(1, 1));
        EncounterActor enemy = encounter.AddDummyEnemy(new GridPos(6, 1), health: 3);
        encounter.Grid.SetTile(new GridPos(3, 1), TileState.Wall);
        var machine = new WorkingMachine();
        var world = new EncounterSpellWorld(encounter);

        WorkingResult stored = machine.Execute(
            CreateStoreWorking(),
            world,
            enemy.Id,
            encounter.Player.Position);
        WorkingResult recalled = machine.Execute(
            CreateRecallDamageWorking(),
            world,
            enemy.Id,
            enemy.Position);

        Assert.True(stored.Succeeded);
        Assert.True(recalled.Succeeded);
        Assert.Equal(encounter.Player.MaxHealth - 1, encounter.Player.Health);
    }

    private static Working CreateStoreWorking()
    {
        var working = new Working("working.test.store_memory", "workings.mark_or_damage.name");
        working.AddNode(new WorkingNode(1, "clause.aim_at_target") { NextNodeId = 2 });
        working.AddNode(new WorkingNode(2, "clause.store_memory_ref"));
        return working;
    }

    private static Working CreateRecallDamageWorking()
    {
        var working = new Working("working.test.recall_memory", "workings.mark_or_damage.name");
        working.AddNode(new WorkingNode(1, "clause.focus_memory_ref") { NextNodeId = 2 });
        working.AddNode(new WorkingNode(2, "clause.damage_them"));
        return working;
    }

    private static Working CreateRecallRaiseWorking()
    {
        var working = new Working("working.test.recall_tile", "workings.emergency_wall.name");
        working.AddNode(new WorkingNode(1, "clause.focus_memory_ref") { NextNodeId = 2 });
        working.AddNode(new WorkingNode(2, "clause.raise_stone"));
        return working;
    }
}
