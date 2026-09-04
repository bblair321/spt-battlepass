using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using SptBattlePass.Client.Models;

namespace SptBattlePass.Client.Services;

internal static class StashSync
{
    private static readonly FieldInfo InventoryControllerField = typeof(ItemUiContext).GetField(
        "_inventoryController",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo ItemControllerField = typeof(ItemUiContext).GetField(
        "_itemController",
        BindingFlags.Instance | BindingFlags.NonPublic);

    public static void Apply(IReadOnlyList<StashItemChangeDto> changes)
    {
        if (changes == null || changes.Count == 0)
        {
            return;
        }

        try
        {
            ItemController controller = ResolveController();
            if (controller == null)
            {
                Plugin.Log?.LogWarning("[BattlePass] Stash changed on server but the open inventory could not be found. Reopen Character before trading.");
                return;
            }

            var updates = new List<StashItemChangeDto>();
            var removals = new List<StashItemChangeDto>();
            foreach (StashItemChangeDto change in changes)
            {
                if (change == null || string.IsNullOrEmpty(change.Id))
                {
                    continue;
                }

                if (change.Count <= 0)
                {
                    removals.Add(change);
                }
                else
                {
                    updates.Add(change);
                }
            }

            int applied = 0;
            foreach (StashItemChangeDto change in updates)
            {
                if (TryUpdateStack(controller, change.Id, change.Count))
                {
                    applied++;
                }
            }

            foreach (StashItemChangeDto change in removals)
            {
                if (TryRemoveItem(controller, change.Id))
                {
                    applied++;
                }
            }

            Plugin.Log?.LogInfo($"[BattlePass] Applied {applied}/{changes.Count} stash change(s) to the open inventory.");
        }
        catch (Exception exception)
        {
            Plugin.Log?.LogError($"[BattlePass] Failed to sync stash after server item removal: {exception.Message}");
        }
    }

    private static ItemController ResolveController()
    {
        ItemUiContext context = ItemUiContext.Instance;
        if (context == null)
        {
            return null;
        }

        if (InventoryControllerField?.GetValue(context) is ItemController inventoryController)
        {
            return inventoryController;
        }

        return ItemControllerField?.GetValue(context) as ItemController;
    }

    private static bool TryGetItem(ItemController controller, string id, out Item item)
    {
        if (controller.TryFindItem(id, out item) && item != null)
        {
            return true;
        }

        foreach (Item candidate in controller.Items)
        {
            if (candidate != null && string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                item = candidate;
                return true;
            }
        }

        item = null;
        return false;
    }

    private static bool TryUpdateStack(ItemController controller, string id, int count)
    {
        if (!TryGetItem(controller, id, out Item item) || item == null)
        {
            return false;
        }

        item.StackObjectsCount = count;
        item.RaiseRefreshEvent(true, false);
        return true;
    }

    private static bool TryRemoveItem(ItemController controller, string id)
    {
        if (!TryGetItem(controller, id, out Item item) || item == null)
        {
            return false;
        }

        ItemAddress address = item.Parent;
        IItemOwner owner = item.Owner;
        if (address == null || owner == null)
        {
            return false;
        }

        address.RaiseRemoveEvent(item, CommandStatus.Begin, owner);
        address.RemoveWithoutRestrictions(item);
        if (item.CurrentAddress != null)
        {
            address.RaiseRemoveEvent(item, CommandStatus.Failed, owner);
            Plugin.Log?.LogWarning($"[BattlePass] Could not detach stash item {id} from the open inventory.");
            return false;
        }

        address.RaiseRemoveEvent(item, CommandStatus.Succeed, owner);
        return true;
    }
}
