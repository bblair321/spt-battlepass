using System.Text.Json.Serialization;

namespace SptBattlePass.Server.Models;

public sealed class BattlePassStatus
{
    [JsonPropertyName("seasonId")]
    public string SeasonId { get; set; } = "";

    [JsonPropertyName("seasonName")]
    public string SeasonName { get; set; } = "";

    [JsonPropertyName("daysRemaining")]
    public int DaysRemaining { get; set; }

    [JsonPropertyName("tickets")]
    public int Tickets { get; set; }

    [JsonPropertyName("lastDailyReset")]
    public string LastDailyReset { get; set; } = "";

    [JsonPropertyName("lastWeeklyReset")]
    public string LastWeeklyReset { get; set; } = "";

    [JsonPropertyName("lastMonthlyReset")]
    public string LastMonthlyReset { get; set; } = "";

    [JsonPropertyName("lastCrateSeasonId")]
    public string LastCrateSeasonId { get; set; } = "";

    [JsonPropertyName("lastCrateTickets")]
    public int LastCrateTickets { get; set; }

    [JsonPropertyName("challenges")]
    public BattlePassChallenges Challenges { get; set; } = new();

    [JsonPropertyName("shop")]
    public List<BattlePassShopOffer> Shop { get; set; } = [];

    [JsonPropertyName("debug")]
    public bool Debug { get; set; }

    [JsonPropertyName("grantAmount")]
    public int GrantAmount { get; set; }

    [JsonPropertyName("dailyRerollCost")]
    public int DailyRerollCost { get; set; }

    [JsonPropertyName("dailyRerollsLeft")]
    public int DailyRerollsLeft { get; set; }

    [JsonPropertyName("weeklyRerollCost")]
    public int WeeklyRerollCost { get; set; }

    [JsonPropertyName("weeklyRerollsLeft")]
    public int WeeklyRerollsLeft { get; set; }

    [JsonPropertyName("xpDaily")]
    public int XpDaily { get; set; }

    [JsonPropertyName("xpWeekly")]
    public int XpWeekly { get; set; }

    [JsonPropertyName("xpMonthly")]
    public int XpMonthly { get; set; }

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

    [JsonPropertyName("lastSeasonName")]
    public string LastSeasonName { get; set; } = "";

    [JsonPropertyName("lastSeasonTicketsEarned")]
    public int LastSeasonTicketsEarned { get; set; }

    [JsonPropertyName("lastSeasonChallengesCompleted")]
    public int LastSeasonChallengesCompleted { get; set; }

    [JsonPropertyName("premium")]
    public bool Premium { get; set; }

    [JsonPropertyName("premiumCost")]
    public int PremiumCost { get; set; }

    [JsonPropertyName("trackXp")]
    public int TrackXp { get; set; }

    [JsonPropertyName("trackLevel")]
    public int TrackLevel { get; set; }

    [JsonPropertyName("trackMaxLevel")]
    public int TrackMaxLevel { get; set; }

    [JsonPropertyName("trackXpIntoLevel")]
    public int TrackXpIntoLevel { get; set; }

    [JsonPropertyName("trackXpForLevel")]
    public int TrackXpForLevel { get; set; }

    [JsonPropertyName("track")]
    public List<TrackTierStatus> Track { get; set; } = [];
}

public sealed class BattlePassChallenges
{
    [JsonPropertyName("daily")]
    public List<BattlePassChallenge> Daily { get; set; } = [];

    [JsonPropertyName("weekly")]
    public List<BattlePassChallenge> Weekly { get; set; } = [];

    [JsonPropertyName("monthly")]
    public List<BattlePassChallenge> Monthly { get; set; } = [];
}

public sealed class BattlePassChallenge
{
    [JsonPropertyName("instanceId")]
    public string InstanceId { get; set; } = "";

    [JsonPropertyName("templateId")]
    public string TemplateId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("progress")]
    public int Progress { get; set; }

    [JsonPropertyName("target")]
    public int Target { get; set; }

    [JsonPropertyName("ticketReward")]
    public int TicketReward { get; set; }

    [JsonPropertyName("completed")]
    public bool Completed { get; set; }

    [JsonPropertyName("claimed")]
    public bool Claimed { get; set; }

    /// <summary>not_started, in_progress, or complete.</summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = "not_started";

    [JsonPropertyName("expiresAt")]
    public string ExpiresAt { get; set; } = "";

    /// <summary>EFT location id for ExtractMap challenges (bigmap, woods, factory4, ...).</summary>
    [JsonPropertyName("map")]
    public string? Map { get; set; }

    [JsonPropertyName("weapon")]
    public string? Weapon { get; set; }

    [JsonPropertyName("tpl")]
    public string? Tpl { get; set; }

    [JsonPropertyName("timeOfDay")]
    public string? TimeOfDay { get; set; }
}

public sealed class BattlePassShopOffer
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("tpl")]
    public string Tpl { get; set; } = "";

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("price")]
    public int Price { get; set; }

    [JsonPropertyName("stockRemaining")]
    public int? StockRemaining { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("preset")]
    public bool Preset { get; set; }
}
