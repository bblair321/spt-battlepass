using System.Text.Json.Serialization;

namespace SptBattlePass.Server.Models;

public sealed class ModConfig
{
    [JsonPropertyName("debugGrants")]
    public bool DebugGrants { get; set; } = false;

    [JsonPropertyName("grantAmount")]
    public int GrantAmount { get; set; } = 10;

    [JsonPropertyName("challengesPerBucket")]
    public int ChallengesPerBucket { get; set; } = 3;

    [JsonPropertyName("monthlyBonus")]
    public int MonthlyBonus { get; set; } = 5;

    [JsonPropertyName("dailyRerollCost")]
    public int DailyRerollCost { get; set; } = 1;

    [JsonPropertyName("dailyRerollMax")]
    public int DailyRerollMax { get; set; } = 3;

    [JsonPropertyName("weeklyRerollCost")]
    public int WeeklyRerollCost { get; set; } = 2;

    [JsonPropertyName("weeklyRerollMax")]
    public int WeeklyRerollMax { get; set; } = 1;

    [JsonPropertyName("xpDaily")]
    public int XpDaily { get; set; } = 500;

    [JsonPropertyName("xpWeekly")]
    public int XpWeekly { get; set; } = 1500;

    [JsonPropertyName("xpMonthly")]
    public int XpMonthly { get; set; } = 4000;

    [JsonPropertyName("xpMonthlyBonus")]
    public int XpMonthlyBonus { get; set; } = 2500;

    [JsonPropertyName("premiumCostRoubles")]
    public int PremiumCostRoubles { get; set; } = 750000;

    [JsonPropertyName("trackXpDaily")]
    public int TrackXpDaily { get; set; } = 10;

    [JsonPropertyName("trackXpWeekly")]
    public int TrackXpWeekly { get; set; } = 25;

    [JsonPropertyName("trackXpMonthly")]
    public int TrackXpMonthly { get; set; } = 60;

    [JsonPropertyName("trackXpMonthlyBonus")]
    public int TrackXpMonthlyBonus { get; set; } = 40;

    /// <summary>Bump this when shop/track/crate/challenge pools change so live profiles restock the shop.</summary>
    [JsonPropertyName("catalogVersion")]
    public int CatalogVersion { get; set; } = 0;

    /// <summary>Maps yyyy-MM season ids to a folder under db/themes/. Missing months use the root shop/track/crate.</summary>
    [JsonPropertyName("seasonThemes")]
    public Dictionary<string, string> SeasonThemes { get; set; } = [];
}
