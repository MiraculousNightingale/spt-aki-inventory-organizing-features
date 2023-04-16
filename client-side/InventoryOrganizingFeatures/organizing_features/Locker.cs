using Aki.Reflection.Patching;
using BepInEx.Logging;
using EFT.InventoryLogic;
using Newtonsoft.Json.UnityConverters;
using System.ComponentModel;
using System.Security.Cryptography;
using IContainer = EFT.InventoryLogic.IContainer;

namespace InventoryOrganizingFeatures
{

    internal static class Locker
    {
        public const string MoveLockTag = "@ml";
        public const string SortLockTag = "@sl";

        public static ISession Session { get; set; } = null;

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
    }

}
