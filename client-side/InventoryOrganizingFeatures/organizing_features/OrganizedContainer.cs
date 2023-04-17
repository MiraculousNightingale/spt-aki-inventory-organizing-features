using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace InventoryOrganizingFeatures
{
    internal class OrganizedContainer
    {
        public const string ParamDefault = "--default";
        public const string ParamFoundInRaid = "--fir";
        public const string ParamNotFoundInRaid = "--not-fir";
        public static string[] DoubleDashParams = { ParamDefault, ParamFoundInRaid, ParamNotFoundInRaid };

        public const string NameParamPrefix = "n:";
        public const char NotParamPrefix = '!';

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
            if (Plugin.EnableLogs) NotificationManagerClass.DisplayMessageNotification(message, duration: EFT.Communications.ENotificationDurationType.Infinite);
        }



        // Keep reflection search outside of the loop for obvious performance reasons.
        private static string[] sortClassMethods = new string[] { "Sort", "ApplyItemToRevolverDrum", "ApplySingleItemToAddress", "Fold", "CanRecode", "CanFold" };
        private static Type sortClassType = ReflectionHelper.FindClassTypeByMethodNames(sortClassMethods);
        private static MethodInfo sortClassMove = AccessTools.Method(sortClassType, "Move");
        private static FieldInfo resultValue = AccessTools.Field(sortClassMove.ReturnType, "Value");
        private static MethodInfo controllerRunNetworkTransaction = AccessTools.Method(typeof(InventoryControllerClass), "RunNetworkTransaction");
        public void Organize()
        {
            var validItems = ValidItems;
            LogNotif($"Valid items: {validItems.Count}");
            //GClass2463 inventoryChanges = new GClass2463(TopLevelItem, Controller);
            foreach (var item in validItems)
            {
                foreach (var grid in TargetItem.Grids)
                {
                    var location = grid.FindLocationForItem(item);
                    if (location == null) continue;

                    // In reference (OnClick from ItemView) simulate = true was used.
                    var moveResult = sortClassMove.Invoke(null, new object[] { item, location, Controller, true });
                    //var moveResult = GClass2429.Move(item, location, Controller, true);
                    //GClass2429.Tran
                    var moveResultValue = resultValue.GetValue(moveResult);
                    controllerRunNetworkTransaction.Invoke(Controller, new object[] { moveResultValue, Type.Missing });
                    //Controller.RunNetworkTransaction(moveResult.Value);
                    LogNotif("Executed move.");
                }
            }
        }

        public bool ItemFitsParams(Item item)
        {
            // If item is Move Locked - ignore it
            if(Locker.IsMoveLocked(item)) return false;

            // FIR check
            if (ParamsContainFoundInRaid && !item.SpawnedInSession) return false;
            if (ParamsContainNotFoundInRaid && item.SpawnedInSession) return false;

            return CanAccept(item) && ItemPassesCategoryConditions(item) && ItemPassesNameConditions(item);
        }

        // Reference GClass2174.CanAccept or just IContainer.CheckItemFilter
        public bool CanAccept(Item item)
        {
            return TargetItem.Grids.Any(grid => grid.CanAccept(item));
        }

        private bool ItemPassesCategoryConditions(Item item)
        {
            return ItemFitsPositiveCategoryParams(item) && ItemFitsNegatedCategoryParams(item);
        }

        private bool ItemPassesNameConditions(Item item)
        {
            return ItemFitsPositiveNameParams(item) && ItemFitsNegatedNameParams(item);
        }

        private bool ItemFitsPositiveCategoryParams(Item item)
        {
            if (PositiveCategoryParams.Length < 1) return true;
            if (ParamsContainDefault) return true;
            var node = Organizer.Handbook.FindNode(item.TemplateId);
            if (node == null)
            {
                NotificationManagerClass.DisplayWarningNotification($"InventoryOrganizingFeatures Warning: Coudln't find {item.LocalizedName()} in handbook. Perhaps it's a modded item?");
                return false;
            }
            return PositiveCategoryParams.Any(param => node.CategoryContains(param));
        }

        private bool ItemFitsNegatedCategoryParams(Item item)
        {
            if (NegatedCategoryParams.Length < 1) return true;
            var node = Organizer.Handbook.FindNode(item.TemplateId);
            if (node == null)
            {
                NotificationManagerClass.DisplayWarningNotification($"InventoryOrganizingFeatures Warning: Coudln't find {item.LocalizedName()} in handbook. Perhaps it's a modded item?");
                return false;
            }
            return NegatedCategoryParams.All(param => !node.CategoryContains(param));
        }

        private bool ItemFitsPositiveNameParams(Item item)
        {
            if (PositiveNameParams.Length < 1) return true;
            return PositiveNameParams.Any(param => item.LocalizedName().ToLower().Contains(param.ToLower()));
        }

        private bool ItemFitsNegatedNameParams(Item item)
        {
            if (NegatedNameParams.Length < 1) return true;
            return NegatedNameParams.All(param => !item.LocalizedName().ToLower().Contains(param.ToLower()));
        }

        public static bool IsPositiveParam(string param)
        {
            return !param.StartsWith(NotParamPrefix.ToString());
        }

        public static bool IsNegatedParam(string param)
        {
            return param.StartsWith(NotParamPrefix.ToString());
        }

        public static bool IsDoubleDashParam(string param)
        {
            return DoubleDashParams.Any(ddp => ddp.Equals(param.ToLower()));
        }

        public static bool IsNameParam(string param)
        {
            return param.StartsWith(NameParamPrefix);
        }

        public static bool IsCategoryParam(string param)
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

        private string[] PositiveCategoryParams
        {
            get
            {
                return CategoryParams.Where(IsPositiveParam).ToArray();
            }
        }

        private string[] NegatedCategoryParams
        {
            get
            {
                return CategoryParams.Where(IsNegatedParam).Select(param => param.TrimStart(NotParamPrefix)).ToArray();
            }
        }

        private string[] NameParams
        {
            get
            {
                return Params.Where(IsNameParam).Select(param => param.Substring(NameParamPrefix.Length).Trim()).ToArray();
            }
        }

        private string[] PositiveNameParams
        {
            get
            {
                return NameParams.Where(IsPositiveParam).ToArray();
            }
        }

        private string[] NegatedNameParams
        {
            get
            {
                return NameParams.Where(IsNegatedParam).Select(param => param.TrimStart(NotParamPrefix)).ToArray();
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
