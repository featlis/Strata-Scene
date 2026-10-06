using StrataScene.Core.Config;

namespace StrataScene.Core.Search;

public enum LauncherItemKind
{
    Scene,
    Command
}

public sealed record LauncherItem(
    string Id,
    string Title,
    string? Subtitle,
    string? Color,
    string? Shortcut,
    LauncherItemKind Kind,
    string? CommandAction = null);

public static class FuzzyMatcher
{
    public static List<LauncherItem> Match(IReadOnlyList<LauncherItem> items, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return items.ToList();
        }

        var trimmed = query.Trim();
        var scored = new List<(LauncherItem Item, int Score, int OriginalIndex)>();

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var score = CalculateScore(item, trimmed);
            if (score > 0)
            {
                scored.Add((item, score, i));
            }
        }

        return scored
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.OriginalIndex)
            .Select(s => s.Item)
            .ToList();
    }

    private static int CalculateScore(LauncherItem item, string query)
    {
        var titleScore = ScoreText(item.Title, query);
        var idScore = ScoreText(item.Id, query);
        var subScore = !string.IsNullOrEmpty(item.Subtitle) ? ScoreText(item.Subtitle, query) : 0;

        return Math.Max(titleScore, Math.Max(idScore, subScore));
    }

    private static int ScoreText(string target, string query)
    {
        if (string.IsNullOrWhiteSpace(target)) return 0;

        var tUpper = target.ToUpperInvariant();
        var qUpper = query.ToUpperInvariant();

        // 1. Exact match
        if (tUpper == qUpper) return 1000;

        // 2. Starts with (Prefix)
        if (tUpper.StartsWith(qUpper, StringComparison.Ordinal)) return 900;

        // 3. Word boundary start (e.g. "Work Mode" matched with "Mode")
        var words = target.Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (word.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            {
                return 800;
            }
        }

        // 4. Substring match
        var substrIdx = tUpper.IndexOf(qUpper, StringComparison.Ordinal);
        if (substrIdx >= 0)
        {
            return 600 - Math.Min(substrIdx * 10, 200);
        }

        // 5. Fuzzy subsequence
        var qIdx = 0;
        var prevMatchIdx = 0;
        var totalGap = 0;

        for (var i = 0; i < tUpper.Length && qIdx < qUpper.Length; i++)
        {
            if (tUpper[i] == qUpper[qIdx])
            {
                if (qIdx > 0)
                {
                    totalGap += (i - prevMatchIdx - 1);
                }
                prevMatchIdx = i;
                qIdx++;
            }
        }

        if (qIdx == qUpper.Length)
        {
            // Successfully matched all characters in order
            var gapPenalty = Math.Min(totalGap * 15, 300);
            return Math.Max(100, 400 - gapPenalty);
        }

        return 0;
    }
}
