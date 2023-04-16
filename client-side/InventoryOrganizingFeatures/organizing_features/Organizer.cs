using EFT.InventoryLogic;
using InventoryOrganizingFeatures.Reflections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryOrganizingFeatures
{
    internal class Organizer
    {
        public const string OrganizeTag = "@o";
        public const char OrganizeTagSeparator = '|';
        public const char OrganizeTagEnd = ';';
        public static Regex OrganizeRegex = new(OrganizeTag + " (.*?)" + OrganizeTagEnd);

        public static Handbook Handbook { get; set; } = null;
        public static Button OrganizeButton { get; set; } = null;
        public static Sprite OrganizeSprite { get; set; } = null;
        public static void Organize(LootItemClass topLevelItem, InventoryControllerClass controller)
        {
            foreach (var grid in topLevelItem.Grids)
            {
                var organizedContainers = grid.Items.Where(IsOrganized).Select(item => new OrganizedContainer((LootItemClass)item, topLevelItem, controller)).ToList();
                foreach (var container in organizedContainers)
                {
                    LogNotif($"Organized Container: {container.TargetItem.LocalizedName()}");
                    container.Organize();
                }
            }
        }

        private static void LogNotif(string message)
        {
            if(Plugin.EnableLogs) NotificationManagerClass.DisplayMessageNotification(message, duration: EFT.Communications.ENotificationDurationType.Infinite);
        }

        public static bool IsOrganized(Item item)
        {
            if (!item.TryGetItemComponent(out TagComponent tagComponent)) return false;
            if (!item.IsContainer) return false;
            return IsOrganized(tagComponent.Name);
        }
        public static bool IsOrganized(string tagName)
        {
            return ParseOrganizeParams(tagName).Length > 0;
        }

        private static bool ContainsSeparate(string tagName, string findTag)
        {
            if (tagName.Contains(findTag))
            {
                // check char before tag
                int beforeTagIdx = tagName.IndexOf(findTag) - 1;
                if (beforeTagIdx >= 0)
                {
                    if (tagName[beforeTagIdx] != ' ') return false;
                }
                // check char after tag
                int afterTagIdx = tagName.IndexOf(findTag) + findTag.Length;
                if (afterTagIdx <= tagName.Length - 1)
                {
                    if (tagName[afterTagIdx] != ' ') return false;
                }
                return true;
            }
            return false;
        }

        public static string[] ParseOrganizeParams(Item item)
        {
            if (!IsOrganized(item)) return new string[0];
            if (!item.TryGetItemComponent(out TagComponent tagComponent)) return new string[0];
            return ParseOrganizeParams(tagComponent.Name);
        }

        public static string[] ParseOrganizeParams(string tagName)
        {
            string organizeStr = OrganizeRegex.Match(tagName).Value;
            if (organizeStr.IsNullOrEmpty())
            {
                // If full organize regex match not found - check shortcut is used
                if (ContainsSeparate(tagName, OrganizeTag))
                {
                    return new string[] { OrganizedContainer.ParamDefault };
                }
                return new string[0];
            }

            var result = organizeStr
                .Substring(OrganizeTag.Length + 1) // remove the tag
                .TrimEnd(OrganizeTagEnd) // remove the closing semicolon
                .Trim() // trim spaces
                .Split(OrganizeTagSeparator) // split by defined separator
                .DoMap(param => param.Trim()) // trim every param
                .Where(param => !param.IsNullOrEmpty()) // filter out empty left-over params
                .ToArray();

            // If params contain only FoundInRaid or NotFoundInRaid param then add Default param to the beginning.
            if (result.Length == 1 && (result.Contains(OrganizedContainer.ParamFoundInRaid) || result.Contains(OrganizedContainer.ParamNotFoundInRaid)))
            {
                result.Prepend(OrganizedContainer.ParamDefault);
            }
            if (result.Length < 1) result.Prepend(OrganizedContainer.ParamDefault);
            return result;
        }
    }

}
