using System.Text.Json.Serialization;

namespace SptBattlePass.Server.Models;

public sealed class ChallengeTemplate
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("target")]
    public int Target { get; set; }

    [JsonPropertyName("ticketReward")]
    public int TicketReward { get; set; }

    [JsonPropertyName("map")]
    public string? Map { get; set; }

    /// <summary>EFT WeapClass for KillWeapon challenges (pistol, smg, shotgun, assaultRifle, ...).</summary>
    [JsonPropertyName("weapon")]
    public string? Weapon { get; set; }

    /// <summary>Item tpl for FindInRaid / HandOver.</summary>
    [JsonPropertyName("tpl")]
    public string? Tpl { get; set; }

    /// <summary>day or night. When set, the raid TOD must match.</summary>
    [JsonPropertyName("timeOfDay")]
    public string? TimeOfDay { get; set; }
}
