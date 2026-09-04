using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Utils;

namespace SptBattlePass.Server.Models;

public sealed class BuyRequest : IRequestData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
}

public sealed class BuyResult
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("delivery")]
    public string? Delivery { get; set; }

    [JsonPropertyName("offerName")]
    public string? OfferName { get; set; }

    [JsonPropertyName("status")]
    public BattlePassStatus? Status { get; set; }
}

public sealed class GrantResult
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("status")]
    public BattlePassStatus? Status { get; set; }
}

public sealed class RerollRequest : IRequestData
{
    [JsonPropertyName("bucket")]
    public string Bucket { get; set; } = "";
}

public sealed class RerollResult
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("bucket")]
    public string? Bucket { get; set; }

    [JsonPropertyName("status")]
    public BattlePassStatus? Status { get; set; }
}

public sealed class HandoverRequest : IRequestData
{
    [JsonPropertyName("instanceId")]
    public string InstanceId { get; set; } = "";
}

public sealed class HandoverResult
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("turnedIn")]
    public int TurnedIn { get; set; }

    [JsonPropertyName("status")]
    public BattlePassStatus? Status { get; set; }
}

public sealed class PremiumRequest : IRequestData
{
    [JsonPropertyName("debug")]
    public bool Debug { get; set; }
}

public sealed class PremiumResult
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("status")]
    public BattlePassStatus? Status { get; set; }
}
