﻿using CMS.UI;
using CMS.UI.Windows;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.IO;
using MelonLoader;
using System.Linq;
using UnityEngine;

namespace TransferAll
{
    /// <summary>
    /// Information for MelonLoader.
    /// </summary>
    public static class BuildInfo
    {
        public const string Name = "Transfer All";
        public const string Description = "Mod to automatically transfer all parts from your Inventory to the Warehouse. Also, works in the Barn and Junkyard; including automatically moving all junk to the shopping cart.";
        public const string Author = "mannly82";
        public const string Company = "The Mann Design";
        public const string Version = "1.0.1";
        public const string DownloadLink = "https://www.nexusmods.com/carmechanicsimulator2021/mods/174";
        public const string MelonGameCompany = "Red Dot Games";
        public const string MelonGameName = "Car Mechanic Simulator 2021";
    }

    /// <summary>
    /// Create a "TransferAll.cfg" file in the Mods folder.
    /// </summary>
    public class ConfigFile
    {
        /// <summary>
        /// Settings Category
        /// </summary>
        private const string SettingsCatName = "Settings";
        private readonly MelonPreferences_Category _settings;
        /// <summary>
        /// User option for the key to transfer the Inventory/Warehouse/Junk items.
        /// </summary>
        public KeyCode TransferAllItemsAndGroups => _transferAllBindKey.Value;
        private readonly MelonPreferences_Entry<KeyCode> _transferAllBindKey;
        /// <summary>
        /// User option for the key to transfer all the junk from the Barn or Junkyard.
        /// </summary>
        public KeyCode TransferEntireJunkyardOrBarn => _transferEntireBindKey.Value;
        private readonly MelonPreferences_Entry<KeyCode> _transferEntireBindKey;
        /// <summary>
        /// User option for the minimum number of items to show a warning message before transfer.
        /// </summary>
        public int MinNumOfItemsWarning => _minNumOfItemsWarning.Value;
        private readonly MelonPreferences_Entry<int> _minNumOfItemsWarning;

        /// <summary>
        /// Implementation of Settings properties.
        /// </summary>
        public ConfigFile()
        {
            _settings = MelonPreferences.CreateCategory(SettingsCatName);
            _settings.SetFilePath($"Mods/TransferAll.cfg");
            _transferAllBindKey = _settings.CreateEntry(nameof(TransferAllItemsAndGroups), KeyCode.P, 
                description: "Press This Key to transfer all current items and groups.");
            _transferEntireBindKey = _settings.CreateEntry(nameof(TransferEntireJunkyardOrBarn), KeyCode.L,
                description: "ONLY WORKS AT BARN AND JUNKYARD." + Il2CppSystem.Environment.NewLine + "Press This Key to transfer all items and groups from ALL junk stashes.");
            _minNumOfItemsWarning = _settings.CreateEntry(nameof(MinNumOfItemsWarning), 500,
                description: "This will display a warning if the items/groups are above this number (to prevent large moves by accident).");

            // Remove the old configuration if it's still there.
            _settings.DeleteEntry("TransferAllItemsAndGroupsLeftControlPlus");
            _settings.DeleteEntry("TransferEntireJunkyardOrBarnLeftAltPlus");
        }
    }

    public class TransferAll : MelonMod
    {
        /// <summary>
        /// Reference to Settings file.
        /// </summary>
        private ConfigFile _configFile;

        /// <summary>
        /// Global reference to the current scene.
        /// </summary>
        private string _currentScene = string.Empty;
        /// <summary>
        /// Global reference to verify if a tutorial is visible.
        /// </summary>
        private bool _myTutorialEnabled = false;

        private string _qolPath = $"{Directory.GetCurrentDirectory()}\\Mods\\QoLmod.dll";
        /// <summary>
        /// QoLMod shows a popup for every part inside a group as its moved and
        /// this causes some serious performance issues during bulk moves,
        /// so these references allow these settings to be disabled temporarily.
        /// </summary>
        private System.Reflection.FieldInfo _qolSettings = null;
        private bool _qolGroupAddedPopup = false;
        private bool _qolAllPartsPopup = false;

        /// <summary>
        /// Global reference used to temporarily hold the individual junk stashes in the Barn and Junkyard.
        /// This allows the individual stashes to be "undone" and transferred back.
        /// </summary>
        private Dictionary<System.IntPtr, List<Item>> _tempItems = new Dictionary<System.IntPtr, List<Item>>();
        /// <summary>
        /// Global reference used to temporarily hold the individual junk stashes in the Barn and Junkyard.
        /// This allows the individual stashes to be "undone" and transferred back.
        /// </summary>
        private Dictionary<System.IntPtr, List<GroupItem>> _tempGroups = new Dictionary<System.IntPtr, List<GroupItem>>();

