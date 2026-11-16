namespace SptBattlePass.Server.Services;

internal static class ItemParents
{
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["meds"] = "543be5664bdc2dd4348b4569",
        ["medkit"] = "5448f39d4bdc2d0a728b4568",
        ["medikit"] = "5448f39d4bdc2d0a728b4568",
        ["drugs"] = "5448f3a14bdc2d27728b4569",
        ["barter"] = "5448eb774bdc2d0a728b4567",
        ["electronics"] = "57864a66245977548f04a81f",
        ["building"] = "57864ada245977548638de91",
        ["info"] = "5448ecbe4bdc2d60728b4568"
    };

    public static string Resolve(string? value)
    {
        string key = (value ?? "").Trim();
        if (key.Length == 0)
        {
            return "";
        }

        return Aliases.TryGetValue(key, out string? tpl) ? tpl : key;
    }

    public static string NormalizeBoss(string? role)
    {
        string value = (role ?? "").Trim().ToLowerInvariant();
        if (value.StartsWith("boss", StringComparison.Ordinal))
        {
            value = value[4..];
        }

        return value.Trim();
    }

    public static bool BossMatches(string role, string required)
    {
        string have = NormalizeBoss(role);
        string want = NormalizeBoss(required);
        return have.Length > 0 && want.Length > 0 && (have == want || have.Contains(want) || want.Contains(have));
    }

    public static bool ExtractMatches(string raidExtract, string required)
    {
        string have = (raidExtract ?? "").Trim();
        string want = (required ?? "").Trim();
        if (have.Length == 0 || want.Length == 0)
        {
            return false;
        }

        return have.Equals(want, StringComparison.OrdinalIgnoreCase)
               || have.IndexOf(want, StringComparison.OrdinalIgnoreCase) >= 0
               || want.IndexOf(have, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
