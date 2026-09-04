using System.Text.Json.Serialization;

namespace SptBattlePass.Server.Models;

public sealed class CrateOffer
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("tpl")]
    public string Tpl { get; set; } = "";

    [JsonPropertyName("count")]
    public int Count { get; set; } = 1;

    [JsonPropertyName("ticketValue")]
    public int TicketValue { get; set; } = 1;

    [JsonPropertyName("max")]
    public int Max { get; set; } = 1;
}
