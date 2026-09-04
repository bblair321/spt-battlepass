using System.Text.Json.Serialization;

namespace SptBattlePass.Server.Models;

public sealed class TrackTier
{
    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("xp")]
    public int Xp { get; set; }

    [JsonPropertyName("free")]
    public TrackReward? Free { get; set; }

    [JsonPropertyName("premium")]
    public TrackReward? Premium { get; set; }
}

public sealed class TrackReward
{
    [JsonPropertyName("tickets")]
    public int Tickets { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("tpl")]
    public string? Tpl { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; } = 1;

    [JsonPropertyName("preset")]
    public bool Preset { get; set; }
}

public sealed class TrackTierStatus
{
    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("xp")]
    public int Xp { get; set; }

    [JsonPropertyName("reached")]
    public bool Reached { get; set; }

    [JsonPropertyName("free")]
    public TrackRewardStatus? Free { get; set; }

    [JsonPropertyName("premium")]
    public TrackRewardStatus? Premium { get; set; }
}

public sealed class TrackRewardStatus
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("tpl")]
    public string? Tpl { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("tickets")]
    public int Tickets { get; set; }

    [JsonPropertyName("preset")]
    public bool Preset { get; set; }

    [JsonPropertyName("claimed")]
    public bool Claimed { get; set; }

    [JsonPropertyName("locked")]
    public bool Locked { get; set; }
}
