using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;
using static InventoryOrganizingFeatures.InventoryOrganizer;
using static System.Net.Mime.MediaTypeNames;

namespace InventoryOrganizingFeatures
{
    internal class PostEditTagWindowShow : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EditTagWindow), "Show", new Type[] { typeof(TagComponent), typeof(Action), typeof(Action), typeof(Action<string, int>) });
        }

        [PatchPrefix]
        private static void PatchPrefix(ref EditTagWindow __instance, ref DefaultUIButton ____saveButtonSpawner, ValidationInputField ____tagInput)
        {
            ____tagInput.characterLimit = 256;
            ____saveButtonSpawner.OnClick.AddListener(new UnityEngine.Events.UnityAction(() =>
            {
                string notifMsg = "";
                if (IsSortLocked(____tagInput.text)) notifMsg += "This container is Sort Locked.";
                if (IsMoveLocked(____tagInput.text))
                {
                    if (notifMsg.Length > 0) notifMsg += "\n";
                    notifMsg += "This container is Move Locked.";
                }
                if (IsOrganized(____tagInput.text))
                {
                    if (notifMsg.Length > 0) notifMsg += "\n";
                    notifMsg += $"This container is organized with following params: {string.Join(", ", ParseOrganizeParams(____tagInput.text))}";
                }
                if (notifMsg.Length > 0) NotificationManagerClass.DisplayMessageNotification(notifMsg);
            }));
        }
    }

    // Deprecated
    internal class PreSortClassSort : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var sortClassMethods = new string[] { "Sort", "ApplyItemToRevolverDrum", "ApplySingleItemToAddress", "Fold", "CanRecode", "CanFold" };
            var targetClassType = ReflectionHelper.FindClassTypeByMethodNames(sortClassMethods);
            return AccessTools.Method(targetClassType, "Sort");
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref LootItemClass sortingItem, ref InventoryControllerClass controller, ref bool simulate, ref object __result)
        {
            //var gclass2463 = ReflectionHelper.FindClassTypeByMethodNames(new string[] { "SetOldPositions", "AddItemToGrid", "RemoveItemFromGrid", "Execute", "RollBack" });
            //object gclass = AccessTools.Constructor(gclass2463, new Type[] { typeof(LootItemClass), typeof(InventoryControllerClass) }).Invoke(new object[] { sortingItem, controller });
            NotificationManagerClass.DisplayMessageNotification("Mothafucka doesnt work bruh Entered patch execution.");

            GClass2463 gclass2463 = new GClass2463(sortingItem, controller);
            if (!gclass2463.CanExecute((TraderControllerClass)controller))
            {
                __result = new GStruct325<GClass2463>(new GClass2856(sortingItem));
                //return (GStruct325<GClass2463>)(GClass2823)new GClass2856((Item)sortingItem);
                return false;
            }
            List<Item> items = new List<Item>();
            foreach (GClass2166 grid in sortingItem.Grids)
            {
                gclass2463.SetOldPositions(grid, grid.ItemCollection.ToListOfLocations());
                items.AddRange(grid.Items);
                grid.RemoveAll();
                controller.RaiseEvent(new GEventArgs23((IContainer)grid));
            }
            List<Item> objList = GClass2412.Sort((IEnumerable<Item>)items);
            int num = 5;
            InventoryError inventoryError = (InventoryError)null;
            for (int index = 0; index < objList.Count; ++index)
            {
                Item obj1 = objList[index];
                if (obj1.CurrentAddress == null)
                {
                    bool flag = false;
                    foreach (GClass2166 grid in sortingItem.Grids)
                    {
                        if (!grid.Add(obj1).Failed)
                        {
                            flag = true;
                            gclass2463.AddItemToGrid(grid, new GClass2174(obj1, ((GClass2424)obj1.CurrentAddress).LocationInGrid));
                            break;
                        }
                    }
                    if (!flag && --num > 0)
                    {
                        GStruct24 cellSize1 = obj1.CalculateCellSize();
                        while (!flag && --index > 0)
                        {
                            Item obj2 = objList[index];
                            GStruct24 cellSize2 = obj2.CalculateCellSize();
                            if (!cellSize1.Equals((object)cellSize2))
                            {
                                GClass2166 grid = gclass2463.RemoveItemFromGrid(obj2);
                                if (grid != null && !grid.Add(obj1).Failed)
                                {
                                    flag = true;
                                    gclass2463.AddItemToGrid(grid, new GClass2174(obj1, ((GClass2424)obj1.CurrentAddress).LocationInGrid));
                                }
                            }
                        }
                        --index;
                    }
                    else if (num <= 0)
                    {
                        inventoryError = (InventoryError)new GClass2857((Item)sortingItem);
                        break;
                    }
                }
            }
            if (inventoryError != null)
            {
                gclass2463.RollBack();
                gclass2463.RaiseEvents((TraderControllerClass)controller, CommandStatus.Failed);
                __result = new GStruct325<GClass2463>(inventoryError);
                //return (GStruct325<GClass2463>)(GClass2823)inventoryError;
                return false;
            }
            if (simulate)
            {
                gclass2463.RollBack();
            }
            foreach (GClass2166 grid in sortingItem.Grids)
            {
                if (grid.ItemCollection.Any<KeyValuePair<Item, LocationInGrid>>() && grid is GClass2169 gclass2169)
                    gclass2169.FindAll(controller.Profile.Id);
            }
            __result = new GStruct325<GClass2463>(gclass2463);
            //return (GStruct325<GClass2463>)gclass2463;
            return false;

        }

    }

    internal class PreGClass2166RemoveAll : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var sortClassMethods = new string[] { "Sort", "ApplyItemToRevolverDrum", "ApplySingleItemToAddress", "Fold", "CanRecode", "CanFold" };
            var targetClassType = ReflectionHelper.FindClassTypeByMethodNames(sortClassMethods);
            return AccessTools.Method(typeof(GClass2166), "RemoveAll");
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref GClass2166 __instance)
        {
            var sortClassMethods = new string[] { "Sort", "ApplyItemToRevolverDrum", "ApplySingleItemToAddress", "Fold", "CanRecode", "CanFold" };
            var sortClassType = ReflectionHelper.FindClassTypeByMethodNames(sortClassMethods);
            var callerClassType = new StackTrace().GetFrame(2).GetMethod().ReflectedType;
            NotificationManagerClass.DisplayMessageNotification($"{sortClassType.Name}\n{callerClassType.Name} - caller Class");
            if (callerClassType != sortClassType) return true;

            // If method is being called from the static SortClass - run patched code instead.
            if (!__instance.ItemCollection.Any())
            {
                return false;
            }
            foreach (var kvp in __instance.ItemCollection.Where(pair => !IsSortLocked(pair.Key)).ToList())
            {
                kvp.Deconstruct(out Item item, out LocationInGrid locationInGrid);
                __instance.ItemCollection.Remove(item, __instance);
                __instance.SetLayout(item, locationInGrid, false);
            }
            AccessTools.Method(__instance.GetType(), "method_13").Invoke(__instance, null); // dynamically reflect this method
            NotificationManagerClass.DisplayMessageNotification("Ran the RemoveAll patch");
            return false;
        }
    }

    internal class PreItemViewOnBeginDrag : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemView), "OnBeginDrag");
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref ItemView __instance)
        {
            if (IsMoveLocked(__instance.Item)) return false;
            return true;
        }
    }

    internal class PreItemViewOnPointerDown : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemView), "OnPointerDown");
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref ItemView __instance)
        {
            if (IsMoveLocked(__instance.Item)) return false;
            return true;
        }
    }

    internal class PostGetFailedProperty : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(AccessTools.Method(typeof(ItemUiContext), "QuickFindAppropriatePlace").ReturnType, "Failed");
        }

        [PatchPostfix]
        private static void PatchPostfix(ref object __instance, ref bool __result)
        {
            if (__instance == null) return;

            //// Make sure to only execute if called for ItemView, OnClick method.
            var callerMethod = new StackTrace().GetFrame(2).GetMethod();
            if (callerMethod.Name.Equals("OnClick") && callerMethod.ReflectedType == typeof(ItemView))
            {
                // instance is actually of type GClass2441 - that's pretty useful. It has lots of info.
                Item item = AccessTools.Property(__instance.GetType(), "Item").GetValue(__instance) as Item;
                if (item.TryGetItemComponent(out TagComponent tagComp))
                {
                    if (IsMoveLocked(tagComp.Name)) __result = true;
                }
            }
        }
    }

    internal class PreQuickFindAppropriatePlace : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), "QuickFindAppropriatePlace");
        }

        [PatchPrefix]
        private static void PatchPrefix(Item item, ref bool displayWarnings)
        {
            if (IsMoveLocked(item)) displayWarnings = false;
        }
    }

    internal class PostInventoryScreenShow : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryScreen), "Show");
        }

        [PatchPostfix]
        private static void PatchPostfix(IHealthController healthController, InventoryControllerClass controller, QuestControllerClass questController, LootItemClass[] lootItems, InventoryScreen.EInventoryTab tab, ISession session)
        {
            Session = session;
        }
    }

    internal class PostGridSortPanelShow : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridSortPanel), "Show");
        }

        [PatchPostfix]
        private static void PatchPostfix(GridSortPanel __instance, InventoryControllerClass controller, LootItemClass item, Button ____button)
        {
            //GameObject gspCloneObj = GameObject.Instantiate(__instance.gameObject, __instance.gameObject.transform.parent);
            //var button = gspCloneObj.GetComponent<Button>();
            //button.
            //gspCloneObj.SetActive(true);
            var callerClassType = new StackTrace().GetFrame(2).GetMethod().ReflectedType;
            //NotificationManagerClass.DisplayMessageNotification($"{callerClassType.Name} - caller Class");
            // Make sure to only copy the button when loading the sort button in the simplePanel of inventory screen.
            // Otherwise sort buttons for separate container views will dupe too. Although it's not a bad idea anyway.
            if (callerClassType != typeof(SimpleStashPanel)) return;
            if (OrganizeButton != null)
                if (!OrganizeButton.IsDestroyed()) return;

            OrganizeButton = GameObject.Instantiate(____button, ____button.transform.parent);
            OrganizeButton.onClick.RemoveAllListeners();
            OrganizeButton.onClick.AddListener(new UnityEngine.Events.UnityAction(() =>
            {
                ItemUiContext.Instance.ShowMessageWindow("Do you want to organize all items by tagged containers?", new Action(() =>
                {
                    foreach (var grid in item.Grids)
                    {
                        var organizedItems = grid.Items.Where(IsOrganized);
                        foreach (var orgItem in organizedItems)
                        {
                            var orgParams = ParseOrganizeParams(orgItem);
                            var validItems = grid.Items.Where(item => orgParams.Any(param => IsChildOfLocalized(item, param)));
                            Logger.LogMessage($"Valid Items: {validItems.Count()}");
                        }
                        //Logger.LogMessage($"Counts: Items - {grid.Items.Count()} ContainedItems - {grid.ContainedItems.Count}");
                    }
                }), new Action(MessageNotifCancel));
            }));
            OrganizeButton.image.sprite = OrganizeSprite;
            OrganizeButton.gameObject.DestroyAllChildren();

            OrganizeButton.gameObject.SetActive(true);
        }

        private static bool IsChildOfLocalized(Item item, string searchedLocalizedName, bool caseSensitive = false)
        {
            for (ItemTemplate parent = item.Template.Parent; parent != null; parent = parent.Parent)
            {
                if (caseSensitive)
                {
                    if (parent.NameLocalizationKey.Localized().Contains(searchedLocalizedName)) return true;
                }
                else
                {
                    if (parent.NameLocalizationKey.Localized().ToLower().Contains(searchedLocalizedName.ToLower())) return true;
                }
                //Logger.LogMessage($"Parent Name {parent.NameLocalizationKey.Localized()}");
            }
            return false;
        }

        private static void OrganizeButtonOnClick()
        {
        }

        private static void MessageNotifAccept()
        {
            NotificationManagerClass.DisplayMessageNotification("Cock suckah");
        }
        private static void MessageNotifCancel()
        {
        }
    }

    internal class PostSimpleStashPanelClose : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SimpleStashPanel), "Close");
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            if (OrganizeButton == null) return;
            if (OrganizeButton.IsDestroyed()) return;

            OrganizeButton.gameObject.SetActive(false);
            GameObject.Destroy(OrganizeButton);

            // Might need it.
            //GameObject.DestroyImmediate(OrganizeButton);
            //OrganizeButton = null;
        }
    }

    internal class PostMenuScreenInit : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MenuScreen), "Init");
        }

        [PatchPostfix]
        private static void PatchPostfix(ref DefaultUIButton ____hideoutButton)
        {
            if (OrganizeSprite != null) return;
            OrganizeSprite = AccessTools.Field(____hideoutButton.GetType(), "_iconSprite").GetValue(____hideoutButton) as Sprite;
        }
    }

}
