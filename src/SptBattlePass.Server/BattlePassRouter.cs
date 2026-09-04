using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;
using SptBattlePass.Server.Models;

namespace SptBattlePass.Server;

[Injectable(TypePriority = OnLoadOrder.Routers)]
public class BattlePassRouter(JsonUtil jsonUtil, BattlePassCallbacks callbacks)
    : StaticRouter(jsonUtil, [
        new RouteAction<EmptyRequestData>(
            BattlePassCallbacks.StatusRoute,
            async (url, info, sessionId, output, cancellationToken) =>
                await callbacks.HandleStatus(url, info, sessionId)
        ),
        new RouteAction<RaidEndRequest>(
            BattlePassCallbacks.RaidEndRoute,
            async (url, info, sessionId, output, cancellationToken) =>
                await callbacks.HandleRaidEnd(url, info, sessionId)
        ),
        new RouteAction<BuyRequest>(
            BattlePassCallbacks.BuyRoute,
            async (url, info, sessionId, output, cancellationToken) =>
                await callbacks.HandleBuy(url, info, sessionId)
        ),
        new RouteAction<EmptyRequestData>(
            BattlePassCallbacks.GrantRoute,
            async (url, info, sessionId, output, cancellationToken) =>
                await callbacks.HandleGrant(url, info, sessionId)
        ),
        new RouteAction<RerollRequest>(
            BattlePassCallbacks.RerollRoute,
            async (url, info, sessionId, output, cancellationToken) =>
                await callbacks.HandleReroll(url, info, sessionId)
        ),
        new RouteAction<HandoverRequest>(
            BattlePassCallbacks.HandoverRoute,
            async (url, info, sessionId, output, cancellationToken) =>
                await callbacks.HandleHandover(url, info, sessionId)
        ),
        new RouteAction<PremiumRequest>(
            BattlePassCallbacks.PremiumRoute,
            async (url, info, sessionId, output, cancellationToken) =>
                await callbacks.HandlePremium(url, info, sessionId)
        )
    ])
{
}
