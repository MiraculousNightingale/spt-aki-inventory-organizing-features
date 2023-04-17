using Aki.Reflection.Patching;
using EFT.HandBook;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using InventoryOrganizingFeatures.Reflections;
using System;
using System.Collections;
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
using static InventoryOrganizingFeatures.Locker;
using static InventoryOrganizingFeatures.Organizer;
using static InventoryOrganizingFeatures.OrganizedContainer;
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
                if (IsSortLocked(____tagInput.text)) notifMsg += "This item is Sort Locked.";
                if (IsMoveLocked(____tagInput.text))
                {
                    if (notifMsg.Length > 0) notifMsg += "\n";
                    notifMsg += "This item is Move Locked.";
                }
                if (IsOrganized(____tagInput.text))
                {
                    if (notifMsg.Length > 0) notifMsg += "\n";
                    // Add pretty notification output
                    var orgParams = ParseOrganizeParams(____tagInput.text);
                    var categoryParams = GetCategoryParams(orgParams);
                    var nameParams = GetNameParams(orgParams);

                    notifMsg += "This item's tag has following organize params:";
                    if (HasParamDefault(orgParams))
                    {
                        notifMsg += $"\n  -  Category: default container categories";
                    }
                    else if (categoryParams.Length > 0)
                    {
                        notifMsg += $"\n  -  Category: {string.Join(", ", categoryParams)}";
                    }

                    if (nameParams.Length > 0)
                    {
                        notifMsg += $"\n  -  Name: {string.Join(", ", nameParams)}";
                    }

                    if (HasParamFoundInRaid(orgParams))
                    {
                        notifMsg += "\n  -  Only \"Found in raid\".";
                    }
                    else if (HasParamNotFoundInRaid(orgParams))
                    {
                        notifMsg += "\n  -  Only \"Not found in raid.\"";
                    }
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
            return AccessTools.Method(ReflectionHelper.FindClassTypeByMethodNames(sortClassMethods), "Sort");
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

    internal class PreGridClassRemoveAll : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Find the Grid class (Per STP-AKI 3.5.5 it's a GClass2166)
            var gridClassMethods = new string[] { "FindItem", "GetItemsInRect", "FindLocationForItem", "Add", "AddItemWithoutRestrictions", "Remove", "RemoveAll", "CanReplace" };
            return AccessTools.Method(ReflectionHelper.FindClassTypeByMethodNames(gridClassMethods), "RemoveAll");
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref object __instance)
        {
            var sortClassMethods = new string[] { "Sort", "ApplyItemToRevolverDrum", "ApplySingleItemToAddress", "Fold", "CanRecode", "CanFold" };
            var sortClassType = ReflectionHelper.FindClassTypeByMethodNames(sortClassMethods);
            var callerClassType = new StackTrace().GetFrame(2).GetMethod().ReflectedType;
            //NotificationManagerClass.DisplayMessageNotification($"{sortClassType.Name}\n{callerClassType.Name} - caller Class");
            // If method is being called from the static SortClass - run patched code, if not - run default code.
            if (callerClassType != sortClassType) return true;

            var itemCollection = (IEnumerable<KeyValuePair<Item, LocationInGrid>>)AccessTools.Property(__instance.GetType(), "ItemCollection").GetValue(__instance);
            //if (!__instance.ItemCollection.Any())
            if (!itemCollection.Any())
            {
                return false;
            }
            var itemCollectionRemove = AccessTools.Method(itemCollection.GetType(), "Remove");
            var gridSetLayout = AccessTools.Method(__instance.GetType(), "SetLayout");
            //foreach (var kvp in __instance.ItemCollection.Where(pair => !IsSortLocked(pair.Key)).ToList())
            foreach (var kvp in itemCollection.Where(pair => !IsSortLocked(pair.Key)).ToList())
                {
                kvp.Deconstruct(out Item item, out LocationInGrid locationInGrid);
                //__instance.ItemCollection.Remove(item, __instance);
                itemCollectionRemove.Invoke(itemCollection, new object[] { item, __instance });
                //__instance.SetLayout(item, locationInGrid, false);
                gridSetLayout.Invoke(__instance, new object[] { item, locationInGrid, false });
            }

            var lastLineMethod = __instance // look for method with generic name, called on the last line of RemoveAll()
                .GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(method =>
                {
                    return method.ReturnType == typeof(void)
                    && method.GetMethodBody().LocalVariables.Count == 6
                    && method.GetMethodBody().LocalVariables.All(variable => variable.LocalType == typeof(int));
                })
                .First(); // let it throw exception if somehow the method wasn't found.
            lastLineMethod.Invoke(__instance, null);
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
            var callerClassType = new StackTrace().GetFrame(2).GetMethod().ReflectedType;
            //NotificationManagerClass.DisplayMessageNotification($"{callerClassType.Name} - caller Class");
            // Make sure to only copy the button when loading the sort button in the simplePanel of inventory screen.
            // Otherwise sort buttons for separate container views will dupe too. Although it's not a bad idea anyway.
            if (callerClassType != typeof(SimpleStashPanel)) return;
            if (OrganizeButton != null)
                if (!OrganizeButton.IsDestroyed()) return;

            OrganizeButton = GameObject.Instantiate(____button, ____button.transform.parent);
            OrganizeButton.onClick.RemoveAllListeners();

            // Use GridSortPanel's progress indicator.
            var gridSortPanelSetInProgress = __instance
                .GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(method =>
                {
                    var args = method.GetParameters();
                    if(args.Length != 1) return false;

                    var firstParam = args.First();
                    return firstParam.ParameterType == typeof(bool) && firstParam.Name.Equals("inProgress");
                })
                .First(); // let it throw exception if somehow method wasn't found.

            OrganizeButton.onClick.AddListener(new UnityEngine.Events.UnityAction(() =>
            {
                ItemUiContext.Instance.ShowMessageWindow("Do you want to organize all items by tagged containers?", new Action(async () =>
                {
                    gridSortPanelSetInProgress.Invoke(__instance, new object[] { true });
                    await Organize(item, controller);
                    gridSortPanelSetInProgress.Invoke(__instance, new object[] { false });
                }), new Action(MessageNotifCancel));
            }));
            OrganizeButton.image.sprite = OrganizeSprite;
            OrganizeButton.gameObject.DestroyAllChildren();

            OrganizeButton.gameObject.SetActive(true);
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

    internal class PostInitHanbook : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MenuTaskBar), "InitHandbook");
        }

        [PatchPostfix]
        private static void PatchPostfix(ref object handbook)
        {
            if (Organizer.Handbook == null) Organizer.Handbook = new Handbook(handbook);
            //Logger.LogMessage($"Elements: {Organizer.Handbook.NodesTree.Count}");
            //var search = Organizer.Handbook.FindNode("5751496424597720a27126da");
            //if (search != null)
            //{
            //    Logger.LogMessage($"Found: {search.Data.Name.Localized()}");
            //    Logger.LogMessage($"Categories: {string.Join(" > ", search.Category.Select(cat => cat.Localized()))}");
            //}
        }
    }

}
