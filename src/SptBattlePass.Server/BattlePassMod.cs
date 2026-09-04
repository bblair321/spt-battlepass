using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace SptBattlePass.Server;

[Injectable(TypePriority = OnLoadOrder.PostLoad)]
public class BattlePassMod(ISptLogger<BattlePassMod> logger) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        logger.Info("[BattlePass] loaded. Routes: /client/battlepass/status, /client/battlepass/raidend, /client/battlepass/buy, /client/battlepass/grant, /client/battlepass/reroll, /client/battlepass/handover, /client/battlepass/premium");
        logger.Info("[BattlePass] Fika: each player's session keeps its own tickets. Host this server mod; every player needs the client plugin.");
        return Task.CompletedTask;
    }
}
