using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;
using SptBattlePass.Server.Models;

namespace SptBattlePass.Server.Services;

[Injectable(InjectionType.Singleton)]
public class ChallengeCatalog
{
    public IReadOnlyList<ChallengeTemplate> Daily { get; }
    public IReadOnlyList<ChallengeTemplate> Weekly { get; }
    public IReadOnlyList<ChallengeTemplate> Monthly { get; }
    public IReadOnlyList<BattlePassShopOffer> Shop { get; }
    public IReadOnlyList<CrateOffer> Crate { get; }
    public IReadOnlyList<TrackTier> Track { get; }
    public ModConfig Config { get; }
    public IReadOnlyDictionary<string, SeasonPack> Themes { get; }

    public ChallengeCatalog(ISptLogger<ChallengeCatalog> logger, JsonUtil jsonUtil)
    {
        string root = Path.Combine(Path.GetDirectoryName(typeof(ChallengeCatalog).Assembly.Location) ?? ".", "db");
        Daily = LoadList<ChallengeTemplate>(jsonUtil, logger, Path.Combine(root, "daily.json"));
        Weekly = LoadList<ChallengeTemplate>(jsonUtil, logger, Path.Combine(root, "weekly.json"));
        Monthly = LoadList<ChallengeTemplate>(jsonUtil, logger, Path.Combine(root, "monthly.json"));
        Shop = LoadList<BattlePassShopOffer>(jsonUtil, logger, Path.Combine(root, "shop.json"));
        Crate = LoadList<CrateOffer>(jsonUtil, logger, Path.Combine(root, "crate.json"));
        Track = LoadTrack(jsonUtil, logger, Path.Combine(root, "track.json"));
        Config = LoadConfig(jsonUtil, logger, Path.Combine(root, "config.json"));
        Themes = LoadThemes(jsonUtil, logger, Path.Combine(root, "themes"), Shop, Crate, Track);
        logger.Info($"[BattlePass] catalog loaded: daily={Daily.Count} weekly={Weekly.Count} monthly={Monthly.Count} shop={Shop.Count} crate={Crate.Count} track={Track.Count} themes={Themes.Count} catalogVersion={Config.CatalogVersion} debug={Config.DebugGrants}");
    }

    public string ThemeIdFor(string? seasonId)
    {
        string month = (seasonId ?? "").Trim();
        if (month.Length > 0
            && Config.SeasonThemes != null
            && Config.SeasonThemes.TryGetValue(month, out string? theme)
            && !string.IsNullOrWhiteSpace(theme)
            && Themes.ContainsKey(theme.Trim()))
        {
            return theme.Trim();
        }

        return "";
    }

    public SeasonThemeInfo ThemeFor(string? seasonId)
    {
        string id = ThemeIdFor(seasonId);
        if (id.Length > 0 && Themes.TryGetValue(id, out SeasonPack? pack))
        {
            return pack.Theme;
        }

        return new SeasonThemeInfo { Id = "default", Name = "Default kit", Blurb = "Root shop, track, and crate." };
    }

    public IReadOnlyList<BattlePassShopOffer> ShopFor(string? seasonId)
    {
        SeasonPack? pack = PackFor(seasonId);
        return pack != null && pack.Shop.Count > 0 ? pack.Shop : Shop;
    }

    public IReadOnlyList<CrateOffer> CrateFor(string? seasonId)
    {
        SeasonPack? pack = PackFor(seasonId);
        return pack != null && pack.Crate.Count > 0 ? pack.Crate : Crate;
    }

    public IReadOnlyList<TrackTier> TrackFor(string? seasonId)
    {
        SeasonPack? pack = PackFor(seasonId);
        return pack != null && pack.Track.Count > 0 ? pack.Track : Track;
    }

    public IEnumerable<(string Source, string? Tpl)> AllItemTpls()
    {
        foreach (ChallengeTemplate challenge in Daily.Concat(Weekly).Concat(Monthly))
        {
            if (!string.IsNullOrWhiteSpace(challenge.Tpl))
            {
                yield return ($"challenge:{challenge.Id}", challenge.Tpl);
            }
        }

        foreach ((string source, IReadOnlyList<BattlePassShopOffer> shop) in ShopSources())
        {
            foreach (BattlePassShopOffer offer in shop)
            {
                if (!string.IsNullOrWhiteSpace(offer.Tpl))
                {
                    yield return ($"{source}/shop/{offer.Id}", offer.Tpl);
                }
            }
        }

        foreach ((string source, IReadOnlyList<CrateOffer> crate) in CrateSources())
        {
            foreach (CrateOffer offer in crate)
            {
                if (!string.IsNullOrWhiteSpace(offer.Tpl))
                {
                    yield return ($"{source}/crate/{offer.Id}", offer.Tpl);
                }
            }
        }

        foreach ((string source, IReadOnlyList<TrackTier> track) in TrackSources())
        {
            foreach (TrackTier tier in track)
            {
                if (!string.IsNullOrWhiteSpace(tier.Free?.Tpl))
                {
                    yield return ($"{source}/track/{tier.Level}/free", tier.Free.Tpl);
                }

                if (!string.IsNullOrWhiteSpace(tier.Premium?.Tpl))
                {
                    yield return ($"{source}/track/{tier.Level}/premium", tier.Premium.Tpl);
                }
            }
        }
    }