        public override void OnInitializeMelon()
        {
            // Tell the user that we're loading the Settings.
            MelonLogger.Msg("Loading Configuration...");
            _configFile = new ConfigFile();
        }

        public override void OnLateInitializeMelon()
        {
            // Specifically for the QoLmod effect explained above.
            // Check that the QoLmod.dll is even installed.
            if (File.Exists(_qolPath))
            {
                // Use reflection to load the dll.
                MelonAssembly cms2021mod = MelonAssembly.LoadMelonAssembly(_qolPath);
                if (cms2021mod != null)
                {
                    // Get the loaded melon (there is only 1).
                    var melons = cms2021mod.LoadedMelons;
                    if (melons.Count == 1)
                    {
                        var melon = melons[0];
                        // This may not be necessary, but doesn't seem to hurt.
                        melon.Register();
                        // The GetType() method of Assembly didn't seem to find the correct Main Class,
                        // so use Linq to find the correct Class.
                        var mainType = melon.MelonAssembly.Assembly.GetTypes().First(n => n.Name.ToLower().Contains("main"));
                        // The GetField() method had the same problem, so use Linq to get the "asetukset" (settings) field.
                        _qolSettings = mainType.GetFields().First(p => p.Name.ToLower().Contains("ase"));
                        // Rather than defining the entire Settingssit Class from QoLmod, load it dynamically at runtime.
                        dynamic settingsValue = _qolSettings.GetValue(null);
                        // Find the "showPopupforGroupAddedInventory" value from Settingssit Class and store it's value.
                        // From QoLmod.cfg: When you remove wheel or suspension assembly from car, popup will show total condition
                        // This happens for every item in the group that is moved and the bulk move causes errors with the PopupManager.
                        _qolGroupAddedPopup = settingsValue.Value.showPopupforGroupAddedInventory;
                        // Find the "showPopupforAllPartsinGroup" value from Settingssit Class and store it's value.
                        // From QoLmod.cfg: When you remove wheel or suspension assembly from car, popup will show parts condition
                        // This happens for every item in the group that is moved and the bulk move causes errors with the PopupManager.
                        _qolAllPartsPopup = settingsValue.Value.showPopupforAllPartsinGroup;
                        // Show the user that the QoLmod was found and the current values of the two settings.
                        // This allows us to restore these settings after doing the bulk moves.
                        MelonLogger.Msg($"QoLmod Found, showPopupforGroupAddedInventory: {_qolGroupAddedPopup}");
                        MelonLogger.Msg($"QoLmod Found, showPopupforAllPartsinGroup: {_qolAllPartsPopup}");
                    }
                }
            }
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            // Save a reference to the current scene.
            _currentScene = sceneName.ToLower();
        }
        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            // Clear the temporary dictionaries when the user leaves the scene.
            if (sceneName.ToLower() == "barn" ||
                sceneName.ToLower() == "junkyard")
            {
                _tempItems.Clear();
                _tempGroups.Clear();
            }
        }

