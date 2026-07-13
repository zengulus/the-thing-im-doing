using System.Text.Json;
using TheThingImDoing.Spells;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class WorkingJsonImportTests
{
    [Fact]
    public void FromJson_RejectsNullNodeCollectionThroughWorkingValidation()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "id": "working.null_nodes",
              "displayNameKey": "workings.mark_or_damage.name",
              "maxSteps": 24,
              "entryNodeId": null,
              "nodes": null,
              "layout": {}
            }
            """;

        JsonException exception = Assert.Throws<JsonException>(() => WorkingJson.FromJson(json));

        Assert.Contains("Working JSON is invalid", exception.Message);
        Assert.Contains("no entry node", exception.Message);
    }

    [Fact]
    public void FromJson_RejectsNullNodesWithAReadableJsonError()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "id": "working.null_node",
              "displayNameKey": "workings.mark_or_damage.name",
              "maxSteps": 24,
              "entryNodeId": 1,
              "nodes": [null],
              "layout": {}
            }
            """;

        JsonException exception = Assert.Throws<JsonException>(() => WorkingJson.FromJson(json));

        Assert.Contains("null node at index 0", exception.Message);
    }

    [Fact]
    public void FromJson_RejectsDuplicateNodeIdsInsteadOfSilentlyDroppingOne()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "id": "working.duplicate",
              "displayNameKey": "workings.mark_or_damage.name",
              "maxSteps": 24,
              "entryNodeId": 1,
              "nodes": [
                { "id": 1, "clauseId": "clause.aim_at_target" },
                { "id": 1, "clauseId": "clause.damage_them" }
              ],
              "layout": {}
            }
            """;

        JsonException exception = Assert.Throws<JsonException>(() => WorkingJson.FromJson(json));

        Assert.Contains("duplicate node ids: 1", exception.Message);
    }

    [Fact]
    public void FromJson_AllowsNullOptionalLayout()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "id": "working.no_layout",
              "displayNameKey": "workings.mark_or_damage.name",
              "maxSteps": 24,
              "entryNodeId": 1,
              "nodes": [
                { "id": 1, "clauseId": "clause.aim_at_target" }
              ],
              "layout": null
            }
            """;

        Working working = WorkingJson.FromJson(json);

        Assert.Empty(working.Layout);
        Assert.Equal(1, working.EntryNodeId);
    }

    [Fact]
    public void FromJson_RejectsNullLayoutEntriesWithAReadableJsonError()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "id": "working.null_layout",
              "displayNameKey": "workings.mark_or_damage.name",
              "maxSteps": 24,
              "entryNodeId": 1,
              "nodes": [
                { "id": 1, "clauseId": "clause.aim_at_target" }
              ],
              "layout": { "1": null }
            }
            """;

        JsonException exception = Assert.Throws<JsonException>(() => WorkingJson.FromJson(json));

        Assert.Contains("null layout data for node 1", exception.Message);
    }

    [Fact]
    public void FromJson_ValidatesTheWorkingBeforeReturningIt()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "id": "working.invalid",
              "displayNameKey": "workings.mark_or_damage.name",
              "maxSteps": 24,
              "entryNodeId": 1,
              "nodes": [
                { "id": 1, "clauseId": "clause.not_registered", "nextNodeId": 99 }
              ],
              "layout": {}
            }
            """;

        JsonException exception = Assert.Throws<JsonException>(() => WorkingJson.FromJson(json));

        Assert.Contains("Working JSON is invalid", exception.Message);
        Assert.Contains("clause.not_registered", exception.Message);
        Assert.Contains("missing node 99", exception.Message);
    }
}
