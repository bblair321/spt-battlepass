using System.Collections.Generic;
using Newtonsoft.Json;

namespace SptBattlePass.Client.Models;

public sealed class BattlePassStatusDto
{
    [JsonProperty("seasonId")]
    public string SeasonId { get; set; }

    [JsonProperty("seasonName")]
    public string SeasonName { get; set; }

    [JsonProperty("daysRemaining")]
    public int DaysRemaining { get; set; }

    [JsonProperty("tickets")]
    public int Tickets { get; set; }

    [JsonProperty("challenges")]
    public BattlePassChallengesDto Challenges { get; set; }

    [JsonProperty("shop")]
    public List<BattlePassShopOfferDto> Shop { get; set; }

    [JsonProperty("lastCrateSeasonId")]
    public string LastCrateSeasonId { get; set; }

    [JsonProperty("lastCrateTickets")]
    public int LastCrateTickets { get; set; }

    [JsonProperty("debug")]
    public bool Debug { get; set; }

    [JsonProperty("grantAmount")]
    public int GrantAmount { get; set; }

    [JsonProperty("dailyRerollCost")]
    public int DailyRerollCost { get; set; }

    [JsonProperty("dailyRerollsLeft")]
    public int DailyRerollsLeft { get; set; }

    [JsonProperty("weeklyRerollCost")]
    public int WeeklyRerollCost { get; set; }

    [JsonProperty("weeklyRerollsLeft")]
    public int WeeklyRerollsLeft { get; set; }

    [JsonProperty("xpDaily")]
    public int XpDaily { get; set; }

    [JsonProperty("xpWeekly")]
    public int XpWeekly { get; set; }

    [JsonProperty("xpMonthly")]
    public int XpMonthly { get; set; }

    [JsonProperty("ticketsEarnedSeason")]
    public int TicketsEarnedSeason { get; set; }

    [JsonProperty("ticketsSpentSeason")]
    public int TicketsSpentSeason { get; set; }

    [JsonProperty("challengesCompletedSeason")]
    public int ChallengesCompletedSeason { get; set; }

    [JsonProperty("dailyCompletedSeason")]
    public int DailyCompletedSeason { get; set; }

    [JsonProperty("weeklyCompletedSeason")]
    public int WeeklyCompletedSeason { get; set; }

    [JsonProperty("monthlyCompletedSeason")]
    public int MonthlyCompletedSeason { get; set; }

    [JsonProperty("xpEarnedSeason")]
    public int XpEarnedSeason { get; set; }

    [JsonProperty("lifetimeTicketsEarned")]
    public int LifetimeTicketsEarned { get; set; }

    [JsonProperty("lifetimeTicketsSpent")]
    public int LifetimeTicketsSpent { get; set; }

    [JsonProperty("lifetimeChallengesCompleted")]
    public int LifetimeChallengesCompleted { get; set; }

    [JsonProperty("lifetimeXpEarned")]
    public int LifetimeXpEarned { get; set; }

    [JsonProperty("lastSeasonId")]
    public string LastSeasonId { get; set; }

    [JsonProperty("lastSeasonName")]
    public string LastSeasonName { get; set; }

    [JsonProperty("lastSeasonTicketsEarned")]
    public int LastSeasonTicketsEarned { get; set; }

    [JsonProperty("lastSeasonChallengesCompleted")]
    public int LastSeasonChallengesCompleted { get; set; }

    [JsonProperty("premium")]
    public bool Premium { get; set; }

    [JsonProperty("premiumCost")]
    public int PremiumCost { get; set; }

    [JsonProperty("trackXp")]
    public int TrackXp { get; set; }

    [JsonProperty("trackLevel")]
    public int TrackLevel { get; set; }

    [JsonProperty("trackMaxLevel")]
    public int TrackMaxLevel { get; set; }

    [JsonProperty("trackXpIntoLevel")]
    public int TrackXpIntoLevel { get; set; }

    [JsonProperty("trackXpForLevel")]
    public int TrackXpForLevel { get; set; }

    [JsonProperty("track")]
    public List<TrackTierStatusDto> Track { get; set; }
}

public sealed class BattlePassChallengesDto
{
    [JsonProperty("daily")]
    public List<BattlePassChallengeDto> Daily { get; set; }

    [JsonProperty("weekly")]
    public List<BattlePassChallengeDto> Weekly { get; set; }

    [JsonProperty("monthly")]
    public List<BattlePassChallengeDto> Monthly { get; set; }
}

public sealed class BattlePassChallengeDto
{
    [JsonProperty("instanceId")]
    public string InstanceId { get; set; }