        public override void OnUpdate()
        {
            // Only work on these scenes.
            if (_currentScene.Equals("garage") ||
                _currentScene.Equals("barn") ||
                _currentScene.Equals("junkyard"))
            {
                var windowManager = WindowManager.Instance;
                if (windowManager != null)
                {
                    // Find out if any windows are currently being shown.
                    if (windowManager.activeWindows.count > 0)
                    {
                        // Check if the current window is the Warehouse or Barn/Junkyard window.
                        if (windowManager.IsWindowActive(WindowID.Warehouse) ||
                            windowManager.IsWindowActive(WindowID.ItemsExchange))
                        {
                            // Close the Tutorial Message if it's open
                            if (_myTutorialEnabled)
                            {
                                UIManager.Get().PopupManager.HideTutorial();
                                _myTutorialEnabled = false;
                            }
                        }
                    }
                }
            }

            // Check if the user pressed the Key in Settings.
            if (Input.GetKeyDown(_configFile.TransferAllItemsAndGroups))
            {
                // Only work on these scenes.
                if (_currentScene.Equals("garage") ||
                    _currentScene.Equals("barn") ||
                    _currentScene.Equals("junkyard"))
                {
                    // These are debug/test methods.
                    //ShowSceneName();
                    //ShowWindowName();
                    //ShowCurrentCategory();

                    // Disable QolMod settings temporarily.
                    if (_qolGroupAddedPopup || _qolAllPartsPopup)
                    {
                        dynamic settingsValue = _qolSettings.GetValue(null);
                        settingsValue.Value.showPopupforGroupAddedInventory = false;
                        settingsValue.Value.showPopupforAllPartsinGroup = false;
                    }

                    // Check that the user is in the Garage.
                    if (_currentScene.Equals("garage"))
                    {
                        // Check if the Warehouse is unlocked first.
                        if (CheckIfWarehouseIsUnlocked())
                        {
                            // Do work on the Inventory or Warehouse.
                            MoveInventoryOrWarehouseItems();
                        }
                        else
                        {
                            // The user hasn't upgraded their Garage, so show them a message.
                            UIManager.Get().ShowPopup(BuildInfo.Name, "You must unlock the Warehouse Expansion first.", PopupType.Normal);
                            // Reset the QoLmod settings.
                            if (_qolGroupAddedPopup || _qolAllPartsPopup)
                            {
                                dynamic settingsValue = _qolSettings.GetValue(null);
                                settingsValue.Value.showPopupforGroupAddedInventory = _qolGroupAddedPopup;
                                settingsValue.Value.showPopupforAllPartsinGroup = _qolAllPartsPopup;
                            }
                        }
                    }
                    // Check that the user is in the Barn or Junkyard.
                    else if (_currentScene.Equals("barn") ||
                             _currentScene.Equals("junkyard"))
                    {
                        // Do work on the Junk and TempInventory.
                        MoveBarnOrJunkyardItems();
                    }
                }
            }
            // Check if the user pressed the Key in Settings.
            if (Input.GetKeyDown(_configFile.TransferEntireJunkyardOrBarn))
            {
                // Check that the user is in the Barn or Junkyard.
                if (_currentScene.Equals("barn") ||
                    _currentScene.Equals("junkyard"))
                {
                    // Disable QolMod settings temporarily.
                    if (_qolGroupAddedPopup || _qolAllPartsPopup)
                    {
                        dynamic settingsValue = _qolSettings.GetValue(null);
                        settingsValue.Value.showPopupforGroupAddedInventory = false;
                        settingsValue.Value.showPopupforAllPartsinGroup = false;
                    }

                    // Do work on the Junk and TempInventory.
                    MoveEntireBarnOrJunkyard();
                }
                else
                {
                    // The user is not at the Barn or Junkyard, so show them a message.
                    UIManager.Get().ShowPopup(BuildInfo.Name, "This function only works at the Barn or Junkyard", PopupType.Normal);
                }
            }

        }

