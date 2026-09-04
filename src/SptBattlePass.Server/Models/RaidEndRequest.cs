using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Utils;

namespace SptBattlePass.Server.Models;

public sealed class RaidEndRequest : IRequestData
{
    [JsonPropertyName("raidId")]
    public string RaidId { get; set; } = "";

    [JsonPropertyName("survived")]
    public bool Survived { get; set; }

    [JsonPropertyName("location")]
    public string Location { get; set; } = "";

    [JsonPropertyName("isScavRaid")]
    public bool IsScavRaid { get; set; }

    [JsonPropertyName("scavKills")]
    public int ScavKills { get; set; }

    [JsonPropertyName("pmcKills")]
    public int PmcKills { get; set; }

    [JsonPropertyName("bossKills")]
    public int BossKills { get; set; }

    [JsonPropertyName("raiderKills")]
    public int RaiderKills { get; set; }

    [JsonPropertyName("rogueKills")]
    public int RogueKills { get; set; }

    [JsonPropertyName("cultistKills")]
    public int CultistKills { get; set; }

    [JsonPropertyName("headshots")]
    public int Headshots { get; set; }

    [JsonPropertyName("pmcHeadshots")]
    public int PmcHeadshots { get; set; }

    [JsonPropertyName("meleeKills")]
    public int MeleeKills { get; set; }

    [JsonPropertyName("grenadeKills")]
    public int GrenadeKills { get; set; }

    [JsonPropertyName("isNight")]
    public bool IsNight { get; set; }

    [JsonPropertyName("weaponKills")]
    public Dictionary<string, int> WeaponKills { get; set; } = [];

    [JsonPropertyName("weaponScavKills")]
    public Dictionary<string, int> WeaponScavKills { get; set; } = [];

    [JsonPropertyName("weaponPmcKills")]
    public Dictionary<string, int> WeaponPmcKills { get; set; } = [];

    [JsonPropertyName("weaponHeadshots")]
    public Dictionary<string, int> WeaponHeadshots { get; set; } = [];

    [JsonPropertyName("firItems")]
    public Dictionary<string, int> FirItems { get; set; } = [];
}