    [JsonProperty("templateId")]
    public string TemplateId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("progress")]
    public int Progress { get; set; }

    [JsonProperty("target")]
    public int Target { get; set; }

    [JsonProperty("ticketReward")]
    public int TicketReward { get; set; }

    [JsonProperty("completed")]
    public bool Completed { get; set; }

    [JsonProperty("claimed")]
    public bool Claimed { get; set; }

    [JsonProperty("state")]
    public string State { get; set; }

    [JsonProperty("expiresAt")]
    public string ExpiresAt { get; set; }

    [JsonProperty("map")]
    public string Map { get; set; }

    [JsonProperty("weapon")]
    public string Weapon { get; set; }

    [JsonProperty("tpl")]
    public string Tpl { get; set; }

    [JsonProperty("timeOfDay")]
    public string TimeOfDay { get; set; }
}

public sealed class BattlePassShopOfferDto
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("tpl")]
    public string Tpl { get; set; }

    [JsonProperty("count")]
    public int Count { get; set; }

    [JsonProperty("price")]
    public int Price { get; set; }

    [JsonProperty("stockRemaining")]
    public int? StockRemaining { get; set; }

    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("preset")]
    public bool Preset { get; set; }
}

public sealed class BuyResultDto
{
    [JsonProperty("ok")]
    public bool Ok { get; set; }

    [JsonProperty("error")]
    public string Error { get; set; }

    [JsonProperty("delivery")]
    public string Delivery { get; set; }

    [JsonProperty("offerName")]
    public string OfferName { get; set; }

    [JsonProperty("status")]
    public BattlePassStatusDto Status { get; set; }
}

public sealed class GrantResultDto
{
    [JsonProperty("ok")]
    public bool Ok { get; set; }

    [JsonProperty("error")]
    public string Error { get; set; }

    [JsonProperty("amount")]
    public int Amount { get; set; }

    [JsonProperty("status")]
    public BattlePassStatusDto Status { get; set; }
}

public sealed class RerollResultDto
{
    [JsonProperty("ok")]
    public bool Ok { get; set; }

    [JsonProperty("error")]
    public string Error { get; set; }

    [JsonProperty("bucket")]
    public string Bucket { get; set; }

    [JsonProperty("status")]
    public BattlePassStatusDto Status { get; set; }
}

public sealed class HandoverResultDto
{
    [JsonProperty("ok")]
    public bool Ok { get; set; }

    [JsonProperty("error")]
    public string Error { get; set; }

    [JsonProperty("turnedIn")]
    public int TurnedIn { get; set; }

    [JsonProperty("status")]
    public BattlePassStatusDto Status { get; set; }
}

public sealed class TrackTierStatusDto
{
    [JsonProperty("level")]
    public int Level { get; set; }

    [JsonProperty("xp")]
    public int Xp { get; set; }

    [JsonProperty("reached")]
    public bool Reached { get; set; }

    [JsonProperty("free")]
    public TrackRewardStatusDto Free { get; set; }

    [JsonProperty("premium")]
    public TrackRewardStatusDto Premium { get; set; }
}

public sealed class TrackRewardStatusDto
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("tpl")]
    public string Tpl { get; set; }

    [JsonProperty("count")]
    public int Count { get; set; }

    [JsonProperty("tickets")]
    public int Tickets { get; set; }

    [JsonProperty("preset")]
    public bool Preset { get; set; }

    [JsonProperty("claimed")]
    public bool Claimed { get; set; }

    [JsonProperty("locked")]
    public bool Locked { get; set; }
}

public sealed class PremiumResultDto
{
    [JsonProperty("ok")]
    public bool Ok { get; set; }

    [JsonProperty("error")]
    public string Error { get; set; }

    [JsonProperty("status")]
    public BattlePassStatusDto Status { get; set; }
}

public sealed class RaidEndResultDto
{
    [JsonProperty("duplicate")]
    public bool Duplicate { get; set; }

    [JsonProperty("ticketsEarned")]
    public int TicketsEarned { get; set; }

    [JsonProperty("monthlyBonus")]
    public int MonthlyBonus { get; set; }

    [JsonProperty("xpEarned")]
    public int XpEarned { get; set; }

    [JsonProperty("monthlyBonusXp")]
    public int MonthlyBonusXp { get; set; }

    [JsonProperty("updates")]
    public List<RaidChallengeUpdateDto> Updates { get; set; }

    [JsonProperty("status")]
    public BattlePassStatusDto Status { get; set; }
}

public sealed class RaidChallengeUpdateDto
{
    [JsonProperty("instanceId")]
    public string InstanceId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("previousProgress")]
    public int PreviousProgress { get; set; }

    [JsonProperty("progress")]
    public int Progress { get; set; }

    [JsonProperty("target")]
    public int Target { get; set; }

    [JsonProperty("completed")]
    public bool Completed { get; set; }

    [JsonProperty("ticketsEarned")]
    public int TicketsEarned { get; set; }

    [JsonProperty("xpEarned")]
    public int XpEarned { get; set; }
}
