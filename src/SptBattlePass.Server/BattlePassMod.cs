using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Items;
using SptBattlePass.Server.Services;

namespace SptBattlePass.Server;

[Injectable(TypePriority = OnLoadOrder.PostLoad)]
public class BattlePassMod(
    ISptLogger<BattlePassMod> logger,
    ChallengeCatalog catalog,
    ItemHelper itemHelper) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        CatalogValidator.Run(catalog, itemHelper, logger);
        logger.Info("[BattlePass] loaded. Routes: /client/battlepass/status, /client/battlepass/raidend, /client/battlepass/buy, /client/battlepass/grant, /client/battlepass/reroll, /client/battlepass/handover, /client/battlepass/premium");
        logger.Info("[BattlePass] Fika: each player's session keeps its own tickets. Host this server mod; every player needs the client plugin.");
        return Task.CompletedTask;
    }
}
