using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services.Modding;
using SptBattlePass.Server.Models;

namespace SptBattlePass.Server.Services;

[Injectable(InjectionType.Singleton)]
public class BattlePassService(
    ISptLogger<BattlePassService> logger,
    ProfileDataService profileDataService,
    ChallengeCatalog catalog,
    ShopDelivery shopDelivery,
    SaveServer saveServer,
    ProfileHelper profileHelper)
{
    public const string ProfileModKey = "com.bblai.battlepass";
    private const int MaxTrackedRaidIds = 24;
    private const string RoubleTpl = "5449016a4bdc2d6f028b456f";

    public async Task<BattlePassStatus> GetStatusAsync(MongoId sessionId)
    {
        var state = await LoadAsync(sessionId);
        RolloverResult rollover = ApplyRollovers(state, sessionId);
        if (rollover.Changed)
        {
            await SaveAsync(sessionId, state);
        }

        if (rollover.MailedCrate)
        {
            await saveServer.SaveProfileAsync(sessionId);
        }

        return ToStatus(state);
    }

    public async Task<RaidEndResult> ApplyRaidAsync(MongoId sessionId, RaidEndRequest raid)
    {
        var state = await LoadAsync(sessionId);
        RolloverResult raidRollover = ApplyRollovers(state, sessionId);
        if (raidRollover.Changed)
        {
            await SaveAsync(sessionId, state);
        }

        if (raidRollover.MailedCrate)
        {
            await saveServer.SaveProfileAsync(sessionId);
        }

        string raidId = string.IsNullOrWhiteSpace(raid.RaidId)
            ? $"anon:{raid.Location}:{raid.IsScavRaid}:{raid.Survived}:{raid.ScavKills}:{raid.PmcKills}:{raid.BossKills}:{raid.RaiderKills}:{raid.RogueKills}:{raid.CultistKills}:{raid.Headshots}:{raid.PmcHeadshots}"
            : raid.RaidId;

        if (state.ProcessedRaidIds.Contains(raidId))
        {
            logger.Info($"[BattlePass] raid {raidId} already applied for {sessionId}");
            return ToRaidEndResult(state, duplicate: true);
        }

        var tally = new RaidTally
        {
            Scavs = Clamp(raid.ScavKills),
            Pmcs = Clamp(raid.PmcKills),
            Bosses = Clamp(raid.BossKills),
            Raiders = Clamp(raid.RaiderKills),
            Rogues = Clamp(raid.RogueKills),
            Cultists = Clamp(raid.CultistKills),
            Headshots = Clamp(raid.Headshots),
            PmcHeadshots = Clamp(raid.PmcHeadshots),
            Melee = Clamp(raid.MeleeKills),
            Grenades = Clamp(raid.GrenadeKills),
            Survived = raid.Survived,
            IsScavRaid = raid.IsScavRaid,
            IsNight = raid.IsNight,
            Location = raid.Location ?? "",
            WeaponKills = ClampDict(raid.WeaponKills),
            WeaponScavKills = ClampDict(raid.WeaponScavKills),
            WeaponPmcKills = ClampDict(raid.WeaponPmcKills),
            WeaponHeadshots = ClampDict(raid.WeaponHeadshots),
            FirItems = ClampDict(raid.FirItems)
        };

        Dictionary<string, ChallengeSnap> before = SnapshotChallenges(state);
        bool monthlyBonusAlreadyClaimed = state.MonthlyBonusClaimed;
        int ticketsBefore = state.Tickets;

        ApplyToBucket(state.Challenges.Daily, tally);
        ApplyToBucket(state.Challenges.Weekly, tally);
        ApplyToBucket(state.Challenges.Monthly, tally);
        ClaimReadyRewards(state);
        TryGrantMonthlyBonus(state);
        bool trackMailed = GrantTrackRewards(sessionId, state);

        state.ProcessedRaidIds.Add(raidId);
        if (state.ProcessedRaidIds.Count > MaxTrackedRaidIds)
        {
            state.ProcessedRaidIds.RemoveRange(0, state.ProcessedRaidIds.Count - MaxTrackedRaidIds);
        }

        await SaveAsync(sessionId, state);
        if (trackMailed)
        {
            await saveServer.SaveProfileAsync(sessionId);
        }

        RaidEndResult result = ToRaidEndResult(
            state,
            duplicate: false,
            before,
            ticketsBefore,
            monthlyBonusAlreadyClaimed);
        if (result.XpEarned > 0)
        {
            try
            {
                profileHelper.AddExperienceToPmc(sessionId, result.XpEarned);
                await saveServer.SaveProfileAsync(sessionId);
                AddXp(state, result.XpEarned);
                await SaveAsync(sessionId, state);
                result.Status = ToStatus(state);
            }
            catch (Exception exception)
            {
                logger.Error($"[BattlePass] XP grant failed: {exception.Message}");
                result.XpEarned = 0;
                result.MonthlyBonusXp = 0;
                foreach (RaidChallengeUpdate update in result.Updates)
                {
                    update.XpEarned = 0;
                }
            }
        }

            logger.Info($"[BattlePass] raid applied session={sessionId} survived={tally.Survived} scavRaid={tally.IsScavRaid} night={tally.IsNight} loc={tally.Location} scav={tally.Scavs} pmc={tally.Pmcs} boss={tally.Bosses} raider={tally.Raiders} rogue={tally.Rogues} cultist={tally.Cultists} hs={tally.Headshots} pmcHs={tally.PmcHeadshots} melee={tally.Melee} nade={tally.Grenades} tickets={state.Tickets} xp={result.XpEarned}");
        return result;
    }

    public async Task<BuyResult> BuyAsync(MongoId sessionId, string offerId)
    {
        var state = await LoadAsync(sessionId);
        RolloverResult buyRollover = ApplyRollovers(state, sessionId);
        if (buyRollover.Changed)
        {
            await SaveAsync(sessionId, state);
        }

        if (buyRollover.MailedCrate)
        {
            await saveServer.SaveProfileAsync(sessionId);
        }

        BattlePassShopOffer? offer = state.Shop.FirstOrDefault(item => item.Id == offerId);
        if (offer == null || string.IsNullOrWhiteSpace(offer.Tpl))
        {
            return Fail("unknown_offer", state);
        }

        if (offer.StockRemaining is <= 0)
        {
            return Fail("out_of_stock", state);
        }

        if (state.Tickets < offer.Price)
        {
            return Fail("insufficient_tickets", state);
        }

        string delivery;
        try
        {
            delivery = shopDelivery.Deliver(sessionId, offer);
        }
        catch (Exception exception)
        {
            logger.Error($"[BattlePass] buy failed for {offerId}: {exception.Message}");
            return Fail("invalid_item", state);
        }

        state.Tickets -= offer.Price;
        AddSpent(state, offer.Price);
        if (offer.StockRemaining != null)
        {
            offer.StockRemaining--;
        }

        await SaveAsync(sessionId, state);
        await saveServer.SaveProfileAsync(sessionId);
        logger.Info($"[BattlePass] bought {offerId} via {delivery} session={sessionId} tickets={state.Tickets}");
        return new BuyResult
        {
            Ok = true,
            Delivery = delivery,
            OfferName = offer.Name,
            Status = ToStatus(state)
        };
    }

    public async Task<GrantResult> GrantAsync(MongoId sessionId)
    {
        if (!catalog.Config.DebugGrants)
        {
            var state = await LoadAsync(sessionId);
            RolloverResult grantRollover = ApplyRollovers(state, sessionId);
            if (grantRollover.Changed)
            {
                await SaveAsync(sessionId, state);
            }

            return new GrantResult
            {
                Ok = false,
                Error = "disabled",
                Status = ToStatus(state)
            };
        }

        var profile = await LoadAsync(sessionId);
        ApplyRollovers(profile, sessionId);
        int amount = Math.Clamp(catalog.Config.GrantAmount, 1, 100);
        profile.Tickets += amount;
        await SaveAsync(sessionId, profile);
        logger.Info($"[BattlePass] debug grant +{amount} session={sessionId} tickets={profile.Tickets}");
        return new GrantResult
        {
            Ok = true,
            Amount = amount,
            Status = ToStatus(profile)
        };
    }

    public async Task<RerollResult> RerollAsync(MongoId sessionId, string bucket)
    {
        var state = await LoadAsync(sessionId);
        RolloverResult rollover = ApplyRollovers(state, sessionId);
        if (rollover.Changed)
        {
            await SaveAsync(sessionId, state);
        }

        string kind = (bucket ?? "").Trim().ToLowerInvariant();
        DateTime now = SeasonClock.UtcNow;
        string profileId = sessionId.ToString();

        if (kind == "daily")
        {
            RerollResult? blocked = ValidateReroll(
                state.Challenges.Daily,
                state.Tickets,
                state.DailyRerolls,
                catalog.Config.DailyRerollCost,
                catalog.Config.DailyRerollMax);
            if (blocked != null)
            {
                blocked.Status = ToStatus(state);
                return blocked;
            }

            int cost = Math.Max(0, catalog.Config.DailyRerollCost);
            state.Tickets -= cost;
            AddSpent(state, cost);
            state.DailyRerolls++;
            state.Challenges.Daily = Roll(
                catalog.Daily,
                "daily",
                profileId,
                state.DailyKey,
                SeasonClock.DailyExpiryIso(now),
                "|r" + state.DailyRerolls);
        }
        else if (kind == "weekly")
        {
            RerollResult? blocked = ValidateReroll(
                state.Challenges.Weekly,
                state.Tickets,
                state.WeeklyRerolls,
                catalog.Config.WeeklyRerollCost,
                catalog.Config.WeeklyRerollMax);
            if (blocked != null)
            {
                blocked.Status = ToStatus(state);
                return blocked;
            }

            int cost = Math.Max(0, catalog.Config.WeeklyRerollCost);
            state.Tickets -= cost;
            AddSpent(state, cost);
            state.WeeklyRerolls++;
            state.Challenges.Weekly = Roll(
                catalog.Weekly,
                "weekly",
                profileId,
                state.WeeklyKey,
                SeasonClock.WeeklyExpiryIso(now),
                "|r" + state.WeeklyRerolls);
        }
        else
        {
            return new RerollResult { Ok = false, Error = "unknown_bucket", Status = ToStatus(state) };
        }

        await SaveAsync(sessionId, state);
        logger.Info($"[BattlePass] reroll {kind} session={sessionId} tickets={state.Tickets}");
        return new RerollResult { Ok = true, Bucket = kind, Status = ToStatus(state) };
    }

    public async Task<HandoverResult> HandoverAsync(MongoId sessionId, string instanceId)
    {
        var state = await LoadAsync(sessionId);
        RolloverResult rollover = ApplyRollovers(state, sessionId);
        if (rollover.Changed)
        {
            await SaveAsync(sessionId, state);
        }

        BattlePassChallenge? challenge = AllChallenges(state)
            .FirstOrDefault(item => item.InstanceId == instanceId);
        if (challenge == null)
        {
            return new HandoverResult { Ok = false, Error = "unknown_challenge", Status = ToStatus(state) };
        }

        if (challenge.Type != "HandOver")
        {
            return new HandoverResult { Ok = false, Error = "wrong_type", Status = ToStatus(state) };
        }

        if (challenge.Completed || string.IsNullOrWhiteSpace(challenge.Tpl))
        {
            return new HandoverResult { Ok = false, Error = "already_complete", Status = ToStatus(state) };
        }

        int need = Math.Max(0, challenge.Target - challenge.Progress);
        var pmc = profileHelper.GetPmcProfile(sessionId);
        StashRemoval removal = RemoveFromStash(pmc, challenge.Tpl, need);
        int turnedIn = removal.Taken;
        if (turnedIn <= 0)
        {
            return new HandoverResult { Ok = false, Error = "insufficient_items", Status = ToStatus(state) };
        }

        Dictionary<string, ChallengeSnap> before = SnapshotChallenges(state);
        bool monthlyBonusAlreadyClaimed = state.MonthlyBonusClaimed;
        int ticketsBefore = state.Tickets;
        challenge.Progress = Math.Min(challenge.Target, challenge.Progress + turnedIn);
        if (challenge.Progress >= challenge.Target)
        {
            challenge.Completed = true;
        }

        challenge.State = ResolveState(challenge);
        ClaimReadyRewards(state);
        TryGrantMonthlyBonus(state);
        GrantTrackRewards(sessionId, state);
        await SaveAsync(sessionId, state);
        await saveServer.SaveProfileAsync(sessionId);

        RaidEndResult xp = ToRaidEndResult(state, duplicate: false, before, ticketsBefore, monthlyBonusAlreadyClaimed);
        if (xp.XpEarned > 0)
        {
            try
            {
                profileHelper.AddExperienceToPmc(sessionId, xp.XpEarned);
                await saveServer.SaveProfileAsync(sessionId);
                AddXp(state, xp.XpEarned);
                await SaveAsync(sessionId, state);
            }
            catch (Exception exception)
            {
                logger.Error($"[BattlePass] handover XP grant failed: {exception.Message}");
            }
        }

        logger.Info($"[BattlePass] handover {instanceId} turnedIn={turnedIn} session={sessionId} tickets={state.Tickets}");
        return new HandoverResult
        {
            Ok = true,
            TurnedIn = turnedIn,
            Status = ToStatus(state),
            StashChanges = removal.Changes
        };
    }

    public async Task<PremiumResult> UnlockPremiumAsync(MongoId sessionId, bool debug)
    {
        var state = await LoadAsync(sessionId);
        RolloverResult rollover = ApplyRollovers(state, sessionId);
        if (rollover.Changed)
        {
            await SaveAsync(sessionId, state);
        }

        if (rollover.MailedCrate)
        {
            await saveServer.SaveProfileAsync(sessionId);
        }

        if (state.Premium)
        {
            return new PremiumResult { Ok = false, Error = "already_premium", Status = ToStatus(state) };
        }

        List<StashItemChange>? stashChanges = null;
        if (debug)
        {
            if (!catalog.Config.DebugGrants)
            {
                return new PremiumResult { Ok = false, Error = "disabled", Status = ToStatus(state) };
            }
        }
        else
        {
            int cost = Math.Max(0, catalog.Config.PremiumCostRoubles);
            if (cost > 0)
            {
                var pmc = profileHelper.GetPmcProfile(sessionId);
                if (CountInStash(pmc, RoubleTpl) < cost)
                {
                    return new PremiumResult { Ok = false, Error = "insufficient_roubles", Status = ToStatus(state) };
                }

                StashRemoval removal = RemoveFromStash(pmc, RoubleTpl, cost);
                if (removal.Taken < cost)
                {
                    return new PremiumResult { Ok = false, Error = "insufficient_roubles", Status = ToStatus(state) };
                }

                stashChanges = removal.Changes;
            }
        }

        state.Premium = true;
        GrantTrackRewards(sessionId, state);
        await SaveAsync(sessionId, state);
        await saveServer.SaveProfileAsync(sessionId);
        logger.Info($"[BattlePass] premium unlocked session={sessionId} debug={debug} trackXp={state.TrackXp}");
        return new PremiumResult { Ok = true, Status = ToStatus(state), StashChanges = stashChanges };
    }

    private static RerollResult? ValidateReroll(
        List<BattlePassChallenge> challenges,
        int tickets,
        int used,
        int cost,
        int max)
    {
        if (cost <= 0 || max <= 0)
        {
            return new RerollResult { Ok = false, Error = "disabled" };
        }

        if (challenges.Any(challenge => challenge.Completed))
        {
            return new RerollResult { Ok = false, Error = "already_complete" };
        }

        if (used >= max)
        {
            return new RerollResult { Ok = false, Error = "max_rerolls" };
        }

        if (tickets < cost)
        {
            return new RerollResult { Ok = false, Error = "insufficient_tickets" };
        }

        return null;
    }

    private BuyResult Fail(string error, BattlePassProfileState state)
    {
        return new BuyResult
        {
            Ok = false,
            Error = error,
            Status = ToStatus(state)
        };
    }

    private async Task<BattlePassProfileState> LoadAsync(MongoId sessionId)
    {
        return await profileDataService.GetProfileDataAsync<BattlePassProfileState>(sessionId, ProfileModKey)
               ?? new BattlePassProfileState();
    }

    private async Task SaveAsync(MongoId sessionId, BattlePassProfileState state)
    {
        Directory.CreateDirectory(System.IO.Path.Combine("user", "profileData", sessionId.ToString()));
        await profileDataService.SaveProfileDataAsync(sessionId, ProfileModKey, state);
    }

    private sealed class RolloverResult
    {
        public bool Changed { get; init; }
        public bool MailedCrate { get; init; }
    }

    private RolloverResult ApplyRollovers(BattlePassProfileState state, MongoId sessionId)
    {
        DateTime now = SeasonClock.UtcNow;
        string profileId = sessionId.ToString();
        string dailyKey = SeasonClock.DailyKey(now);
        string weeklyKey = SeasonClock.WeeklyKey(now);
        string monthlyKey = SeasonClock.MonthlyKey(now);
        bool changed = false;
        bool mailedCrate = false;

        if (state.MonthlyKey != monthlyKey)
        {
            int leftover = state.Tickets;
            string previousSeason = state.MonthlyKey;
            if (leftover > 0 && !string.IsNullOrEmpty(previousSeason))
            {
                mailedCrate = SendConsolationCrate(sessionId, leftover, previousSeason);
                if (mailedCrate)
                {
                    state.LastCrateSeasonId = previousSeason;
                    state.LastCrateTickets = leftover;
                }
            }

            if (!string.IsNullOrEmpty(previousSeason))
            {
                state.LastSeasonId = previousSeason;
                state.LastSeasonTicketsEarned = state.TicketsEarnedSeason;
                state.LastSeasonChallengesCompleted = state.ChallengesCompletedSeason;
            }

            ResetSeasonStats(state);
            state.SeasonId = monthlyKey;
            state.Tickets = 0;
            state.Premium = false;
            state.TrackXp = 0;
            state.TrackFreeClaimed = 0;
            state.TrackPremiumClaimed = 0;
            state.MonthlyBonusClaimed = false;
            state.MonthlyKey = monthlyKey;
            state.Challenges.Monthly = Roll(catalog.Monthly, "monthly", profileId, monthlyKey, SeasonClock.MonthlyExpiryIso(now));
            state.Shop = CloneShop();
            changed = true;
            logger.Info($"[BattlePass] monthly rollover {profileId} {previousSeason} -> {monthlyKey} leftover={leftover} crate={mailedCrate}");
        }

        if (state.WeeklyKey != weeklyKey)
        {
            state.WeeklyKey = weeklyKey;
            state.WeeklyRerolls = 0;
            state.Challenges.Weekly = Roll(catalog.Weekly, "weekly", profileId, weeklyKey, SeasonClock.WeeklyExpiryIso(now));
            changed = true;
        }

        if (state.DailyKey != dailyKey)
        {
            state.DailyKey = dailyKey;
            state.DailyRerolls = 0;
            state.Challenges.Daily = Roll(catalog.Daily, "daily", profileId, dailyKey, SeasonClock.DailyExpiryIso(now));
            changed = true;
        }

        if (MergeShopFromCatalog(state))
        {
            changed = true;
        }

        if (string.IsNullOrEmpty(state.SeasonId))
        {
            state.SeasonId = monthlyKey;
            changed = true;
        }

        return new RolloverResult { Changed = changed, MailedCrate = mailedCrate };
    }

    private bool SendConsolationCrate(MongoId sessionId, int leftoverTickets, string previousSeason)
    {
        List<(string Tpl, int Count)> contents = ConvertTicketsToCrate(leftoverTickets);
        if (contents.Count == 0)
        {
            logger.Warning($"[BattlePass] crate conversion produced nothing for {leftoverTickets} tickets");
            return false;
        }

        string seasonName = SeasonClock.SeasonName(ParseSeasonMonth(previousSeason, SeasonClock.UtcNow));
        return shopDelivery.DeliverMail(
            sessionId,
            $"Battle Pass {seasonName} ended. Your {leftoverTickets} unspent tickets were converted into a consolation crate. Collect it from Messages.",
            contents);
    }

    private List<(string Tpl, int Count)> ConvertTicketsToCrate(int tickets)
    {
        const int maxStacks = 10;
        var contents = new List<(string Tpl, int Count)>();
        int remaining = tickets;
        foreach (CrateOffer offer in catalog.Crate
                     .Where(entry => entry.TicketValue > 0 && !string.IsNullOrWhiteSpace(entry.Tpl))
                     .OrderByDescending(entry => entry.TicketValue)
                     .ThenBy(entry => entry.Id))
        {
            int taken = 0;
            int cap = Math.Max(1, offer.Max);
            while (remaining >= offer.TicketValue && taken < cap && contents.Count < maxStacks)
            {
                contents.Add((offer.Tpl, Math.Max(1, offer.Count)));
                remaining -= offer.TicketValue;
                taken++;
            }

            if (contents.Count >= maxStacks)
            {
                break;
            }
        }

        return contents;
    }

    private static DateTime ParseSeasonMonth(string monthlyKey, DateTime fallback)
    {
        return DateTime.TryParseExact(
            monthlyKey + "-01",
            "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
            out DateTime parsed)
            ? parsed
            : fallback;
    }

    private bool MergeShopFromCatalog(BattlePassProfileState state)
    {
        bool changed = false;
        var existing = state.Shop.ToDictionary(offer => offer.Id, StringComparer.OrdinalIgnoreCase);
        foreach (BattlePassShopOffer catalogOffer in catalog.Shop)
        {
            if (existing.TryGetValue(catalogOffer.Id, out BattlePassShopOffer? current))
            {
                if (current.Name != catalogOffer.Name
                    || current.Tpl != catalogOffer.Tpl
                    || current.Count != catalogOffer.Count
                    || current.Price != catalogOffer.Price
                    || current.Category != catalogOffer.Category
                    || current.Preset != catalogOffer.Preset)
                {
                    current.Name = catalogOffer.Name;
                    current.Tpl = catalogOffer.Tpl;
                    current.Count = catalogOffer.Count;
                    current.Price = catalogOffer.Price;
                    current.Category = catalogOffer.Category;
                    current.Preset = catalogOffer.Preset;
                    changed = true;
                }

                continue;
            }

            state.Shop.Add(CloneOffer(catalogOffer));
            changed = true;
        }

        return changed;
    }

    private List<BattlePassShopOffer> CloneShop()
    {
        return catalog.Shop.Select(CloneOffer).ToList();
    }

    private static BattlePassShopOffer CloneOffer(BattlePassShopOffer offer)
    {
        return new BattlePassShopOffer
        {
            Id = offer.Id,
            Name = offer.Name,
            Tpl = offer.Tpl,
            Count = offer.Count,
            Price = offer.Price,
            StockRemaining = offer.StockRemaining,
            Category = offer.Category,
            Preset = offer.Preset
        };
    }

    private List<BattlePassChallenge> Roll(
        IReadOnlyList<ChallengeTemplate> pool,
        string category,
        string profileId,
        string periodKey,
        string expiresAt,
        string seedSuffix = "")
    {
        int count = Math.Clamp(catalog.Config.ChallengesPerBucket, 1, 6);
        var picked = Pick(pool, count, profileId + "|" + periodKey + seedSuffix);
        return picked.Select(template => new BattlePassChallenge
        {
            InstanceId = string.IsNullOrEmpty(seedSuffix)
                ? $"{periodKey}:{template.Id}"
                : $"{periodKey}{seedSuffix}:{template.Id}",
            TemplateId = template.Id,
            Name = template.Name,
            Description = template.Description,
            Category = category,
            Type = template.Type,
            Progress = 0,
            Target = Math.Max(1, template.Target),
            TicketReward = template.TicketReward,
            Completed = false,
            Claimed = false,
            State = "not_started",
            ExpiresAt = expiresAt,
            Map = template.Map,
            Weapon = template.Weapon,
            Tpl = template.Tpl,
            TimeOfDay = template.TimeOfDay
        }).ToList();
    }

    private static List<T> Pick<T>(IReadOnlyList<T> pool, int count, string seed)
    {
        var list = pool.ToList();
        var rng = new Random(DeterministicSeed(seed));
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list.Take(Math.Min(count, list.Count)).ToList();
    }

    private static int DeterministicSeed(string value)
    {
        unchecked
        {
            int hash = 5381;
            foreach (char c in value)
            {
                hash = (hash << 5) + hash + c;
            }

            return hash;
        }
    }

    private sealed class RaidTally
    {
        public int Scavs { get; init; }
        public int Pmcs { get; init; }
        public int Bosses { get; init; }
        public int Raiders { get; init; }
        public int Rogues { get; init; }
        public int Cultists { get; init; }
        public int Headshots { get; init; }
        public int PmcHeadshots { get; init; }
        public int Melee { get; init; }
        public int Grenades { get; init; }
        public bool Survived { get; init; }
        public bool IsScavRaid { get; init; }
        public bool IsNight { get; init; }
        public string Location { get; init; } = "";
        public Dictionary<string, int> WeaponKills { get; init; } = [];
        public Dictionary<string, int> WeaponScavKills { get; init; } = [];
        public Dictionary<string, int> WeaponPmcKills { get; init; } = [];
        public Dictionary<string, int> WeaponHeadshots { get; init; } = [];
        public Dictionary<string, int> FirItems { get; init; } = [];
    }

    private static void ApplyToBucket(List<BattlePassChallenge> challenges, RaidTally raid)
    {
        foreach (BattlePassChallenge challenge in challenges)
        {
            if (challenge.Completed)
            {
                continue;
            }

            bool onMap = LocationMatches(raid.Location, challenge.Map);
            int amount = challenge.Type switch
            {
                "KillScavs" => raid.Scavs,
                "KillPmcs" => raid.Pmcs,
                "KillBosses" => raid.Bosses,
                "KillRaiders" => raid.Raiders,
                "KillRogues" => raid.Rogues,
                "KillCultists" => raid.Cultists,
                "Headshots" => raid.Headshots,
                "HeadshotPmcs" => raid.PmcHeadshots,
                "KillMelee" => raid.Melee,
                "KillGrenade" => raid.Grenades,
                "Survive" => raid.Survived ? 1 : 0,
                "SurviveCount" => raid.Survived ? 1 : 0,
                "SurvivePmc" => raid.Survived && !raid.IsScavRaid ? 1 : 0,
                "SurviveScav" => raid.Survived && raid.IsScavRaid ? 1 : 0,
                "SurviveNight" => raid.Survived && raid.IsNight ? 1 : 0,
                "SurviveDay" => raid.Survived && !raid.IsNight ? 1 : 0,
                "ExtractMap" => raid.Survived && onMap ? 1 : 0,
                "KillScavsMap" => onMap ? raid.Scavs : 0,
                "KillPmcsMap" => onMap ? raid.Pmcs : 0,
                "HeadshotsMap" => onMap ? raid.Headshots : 0,
                "KillWeapon" => WeaponCount(raid.WeaponKills, challenge.Weapon),
                "KillScavsWeapon" => WeaponCount(raid.WeaponScavKills, challenge.Weapon),
                "KillPmcsWeapon" => WeaponCount(raid.WeaponPmcKills, challenge.Weapon),
                "HeadshotWeapon" => WeaponCount(raid.WeaponHeadshots, challenge.Weapon),
                "FindInRaid" => raid.Survived ? WeaponCount(raid.FirItems, challenge.Tpl) : 0,
                _ => 0
            };

            if (!TimeOfDayMatches(raid.IsNight, challenge.TimeOfDay))
            {
                amount = 0;
            }

            if (amount <= 0)
            {
                continue;
            }

            challenge.Progress = Math.Min(challenge.Target, challenge.Progress + amount);
            if (challenge.Progress >= challenge.Target)
            {
                challenge.Completed = true;
            }

            challenge.State = ResolveState(challenge);
        }
    }

    private void ClaimReadyRewards(BattlePassProfileState state)
    {
        foreach (BattlePassChallenge challenge in AllChallenges(state))
        {
            if (challenge.Completed && !challenge.Claimed)
            {
                AddEarned(state, challenge.TicketReward);
                state.TrackXp += TrackXpForCategory(challenge.Category);
                state.ChallengesCompletedSeason++;
                state.LifetimeChallengesCompleted++;
                switch (challenge.Category)
                {
                    case "weekly":
                        state.WeeklyCompletedSeason++;
                        break;
                    case "monthly":
                        state.MonthlyCompletedSeason++;
                        break;
                    default:
                        state.DailyCompletedSeason++;
                        break;
                }

                challenge.Claimed = true;
            }

            challenge.State = ResolveState(challenge);
        }
    }

    private void TryGrantMonthlyBonus(BattlePassProfileState state)
    {
        if (state.MonthlyBonusClaimed || state.Challenges.Monthly.Count == 0)
        {
            return;
        }

        if (state.Challenges.Monthly.All(challenge => challenge.Completed))
        {
            AddEarned(state, catalog.Config.MonthlyBonus);
            state.TrackXp += Math.Max(0, catalog.Config.TrackXpMonthlyBonus);
            state.MonthlyBonusClaimed = true;
        }
    }

    private static void AddEarned(BattlePassProfileState state, int amount)
    {
        int add = Math.Max(0, amount);
        state.Tickets += add;
        if (add <= 0)
        {
            return;
        }

        state.TicketsEarnedSeason += add;
        state.LifetimeTicketsEarned += add;
    }

    private static void AddSpent(BattlePassProfileState state, int amount)
    {
        int spend = Math.Max(0, amount);
        if (spend <= 0)
        {
            return;
        }

        state.TicketsSpentSeason += spend;
        state.LifetimeTicketsSpent += spend;
    }

    private static void AddXp(BattlePassProfileState state, int amount)
    {
        int add = Math.Max(0, amount);
        if (add <= 0)
        {
            return;
        }

        state.XpEarnedSeason += add;
        state.LifetimeXpEarned += add;
    }

    private static void ResetSeasonStats(BattlePassProfileState state)
    {
        state.TicketsEarnedSeason = 0;
        state.TicketsSpentSeason = 0;
        state.ChallengesCompletedSeason = 0;
        state.DailyCompletedSeason = 0;
        state.WeeklyCompletedSeason = 0;
        state.MonthlyCompletedSeason = 0;
        state.XpEarnedSeason = 0;
    }

    private static IEnumerable<BattlePassChallenge> AllChallenges(BattlePassProfileState state)
    {
        return state.Challenges.Daily
            .Concat(state.Challenges.Weekly)
            .Concat(state.Challenges.Monthly);
    }

    private static int WeaponCount(Dictionary<string, int> counts, string? weapon)
    {
        if (counts == null || counts.Count == 0 || string.IsNullOrWhiteSpace(weapon))
        {
            return 0;
        }

        string key = weapon.Trim().ToLowerInvariant();
        foreach (KeyValuePair<string, int> pair in counts)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return Clamp(pair.Value);
            }
        }

        return 0;
    }

    private static bool TimeOfDayMatches(bool raidIsNight, string? timeOfDay)
    {
        if (string.IsNullOrWhiteSpace(timeOfDay))
        {
            return true;
        }

        bool wantNight = timeOfDay.Equals("night", StringComparison.OrdinalIgnoreCase);
        return wantNight == raidIsNight;
    }

    private static Dictionary<string, int> ClampDict(Dictionary<string, int>? source)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (source == null)
        {
            return result;
        }

        foreach (KeyValuePair<string, int> pair in source)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                continue;
            }

            result[pair.Key.Trim()] = Clamp(pair.Value);
        }

        return result;
    }

    private int CountInStash(SPTarkov.Server.Core.Models.Eft.Common.PmcData? pmc, string tpl)
    {
        if (pmc?.Inventory?.Items == null || string.IsNullOrWhiteSpace(tpl))
        {
            return 0;
        }

        HashSet<MongoId> inStash = StashItemIds(pmc);
        string wanted = tpl.Trim();
        long total = 0;
        foreach (Item item in pmc.Inventory.Items)
        {
            if (item == null || !inStash.Contains(item.Id)
                || !item.Template.ToString().Equals(wanted, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int stack = (int)(item.Upd?.StackObjectsCount ?? 1);
            if (stack <= 0)
            {
                stack = 1;
            }

            total += stack;
            if (total >= int.MaxValue)
            {
                return int.MaxValue;
            }
        }

        return (int)total;
    }

    private StashRemoval RemoveFromStash(SPTarkov.Server.Core.Models.Eft.Common.PmcData? pmc, string tpl, int need)
    {
        var result = new StashRemoval();
        if (pmc?.Inventory?.Items == null || need <= 0 || string.IsNullOrWhiteSpace(tpl))
        {
            return result;
        }

        HashSet<MongoId> inStash = StashItemIds(pmc);
        string wanted = tpl.Trim();
        List<Item> items = pmc.Inventory.Items as List<Item> ?? pmc.Inventory.Items.ToList();
        var matches = items
            .Where(item => item != null
                           && inStash.Contains(item.Id)
                           && item.Template.ToString().Equals(wanted, StringComparison.OrdinalIgnoreCase))
            .ToList();

        int remaining = need;
        var removed = new HashSet<MongoId>();
        foreach (Item item in matches)
        {
            if (remaining <= 0 || removed.Contains(item.Id))
            {
                continue;
            }

            int stack = (int)(item.Upd?.StackObjectsCount ?? 1);
            if (stack <= 0)
            {
                stack = 1;
            }

            int take = Math.Min(stack, remaining);
            if (take >= stack)
            {
                result.Changes.Add(new StashItemChange { Id = item.Id.ToString(), Count = 0 });
                string itemId = item.Id.ToString();
                for (int index = items.Count - 1; index >= 0; index--)
                {
                    Item candidate = items[index];
                    if (candidate == null)
                    {
                        continue;
                    }

                    bool self = candidate.Id == item.Id;
                    bool child = !string.IsNullOrEmpty(candidate.ParentId)
                                 && candidate.ParentId.Equals(itemId, StringComparison.OrdinalIgnoreCase);
                    if (!self && !child)
                    {
                        continue;
                    }

                    removed.Add(candidate.Id);
                    items.RemoveAt(index);
                }
            }
            else
            {
                item.AddUpd();
                int left = stack - take;
                if (item.Upd != null)
                {
                    item.Upd.StackObjectsCount = left;
                }

                result.Changes.Add(new StashItemChange { Id = item.Id.ToString(), Count = left });
            }

            remaining -= take;
            result.Taken += take;
        }

        if (!ReferenceEquals(items, pmc.Inventory.Items))
        {
            pmc.Inventory.Items = items;
        }

        return result;
    }

    private sealed class StashRemoval
    {
        public int Taken { get; set; }
        public List<StashItemChange> Changes { get; } = new();
    }

    private static HashSet<MongoId> StashItemIds(SPTarkov.Server.Core.Models.Eft.Common.PmcData pmc)
    {
        var ids = new HashSet<MongoId>();
        if (pmc.Inventory?.Items == null || pmc.Inventory.Stash == null)
        {
            return ids;
        }

        var inTree = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            pmc.Inventory.Stash.ToString()
        };

        bool added = true;
        while (added)
        {
            added = false;
            foreach (Item item in pmc.Inventory.Items)
            {
                if (item == null || string.IsNullOrEmpty(item.ParentId) || !inTree.Contains(item.ParentId))
                {
                    continue;
                }

                if (!inTree.Add(item.Id.ToString()))
                {
                    continue;
                }

                ids.Add(item.Id);
                added = true;
            }
        }

        return ids;
    }

    private static bool LocationMatches(string raidLocation, string? requiredMap)
    {
        if (string.IsNullOrWhiteSpace(requiredMap))
        {
            return false;
        }

        return NormalizeLocation(raidLocation) == NormalizeLocation(requiredMap);
    }

    private static string NormalizeLocation(string location)
    {
        string value = (location ?? "").Trim().ToLowerInvariant();
        if (value.StartsWith("factory4", StringComparison.Ordinal))
        {
            return "factory4";
        }

        if (value.StartsWith("sandbox", StringComparison.Ordinal))
        {
            return "sandbox";
        }

        return value;
    }

    private static int Clamp(int value) => Math.Clamp(value, 0, 200);

    private static string ResolveState(BattlePassChallenge challenge)
    {
        if (challenge.Completed || (challenge.Target > 0 && challenge.Progress >= challenge.Target))
        {
            return "complete";
        }

        return challenge.Progress > 0 ? "in_progress" : "not_started";
    }

    private BattlePassStatus ToStatus(BattlePassProfileState state)
    {
        DateTime now = SeasonClock.UtcNow;
        string monthlyKey = string.IsNullOrEmpty(state.SeasonId) ? SeasonClock.MonthlyKey(now) : state.SeasonId;
        DateTime seasonMonth = ParseSeasonMonth(monthlyKey, now);
        TrackProgress progress = ComputeTrackProgress(state.TrackXp);

        return new BattlePassStatus
        {
            SeasonId = monthlyKey,
            SeasonName = SeasonClock.SeasonName(seasonMonth),
            DaysRemaining = SeasonClock.DaysRemainingInMonth(now),
            Tickets = state.Tickets,
            LastDailyReset = state.DailyKey,
            LastWeeklyReset = state.WeeklyKey,
            LastMonthlyReset = state.MonthlyKey,
            LastCrateSeasonId = state.LastCrateSeasonId,
            LastCrateTickets = state.LastCrateTickets,
            Challenges = state.Challenges,
            Shop = state.Shop,
            Debug = catalog.Config.DebugGrants,
            GrantAmount = Math.Clamp(catalog.Config.GrantAmount, 1, 100),
            DailyRerollCost = Math.Max(0, catalog.Config.DailyRerollCost),
            DailyRerollsLeft = RerollsLeft(state.DailyRerolls, catalog.Config.DailyRerollCost, catalog.Config.DailyRerollMax),
            WeeklyRerollCost = Math.Max(0, catalog.Config.WeeklyRerollCost),
            WeeklyRerollsLeft = RerollsLeft(state.WeeklyRerolls, catalog.Config.WeeklyRerollCost, catalog.Config.WeeklyRerollMax),
            XpDaily = Math.Max(0, catalog.Config.XpDaily),
            XpWeekly = Math.Max(0, catalog.Config.XpWeekly),
            XpMonthly = Math.Max(0, catalog.Config.XpMonthly),
            TicketsEarnedSeason = state.TicketsEarnedSeason,
            TicketsSpentSeason = state.TicketsSpentSeason,
            ChallengesCompletedSeason = state.ChallengesCompletedSeason,
            DailyCompletedSeason = state.DailyCompletedSeason,
            WeeklyCompletedSeason = state.WeeklyCompletedSeason,
            MonthlyCompletedSeason = state.MonthlyCompletedSeason,
            XpEarnedSeason = state.XpEarnedSeason,
            LifetimeTicketsEarned = state.LifetimeTicketsEarned,
            LifetimeTicketsSpent = state.LifetimeTicketsSpent,
            LifetimeChallengesCompleted = state.LifetimeChallengesCompleted,
            LifetimeXpEarned = state.LifetimeXpEarned,
            LastSeasonId = state.LastSeasonId,
            LastSeasonName = string.IsNullOrEmpty(state.LastSeasonId)
                ? ""
                : SeasonClock.SeasonName(ParseSeasonMonth(state.LastSeasonId, now)),
            LastSeasonTicketsEarned = state.LastSeasonTicketsEarned,
            LastSeasonChallengesCompleted = state.LastSeasonChallengesCompleted,
            Premium = state.Premium,
            PremiumCost = Math.Max(0, catalog.Config.PremiumCostRoubles),
            TrackXp = Math.Max(0, state.TrackXp),
            TrackLevel = progress.Level,
            TrackMaxLevel = progress.Max,
            TrackXpIntoLevel = progress.Into,
            TrackXpForLevel = progress.Needed,
            Track = BuildTrackStatus(state, progress.Level)
        };
    }

    private static int RerollsLeft(int used, int cost, int max)
    {
        if (cost <= 0 || max <= 0)
        {
            return 0;
        }

        return Math.Max(0, max - used);
    }

    private readonly record struct ChallengeSnap(
        int Progress,
        bool Completed,
        bool Claimed,
        string Name,
        string Category,
        int Target,
        int TicketReward);

    private static Dictionary<string, ChallengeSnap> SnapshotChallenges(BattlePassProfileState state)
    {
        var snapshot = new Dictionary<string, ChallengeSnap>(StringComparer.Ordinal);
        foreach (BattlePassChallenge challenge in AllChallenges(state))
        {
            if (string.IsNullOrEmpty(challenge.InstanceId) || snapshot.ContainsKey(challenge.InstanceId))
            {
                continue;
            }

            snapshot[challenge.InstanceId] = new ChallengeSnap(
                challenge.Progress,
                challenge.Completed,
                challenge.Claimed,
                challenge.Name,
                challenge.Category,
                challenge.Target,
                challenge.TicketReward);
        }

        return snapshot;
    }

    private RaidEndResult ToRaidEndResult(
        BattlePassProfileState state,
        bool duplicate,
        Dictionary<string, ChallengeSnap>? before = null,
        int ticketsBefore = 0,
        bool monthlyBonusAlreadyClaimed = true)
    {
        var updates = new List<RaidChallengeUpdate>();
        if (!duplicate && before != null)
        {
            foreach (BattlePassChallenge challenge in AllChallenges(state))
            {
                if (string.IsNullOrEmpty(challenge.InstanceId)
                    || !before.TryGetValue(challenge.InstanceId, out ChallengeSnap previous))
                {
                    continue;
                }

                bool progressed = challenge.Progress != previous.Progress;
                bool completedNow = challenge.Completed && !previous.Completed;
                bool claimedNow = challenge.Claimed && !previous.Claimed;
                if (!progressed && !completedNow && !claimedNow)
                {
                    continue;
                }

                updates.Add(new RaidChallengeUpdate
                {
                    InstanceId = challenge.InstanceId,
                    Name = challenge.Name,
                    Category = challenge.Category,
                    PreviousProgress = previous.Progress,
                    Progress = challenge.Progress,
                    Target = challenge.Target,
                    Completed = challenge.Completed,
                    TicketsEarned = claimedNow ? Math.Max(0, challenge.TicketReward) : 0,
                    XpEarned = claimedNow ? XpForCategory(challenge.Category) : 0
                });
            }
        }

        int monthlyBonus = 0;
        int monthlyBonusXp = 0;
        if (!duplicate && !monthlyBonusAlreadyClaimed && state.MonthlyBonusClaimed)
        {
            monthlyBonus = Math.Max(0, catalog.Config.MonthlyBonus);
            monthlyBonusXp = Math.Max(0, catalog.Config.XpMonthlyBonus);
        }

        return new RaidEndResult
        {
            Duplicate = duplicate,
            TicketsEarned = duplicate ? 0 : Math.Max(0, state.Tickets - ticketsBefore),
            MonthlyBonus = monthlyBonus,
            XpEarned = updates.Sum(update => update.XpEarned) + monthlyBonusXp,
            MonthlyBonusXp = monthlyBonusXp,
            Updates = updates,
            Status = ToStatus(state)
        };
    }

    private int TrackXpForCategory(string category)
    {
        return category switch
        {
            "daily" => Math.Max(0, catalog.Config.TrackXpDaily),
            "weekly" => Math.Max(0, catalog.Config.TrackXpWeekly),
            "monthly" => Math.Max(0, catalog.Config.TrackXpMonthly),
            _ => 0
        };
    }

    private bool GrantTrackRewards(MongoId sessionId, BattlePassProfileState state)
    {
        IReadOnlyList<TrackTier> tiers = catalog.Track;
        if (tiers.Count == 0)
        {
            return false;
        }

        int reached = ComputeTrackProgress(state.TrackXp).Level;
        bool mailed = false;
        foreach (TrackTier tier in tiers)
        {
            if (tier.Level > reached)
            {
                break;
            }

            if (state.TrackFreeClaimed < tier.Level)
            {
                mailed |= DeliverTrackReward(sessionId, state, tier, premium: false);
                state.TrackFreeClaimed = tier.Level;
            }

            if (state.Premium && state.TrackPremiumClaimed < tier.Level)
            {
                mailed |= DeliverTrackReward(sessionId, state, tier, premium: true);
                state.TrackPremiumClaimed = tier.Level;
            }
        }

        return mailed;
    }

    private bool DeliverTrackReward(MongoId sessionId, BattlePassProfileState state, TrackTier tier, bool premium)
    {
        TrackReward? reward = premium ? tier.Premium : tier.Free;
        if (reward == null)
        {
            return false;
        }

        if (reward.Tickets > 0)
        {
            AddEarned(state, reward.Tickets);
        }

        if (string.IsNullOrWhiteSpace(reward.Tpl))
        {
            return false;
        }

        string lane = premium ? "premium" : "free";
        string name = RewardName(reward);
        try
        {
            shopDelivery.Deliver(
                sessionId,
                new BattlePassShopOffer
                {
                    Id = $"track-{tier.Level}-{lane}",
                    Name = name,
                    Tpl = reward.Tpl,
                    Count = Math.Max(1, reward.Count),
                    Preset = reward.Preset
                },
                $"Battle Pass track level {tier.Level} ({lane}): {name}. Collect this from Messages.");
            return true;
        }
        catch (Exception exception)
        {
            logger.Error($"[BattlePass] track reward failed level={tier.Level} {lane}: {exception.Message}");
            return false;
        }
    }

    private List<TrackTierStatus> BuildTrackStatus(BattlePassProfileState state, int reached)
    {
        var rows = new List<TrackTierStatus>();
        foreach (TrackTier tier in catalog.Track)
        {
            rows.Add(new TrackTierStatus
            {
                Level = tier.Level,
                Xp = Math.Max(1, tier.Xp),
                Reached = reached >= tier.Level,
                Free = ToRewardStatus(tier.Free, claimed: state.TrackFreeClaimed >= tier.Level, locked: false),
                Premium = ToRewardStatus(tier.Premium, claimed: state.TrackPremiumClaimed >= tier.Level, locked: !state.Premium)
            });
        }

        return rows;
    }

    private static TrackRewardStatus? ToRewardStatus(TrackReward? reward, bool claimed, bool locked)
    {
        if (reward == null)
        {
            return null;
        }

        return new TrackRewardStatus
        {
            Name = RewardName(reward),
            Tpl = string.IsNullOrWhiteSpace(reward.Tpl) ? null : reward.Tpl,
            Count = Math.Max(0, reward.Count),
            Tickets = Math.Max(0, reward.Tickets),
            Preset = reward.Preset,
            Claimed = claimed,
            Locked = locked
        };
    }

    private static string RewardName(TrackReward reward)
    {
        if (!string.IsNullOrWhiteSpace(reward.Name))
        {
            return reward.Name;
        }

        if (reward.Tickets > 0)
        {
            return reward.Tickets == 1 ? "+1 ticket" : $"+{reward.Tickets} tickets";
        }

        return reward.Tpl ?? "Reward";
    }

    private TrackProgress ComputeTrackProgress(int xp)
    {
        IReadOnlyList<TrackTier> tiers = catalog.Track;
        int max = tiers.Count == 0 ? 0 : tiers[^1].Level;
        int remaining = Math.Max(0, xp);
        int level = 0;
        foreach (TrackTier tier in tiers)
        {
            int gap = Math.Max(1, tier.Xp);
            if (remaining >= gap)
            {
                remaining -= gap;
                level = tier.Level;
            }
            else
            {
                return new TrackProgress(level, remaining, gap, max);
            }
        }

        return new TrackProgress(level, 0, 0, max);
    }

    private readonly record struct TrackProgress(int Level, int Into, int Needed, int Max);

    private int XpForCategory(string category)
    {
        return category switch
        {
            "daily" => Math.Max(0, catalog.Config.XpDaily),
            "weekly" => Math.Max(0, catalog.Config.XpWeekly),
            "monthly" => Math.Max(0, catalog.Config.XpMonthly),
            _ => 0
        };
    }
}