        /// <summary>
        /// Debug method to show the current scene.
        /// </summary>
        private void ShowSceneName()
        {
            UIManager.Get().ShowPopup(BuildInfo.Name, $"Scene: {_currentScene}", PopupType.Normal);
        }
        /// <summary>
        /// Debug method to show the current open window's name.
        /// </summary>
        private void ShowWindowName()
        {
            var windowManager = WindowManager.Instance;
            if (windowManager != null)
            {
                if (windowManager.activeWindows.Count > 0)
                {
                    UIManager.Get().ShowPopup(BuildInfo.Name, $"Window: {windowManager.GetLastOpenedWindow().name}", PopupType.Normal);
                }
            }
        }
        /// <summary>
        /// Debug method to show the current selected category.
        /// </summary>
        private void ShowCurrentCategory()
        {
            string categoryName = string.Empty;
            var windowManager = WindowManager.Instance;
            if (windowManager != null)
            {
                if (windowManager.activeWindows.count > 0)
                {
                    if (windowManager.IsWindowActive(WindowID.Warehouse))
                    {
                        var warehouseWindow = windowManager.GetWindowByID<WarehouseWindow>(WindowID.Warehouse);
                        if (warehouseWindow.currentTab == 0)
                        {
                            var inventoryTab = warehouseWindow.warehouseInventoryTab;
                            categoryName = inventoryTab.currentCategory.ToString();
                        }
                        else
                        {
                            var warehouseTab = warehouseWindow.warehouseTab;
                            categoryName = warehouseTab.currentCategory.ToString();
                        }
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                UIManager.Get().ShowPopup(BuildInfo.Name, $"Selected Category: {categoryName}", PopupType.Normal);
            }
        }
        /// <summary>
        /// This mod only works if the Warehouse has been unlocked
        /// as an Expansion, so check if it has been unlocked.
        /// </summary>
        /// <returns>(bool) True if the Warehouse Expansion has been unlocked.</returns>
        private bool CheckIfWarehouseIsUnlocked()
        {
            var garageLevelManager = Singleton<GarageLevelManager>.Instance;
            // The GarageLevelManager seems to be the easiest way to find out.
            if (garageLevelManager != null)
            {
                // Get a reference to the Garage and Tools Tab of the Toolbox.
                var garageAndToolsTab = garageLevelManager.garageAndToolsTab;
                // The upgradeItems list isn't populated until the Tab becomes active,
                // This fakes the manager into populating the list.
                garageAndToolsTab.PrepareItems();
                // The Warehouse Expansion is item 8 (index 7) in the list.
                var warehouseUpgrade = garageAndToolsTab.upgradeItems[7];
                // Debug to show the status.
                //UIManager.Get().ShowPopup(BuildInfo.Name, $"Warehouse Unlocked: {warehouseUpgrade.IsUnlocked}", PopupType.Normal);
                return warehouseUpgrade.IsUnlocked;
            }

            // Default to failing.
            return false;
        }

        /// <summary>
        /// If the user tries to use the mod in the wrong place,
        /// show a Tutorial message to explain how to use it correctly.
        /// </summary>
        /// <remarks>
        /// (I hate when you press a button and nothing happens and
        /// you don't know if the code failed or you used it wrong).
        /// </remarks>
        /// <param name="isWarehouse">(True) if the user is at the Garage.</param>
        private void ShowTutorialMessage(bool isWarehouse)
        {
            // Create a message depending on the location.
            string message;
            if (isWarehouse)
            {
                message = "Please open the Warehouse first.";
            }
            else
            {
                message = "Please open a junk pile first.";
            }

            var uiManager = UIManager.Get();
            if (!uiManager.PopupManager.PopupTutorial.isActiveAndEnabled)
            {
                // Show the Tutorial.
                uiManager.ShowPopup(BuildInfo.Name, message, PopupType.Tutorial);
                // Keep track of our Tutorial in case another one gets activated.
                _myTutorialEnabled = true;
            }
            else
            {
                // Only close our Tutorial.
                if (!_myTutorialEnabled)
                {
                    uiManager.ShowPopup(BuildInfo.Name, message, PopupType.Normal);
                }
            }
        }

        /// <summary>
        /// Method to move Inventory items to a Warehouse (called from MoveInventoryOrWarehouseItems).
        /// </summary>
        /// <param name="inventory">The user's Inventory.</param>
        /// <param name="warehouse">The Garage Warehouse.</param>
        /// <returns>(ItemCount, GroupCount) Tuple with the number of items/groups that were moved.</returns>
        private (int items, int groupItems) MoveInventoryItems(Inventory inventory, Warehouse warehouse)
        {
            // Get the number of items in the user's Inventory.
            int invItemCount = inventory.items.Count;
            // Use the standard For loop instead of ForEach
            // so the list isn't edited during the operation.
            for (int i = 0; i < invItemCount; i++)
            {
                // Always get a reference to the first item in the list.
                var invItem = inventory.items[0];
                // Add the item to the current Warehouse.
                warehouse.Add(invItem);
                // Remove the item from the user's Inventory.
                inventory.items.RemoveAt(0);
            }
            // The user's Item Inventory should now be empty.
            // If not, show the user a message.
            if (inventory.items.Count > 0)
            {
                UIManager.Get().ShowPopup(BuildInfo.Name, "Failed to move items from Inventory", PopupType.Normal);
            }

            // Get the number of groups in the user's Inventory.
            int invGroupCount = inventory.groups.Count;
            // Use the standard For loop instead of ForEach,
            // so the list isn't edited during the operation.
            for (int i = 0; i < invGroupCount; i++)
            {
                // Always get a reference to the first group in the list.
                var invGroupItem = inventory.groups[0];
                // Add the group to the current Warehouse.
                warehouse.Add(invGroupItem);
                // Remove the group from the user's Inventory.
                inventory.groups.RemoveAt(0);
            }
            // The user's Group Inventory should now be empty.
            // If not, show the user a message.
            if (inventory.groups.Count > 0)
            {
                UIManager.Get().ShowPopup(BuildInfo.Name, "Failed to move groups from Inventory", PopupType.Normal);
            }

            // Return the number of Items and Groups that were moved.
            return (invItemCount, invGroupCount);
        }
        /// <summary>
        /// Method to move Warehouse items to the user's Inventory (called from MoveInventoryOrWarehouseItems).
        /// </summary>
        /// <param name="inventory">The user's Inventory.</param>
        /// <param name="warehouse">The Garage Warehouse.</param>
        /// <returns>(ItemCount, GroupCount) Tuple with the number of items/groups that were moved.</returns>
        private (int items, int groupItems) MoveWarehouseItems(Inventory inventory, Warehouse warehouse)
        {
            // There is no easy way to get individual item and group lists,
            // so get all the items and then separate them later.
            var warehouseItems = warehouse.GetAllItemsAndGroups();
            // Get the nummber of total items in the Warehouse.
            int wareCount = warehouseItems.Count;
            // Setup temporary counts to return at the end.
            int wareItemCount = 0;
            int wareGroupCount = 0;
            // Use the standard For loop instead of ForEach,
            // so the list isn't edited during the operation.
            for (int i = 0; i < wareCount; i++)
            {
                // Always get a reference to the first BaseItem in the list.
                var baseItem = warehouseItems[0];
                // Try to cast the BaseItem to an Item.
                if (baseItem.TryCast<Item>() != null)
                {
                    // The BaseItem is an Item, so add it to the user's Inventory.
                    inventory.Add(baseItem.TryCast<Item>());
                    // Delete the Item from the Warehouse.
                    warehouse.Delete(baseItem.TryCast<Item>());
                    // Remove the BaseItem from the temporary list.
                    warehouseItems.RemoveAt(0);
                    // Increment the temporary count of items.
                    wareItemCount++;
                }
                // Try to cast the BaseItem to a GroupItem.
                if (baseItem.TryCast<GroupItem>() != null)
                {
                    // The BaseItem is a GroupItem, so add it to the user's Inventory.
                    inventory.AddGroup(baseItem.TryCast<GroupItem>());
                    // Delete the GroupItem from the Warehouse.
                    warehouse.Delete(baseItem.TryCast<GroupItem>());
                    // Remove the BaseItem from the temporary list.
                    warehouseItems.RemoveAt(0);
                    // Increment the temporary count of groups.
                    wareGroupCount++;
                }
            }
            // The Warehouse should now be empty.
            // If not, show the user a message.
            if (warehouse.GetAllItemsAndGroups().Count > 0)
            {
                UIManager.Get().ShowPopup(BuildInfo.Name, "Failed to move items from Warehouse", PopupType.Normal);
            }

            // Return the number of Items and Groups that were moved.
            return (wareItemCount, wareGroupCount);
        }
        /// <summary>
        /// Method to move items and groups between the user's Inventory and Garage Warehouse.
        /// </summary>
        private void MoveInventoryOrWarehouseItems()
        {
            var inventory = Singleton<Inventory>.Instance;
            var warehouse = Singleton<Warehouse>.Instance;
            var windowManager = WindowManager.Instance;
            var uiManager = UIManager.Get();

            // Check that the Warehouse Window is displayed.
            if (windowManager.activeWindows.count > 0 &&
                windowManager.IsWindowActive(WindowID.Warehouse))
            {
                // Get a reference to the Warehouse Window.
                var warehouseWindow = windowManager.GetWindowByID<WarehouseWindow>(WindowID.Warehouse);
                if (warehouseWindow != null)
                {
                    // Check which Tab is currently being displayed.
                    if (warehouseWindow.currentTab == 0)
                    {
                        // This is the Inventory Tab
                        if (inventory.GetAllItemsAndGroups().Count > 0)
                        {
                            // Check if the user's Inventory has more items/groups than the user has configured in Settings.
                            if (inventory.GetAllItemsAndGroups().Count >= _configFile.MinNumOfItemsWarning)
                            {
                                // Ask the user to confirm the move because there are a lot of items/groups.
                                System.Action<bool> confirmMove = new System.Action<bool>(response =>
                                {
                                    if (response)
                                    {
                                        (var tempItems, var tempGroups) = MoveInventoryItems(inventory, warehouse);
                                        // Show the user the number of items and groups that were moved.
                                        uiManager.ShowPopup(BuildInfo.Name, $"Items Moved From Inventory: {tempItems}", PopupType.Normal);
                                        uiManager.ShowPopup(BuildInfo.Name, $"Groups Moved From Inventory: {tempGroups}", PopupType.Normal);

                                        // Refresh the Warehouse Inventory Tab.
                                        warehouseWindow.warehouseInventoryTab.Refresh();
                                    }
                                });
                                uiManager.ShowAskWindow("Move Items", $"Move your entire inventory to {warehouse.GetCurrentSelectedWarehouseName()}?", confirmMove);
                            }
                            else
                            {
                                // The number of items is below the settings, so move the items/groups.
                                (var tempItems, var tempGroups) = MoveInventoryItems(inventory, warehouse);
                                // Show the user the number of items and groups that were moved.
                                uiManager.ShowPopup(BuildInfo.Name, $"Items Moved From Inventory: {tempItems}", PopupType.Normal);
                                uiManager.ShowPopup(BuildInfo.Name, $"Groups Moved From Inventory: {tempGroups}", PopupType.Normal);

                                // Refresh the Warehouse Inventory Tab.
                                warehouseWindow.warehouseInventoryTab.Refresh();
                            }
                        }
                        // The user's Inventory was empty, so show the user a message.
                        else
                        {
                            uiManager.ShowPopup(BuildInfo.Name, $"No items to move", PopupType.Normal);
                        }
                    }
                    else if (warehouseWindow.currentTab == 1)
                    {
                        // This is the Warehouse Tab
                        if (warehouse.GetAllItemsAndGroups().Count > 0)
                        {
                            // Check if the Warehouse has more items/groups than the user has configured in Settings.
                            if (warehouse.GetAllItemsAndGroups().Count >= _configFile.MinNumOfItemsWarning)
                            {
                                // Ask the user to confirm the move because there are a lot of items/groups.
                                System.Action<bool> confirmMove = new System.Action<bool>(response =>
                                {
                                    (var tempItems, var tempGroups) = MoveWarehouseItems(inventory, warehouse);
                                    // Show the user the number of items and groups that were moved.
                                    uiManager.ShowPopup(BuildInfo.Name, $"Items Moved From Warehouse: {tempItems}", PopupType.Normal);
                                    uiManager.ShowPopup(BuildInfo.Name, $"Groups Moved From Warehouse: {tempGroups}", PopupType.Normal);

                                    // Refresh the Warehouse Tab.
                                    warehouseWindow.warehouseTab.Refresh(true);
                                });
                                uiManager.ShowAskWindow("Move Items", $"Move all items in {warehouse.GetCurrentSelectedWarehouseName()} to your Inventory?", confirmMove);
                            }
                            else
                            {
                                // The number of items is below the settings, so move the items/groups.
                                (var tempItems, var tempGroups) = MoveWarehouseItems(inventory, warehouse);
                                // Show the user the number of items and groups that were moved.
                                uiManager.ShowPopup(BuildInfo.Name, $"Items Moved From Warehouse: {tempItems}", PopupType.Normal);
                                uiManager.ShowPopup(BuildInfo.Name, $"Groups Moved From Warehouse: {tempGroups}", PopupType.Normal);

                                // Refresh the Warehouse Tab
                                warehouseWindow.warehouseTab.Refresh(true);
                            }
                        }
                        // The Warehouse was empty, so show the user a message.
                        else
                        {
                            uiManager.ShowPopup(BuildInfo.Name, $"No items to move", PopupType.Normal);
                        }
                    }
                }
            }
            // The Warehouse Window is not displayed,
            // so show the user a Tutorial message.
            else
            {
                ShowTutorialMessage(isWarehouse: true);
            }

            // Reset the QoLmod settings.
            if (_qolGroupAddedPopup || _qolAllPartsPopup)
            {
                dynamic settingsValue = _qolSettings.GetValue(null);
                settingsValue.Value.showPopupforGroupAddedInventory = _qolGroupAddedPopup;
                settingsValue.Value.showPopupforAllPartsinGroup = _qolAllPartsPopup;
            }
        }

        /// <summary>
        /// Method to move items and groups between Junk Stashes and the Temp Inventory of the Barn and Junkyard.
        /// </summary>
        /// <remarks>
        /// There are never more than 30 items/groups, so we aren't checking the user setting.
        /// This might change in the future if people set it that low.
        /// There also doesn't seem to be an groups in these stashes.
        /// </remarks>
        private void MoveBarnOrJunkyardItems()
        {
            var gameManager = Singleton<GameManager>.Instance;
            var windowManager = WindowManager.Instance;
            var uiManager = UIManager.Get();

            // Check that the Junk Items Window (ItemsExchangeWindow) is displayed.
            if (windowManager.activeWindows.count > 0 &&
                windowManager.IsWindowActive(WindowID.ItemsExchange))
            {
                // Get a reference to the Junk Items Window.
                var itemsExchangeWindow = windowManager.GetWindowByID<ItemsExchangeWindow>(WindowID.ItemsExchange);
                if (itemsExchangeWindow != null)
                {
                    // Check which Tab is currently being displayed.
                    if (itemsExchangeWindow.currentTab == 0)
                    {
                        // This is the Junk (Found) Tab.
                        // Get a reference to the Junk object.
                        var junk = itemsExchangeWindow.junk;
                        // Get the list of junk items/groups.
                        var junkItems = junk.ItemsInTrash;
                        // Store the number of junk in the Stash.
                        int junkCount = junkItems.Count;
                        // Check if there is junk to move.
                        if (junkCount > 0)
                        {
                            // Create temporay lists to hold the items and group.
                            List<Item> tempItems = new List<Item>();
                            List<GroupItem> tempGroups = new List<GroupItem>();
                            // We are only storing items, so a ForEach loop works here.
                            foreach (var baseItem in junkItems)
                            {
                                // Try to cast the BaseItem to an Item.
                                if (baseItem.TryCast<Item>() != null)
                                {
                                    // The BaseItem is an Item, so add it to the correct list.
                                    tempItems.Add(baseItem.TryCast<Item>());
                                }
                                else if (baseItem.TryCast<GroupItem>() != null)
                                {
                                    // The BaseItem is a GroupItem, so add it to the correct list.
                                    tempGroups.Add(baseItem.TryCast<GroupItem>());
                                }
                            }
                            // Check that all the items and groups were added to the temporary lists.
                            if ((tempItems.Count + tempGroups.Count) == junkCount)
                            {
                                // Get a reference to the Temp Inventory.
                                var tempInventory = gameManager.TempInventory;
                                // We're using a temporary list, so ForEach loops work here.
                                // We aren't editing the temporary list, just the Junk and Temp Inventory.
                                foreach (var tempItem in tempItems)
                                {
                                    // Add the Item to the Temp Inventory (Collected Tab).
                                    tempInventory.items.Add(tempItem);
                                    // Remove the Item from the Junk Stash.
                                    junk.ItemsInTrash.Remove(tempItem);
                                }
                                // We're using a temporary list, so ForEach loops work here.
                                // We aren't editing the temporary list, just the Junk and Temp Inventory.
                                // *** The Barn and Junkyard don't seem to generate groups, ***
                                // *** but we can't take that for granted in the future. ***
                                foreach (var tempGroup in tempGroups)
                                {
                                    // Add the Group to the Temp Inventory (Collected Tab).
                                    tempInventory.items.Add(tempGroup);
                                    // Remove the Group from the Junk Stash.
                                    junk.ItemsInTrash.Remove(tempGroup);
                                }

                                // Add the temporary lists to the Global Dictionaries.
                                // This allows the user to "undo" the moves to each Junk Stash.
                                _tempItems.Add(junk.Pointer, tempItems);
                                _tempGroups.Add(junk.Pointer, tempGroups);

                                // Show the user the number of items and groups that were moved.
                                uiManager.ShowPopup(BuildInfo.Name, $"Items Moved From Junk: {tempItems.Count}", PopupType.Normal);
                                uiManager.ShowPopup(BuildInfo.Name, $"Groups Moved From Junk: {tempGroups.Count}", PopupType.Normal);
                            }
                            // Something went wrong with the temporary moves,
                            // so show the user a message.
                            else
                            {
                                uiManager.ShowPopup(BuildInfo.Name, $"Failed to move items", PopupType.Normal);
                            }

                            // Refresh the Items Exchange Tab
                            itemsExchangeWindow.foundTab.Refresh(true);
                        }
                        // The junk was empty, so show the user a message.
                        else
                        {
                            uiManager.ShowPopup(BuildInfo.Name, $"No items to move", PopupType.Normal);
                        }
                    }
                    else if (itemsExchangeWindow.currentTab == 1)
                    {
                        // This is the Temp Inventory (Collected) Tab
                        // Get a reference to the Temp Inventory.
                        var tempInventory = gameManager.TempInventory;
                        // Get a reference to the Junk object.
                        var junk = itemsExchangeWindow.junk;

                        // Get the list of temporary items from the Global Dictionary.
                        // We will use these lists to move the items back to the individual Junk Stashes.
                        _tempItems.TryGetValue(junk.Pointer, out List<Item> tempItems);
                        // Check if there are items to move.
                        if (tempItems.Count > 0)
                        {
                            // Loop through the temporary list and add the items to the Junk Stash.
                            // A ForEach loop probably would have worked here, also.
                            for (int i = 0; tempItems.Count > i; i++)
                            {
                                junk.ItemsInTrash.Add(tempItems[i]);
                            }
                        }
                        // There were no items to move, so show the user a message.
                        else
                        {
                            uiManager.ShowPopup(BuildInfo.Name, "No items to move", PopupType.Normal);
                        }
                        // Get the list of temporary groups from the Global Dictionary.
                        // We will use these lists to move the groups back to the individual Junk Stashes.
                        _tempGroups.TryGetValue(junk.Pointer, out List<GroupItem> tempGroups);
                        // Check if there are groups to move.
                        if (tempGroups.Count > 0)
                        {
                            // Loop through the temporary list and add the groups to the Junk Stash.
                            // A ForEach loop probably would have worked here, also.
                            for (int i = 0; i < tempGroups.Count; i++)
                            {
                                junk.ItemsInTrash.Add(tempGroups[i]);
                            }
                        }
                        // There were no groups to move, so show the user a message.
                        else
                        {
                            uiManager.ShowPopup(BuildInfo.Name, "No groups to move", PopupType.Normal);
                        }
                        // Check that all the items and groups were added to the Junk Stash.
                        if ((tempItems.Count + tempGroups.Count) == junk.ItemsInTrash.Count)
                        {
                            // Loop through the temporary list and remove the items from the Temp Inventory.
                            // A ForEach loop probably would have worked here, also.
                            for (int i = 0; tempItems.Count > i; i++)
                            {
                                tempInventory.RemoveItem(tempItems[i]);
                            }
                            // Loop through the temporary list and remove the groups from the Temp Inventory.
                            // A ForEach loop probably would have worked here, also.
                            for (int i = 0; i < tempGroups.Count; i++)
                            {
                                tempInventory.RemoveItem(tempGroups[i]);
                            }
                            // Show the user the number of items and groups that were moved.
                            uiManager.ShowPopup(BuildInfo.Name, $"Items Moved From Junk: {tempItems.Count}", PopupType.Normal);
                            uiManager.ShowPopup(BuildInfo.Name, $"Groups Moved From Junk: {tempGroups.Count}", PopupType.Normal);

                            // Remove the temporary lists from the Global Dictionaries.
                            _tempItems.Remove(junk.Pointer);
                            _tempGroups.Remove(junk.Pointer);
                        }
                        // Something went wrong with the moves,
                        // so show the user a message.
                        else
                        {
                            uiManager.ShowPopup(BuildInfo.Name, "Failed to move items", PopupType.Normal);
                        }
                        // Clear the temporary lists for the next Junk Stash.
                        tempItems.Clear();
                        tempGroups.Clear();

                        // Refresh the Items Exchange Tab
                        itemsExchangeWindow.collectedTab.Refresh(true);
                    }
                }
            }
            // The Junk Items Window is not displayed,
            // so show the user a Tutorial message.
            else
            {
                ShowTutorialMessage(isWarehouse: false);
            }

            // Reset the QoLmod settings.
            if (_qolGroupAddedPopup || _qolAllPartsPopup)
            {
                dynamic settingsValue = _qolSettings.GetValue(null);
                settingsValue.Value.showPopupforGroupAddedInventory = _qolGroupAddedPopup;
                settingsValue.Value.showPopupforAllPartsinGroup = _qolAllPartsPopup;
            }
        }
        /// <summary>
        /// Method to move all items and groups from all the Junk Stashes to the Temp Inventory of the Barn or Junkyard.
        /// </summary>
        private void MoveEntireBarnOrJunkyard()
        {
            var gameManager = Singleton<GameManager>.Instance;
            var uiManager = UIManager.Get();

            // Get a reference to the Temp Inventory.
            var tempInv = gameManager.TempInventory;
            // Get a reference to all the objects of type Junk.
            var junks = UnityEngine.Object.FindObjectsOfType<Junk>();
            // Setup a temporary count to show the user at the end.
            int junkCount = 0;
            // Use this bool to verify that all the Junk was moved from all the Stashes.
            bool junkEmptied = true;
            // Loop through each Junk object and move the items and groups.
            // We are only getting the Junk lists, so a ForEach loop works here.
            foreach (var junk in junks)
            {
                // Get the list of junk in the Stash.
                var trashItems = junk.ItemsInTrash;
                // Store the number of junk in the Stash.
                int trashCount = trashItems.Count;
                // Use the standard For loop instead of ForEach,
                // so the list isn't edited during the operation.
                for (int i = 0; i < trashCount; i++)
                {
                    // Always get a reference to the first BaseItem in the list.
                    var junkItem = trashItems[0];
                    // Add the BaseItem to the Temp Inventory.
                    tempInv.AddItem(junkItem);
                    // Remove the first Item from the Junk Stash.
                    trashItems.RemoveAt(0);
                    // Increment the temporary count of items.
                    junkCount++;
                }
                // The Stash should now be empty.
                // If not, update the bool to fail at the end.
                if (trashItems.Count > 0)
                {
                    junkEmptied = false;
                }
            }
            // One of the Stashes was not emptied, so show the user a message.
            if (!junkEmptied)
            {
                uiManager.ShowPopup(BuildInfo.Name, "Failed to empty a junk pile.", PopupType.Normal);
            }
            // Show the user the number of items and groups that were moved.
            if (junkCount > 0)
            {
                uiManager.ShowPopup(BuildInfo.Name, $"Junk items moved: {junkCount}.", PopupType.Normal);
            }

            // Reset the QoLmod settings.
            if (_qolGroupAddedPopup || _qolAllPartsPopup)
            {
                dynamic settingsValue = _qolSettings.GetValue(null);
                settingsValue.Value.showPopupforGroupAddedInventory = _qolGroupAddedPopup;
                settingsValue.Value.showPopupforAllPartsinGroup = _qolAllPartsPopup;
            }
        }
    }
}
