using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;
using SptBattlePass.Server.Models;
using SptBattlePass.Server.Services;

namespace SptBattlePass.Server;

[Injectable]
public class BattlePassCallbacks(ISptLogger<BattlePassCallbacks> logger, HttpResponseUtil httpResponseUtil, BattlePassService battlePassService)
{
    public const string StatusRoute = "/client/battlepass/status";
    public const string RaidEndRoute = "/client/battlepass/raidend";
    public const string BuyRoute = "/client/battlepass/buy";
    public const string GrantRoute = "/client/battlepass/grant";
    public const string RerollRoute = "/client/battlepass/reroll";
    public const string HandoverRoute = "/client/battlepass/handover";
    public const string PremiumRoute = "/client/battlepass/premium";

    public async ValueTask<string> HandleStatus(string url, EmptyRequestData info, MongoId sessionId)
    {
        logger.Info($"[BattlePass] {StatusRoute} session={sessionId}");
        BattlePassStatus status = await battlePassService.GetStatusAsync(sessionId);
        return httpResponseUtil.GetBody(status);
    }

    public async ValueTask<string> HandleRaidEnd(string url, RaidEndRequest info, MongoId sessionId)
    {
        logger.Info($"[BattlePass] {RaidEndRoute} session={sessionId} raid={info.RaidId}");
        RaidEndResult result = await battlePassService.ApplyRaidAsync(sessionId, info);
        return httpResponseUtil.GetBody(result);
    }

    public async ValueTask<string> HandleBuy(string url, BuyRequest info, MongoId sessionId)
    {
        logger.Info($"[BattlePass] {BuyRoute} session={sessionId} offer={info.Id}");
        BuyResult result = await battlePassService.BuyAsync(sessionId, info.Id ?? "");
        return httpResponseUtil.GetBody(result);
    }

    public async ValueTask<string> HandleGrant(string url, EmptyRequestData info, MongoId sessionId)
    {
        logger.Info($"[BattlePass] {GrantRoute} session={sessionId}");
        GrantResult result = await battlePassService.GrantAsync(sessionId);
        return httpResponseUtil.GetBody(result);
    }

    public async ValueTask<string> HandleReroll(string url, RerollRequest info, MongoId sessionId)
    {
        logger.Info($"[BattlePass] {RerollRoute} session={sessionId} bucket={info.Bucket}");
        RerollResult result = await battlePassService.RerollAsync(sessionId, info.Bucket ?? "");
        return httpResponseUtil.GetBody(result);
    }

    public async ValueTask<string> HandleHandover(string url, HandoverRequest info, MongoId sessionId)
    {
        logger.Info($"[BattlePass] {HandoverRoute} session={sessionId} challenge={info.InstanceId}");
        HandoverResult result = await battlePassService.HandoverAsync(sessionId, info.InstanceId ?? "");
        return httpResponseUtil.GetBody(result);
    }

    public async ValueTask<string> HandlePremium(string url, PremiumRequest info, MongoId sessionId)
    {
        logger.Info($"[BattlePass] {PremiumRoute} session={sessionId} debug={info.Debug}");
        PremiumResult result = await battlePassService.UnlockPremiumAsync(sessionId, info.Debug);
        return httpResponseUtil.GetBody(result);
    }
}
