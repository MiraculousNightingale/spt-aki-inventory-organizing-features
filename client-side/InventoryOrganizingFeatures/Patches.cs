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
using static InventoryOrganizingFeatures.PluginConstants;

namespace InventoryOrganizingFeatures
{
    internal static class PluginConstants
    {
        public const string MoveLockTag = "@ml";
        public const string SortLockTag = "@sl";
        public const string OrganizeTag = "@o";
        public static ISession Session { get; set; }
        public static Button OrganizeButton { get; set; }
        public static bool ItemIsMoveLocked(Item item)
        {
            return item.TryGetItemComponent(out TagComponent tagComponent) && tagComponent.Name.Contains(MoveLockTag);
        }

        public static bool ItemIsSortLocked(Item item)
        {
            return item.TryGetItemComponent(out TagComponent tagComponent) && tagComponent.Name.Contains(SortLockTag);
        }
    }
    internal class PostEditTagWindowShow : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EditTagWindow), "Show", new Type[] { typeof(TagComponent), typeof(Action), typeof(Action), typeof(Action<string, int>) });
        }

        [PatchPrefix]
        private static void PatchPrefix(ref EditTagWindow __instance, ref ValidationInputField ____tagInput)
        {
            ____tagInput.characterLimit = 256;
        }
    }
    internal class PostEditTagWindowClose : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Change to "on_save" or smth
            return AccessTools.Method(typeof(EditTagWindow), "Close");
        }

        [PatchPostfix]
        private static void PatchPostFix(ref EditTagWindow __instance, ref ValidationInputField ____tagInput)
        {
            bool isSortLocked = ____tagInput.text.Contains(SortLockTag);
            bool isDragLocked = ____tagInput.text.Contains(MoveLockTag);
            bool isOrganized = ____tagInput.text.Contains(OrganizeTag);
            string notifMsg = "";
            if (isSortLocked) notifMsg += "This container is Sort Locked.";
            if (notifMsg.Length > 0) notifMsg += "\n";
            if (isDragLocked) notifMsg += "This container is Drag Locked.";
            if (notifMsg.Length > 0) notifMsg += "\n";
            if (isOrganized)
            {
                Regex regex = new(OrganizeTag + " \\b[a-zA-Z]{2,}\\b");
                string organizeStr = regex.Match(____tagInput.text).Value;
                if (organizeStr != string.Empty)
                {
                    Logger.LogError("Is not empty");
                    if (notifMsg.Length > 0) notifMsg += "\n";
                    notifMsg += $"This container will siphon {organizeStr.Substring(OrganizeTag.Length + 1)}";
                }
            }
            NotificationManagerClass.DisplayMessageNotification(notifMsg);
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
            foreach (var kvp in __instance.ItemCollection.Where(pair => !ItemIsSortLocked(pair.Key)).ToList())
            {
                kvp.Deconstruct(out Item item, out LocationInGrid locationInGrid);
                __instance.ItemCollection.Remove(item, __instance);
                __instance.SetLayout(item, locationInGrid, false);
            }
            AccessTools.Method(__instance.GetType(), "method_13").Invoke(__instance, null);
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
            if (ItemIsMoveLocked(__instance.Item)) return false;
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
            if (ItemIsMoveLocked(__instance.Item)) return false;
            return true;
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
            NotificationManagerClass.DisplayMessageNotification($"{callerClassType.Name} - caller Class");
            if (callerClassType != typeof(SimpleStashPanel)) return;
            if (OrganizeButton != null) return;

            OrganizeButton = GameObject.Instantiate(____button, ____button.transform.parent);
            OrganizeButton.onClick.RemoveAllListeners();
            OrganizeButton.onClick.AddListener(new UnityEngine.Events.UnityAction(() =>
            {
                NotificationManagerClass.DisplayMessageNotification("HUESOS HAAHAHAHAHAH");
            }));
            //clone.image.sprite = Resources.Load<Sprite>("icon_itemtype_cont");
            OrganizeButton.gameObject.SetActive(true);

            NotificationManagerClass.DisplayMessageNotification("Cloned GridSortPanel");
            //go.transform.parent =
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
            OrganizeButton.gameObject.SetActive(false);
            GameObject.Destroy(OrganizeButton);
            // Might need it.
            //GameObject.DestroyImmediate(OrganizeButton);
            //OrganizeButton = null;
        }
    }

}