    private SeasonPack? PackFor(string? seasonId)
    {
        string id = ThemeIdFor(seasonId);
        return id.Length > 0 && Themes.TryGetValue(id, out SeasonPack? pack) ? pack : null;
    }

    private IEnumerable<(string Source, IReadOnlyList<BattlePassShopOffer> Shop)> ShopSources()
    {
        yield return ("default", Shop);
        foreach (KeyValuePair<string, SeasonPack> entry in Themes)
        {
            yield return (entry.Key, entry.Value.Shop);
        }
    }

    private IEnumerable<(string Source, IReadOnlyList<CrateOffer> Crate)> CrateSources()
    {
        yield return ("default", Crate);
        foreach (KeyValuePair<string, SeasonPack> entry in Themes)
        {
            yield return (entry.Key, entry.Value.Crate);
        }
    }

    private IEnumerable<(string Source, IReadOnlyList<TrackTier> Track)> TrackSources()
    {
        yield return ("default", Track);
        foreach (KeyValuePair<string, SeasonPack> entry in Themes)
        {
            yield return (entry.Key, entry.Value.Track);
        }
    }

    private static Dictionary<string, SeasonPack> LoadThemes(
        JsonUtil jsonUtil,
        ISptLogger<ChallengeCatalog> logger,
        string themesRoot,
        IReadOnlyList<BattlePassShopOffer> fallbackShop,
        IReadOnlyList<CrateOffer> fallbackCrate,
        IReadOnlyList<TrackTier> fallbackTrack)
    {
        var themes = new Dictionary<string, SeasonPack>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(themesRoot))
        {
            return themes;
        }

        foreach (string folder in Directory.GetDirectories(themesRoot))
        {
            string id = Path.GetFileName(folder);
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            SeasonThemeInfo theme = LoadThemeInfo(jsonUtil, Path.Combine(folder, "theme.json"), id);
            IReadOnlyList<BattlePassShopOffer> shop = LoadList<BattlePassShopOffer>(jsonUtil, logger, Path.Combine(folder, "shop.json"));
            IReadOnlyList<CrateOffer> crate = LoadList<CrateOffer>(jsonUtil, logger, Path.Combine(folder, "crate.json"));
            IReadOnlyList<TrackTier> track = LoadTrack(jsonUtil, logger, Path.Combine(folder, "track.json"));
            themes[id] = new SeasonPack
            {
                Theme = theme,
                Shop = shop.Count > 0 ? shop : fallbackShop,
                Crate = crate.Count > 0 ? crate : fallbackCrate,
                Track = track.Count > 0 ? track : fallbackTrack
            };
            logger.Info($"[BattlePass] theme '{id}' shop={themes[id].Shop.Count} crate={themes[id].Crate.Count} track={themes[id].Track.Count}");
        }

        return themes;
    }

    private static SeasonThemeInfo LoadThemeInfo(JsonUtil jsonUtil, string path, string fallbackId)
    {
        if (File.Exists(path))
        {
            SeasonThemeInfo? loaded = jsonUtil.DeserializeFromFile<SeasonThemeInfo>(path);
            if (loaded != null)
            {
                if (string.IsNullOrWhiteSpace(loaded.Id))
                {
                    loaded.Id = fallbackId;
                }

                if (string.IsNullOrWhiteSpace(loaded.Name))
                {
                    loaded.Name = fallbackId;
                }

                return loaded;
            }
        }

        return new SeasonThemeInfo { Id = fallbackId, Name = fallbackId, Blurb = "" };
    }

    private static ModConfig LoadConfig(JsonUtil jsonUtil, ISptLogger<ChallengeCatalog> logger, string path)
    {
        if (!File.Exists(path))
        {
            logger.Warning($"[BattlePass] missing config file: {path}, using defaults");
            return new ModConfig();
        }

        return jsonUtil.DeserializeFromFile<ModConfig>(path) ?? new ModConfig();
    }

    private static List<TrackTier> LoadTrack(JsonUtil jsonUtil, ISptLogger<ChallengeCatalog> logger, string path)
    {
        return LoadList<TrackTier>(jsonUtil, logger, path)
            .Where(tier => tier.Level > 0 && tier.Xp > 0)
            .OrderBy(tier => tier.Level)
            .ToList();
    }

    private static List<T> LoadList<T>(JsonUtil jsonUtil, ISptLogger<ChallengeCatalog> logger, string path)
    {
        if (!File.Exists(path))
        {
            logger.Error($"[BattlePass] missing catalog file: {path}");
            return [];
        }

        return jsonUtil.DeserializeFromFile<List<T>>(path) ?? [];
    }
}
