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

    public ChallengeCatalog(ISptLogger<ChallengeCatalog> logger, JsonUtil jsonUtil)
    {
        string root = Path.Combine(Path.GetDirectoryName(typeof(ChallengeCatalog).Assembly.Location) ?? ".", "db");
        Daily = LoadList<ChallengeTemplate>(jsonUtil, logger, Path.Combine(root, "daily.json"));
        Weekly = LoadList<ChallengeTemplate>(jsonUtil, logger, Path.Combine(root, "weekly.json"));
        Monthly = LoadList<ChallengeTemplate>(jsonUtil, logger, Path.Combine(root, "monthly.json"));
        Shop = LoadList<BattlePassShopOffer>(jsonUtil, logger, Path.Combine(root, "shop.json"));
        Crate = LoadList<CrateOffer>(jsonUtil, logger, Path.Combine(root, "crate.json"));
        Track = LoadList<TrackTier>(jsonUtil, logger, Path.Combine(root, "track.json"))
            .Where(tier => tier.Level > 0 && tier.Xp > 0)
            .OrderBy(tier => tier.Level)
            .ToList();
        Config = LoadConfig(jsonUtil, logger, Path.Combine(root, "config.json"));
        logger.Info($"[BattlePass] catalog loaded: daily={Daily.Count} weekly={Weekly.Count} monthly={Monthly.Count} shop={Shop.Count} crate={Crate.Count} track={Track.Count} debug={Config.DebugGrants}");
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
