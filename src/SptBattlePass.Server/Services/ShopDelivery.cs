using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services.Commerce;
using SptBattlePass.Server.Models;

namespace SptBattlePass.Server.Services;

[Injectable(InjectionType.Singleton)]
public class ShopDelivery(
    ISptLogger<ShopDelivery> logger,
    ItemHelper itemHelper,
    PresetHelper presetHelper,
    MailSendService mailSendService)
{
    private const long MailStorageSeconds = 7 * 24 * 60 * 60;

    /// <summary>
    /// Custom HTTP routes cannot push stash grid updates to the open inventory screen.
    /// System mail is the vanilla attach+notify path, so purchases are always mailed.
    /// </summary>
    public string Deliver(MongoId sessionId, BattlePassShopOffer offer)
    {
        return Deliver(sessionId, offer, $"Battle Pass exchange: {offer.Name}. Collect this from Messages.");
    }

    public string Deliver(MongoId sessionId, BattlePassShopOffer offer, string message)
    {
        List<Item> items = offer.Preset
            ? BuildPreset(offer.Tpl, offer.Count)
            : BuildItems(offer.Tpl, offer.Count);
        SendMail(sessionId, message, items);
        logger.Info($"[BattlePass] delivered '{offer.Name}' to mail for {sessionId} ({items.Count} stacks) preset={offer.Preset}");
        return "mail";
    }

    public bool DeliverMail(MongoId sessionId, string message, IReadOnlyList<(string Tpl, int Count)> contents)
    {
        var items = new List<Item>();
        foreach ((string tpl, int count) in contents)
        {
            try
            {
                items.AddRange(BuildItems(tpl, count));
            }
            catch (Exception exception)
            {
                logger.Error($"[BattlePass] skipped crate tpl {tpl}: {exception.Message}");
            }
        }

        if (items.Count == 0)
        {
            return false;
        }

        SendMail(sessionId, message, items);
        logger.Info($"[BattlePass] mailed crate ({items.Count} stacks) to {sessionId}");
        return true;
    }

    private void SendMail(MongoId sessionId, string message, List<Item> items)
    {
        mailSendService.SendSystemMessageToPlayer(sessionId, message, items, MailStorageSeconds, null);
    }

    private List<Item> BuildPreset(string tpl, int count)
    {
        var tplId = new MongoId(tpl);
        var preset = presetHelper.GetDefaultPreset(tplId);
        if (preset?.Items == null || preset.Items.Count == 0)
        {
            throw new InvalidOperationException($"No default preset for tpl {tpl}");
        }

        int copies = Math.Max(1, count);
        var items = new List<Item>();
        for (int i = 0; i < copies; i++)
        {
            List<Item> clone = preset.Items.ReplaceIDs().ToList();
            clone.RemapRootItemId();
            foreach (Item item in clone)
            {
                item.AddUpd();
                if (item.Upd != null)
                {
                    item.Upd.SpawnedInSession = false;
                }
            }

            items.AddRange(clone);
        }

        return items;
    }

    private List<Item> BuildItems(string tpl, int count)
    {
        var tplId = new MongoId(tpl);
        KeyValuePair<bool, TemplateItem?> dbItem = itemHelper.GetItem(tplId);
        if (!dbItem.Key || dbItem.Value == null)
        {
            throw new InvalidOperationException($"Unknown item tpl {tpl}");
        }

        int amount = Math.Max(1, count);
        if (itemHelper.IsItemTplStackable(tplId) == true)
        {
            return itemHelper.SplitStackIntoSeparateItems(CreateItem(tplId, dbItem.Value, amount))
                .SelectMany(bundle => bundle)
                .ToList();
        }

        var items = new List<Item>(amount);
        for (int i = 0; i < amount; i++)
        {
            items.Add(CreateItem(tplId, dbItem.Value, 1));
        }

        return items;
    }

    private Item CreateItem(MongoId tpl, TemplateItem template, int stack)
    {
        Upd upd = itemHelper.GenerateUpdForItem(template);
        upd.StackObjectsCount = stack;
        upd.SpawnedInSession = false;
        return new Item
        {
            Id = new MongoId(),
            Template = tpl,
            Upd = upd
        };
    }
}
