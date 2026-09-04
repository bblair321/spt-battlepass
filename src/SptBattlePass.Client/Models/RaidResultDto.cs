using System.Collections.Generic;
using Newtonsoft.Json;

namespace SptBattlePass.Client.Models;

public sealed class RaidResultDto
{
    [JsonProperty("raidId")]
    public string RaidId { get; set; }

    [JsonProperty("survived")]
    public bool Survived { get; set; }

    [JsonProperty("location")]
    public string Location { get; set; }

    [JsonProperty("isScavRaid")]
    public bool IsScavRaid { get; set; }

    [JsonProperty("scavKills")]
    public int ScavKills { get; set; }

    [JsonProperty("pmcKills")]
    public int PmcKills { get; set; }

    [JsonProperty("bossKills")]
    public int BossKills { get; set; }

    [JsonProperty("raiderKills")]
    public int RaiderKills { get; set; }

    [JsonProperty("rogueKills")]
    public int RogueKills { get; set; }

    [JsonProperty("cultistKills")]
    public int CultistKills { get; set; }

    [JsonProperty("headshots")]
    public int Headshots { get; set; }

    [JsonProperty("pmcHeadshots")]
    public int PmcHeadshots { get; set; }

    [JsonProperty("meleeKills")]
    public int MeleeKills { get; set; }

    [JsonProperty("grenadeKills")]
    public int GrenadeKills { get; set; }

    [JsonProperty("isNight")]
    public bool IsNight { get; set; }

    [JsonProperty("weaponKills")]
    public Dictionary<string, int> WeaponKills { get; set; }

    [JsonProperty("weaponScavKills")]
    public Dictionary<string, int> WeaponScavKills { get; set; }

    [JsonProperty("weaponPmcKills")]
    public Dictionary<string, int> WeaponPmcKills { get; set; }

    [JsonProperty("weaponHeadshots")]
    public Dictionary<string, int> WeaponHeadshots { get; set; }

    [JsonProperty("firItems")]
    public Dictionary<string, int> FirItems { get; set; }
}
