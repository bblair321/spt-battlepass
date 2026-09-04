using System.Text.Json.Serialization;

namespace SptBattlePass.Server.Models;

public sealed class BattlePassProfileState
{
    [JsonPropertyName("seasonId")]
    public string SeasonId { get; set; } = "";

    [JsonPropertyName("tickets")]
    public int Tickets { get; set; }

    [JsonPropertyName("dailyKey")]
    public string DailyKey { get; set; } = "";

    [JsonPropertyName("weeklyKey")]
    public string WeeklyKey { get; set; } = "";

    [JsonPropertyName("monthlyKey")]
    public string MonthlyKey { get; set; } = "";

    [JsonPropertyName("monthlyBonusClaimed")]
    public bool MonthlyBonusClaimed { get; set; }

    [JsonPropertyName("processedRaidIds")]
    public List<string> ProcessedRaidIds { get; set; } = [];

    [JsonPropertyName("challenges")]
    public BattlePassChallenges Challenges { get; set; } = new();

    [JsonPropertyName("shop")]
    public List<BattlePassShopOffer> Shop { get; set; } = [];

    [JsonPropertyName("lastCrateSeasonId")]
    public string LastCrateSeasonId { get; set; } = "";

    [JsonPropertyName("lastCrateTickets")]
    public int LastCrateTickets { get; set; }

    [JsonPropertyName("dailyRerolls")]
    public int DailyRerolls { get; set; }

    [JsonPropertyName("weeklyRerolls")]
    public int WeeklyRerolls { get; set; }

    [JsonPropertyName("ticketsEarnedSeason")]
    public int TicketsEarnedSeason { get; set; }

    [JsonPropertyName("ticketsSpentSeason")]
    public int TicketsSpentSeason { get; set; }

    [JsonPropertyName("challengesCompletedSeason")]
    public int ChallengesCompletedSeason { get; set; }

    [JsonPropertyName("dailyCompletedSeason")]
    public int DailyCompletedSeason { get; set; }

    [JsonPropertyName("weeklyCompletedSeason")]
    public int WeeklyCompletedSeason { get; set; }

    [JsonPropertyName("monthlyCompletedSeason")]
    public int MonthlyCompletedSeason { get; set; }

    [JsonPropertyName("xpEarnedSeason")]
    public int XpEarnedSeason { get; set; }

    [JsonPropertyName("lifetimeTicketsEarned")]
    public int LifetimeTicketsEarned { get; set; }

    [JsonPropertyName("lifetimeTicketsSpent")]
    public int LifetimeTicketsSpent { get; set; }

    [JsonPropertyName("lifetimeChallengesCompleted")]
    public int LifetimeChallengesCompleted { get; set; }

    [JsonPropertyName("lifetimeXpEarned")]
    public int LifetimeXpEarned { get; set; }

    [JsonPropertyName("lastSeasonId")]
    public string LastSeasonId { get; set; } = "";

    [JsonPropertyName("lastSeasonTicketsEarned")]
    public int LastSeasonTicketsEarned { get; set; }

    [JsonPropertyName("lastSeasonChallengesCompleted")]
    public int LastSeasonChallengesCompleted { get; set; }

    [JsonPropertyName("premium")]
    public bool Premium { get; set; }

    [JsonPropertyName("trackXp")]
    public int TrackXp { get; set; }

    [JsonPropertyName("trackFreeClaimed")]
    public int TrackFreeClaimed { get; set; }

    [JsonPropertyName("trackPremiumClaimed")]
    public int TrackPremiumClaimed { get; set; }
}
