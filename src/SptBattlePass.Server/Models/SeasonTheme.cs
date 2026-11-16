using System.Text.Json.Serialization;

namespace SptBattlePass.Server.Models;

public sealed class SeasonThemeInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("blurb")]
    public string Blurb { get; set; } = "";
}

public sealed class SeasonPack
{
    public SeasonThemeInfo Theme { get; set; } = new();
    public IReadOnlyList<BattlePassShopOffer> Shop { get; set; } = [];
    public IReadOnlyList<CrateOffer> Crate { get; set; } = [];
    public IReadOnlyList<TrackTier> Track { get; set; } = [];
}

public sealed class CratePreview
{
    [JsonPropertyName("grade")]
    public string Grade { get; set; } = "";

    [JsonPropertyName("tickets")]
    public int Tickets { get; set; }

    [JsonPropertyName("items")]
    public List<CratePreviewItem> Items { get; set; } = [];
}

public sealed class CratePreviewItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("tpl")]
    public string Tpl { get; set; } = "";

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("preset")]
    public bool Preset { get; set; }
}
