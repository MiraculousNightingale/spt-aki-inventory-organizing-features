using EFT.InventoryLogic;
using System.Collections.Generic;
using System.Linq;

namespace InventoryOrganizingFeatures
{
    internal class OrganizedContainer
    {
        public const string ParamDefault = "--default";
        public const string ParamFoundInRaid = "--fir";
        public const string ParamNotFoundInRaid = "--not-fir";
        public static string[] DoubleDashParams = { ParamDefault, ParamFoundInRaid, ParamNotFoundInRaid };

        public const string NameParamPrefix = "n:";

        public string[] Params { get; }
        public LootItemClass TargetItem { get; }
        public InventoryControllerClass Controller { get; }
        public LootItemClass TopLevelItem { get; }
        public List<Item> ValidItems
        {
            get
            {
                LogNotif($"Parent name: {TopLevelItem.LocalizedName()}");
                List<Item> result = new List<Item>();
                foreach (var grid in TopLevelItem.Grids)
                {
                    // ToList is important, since when organizing we can accidentally affect the iterated enumerable.
                    result.AddRange(grid.Items.Where(ItemFitsParams).ToList());
                }
                return result;
            }
        }

        public OrganizedContainer(LootItemClass item, LootItemClass topLevelItem, InventoryControllerClass controller)
        {
            TargetItem = item;
            Controller = controller;
            TopLevelItem = topLevelItem;
            Params = Organizer.ParseOrganizeParams(item);
        }

        private void LogNotif(string message)
        {
            NotificationManagerClass.DisplayMessageNotification(message, duration: EFT.Communications.ENotificationDurationType.Infinite);
        }

        public void Organize()
        {
            var validItems = ValidItems;
            LogNotif($"Valid items: {validItems.Count}");
            foreach (var item in validItems)
            {
                foreach (var grid in TargetItem.Grids)
                {
                    var location = grid.FindLocationForItem(item);
                    if (location == null) continue;
                    // In reference (OnClick from ItemView) simulate = true was used.
                    var moveResult = GClass2429.Move(item, location, Controller, true);
                    Controller.RunNetworkTransaction(moveResult.Value);
                    LogNotif("Executed move.");
                }
            }
        }

        public bool ItemFitsParams(Item item)
        {
            // FIR check
            if (ParamsContainFoundInRaid && !item.SpawnedInSession) return false;
            if (ParamsContainNotFoundInRaid && item.SpawnedInSession) return false;

            return CanAccept(item) && ItemFitsCategoryParams(item) && ItemFitsNameParams(item);
        }

        // Reference GClass2174.CanAccept or just IContainer.CheckItemFilter
        public bool CanAccept(Item item)
        {
            return TargetItem.Grids.Any(grid => grid.CanAccept(item));
        }

        private bool ItemFitsCategoryParams(Item item)
        {
            if (CategoryParams.Length < 1) return true;
            if (ParamsContainDefault) return true;
            var node = Organizer.Handbook.FindNode(item.TemplateId);
            if (node == null)
            {
                NotificationManagerClass.DisplayWarningNotification($"InventoryOrganizingFeatures Warning: Coudln't find {item.LocalizedName()} in handbook. Perhaps it's a modded item?");
            }
            return CategoryParams.Any(param => node.CategoryContains(param));
        }

        private bool ItemFitsNameParams(Item item)
        {
            if (NameParams.Length < 1) return true;
            return NameParams.Any(param => item.LocalizedName().ToLower().Contains(param.ToLower()));
        }

        private static bool IsDoubleDashParam(string param)
        {
            return DoubleDashParams.Any(ddp => ddp.Equals(param.ToLower()));
        }

        private static bool IsNameParam(string param)
        {
            return param.StartsWith(NameParamPrefix);
        }

        private static bool IsCategoryParam(string param)
        {
            return !IsDoubleDashParam(param) && !IsNameParam(param);
        }

        private string[] CategoryParams
        {
            get
            {
                return Params.Where(IsCategoryParam).Select(param => param.Trim()).ToArray();
            }
        }

        private string[] NameParams
        {
            get
            {
                return Params.Where(IsNameParam).Select(param => param.Substring(NameParamPrefix.Length).Trim()).ToArray();
            }
        }

        private bool ParamsContainDefault
        {
            get
            {
                return Params.Any(param => param.Equals(ParamDefault)) || CategoryParams.Length < 1;
            }
        }

        private bool ParamsContainFoundInRaid
        {
            get
            {
                return Params.Any(param => param.Equals(ParamFoundInRaid));
            }
        }
        private bool ParamsContainNotFoundInRaid
        {
            get
            {
                return Params.Any(param => param.Equals(ParamNotFoundInRaid));
            }
        }

        public static string[] GetCategoryParams(string[] parameters)
        {
            return parameters.Where(IsCategoryParam).Select(param => param.Trim()).ToArray();
        }

        public static string[] GetNameParams(string[] parameters)
        {
            return parameters.Where(IsNameParam).Select(param => param.Substring(NameParamPrefix.Length).Trim()).ToArray();
        }

        public static bool HasParamDefault(string[] parameters)
        {
            return parameters.Any(param => param.Equals(ParamDefault)) || GetCategoryParams(parameters).Length < 1;
        }

        public static bool HasParamFoundInRaid(string[] parameters)
        {
            return parameters.Any(param => param.Equals(ParamFoundInRaid));
        }
        public static bool HasParamNotFoundInRaid(string[] parameters)
        {
            return parameters.Any(param => param.Equals(ParamNotFoundInRaid));
        }
    }

}
