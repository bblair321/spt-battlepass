using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace SptBattlePass.Server.Services;

internal static class CatalogValidator
{
    public static void Run<T>(ChallengeCatalog catalog, ItemHelper itemHelper, ISptLogger<T> logger)
    {
        int missing = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach ((string source, string? tpl) in catalog.AllItemTpls())
        {
            if (string.IsNullOrWhiteSpace(tpl) || !seen.Add(tpl))
            {
                continue;
            }

            if (ItemExists(itemHelper, tpl))
            {
                continue;
            }

            missing++;
            logger.Error($"[BattlePass] unknown item tpl {tpl} ({source})");
        }

        if (missing == 0)
        {
            logger.Info($"[BattlePass] catalog tpls ok ({seen.Count} unique)");
        }
        else
        {
            logger.Warning($"[BattlePass] catalog has {missing} unknown item tpls — those rows will have no icon and may fail to mail");
        }
    }

    private static bool ItemExists(ItemHelper itemHelper, string tpl)
    {
        try
        {
            KeyValuePair<bool, TemplateItem?> dbItem = itemHelper.GetItem(new MongoId(tpl));
            return dbItem.Key && dbItem.Value != null;
        }
        catch
        {
            return false;
        }
    }

    public static bool ItemMatchesParent(ItemHelper itemHelper, string tpl, string requiredParent)
    {
        string want = ItemParents.Resolve(requiredParent);
        if (string.IsNullOrWhiteSpace(tpl) || want.Length == 0)
        {
            return false;
        }

        string current = tpl;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < 10 && seen.Add(current); i++)
        {
            if (current.Equals(want, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string parent = ReadParent(itemHelper, current);
            if (string.IsNullOrWhiteSpace(parent) || parent == "0")
            {
                break;
            }

            current = parent;
        }

        return false;
    }

    private static string ReadParent(ItemHelper itemHelper, string tpl)
    {
        try
        {
            KeyValuePair<bool, TemplateItem?> dbItem = itemHelper.GetItem(new MongoId(tpl));
            if (!dbItem.Key || dbItem.Value == null)
            {
                return "";
            }

            object template = dbItem.Value;
            Type type = template.GetType();
            foreach (string name in new[] { "Parent", "ParentId", "_parent" })
            {
                PropertyInfo? property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object? value = property?.GetValue(template);
                if (value != null && !string.IsNullOrWhiteSpace(value.ToString()) && value.ToString() != "0")
                {
                    return value.ToString() ?? "";
                }

                FieldInfo? field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                value = field?.GetValue(template);
                if (value != null && !string.IsNullOrWhiteSpace(value.ToString()) && value.ToString() != "0")
                {
                    return value.ToString() ?? "";
                }
            }
        }
        catch
        {
        }

        return "";
    }
}
