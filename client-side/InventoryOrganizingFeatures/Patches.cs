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
using InventoryOrganizingFeatures.Reflections.Extensions;
using TMPro;

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
            try
            {
                ____tagInput.characterLimit = 256;
                ____saveButtonSpawner.OnClick.AddListener(new UnityEngine.Events.UnityAction(() =>
                {
                    try
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
                            if (HasOrderParam(orgParams))
                            {
                                notifMsg += $"\n  -  Order #{GetOrderParam(orgParams).GetValueOrDefault()}";
                            }
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
                    }
                    catch (Exception ex)
                    {
                        throw Plugin.ShowErrorNotif(ex);
                    }
                }));
            }
            catch (Exception ex)
            {
                throw Plugin.ShowErrorNotif(ex);
            }

        }
    }

    // Static SortClass.Sort() checks if Item.CurrentLocation is null
    // so preventing sort locked items from being removed
    // makes the sort method ignore them.
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
            try
            {
                // Dynamically find static SortClass
                var sortClassMethods = new string[] { "Sort", "ApplyItemToRevolverDrum", "ApplySingleItemToAddress", "Fold", "CanRecode", "CanFold" };
                var sortClassType = ReflectionHelper.FindClassTypeByMethodNames(sortClassMethods);
                var callerClassType = new StackTrace().GetFrame(2).GetMethod().ReflectedType;
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
                    //kvp.Deconstruct(out Item item, out LocationInGrid locationInGrid); - uses a GClass781 extension
                    var item = kvp.Key;
                    var locationInGrid = kvp.Value;
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
                //NotificationManagerClass.DisplayMessageNotification("Ran the RemoveAll patch");
                return false;
            }
            catch (Exception ex)
            {
                throw Plugin.ShowErrorNotif(ex);
            }
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
            try
            {
                if (IsMoveLocked(__instance.Item)) return false;
                return true;
            }
            catch (Exception ex)
            {
                throw Plugin.ShowErrorNotif(ex);
            }
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
            try
            {
                if (IsMoveLocked(__instance.Item)) return false;
                return true;
            }
            catch (Exception ex)
            {
                throw Plugin.ShowErrorNotif(ex);
            }
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
            // Seems to throw null-ref exceptions even when versions are compatible
            // Added some null safety, because the cause can be pretty much anything.
            try
            {
                if (__instance == null) return;

                //// Make sure to only execute if called for ItemView, OnClick method.
                var callerMethod = new StackTrace()?.GetFrame(2)?.GetMethod();
                if (callerMethod == null) return; // some more wacky null safety
                if (callerMethod.Name.Equals("OnClick") && callerMethod.ReflectedType == typeof(ItemView))
                {
                    // instance is actually of type GClass2441 - that's pretty useful. It has lots of info.
                    Item item = AccessTools.Property(__instance.GetType(), "Item").GetValue(__instance) as Item;
                    if (item == null) return; // null safety
                    if (item.TryGetItemComponent(out TagComponent tagComp))
                    {
                        if (IsMoveLocked(tagComp.Name)) __result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw Plugin.ShowErrorNotif(ex);
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
            try
            {
                if (IsMoveLocked(item)) displayWarnings = false;
            }
            catch (Exception ex)
            {
                throw Plugin.ShowErrorNotif(ex);
            }
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
            try
            {
                var callerClassType = new StackTrace().GetFrame(2).GetMethod().ReflectedType;
                //NotificationManagerClass.DisplayMessageNotification($"Caller class {callerClassType.Name}");

                // For Stash panel
                // TraderDealScreen - when opening trader
                // SimpleStashPanel - in stash
                // GridWindow - when opening a container
                if (callerClassType == typeof(SimpleStashPanel))
                {
                    if (OrganizeButtonStash != null)
                        if (!OrganizeButtonStash.IsDestroyed()) return;
                    OrganizeButtonStash = SetupOrganizeButton(____button, item, controller);
                    return;
                }

                if (callerClassType == typeof(TraderDealScreen))
                {
                    if (OrganizeButtonTrader != null)
                        if (!OrganizeButtonTrader.IsDestroyed()) return;
                    OrganizeButtonTrader = SetupOrganizeButton(____button, item, controller);
                    return;
                }
                // For Container view panel (caller class is GridWindow)
                // Hoping container panel disposes children properly.
                var orgbtn = SetupOrganizeButton(____button, item, controller);
                orgbtn.transform.parent.GetChild(orgbtn.transform.parent.childCount - 2).SetAsLastSibling();
            }
            catch (Exception ex)
            {
                throw Plugin.ShowErrorNotif(ex);
            }
        }

        private static Button SetupOrganizeButton(Button sourceForCloneButton, LootItemClass item, InventoryControllerClass controller)
        {
            var clone = GameObject.Instantiate(sourceForCloneButton, sourceForCloneButton.transform.parent);
            clone.onClick.RemoveAllListeners();

            // - Using async organizing causes issues with UI (index out of bounds)
            // - so there's no reason to use inProgress indicator.
            // Use GridSortPanel's progress indicator.
            //var gridSortPanelSetInProgress = __instance
            //    .GetType()
            //    .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            //    .Where(method =>
            //    {
            //        var args = method.GetParameters();
            //        if(args.Length != 1) return false;

            //        var firstParam = args.First();
            //        return firstParam.ParameterType == typeof(bool) && firstParam.Name.Equals("inProgress");
            //    })
            //    .First(); // let it throw exception if somehow method wasn't found.

            clone.onClick.AddListener(new UnityEngine.Events.UnityAction(() =>
            {
                try
                {
                    //Reflected invoke of ItemUiContext.Instance.ShowMessageWindow() because it returns a GClass2709(as of SPT-AKI 3.5.3);
                    //If used often, should be moved into a special helper method.
                    var showMessageWindowArgs = new object[]
                    {
                        "Do you want to organize all items by tagged containers?",
                        new Action(() =>
                        {
                            //gridSortPanelSetInProgress.Invoke(__instance, new object[] { true });
                            Organize(item, controller);
                            //gridSortPanelSetInProgress.Invoke(__instance, new object[] { false });
                        }),
                        new Action(MessageNotifCancel),
                    };
                    var showMessageWindowArgTypes = new Type[] 
                    {
                        typeof(string), // description
                        typeof(Action), // acceptAction
                        typeof(Action), // cancelAction
                        typeof(string), // caption
                        typeof(float), // time
                        typeof(bool), // forceShow
                        typeof(TextAlignmentOptions), // alignment
                    };
                    ReflectionHelper.InvokeMethod(
                        ItemUiContext.Instance,
                        "ShowMessageWindow",
                        showMessageWindowArgs,
                        showMessageWindowArgTypes
                    );
                }
                catch (Exception ex)
                {
                    throw Plugin.ShowErrorNotif(ex);
                }
            }));
            //clone.image.sprite = OrganizeSprite;
            //clone.gameObject.DestroyAllChildren();

            // For stash panel
            var childImage = clone.transform.GetChildren().Where(child => child.name.Equals("Image")).FirstOrDefault();
            if (childImage != null)
            {
                //Logger.LogMessage($"Sprite path: {childImage.GetComponent<UnityEngine.UI.Image>().path}")
                //childImage.GetComponent<UnityEngine.UI.Image>().sprite = OrganizeSprite; - looks badly stretched
                // Just replace the background image with the new one, sort of looks better.
                clone.gameObject.DestroyAllChildren();
                clone.image.sprite = OrganizeSprite;
            }
            else
            {
                // For container view panel
                var sortIcon = clone.transform.GetChildren().Where(child => child.name.Equals("SortIcon")).FirstOrDefault();
                if (sortIcon != null)
                {
                    sortIcon.GetComponent<UnityEngine.UI.Image>().sprite = OrganizeSprite;
                }
                var text = clone.transform.GetChildren().Where(child => child.name.Equals("Text")).FirstOrDefault();
                if (text != null)
                {
                    text.GetComponent<CustomTextMeshProUGUI>().text = "ORG.";
                }
            }

            clone.gameObject.SetActive(true);



            return clone;
        }

        private static void MessageNotifCancel()
        {
            // Empty method is used to do nothing and close message window,
            // since simply passing null doesn't work.
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
            try
            {
                if (OrganizeButtonStash == null) return;
                if (OrganizeButtonStash.IsDestroyed()) return;

                OrganizeButtonStash.gameObject.SetActive(false);
                GameObject.Destroy(OrganizeButtonStash);

                // Might need it.
                //GameObject.DestroyImmediate(OrganizeButton);
                //OrganizeButton = null;
            }
            catch (Exception ex)
            {
                throw Plugin.ShowErrorNotif(ex);
            }
        }
    }

    internal class PostTraderDealScreenClose : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderDealScreen), "Close");
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            try
            {
                if (OrganizeButtonTrader == null) return;
                if (OrganizeButtonTrader.IsDestroyed()) return;

                OrganizeButtonTrader.gameObject.SetActive(false);
                GameObject.Destroy(OrganizeButtonTrader);

                // Might need it.
                //GameObject.DestroyImmediate(OrganizeButton);
                //OrganizeButton = null;
            }
            catch (Exception ex)
            {
                throw Plugin.ShowErrorNotif(ex);
            }
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
            try
            {
                if (OrganizeSprite != null) return;
                OrganizeSprite = AccessTools.Field(____hideoutButton.GetType(), "_iconSprite").GetValue(____hideoutButton) as Sprite;
            }
            catch (Exception ex)
            {
                throw Plugin.ShowErrorNotif(ex);
            }
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
            try
            {
                Organizer.Handbook ??= new Handbook(handbook);
                //Logger.LogMessage($"Elements: {Organizer.Handbook.NodesTree.Count}");
                //var search = Organizer.Handbook.FindNode("5751496424597720a27126da");
                //if (search != null)
                //{
                //    Logger.LogMessage($"Found: {search.Data.Name.Localized()}");
                //    Logger.LogMessage($"Categories: {string.Join(" > ", search.Category.Select(cat => cat.Localized()))}");
                //}
            }
            catch (Exception ex)
            {
                throw Plugin.ShowErrorNotif(ex);
            }
        }
    }
}
