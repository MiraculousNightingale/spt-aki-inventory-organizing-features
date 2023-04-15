using EFT.InventoryLogic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryOrganizingFeatures
{
    internal static class InventoryOrganizer
    {
        public const string MoveLockTag = "@ml";
        public const string SortLockTag = "@sl";
        public const string OrganizeTag = "@o";
        public static Regex OrganizeRegex = new(OrganizeTag + " (.*?);");
        public static ISession Session { get; set; } = null;
        public static Button OrganizeButton { get; set; } = null;
        public static Sprite OrganizeSprite { get; set; } = null;
        public static bool IsMoveLocked(Item item)
        {
            return item.TryGetItemComponent(out TagComponent tagComponent) && tagComponent.Name.Contains(MoveLockTag);
        }

        public static bool IsMoveLocked(string tagName)
        {
            return tagName.Contains(MoveLockTag);
        }

        public static bool IsSortLocked(Item item)
        {
            return item.TryGetItemComponent(out TagComponent tagComponent) && tagComponent.Name.Contains(SortLockTag);
        }

        public static bool IsSortLocked(string tagName)
        {
            return tagName.Contains(SortLockTag);
        }

        public static bool IsOrganized(Item item)
        {
            if (!item.TryGetItemComponent(out TagComponent tagComponent)) return false;
            if (!item.IsContainer) return false;
            return IsOrganized(tagComponent.Name);
        }

        public static bool IsOrganized(string tagName)
        {
            string organizeStr = OrganizeRegex.Match(tagName).Value;
            if (organizeStr == string.Empty) return false;
            return ParseOrganizeParams(tagName).Length > 0;
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
            if (organizeStr == string.Empty) return new string[0];
            return organizeStr.Substring(OrganizeTag.Length + 1).TrimEnd(';').Split('|').DoMap(param => param.Trim()).ToArray();
        }
    }

}
