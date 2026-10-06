using StrataScene.Core.Search;
using Xunit;

namespace StrataScene.Core.Tests;

public class FuzzyMatcherTests
{
    private static readonly List<LauncherItem> SampleItems =
    [
        new("scene_work", "Work", "Focus work mode", "#0078D4", "Ctrl+Alt+W", LauncherItemKind.Scene),
        new("scene_game", "Game", "Gaming setup", "#107C10", "Ctrl+Alt+G", LauncherItemKind.Scene),
        new("cmd_restore", "restore", "ウィンドウを復元", null, "Ctrl+Alt+Back", LauncherItemKind.Command, "restore"),
        new("cmd_settings", "settings", "設定を開く", null, null, LauncherItemKind.Command, "settings"),
        new("cmd_quit", "quit", "Strata Scene の終了", null, null, LauncherItemKind.Command, "quit")
    ];

    [Fact]
    public void Match_EmptyQuery_ReturnsAllInOriginalOrder()
    {
        var result = FuzzyMatcher.Match(SampleItems, "");
        Assert.Equal(SampleItems.Count, result.Count);
        Assert.Equal("scene_work", result[0].Id);
        Assert.Equal("scene_game", result[1].Id);
    }

    [Fact]
    public void Match_ExactMatch_RanksHighest()
    {
        var result = FuzzyMatcher.Match(SampleItems, "Game");
        Assert.NotEmpty(result);
        Assert.Equal("scene_game", result[0].Id);
    }

    [Fact]
    public void Match_Prefix_MatchesCorrectly()
    {
        var result = FuzzyMatcher.Match(SampleItems, "set");
        Assert.NotEmpty(result);
        Assert.Equal("cmd_settings", result[0].Id);
    }

    [Fact]
    public void Match_FuzzySubsequence_FindsItem()
    {
        var result = FuzzyMatcher.Match(SampleItems, "stt");
        Assert.Contains(result, r => r.Id == "cmd_settings");
    }

    [Fact]
    public void Match_NoMatch_ReturnsEmpty()
    {
        var result = FuzzyMatcher.Match(SampleItems, "xyznonexistent");
        Assert.Empty(result);
    }
}
