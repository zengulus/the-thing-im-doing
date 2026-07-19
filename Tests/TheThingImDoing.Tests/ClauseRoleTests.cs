using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using TheThingImDoing.Spells;
using TheThingImDoing.UI;
using Xunit;

namespace TheThingImDoing.Tests;

public sealed class ClauseRoleTests
{
    [Fact]
    public void BaseClauses_EachDeclareExactlyOneValidRole()
    {
        string contentPath = FindProjectFile("Content", "Base", "clauses.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(contentPath));
        ClauseDefinition[] definitions = ClauseDefinitionCatalog.All.ToArray();

        foreach (JsonElement clause in document.RootElement.GetProperty("clauses").EnumerateArray())
        {
            JsonProperty roleProperty = Assert.Single(
                clause.EnumerateObject(),
                property => string.Equals(property.Name, "role", StringComparison.OrdinalIgnoreCase));
            string roleName = Assert.IsType<string>(roleProperty.Value.GetString());

            Assert.True(TryParseExactRole(roleName, out ClauseRole declaredRole));

            string clauseId = clause.GetProperty("id").GetString()!;
            ClauseDefinition definition = Assert.Single(definitions, item => item.Id == clauseId);
            Assert.Equal(declaredRole, definition.Role);
        }
    }

    [Theory]
    [InlineData("clause.aim_at_target", ClauseRole.Generator)]
    [InlineData("clause.aim_at_nearest_foe", ClauseRole.Generator)]
    [InlineData("clause.mark_them", ClauseRole.Generator)]
    [InlineData("clause.raise_stone", ClauseRole.Generator)]
    [InlineData("clause.poison_them", ClauseRole.Generator)]
    [InlineData("clause.bleed_them", ClauseRole.Generator)]
    [InlineData("clause.add_lightning_charge", ClauseRole.Generator)]
    [InlineData("clause.add_lightning_ward", ClauseRole.Generator)]
    [InlineData("clause.store_memory_ref", ClauseRole.Generator)]
    [InlineData("clause.if_marked", ClauseRole.Operator)]
    [InlineData("clause.if_occupied", ClauseRole.Operator)]
    [InlineData("clause.if_clear", ClauseRole.Operator)]
    [InlineData("clause.push_them", ClauseRole.Operator)]
    [InlineData("clause.focus_memory_ref", ClauseRole.Operator)]
    [InlineData("clause.damage_them", ClauseRole.Consumer)]
    [InlineData("clause.spend_poison", ClauseRole.Consumer)]
    [InlineData("clause.spend_bleed", ClauseRole.Consumer)]
    [InlineData("clause.spend_focus", ClauseRole.Consumer)]
    [InlineData("clause.spend_memory", ClauseRole.Consumer)]
    [InlineData("clause.lightning_shield", ClauseRole.Consumer)]
    public void BaseClauses_HaveTheirIntendedRole(string clauseId, ClauseRole expectedRole)
    {
        Assert.Equal(expectedRole, ClauseDefinitionCatalog.Get(clauseId).Role);
    }

    [Theory]
    [InlineData(ClauseRole.Generator, "Generator", "G")]
    [InlineData(ClauseRole.Operator, "Operator", "O")]
    [InlineData(ClauseRole.Consumer, "Consumer", "C")]
    public void RolePresentation_MapsNamesAndShortNames(
        ClauseRole role,
        string expectedName,
        string expectedShortName)
    {
        Assert.Equal(expectedName, ClauseRolePresentation.GetName(role));
        Assert.Equal(expectedShortName, ClauseRolePresentation.GetShortName(role));
    }

    [Fact]
    public void RolePresentation_MapsDistinctRoleColors()
    {
        Color generator = ClauseRolePresentation.GetColor(ClauseRole.Generator);
        Color @operator = ClauseRolePresentation.GetColor(ClauseRole.Operator);
        Color consumer = ClauseRolePresentation.GetColor(ClauseRole.Consumer);

        Assert.Equal(new Color(0.38f, 0.82f, 0.96f), generator);
        Assert.Equal(new Color(0.82f, 0.65f, 1.0f), @operator);
        Assert.Equal(new Color(1.0f, 0.66f, 0.30f), consumer);
        Assert.Equal(3, new[] { generator, @operator, consumer }.Distinct().Count());
    }

    private static bool TryParseExactRole(string value, out ClauseRole role)
    {
        string candidate = value.Trim();

        return Enum.TryParse(candidate, ignoreCase: true, out role)
            && Enum.IsDefined(role)
            && string.Equals(Enum.GetName(role), candidate, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindProjectFile(params string[] segments)
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory);
             directory != null;
             directory = directory.Parent)
        {
            string path = Path.Combine([directory.FullName, .. segments]);

            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException($"Could not find project file '{Path.Combine(segments)}'.");
    }
}
