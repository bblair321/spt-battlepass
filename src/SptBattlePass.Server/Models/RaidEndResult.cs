using System.Text.Json.Serialization;

namespace SptBattlePass.Server.Models;

public sealed class RaidEndResult
{
    [JsonPropertyName("duplicate")]
    public bool Duplicate { get; set; }

    [JsonPropertyName("ticketsEarned")]
    public int TicketsEarned { get; set; }

    [JsonPropertyName("monthlyBonus")]
    public int MonthlyBonus { get; set; }

    [JsonPropertyName("xpEarned")]
    public int XpEarned { get; set; }

    [JsonPropertyName("monthlyBonusXp")]
    public int MonthlyBonusXp { get; set; }

    [JsonPropertyName("updates")]
    public List<RaidChallengeUpdate> Updates { get; set; } = [];

    [JsonPropertyName("status")]
    public BattlePassStatus Status { get; set; } = new();
}

public sealed class RaidChallengeUpdate
{
    [JsonPropertyName("instanceId")]
    public string InstanceId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("previousProgress")]
    public int PreviousProgress { get; set; }

    [JsonPropertyName("progress")]
    public int Progress { get; set; }

    [JsonPropertyName("target")]
    public int Target { get; set; }

    [JsonPropertyName("completed")]
    public bool Completed { get; set; }

    [JsonPropertyName("ticketsEarned")]
    public int TicketsEarned { get; set; }

    [JsonPropertyName("xpEarned")]
    public int XpEarned { get; set; }
}
