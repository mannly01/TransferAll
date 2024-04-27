using CMS.Helpers;
using CMS.UI;
using CMS.UI.Logic;
using CMS.UI.Windows;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

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
        public const string Version = "1.4.2";
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
        /// User setting for the key to transfer the Inventory/Warehouse/Junk items.
        /// </summary>
        public KeyCode TransferAllItemsAndGroups => _transferAllBindKey.Value;
        private readonly MelonPreferences_Entry<KeyCode> _transferAllBindKey;
        /// <summary>
        /// User setting for the key to transfer all the junk from the Barn or Junkyard.
        /// </summary>
        public KeyCode TransferEntireJunkyardOrBarn => _transferEntireBindKey.Value;
        private readonly MelonPreferences_Entry<KeyCode> _transferEntireBindKey;
        /// <summary>
        /// User setting for the key to set the condition of parts percentage lower by 10%.
        /// </summary>
        public KeyCode SetPartConditionLower => _setPartConditionLower.Value;
        private readonly MelonPreferences_Entry<KeyCode> _setPartConditionLower;
        /// <summary>
        /// User setting for the key to set the condition of parts percentage higher by 10%.
        /// </summary>
        public KeyCode SetPartConditionHigher => _setPartConditionHigher.Value;
        private readonly MelonPreferences_Entry<KeyCode> _setPartConditionHigher;
        /// <summary>
        /// User setting for the minimum part condition to transfer.
        /// </summary>
        public int MinPartCondition
        {
            get => _minPartCondition.Value;
            set { _minPartCondition.Value = value; }
        }
        private readonly MelonPreferences_Entry<int> _minPartCondition;
        /// <summary>
        /// User setting for the minimum number of items to show a warning message before transfer.
        /// </summary>
        public int MinNumOfItemsWarning => _minNumOfItemsWarning.Value;
        private readonly MelonPreferences_Entry<int> _minNumOfItemsWarning;
        /// <summary>
        /// User setting to select whether the TransferAll key uses the current category or not.
        /// </summary>
        public bool TransferByCategory => _transferByCategory.Value;
        private readonly MelonPreferences_Entry<bool> _transferByCategory;
        /// <summary>
        /// User setting to select whether the TransferEntire key only moves parts and not body parts.
        /// </summary>
        public bool TransferPartsOnlyAtBarnOrJunkyard => _transferPartsOnlyAtBarnOrJunkyard.Value;
        private readonly MelonPreferences_Entry<bool> _transferPartsOnlyAtBarnOrJunkyard;
        /// <summary>
        /// User setting to select whether the TransferEntire key only moves maps or cases and nothing else.
        /// </summary>
        public bool TransferMapsOrCasesOnlyAtBarnOrJunkyard => _transferMapsOrCasesOnlyAtBarnOrJunkyard.Value;
        private readonly MelonPreferences_Entry<bool> _transferMapsOrCasesOnlyAtBarnOrJunkyard;

        /// <summary>
        /// Implementation of Settings properties.
        /// </summary>
        public ConfigFile()
        {
#if DEBUG
            LogService.Instance.WriteToLog($"Called", "ConfigFile.Init");
#endif
            _settings = MelonPreferences.CreateCategory(SettingsCatName);
            _settings.SetFilePath("Mods/TransferAll.cfg");
            _transferAllBindKey = _settings.CreateEntry(nameof(TransferAllItemsAndGroups), KeyCode.K, 
                description: "Press This Key to transfer all current items and groups.");
            _transferEntireBindKey = _settings.CreateEntry(nameof(TransferEntireJunkyardOrBarn), KeyCode.L,
                description: "ONLY WORKS AT BARN AND JUNKYARD." + Il2CppSystem.Environment.NewLine + "Press This Key to transfer all items and groups from ALL junk stashes.");
            _setPartConditionLower = _settings.CreateEntry(nameof(SetPartConditionLower), KeyCode.Minus,
                description: "Press This Key to set the condition percentage lower by 10%." + Il2CppSystem.Environment.NewLine + "This value is saved to MinPartCondition value below on exit.");
            _setPartConditionHigher = _settings.CreateEntry(nameof(SetPartConditionHigher), KeyCode.Equals,
                description: "Press This Key to set the condition percentage higher by 10%." + Il2CppSystem.Environment.NewLine + "This value is saved to MinPartCondition value below on exit.");
            _minPartCondition = _settings.CreateEntry(nameof(MinPartCondition), 0,
                description: "This value will transfer parts with a condition equal to and above this number. SET TO 0 TO TRANSFER EVERYTHING.");
            _minNumOfItemsWarning = _settings.CreateEntry(nameof(MinNumOfItemsWarning), 500,
                description: "This will display a warning if the items/groups are above this number (to prevent large moves by accident).");
            _transferByCategory = _settings.CreateEntry(nameof(TransferByCategory), true,
                description: "Set this to false and the TransferAllItemsAndGroups key will move all items regardless of the selected category (engine, suspension, etc.).");
            _transferPartsOnlyAtBarnOrJunkyard = _settings.CreateEntry(nameof(TransferPartsOnlyAtBarnOrJunkyard), false,
                description: "Set this to true and only non-body parts are moved to the Shopping Cart in the Barn/Junkyard.");
            _transferMapsOrCasesOnlyAtBarnOrJunkyard = _settings.CreateEntry(nameof(TransferMapsOrCasesOnlyAtBarnOrJunkyard), false,
                description: "Set this to true and only maps or cases are moved to the Shopping Cart in the Barn/Junkyard." + Il2CppSystem.Environment.NewLine + "THIS OVERRIDES THE TransferPartsOnlyAtBarnOrJunkyard SETTING ABOVE.");

            // Remove the old configuration if it's still there.
            _settings.DeleteEntry("TransferAllItemsAndGroupsLeftControlPlus");
            _settings.DeleteEntry("TransferEntireJunkyardOrBarnLeftAltPlus");
#if DEBUG
            // Logging for debug purposes.
            LogService.Instance.WriteToLog($"TransferAllItemsAndGroups: {TransferAllItemsAndGroups}", "ConfigFile.Init");
            LogService.Instance.WriteToLog($"TransferEntireJunkyardOrBarn: {TransferEntireJunkyardOrBarn}", "ConfigFile.Init");
            LogService.Instance.WriteToLog($"SetPartConditionLower: {SetPartConditionLower}", "ConfigFile.Init");
            LogService.Instance.WriteToLog($"SetPartConditionHigher: {SetPartConditionHigher}", "ConfigFile.Init");
            LogService.Instance.WriteToLog($"MinPartCondition: {MinPartCondition}", "ConfigFile.Init");
            LogService.Instance.WriteToLog($"MinNumOfItemsWarning: {MinNumOfItemsWarning}", "ConfigFile.Init");
            LogService.Instance.WriteToLog($"TransferByCategory: {TransferByCategory}", "ConfigFile.Init");
            LogService.Instance.WriteToLog($"TransferPartsOnlyAtBarnOrJunkyard: {TransferPartsOnlyAtBarnOrJunkyard}", "ConfigFile.Init");
            LogService.Instance.WriteToLog($"TransferMapsOrCasesOnlyAtBarnOrJunkyard: {TransferMapsOrCasesOnlyAtBarnOrJunkyard}", "ConfigFile.Init");
#endif
        }

        /// <summary>
        /// Method to change the MinPartCondition setting lower by 10%.
        /// </summary>
        public void SetPartConditionLowerBy10()
        {
            MinPartCondition -= 10;
            if (MinPartCondition < 0)
            {
                MinPartCondition = 0;
            }
#if DEBUG
            LogService.Instance.WriteToLog($"Called");
#endif
        }
        /// <summary>
        /// Method to change the MinPartCondition setting higher by 10%.
        /// </summary>
        public void SetPartConditionHigherBy10()
        {
            MinPartCondition += 10;
            if (MinPartCondition > 100)
            {
                MinPartCondition = 100;
            }
#if DEBUG
            LogService.Instance.WriteToLog($"Called");
#endif
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
        /// Global reference to verify if the Warehouse Expansion is unlocked.
        /// </summary>
        private bool? _warehouseUnlocked = null;

        private string _qolPath = $"{Directory.GetCurrentDirectory()}\\Mods\\QoLmod.dll";
        /// <summary>
        /// QoLmod shows a popup for every part inside a group as its moved and
        /// this causes some serious performance issues during bulk moves,
        /// so these references allow these settings to be disabled temporarily.
        /// </summary>
        private FieldInfo _qolSettings = null;
        private bool _qolGroupAddedPopup = false;
        private bool _qolAllPartsPopup = false;

        /// <summary>
        /// Global reference used to temporarily hold the individual junk stashes in the Barn and Junkyard.
        /// This allows the individual stashes to be "undone" and transferred back.
        /// </summary>
        private Dictionary<IntPtr, List<Item>> _tempItems = new Dictionary<IntPtr, List<Item>>();
        /// <summary>
        /// Global reference used to temporarily hold the individual junk stashes in the Barn and Junkyard.
        /// This allows the individual stashes to be "undone" and transferred back.
        /// </summary>
        private Dictionary<IntPtr, List<GroupItem>> _tempGroups = new Dictionary<IntPtr, List<GroupItem>>();

        public override void OnInitializeMelon()
        {
            // Tell the user a log file was created.
            MelonLogger.Msg("Creating Log File...");
            LogService.Instance.Initialize();
            // Tell the user that we're loading the Settings.
            MelonLogger.Msg("Loading Settings...");
            _configFile = new ConfigFile();
        }

        public override void OnLateInitializeMelon()
        {
#if DEBUG
            LogService.Instance.WriteToLog($"Called");
#endif
            // Specifically for the QoLmod effect explained above.
            // Check that the QoLmod.dll is installed.
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
                        // Log that the QoLmod was found and the current values of the two settings.
                        // This allows us to restore these settings after doing the bulk moves.
                        LogService.Instance.WriteToLog($"QoLmod Found, showPopupforGroupAddedInventory: {_qolGroupAddedPopup}");
                        LogService.Instance.WriteToLog($"QoLmod Found, showPopupforAllPartsinGroup: {_qolAllPartsPopup}");
                    }
                    else
                    {
                        // The number of Melons has changed and loading failed.
                        LogService.Instance.WriteToLog("Number of Melons in QoLmod has changed");
                    }
                }
                else
                {
                    // The QoLmod was found, but it can't be loaded.
                    LogService.Instance.WriteToLog("QoLmod not loaded");
                }
            }
            else
            {
                // Log that the QoLmod was not found.
                LogService.Instance.WriteToLog("QoLmod not found");
            }
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            // Save a reference to the current scene.
            _currentScene = sceneName.ToLower();
            // Change the scene name if a holiday is happening.
            if (_currentScene.Equals("christmas") ||
                _currentScene.Equals("easter") ||
                _currentScene.Equals("halloween"))
            {
                _currentScene = "garage";
                LogService.Instance.WriteToLog($"{sceneName} holiday is active");
            }
#if DEBUG
            if (_currentScene.Equals("garage") ||
                _currentScene.Equals("barn") ||
                _currentScene.Equals("junkyard"))
            {
                LogService.Instance.WriteToLog($"SceneName: {sceneName}");
            }
#endif
        }
        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            // Clear the temporary dictionaries when the user leaves the scene.
            if (sceneName.ToLower() == "barn" ||
                sceneName.ToLower() == "junkyard")
            {
                _tempItems.Clear();
                _tempGroups.Clear();
                LogService.Instance.WriteToLog($"Leaving {sceneName}");
            }
#if DEBUG
            if (_currentScene.Equals("garage") ||
                _currentScene.Equals("barn") ||
                _currentScene.Equals("junkyard"))
            {
                LogService.Instance.WriteToLog($"SceneName: {sceneName}");
            }
#endif
        }

        public override void OnDeinitializeMelon()
        {
#if DEBUG
            LogService.Instance.WriteToLog($"Called");
#endif
            // If this mod has an error, make sure the QoLmod settings
            // are still returned to their original values.
            ToggleQoLSettings(reset: true);
            LogService.Instance.WriteToLog($"QoLmod Found, showPopupforGroupAddedInventory: {_qolGroupAddedPopup}");
            LogService.Instance.WriteToLog($"QoLmod Found, showPopupforAllPartsinGroup: {_qolAllPartsPopup}");
            // The above lines should write the log strings to the file
            // but, just in case, verify all the log strings have been written.
            if (LogService.Instance.LogCount > 0)
            {
                LogService.Instance.WriteToLog("Mod Deinitializing with pending log strings");
            }
        }

        public override void OnUpdate()
        {
            // This is a test key that should not be shipped with a release.
#if DEBUG
            if (Input.GetKeyDown(KeyCode.J))
            {
                LogService.Instance.WriteToLog($"Debug Key (J) Pressed");
                // These are debug/test methods.
                //ShowSceneName();
                //ShowWindowName();
                //ShowCurrentCategory();
                //ShowMapsAndCases();

                // Used to debug an error in the mod.
                // This will cause a failure if the Warehouse window isn't open.
                //LogService.Instance.WriteToLog("Error induced manually");
                //var warehouseWindow = Singleton<WindowManager>.Instance.GetWindowByID<WarehouseWindow>(WindowID.Warehouse);
                //var warehouseTab = warehouseWindow.warehouseTab;
                //warehouseTab.Refresh();
            }
#endif

            // This key will set the MinPartCondition value lower by 10%
            // and then show the user the current value.
            if (Input.GetKeyDown(_configFile.SetPartConditionLower))
            {
                _configFile.SetPartConditionLowerBy10();
                UIManager.Get().ShowPopup(BuildInfo.Name, $"Transfer Part Condition: {_configFile.MinPartCondition}%", PopupType.Normal);
                LogService.Instance.WriteToLog($"Transfer Part Condition: {_configFile.MinPartCondition}%");
            }
            // This key will set the MinPartCondition value higher by 10%
            // and then show the user the current value.
            if (Input.GetKeyDown(_configFile.SetPartConditionHigher))
            {
                _configFile.SetPartConditionHigherBy10();
                UIManager.Get().ShowPopup(BuildInfo.Name, $"Transfer Part Condition: {_configFile.MinPartCondition}%", PopupType.Normal);
                LogService.Instance.WriteToLog($"Transfer Part Condition: {_configFile.MinPartCondition}%");
            }

            // Only work on these scenes.
            if (_currentScene.Equals("garage") ||
                _currentScene.Equals("barn") ||
                _currentScene.Equals("junkyard"))
            {
                // Check if the user pressed the TransferAllItemsAndGroups Key in Settings.
                if (Input.GetKeyDown(_configFile.TransferAllItemsAndGroups))
                {
#if DEBUG
                    LogService.Instance.WriteToLog($"TransferAllItemsAndGroups ({_configFile.TransferAllItemsAndGroups}) key pressed");
#endif
                    // Check if the user is currently using the Search box.
                    if (!CheckIfInputIsFocused())
                    {
                        // Check that the user is in the Garage.
                        if (_currentScene.Equals("garage"))
                        {
                            // Check if the Warehouse is unlocked first.
                            // If the warehouse hasn't been checked, do that first.
                            if (_warehouseUnlocked == null)
                            {
                                _warehouseUnlocked = CheckIfWarehouseIsUnlocked();
                            }
                            if (_warehouseUnlocked == true)
                            {
                                // Do work on the Inventory or Warehouse.
                                MoveInventoryOrWarehouseItems();
                            }
                            else
                            {
                                // The user hasn't upgraded their Garage, so show them a message.
                                UIManager.Get().ShowPopup(BuildInfo.Name, "You must unlock the Warehouse Expansion first.", PopupType.Normal);
                                LogService.Instance.WriteToLog("Warehouse has not been unlocked");
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
                    else
                    {
                        LogService.Instance.WriteToLog("Search box has focus");
                    }
                }
                // Check if the user pressed the TransferEntireJunkyardOrBarn Key in Settings.
                if (Input.GetKeyDown(_configFile.TransferEntireJunkyardOrBarn))
                {
#if DEBUG
                    LogService.Instance.WriteToLog($"TransferEntireJunkyardOrBarn ({_configFile.TransferEntireJunkyardOrBarn}) key pressed");
#endif
                    // Check if the user is currently using the Seach box.
                    if (!CheckIfInputIsFocused())
                    {
                        // Check that the user is in the Barn or Junkyard.
                        if (_currentScene.Equals("barn") ||
                            _currentScene.Equals("junkyard"))
                        {
                            // Do work on the Junk and TempInventory.
                            MoveEntireBarnOrJunkyard();
                        }
                        else
                        {
                            // The user is not at the Barn or Junkyard, so show them a message.
                            UIManager.Get().ShowPopup(BuildInfo.Name, "This function only works at the Barn or Junkyard", PopupType.Normal);
                            LogService.Instance.WriteToLog("TransferEntireJunkyardOrBarn key pressed outside of Barn/Junkyard");
                        }
                    }
                    else
                    {
                        LogService.Instance.WriteToLog("Search box has focus");
                    }
                }
            }
        }

#if DEBUG
        /// <summary>
        /// Debug method to show the current scene.
        /// </summary>
        private void ShowSceneName()
        {
            UIManager.Get().ShowPopup(BuildInfo.Name, $"Scene: {_currentScene}", PopupType.Normal);
            LogService.Instance.WriteToLog($"Scene: {_currentScene}");
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
                    LogService.Instance.WriteToLog($"Window: {windowManager.GetLastOpenedWindow().name}");
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
                LogService.Instance.WriteToLog($"Selected Category: {categoryName}");
            }
        }
        /// <summary>
        /// Debug method to show if maps or cases are in any of the inventories.
        /// </summary>
        private void ShowMapsAndCases()
        {
            int mapCount = 0;
            int caseCount = 0;

            if (_currentScene.Equals("garage"))
            {
                var inventory = Singleton<Inventory>.Instance;
                var invItems = inventory.GetAllItemsAndGroups();
                if (invItems != null)
                {
                    foreach (var item in invItems)
                    {
                        if (item.ID.ToLower().Contains("specialmap"))
                        {
                            mapCount++;
                        }
                        else if (item.ID.ToLower().Contains("specialcase"))
                        {
                            caseCount++;
                        }
                    }
                    if (mapCount > 0 || caseCount > 0)
                    {
                        LogService.Instance.WriteToLog($"Inventory Maps: {mapCount} Cases: {caseCount}");
                        mapCount = 0;
                        caseCount = 0;
                    }
                }

                var warehouse = Singleton<Warehouse>.Instance;
                var wareItems = warehouse.GetAllItemsAndGroups();
                if (wareItems != null)
                {
                    foreach (var item in wareItems)
                    {
                        if (item.ID.ToLower().Contains("specialmap"))
                        {
                            mapCount++;
                        }
                        else if (item.ID.ToLower().Contains("specialcase"))
                        {
                            caseCount++;
                        }
                    }
                    if (mapCount > 0 || caseCount > 0)
                    {
                        LogService.Instance.WriteToLog($"Warehouse Maps: {mapCount} Cases: {caseCount}");
                        mapCount = 0;
                        caseCount = 0;
                    }
                }
            }

            if (_currentScene.Equals("barn") ||
                _currentScene.Equals("junkyard"))
            {
                var junks = UnityEngine.Object.FindObjectsOfType<Junk>();
                if (junks != null)
                {
                    foreach(var junk in junks)
                    {
                        var junkItems = junk.ItemsInTrash;
                        if (junkItems != null)
                        {
                            foreach (var item in junkItems)
                            {
                                if (item.ID.ToLower().Contains("specialmap"))
                                {
                                    mapCount++;
                                }
                                else if (item.ID.ToLower().Contains("specialcase"))
                                {
                                    caseCount++;
                                }
                            }
                        }
                    }
                    if (mapCount > 0 || caseCount > 0)
                    {
                        LogService.Instance.WriteToLog($"Stash Maps: {mapCount} Cases: {caseCount}");
                        mapCount = 0;
                        caseCount = 0;
                    }
                }

                var gameManager = Singleton<GameManager>.Instance;
                if (gameManager != null)
                {
                    var tempInv = gameManager.TempInventory;
                    if (tempInv != null)
                    {
                        var items = tempInv.items;
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                if (item.ID.ToLower().Contains("specialmap"))
                                {
                                    mapCount++;
                                }
                                else if (item.ID.ToLower().Contains("specialcase"))
                                {
                                    caseCount++;
                                }
                            }
                            if (mapCount > 0 || caseCount > 0)
                            {
                                LogService.Instance.WriteToLog($"Shopping Cart Maps: {mapCount} Cases: {caseCount}");
                            }
                        }
                    }
                }
            }
        }

#endif
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
                // this fakes the manager into populating the list.
                garageAndToolsTab.PrepareItems();
                // The Warehouse Expansion is item 8 (index 7) in the list.
                var warehouseUpgrade = garageAndToolsTab.upgradeItems[7];
#if DEBUG
                // Debug to show the status.
                LogService.Instance.WriteToLog($"Warehouse Unlocked: {warehouseUpgrade.IsUnlocked}");
#endif
                return warehouseUpgrade.IsUnlocked;
            }

            // Default to failing.
            return false;
        }
        /// <summary>
        /// If the user is using the Search box,
        /// the mod should do nothing.
        /// </summary>
        /// <returns>(bool) True if the Search/Input Field is being used.</returns>
        private bool CheckIfInputIsFocused()
        {
            var inputFields = UnityEngine.Object.FindObjectsOfType<InputField>();
            foreach (var inputField in inputFields)
            {
                if (inputField != null)
                {
                    if (inputField.isFocused)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Used to Toggle the QoLmod settings temporarily.
        /// </summary>
        /// <param name="reset">(True) if we are setting the values back to original.</param>
        private void ToggleQoLSettings(bool reset = false)
        {
            if (File.Exists(_qolPath))
            {
                bool tempGroupAdded = false;
                bool tempAllPartsAdded = false;
                if (reset)
                {
                    tempGroupAdded = _qolGroupAddedPopup;
                    tempAllPartsAdded = _qolAllPartsPopup;
                }

                dynamic settingsValue = _qolSettings.GetValue(null);
                settingsValue.Value.showPopupforGroupAddedInventory = tempGroupAdded;
                settingsValue.Value.showPopupforAllPartsinGroup = tempAllPartsAdded;

#if DEBUG
                LogService.Instance.WriteToLog($"QoLmod Found, sPGAI: {settingsValue.Value.showPopupforGroupAddedInventory}, sPAPG: {settingsValue.Value.showPopupforAllPartsinGroup}");
#endif
            }
        }

        /// <summary>
        /// Method to move Inventory items to a Warehouse (called from MoveInventoryOrWarehouseItems).
        /// </summary>
        /// <param name="invItems">The list of items to move.</param>
        /// <param name="inventory">The user's Inventory.</param>
        /// <param name="warehouse">The Garage Warehouse.</param>
        /// <returns>(ItemCount, GroupCount) Tuple with the number of items/groups that were moved.</returns>
        private (int items, int groupItems) MoveInventoryItems(
            Il2CppSystem.Collections.Generic.List<BaseItem> invItems, 
            Inventory inventory, Warehouse warehouse)
        {
#if DEBUG
            LogService.Instance.WriteToLog($"Called");
#endif
            // Disable QoLmod settings temporarily.
            ToggleQoLSettings();

            // Setup temporary counts to return at the end.
            int invItemCount = 0;
            int invGroupCount = 0;
            // Use the standard For loop instead of ForEach
            // so the list isn't edited during the operation.
            for (int i = 0; i < invItems.Count; i++)
            {
                // Get a reference to the BaseItem in the list.
                var baseItem = invItems[i];
                // Check if the condition of the part is
                // greater than or equal to the user setting.
                if ((baseItem.GetCondition() * 100) >= _configFile.MinPartCondition)
                {
                    // Try to cast the BaseItem to an Item.
                    if (baseItem.TryCast<Item>() != null)
                    {
                        // The BaseItem is an Item, so add it to the current Warehouse.
                        warehouse.Add(baseItem.TryCast<Item>());
                        // Delete the Item from the Inventory.
                        inventory.Delete(baseItem.TryCast<Item>());
                        // Increment the temporary count of items.
                        invItemCount++;
                    }
                    // Try to cast the BaseItem to a GroupItem.
                    if (baseItem.TryCast<GroupItem>() != null)
                    {
                        // The BaseItem is a GroupItem, so add it to the current Warehouse.
                        warehouse.Add(baseItem.TryCast<GroupItem>());
                        // Delete the GroupItem from the Inventory.
                        inventory.DeleteGroup(baseItem.UID);
                        // Increment the temporary count of groups.
                        invGroupCount++;
                    }
                }
            }

            // Reset the QoLmod settings.
            ToggleQoLSettings(reset: true);

            // Return the number of Items and Groups that were moved.
            return (invItemCount, invGroupCount);
        }
        /// <summary>
        /// Method to move Warehouse items to the user's Inventory (called from MoveInventoryOrWarehouseItems).
        /// </summary>
        /// <param name="warehouseItems">The list of items to move.</param>
        /// <param name="inventory">The user's Inventory.</param>
        /// <param name="warehouse">The Garage Warehouse.</param>
        /// <returns>(ItemCount, GroupCount) Tuple with the number of items/groups that were moved.</returns>
        private (int items, int groupItems) MoveWarehouseItems(
            Il2CppSystem.Collections.Generic.List<BaseItem> warehouseItems, 
            Inventory inventory, Warehouse warehouse)
        {
#if DEBUG
            LogService.Instance.WriteToLog($"Called");
#endif
            // Disable QoLmod settings temporarily.
            ToggleQoLSettings();

            // Setup temporary counts to return at the end.
            int wareItemCount = 0;
            int wareGroupCount = 0;
            // Use the standard For loop instead of ForEach,
            // so the list isn't edited during the operation.
            for (int i = 0; i < warehouseItems.Count; i++)
            {
                // Get a reference to the BaseItem in the list.
                var baseItem = warehouseItems[i];
                // Check if the condition of the part is
                // greater than or equal to the user setting.
                if ((baseItem.GetCondition() * 100) >= _configFile.MinPartCondition)
                {
                    // Try to cast the BaseItem to an Item.
                    if (baseItem.TryCast<Item>() != null)
                    {
                        // The BaseItem is an Item, so add it to the user's Inventory.
                        inventory.Add(baseItem.TryCast<Item>());
                        // Delete the Item from the Warehouse.
                        warehouse.Delete(baseItem.TryCast<Item>());
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
                        // Increment the temporary count of groups.
                        wareGroupCount++;
                    }
                }
            }

            // Reset the QoLmod settings.
            ToggleQoLSettings(reset: true);

            // Return the number of Items and Groups that were moved.
            return (wareItemCount, wareGroupCount);
        }
        /// <summary>
        /// Method to move items and groups between the user's Inventory and Garage Warehouse.
        /// </summary>
        private void MoveInventoryOrWarehouseItems()
        {
#if DEBUG
            LogService.Instance.WriteToLog($"Called");
#endif
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
#if DEBUG
                        LogService.Instance.WriteToLog($"Inventory Tab displayed");
#endif
                        // Setup a temporary List<BaseItem> to hold the items.
                        Il2CppSystem.Collections.Generic.List<BaseItem> items = new Il2CppSystem.Collections.Generic.List<BaseItem>();
                        // Check if the user has selected to move items
                        // for the current category only.
                        if (_configFile.TransferByCategory)
                        {
#if DEBUG
                            LogService.Instance.WriteToLog($"TransferByCategory enabled");
#endif
                            var currentTab = warehouseWindow.warehouseInventoryTab;
                            items = inventory.GetItemsForCategory(currentTab.currentCategory);
                        }
                        else
                        {
                            // The user wants to move everything, so get that list.
                            items = inventory.GetAllItemsAndGroups();
                        }

                        if (items.Count > 0)
                        {
                            // Check if the user's Inventory has more items/groups than the user has configured in Settings.
                            if (items.Count >= _configFile.MinNumOfItemsWarning)
                            {
#if DEBUG
                                LogService.Instance.WriteToLog($"MinNumOfItemsWarning threshold reached");
#endif
                                // Ask the user to confirm the move because there are a lot of items/groups.
                                Action<bool> confirmMove = new Action<bool>(response =>
                                {
                                    if (response)
                                    {
#if DEBUG
                                        LogService.Instance.WriteToLog($"User accepted MinNumOfItemsWarning dialog");
#endif
                                        (var tempItems, var tempGroups) = MoveInventoryItems(items, inventory, warehouse);
                                        // Show the user the number of items and groups that were moved.
                                        uiManager.ShowPopup(BuildInfo.Name, $"Items Moved From Inventory: {tempItems}", PopupType.Normal);
                                        uiManager.ShowPopup(BuildInfo.Name, $"Groups Moved From Inventory: {tempGroups}", PopupType.Normal);
                                        LogService.Instance.WriteToLog($"{tempItems} Item(s) and {tempGroups} Group(s) moved from Inventory");

                                        // Refresh the Warehouse Inventory Tab.
                                        warehouseWindow.warehouseInventoryTab.Refresh();
                                    }
#if DEBUG
                                    else
                                    {
                                        LogService.Instance.WriteToLog($"User cancelled MinNumOfItemsWarning dialog");
                                    }
#endif
                                });
                                string category = string.Empty;
                                if (_configFile.TransferByCategory)
                                {
                                    if (warehouseWindow.warehouseInventoryTab.currentCategory != InventoryCategories.All)
                                    {
                                        category = $" {warehouseWindow.warehouseInventoryTab.currentCategory}";
                                    }
                                }
                                string condition = string.Empty;
                                if (_configFile.MinPartCondition > 0)
                                {
                                    condition = $" above {_configFile.MinPartCondition - 1}%";
                                }
                                uiManager.ShowAskWindow("Move Items", $"Move all{category} items{condition} in inventory to {warehouse.GetCurrentSelectedWarehouseName()}?", confirmMove);
                            }
                            else
                            {
#if DEBUG
                                LogService.Instance.WriteToLog($"Item count below MinNumOfItemsWarning threshold");
#endif
                                // The number of items is below the settings, so move the items/groups.
                                (var tempItems, var tempGroups) = MoveInventoryItems(items, inventory, warehouse);
                                // Show the user the number of items and groups that were moved.
                                uiManager.ShowPopup(BuildInfo.Name, $"Items Moved From Inventory: {tempItems}", PopupType.Normal);
                                uiManager.ShowPopup(BuildInfo.Name, $"Groups Moved From Inventory: {tempGroups}", PopupType.Normal);
                                LogService.Instance.WriteToLog($"{tempItems} Item(s) and {tempGroups} Group(s) moved from Inventory");

                                // Refresh the Warehouse Inventory Tab.
                                warehouseWindow.warehouseInventoryTab.Refresh();
                            }
                        }
                        // The user's Inventory was empty, so show the user a message.
                        else
                        {
                            uiManager.ShowPopup(BuildInfo.Name, "No items to move", PopupType.Normal);
                            LogService.Instance.WriteToLog("No items to move");
                        }
                    }
                    else if (warehouseWindow.currentTab == 1)
                    {
                        // This is the Warehouse Tab
#if DEBUG
                        LogService.Instance.WriteToLog($"Warehouse Tab displayed");
#endif
                        // Setup a temporary List<BaseItem> to hold the items.
                        Il2CppSystem.Collections.Generic.List<BaseItem> items = new Il2CppSystem.Collections.Generic.List<BaseItem>();
                        // Check if the user has selected to move items
                        // for the current category only.
                        if (_configFile.TransferByCategory)
                        {
#if DEBUG
                            LogService.Instance.WriteToLog($"TransferByCategory enabled");
#endif
                            var currentTab = warehouseWindow.warehouseTab;
                            items = warehouse.GetItemsForCategory(currentTab.currentCategory);
                        }
                        else
                        {
                            // The user wants to move everything, so get that list.
                            items = warehouse.GetAllItemsAndGroups();
                        }

                        if (items.Count > 0)
                        {
                            // Check if the Warehouse has more items/groups than the user has configured in Settings.
                            if (items.Count >= _configFile.MinNumOfItemsWarning)
                            {
#if DEBUG
                                LogService.Instance.WriteToLog($"MinNumOfItemsWarning threshold reached");
#endif
                                // Ask the user to confirm the move because there are a lot of items/groups.
                                Action<bool> confirmMove = new Action<bool>(response =>
                                {
                                    if (response)
                                    {
#if DEBUG
                                        LogService.Instance.WriteToLog($"User accepted MinNumOfItemsWarning dialog");
#endif
                                        (var tempItems, var tempGroups) = MoveWarehouseItems(items, inventory, warehouse);
                                        // Show the user the number of items and groups that were moved.
                                        uiManager.ShowPopup(BuildInfo.Name, $"Items Moved From Warehouse: {tempItems}", PopupType.Normal);
                                        uiManager.ShowPopup(BuildInfo.Name, $"Groups Moved From Warehouse: {tempGroups}", PopupType.Normal);
                                        LogService.Instance.WriteToLog($"{tempItems} Item(s) and {tempGroups} Group(s) moved from Warehouse");

                                        // Refresh the Warehouse Tab.
                                        warehouseWindow.warehouseTab.Refresh(true);
                                    }
#if DEBUG
                                    else
                                    {
                                        LogService.Instance.WriteToLog($"User cancelled MinNumOfItemsWarning dialog");
                                    }
#endif
                                });
                                string category = string.Empty;
                                if (_configFile.TransferByCategory)
                                {
                                    if (warehouseWindow.warehouseTab.currentCategory != InventoryCategories.All)
                                    {
                                        category = $" {warehouseWindow.warehouseTab.currentCategory}";
                                    }
                                }
                                string condition = string.Empty;
                                if (_configFile.MinPartCondition > 0)
                                {
                                    condition = $" above {_configFile.MinPartCondition - 1}%";
                                }
                                uiManager.ShowAskWindow("Move Items", $"Move all{category} items{condition} in {warehouse.GetCurrentSelectedWarehouseName()} to your Inventory?", confirmMove);
                            }
                            else
                            {
#if DEBUG
                                LogService.Instance.WriteToLog($"Item count below MinNumOfItemsWarning threshold");
#endif
                                // The number of items is below the settings, so move the items/groups.
                                (var tempItems, var tempGroups) = MoveWarehouseItems(items, inventory, warehouse);
                                // Show the user the number of items and groups that were moved.
                                uiManager.ShowPopup(BuildInfo.Name, $"Items Moved From Warehouse: {tempItems}", PopupType.Normal);
                                uiManager.ShowPopup(BuildInfo.Name, $"Groups Moved From Warehouse: {tempGroups}", PopupType.Normal);
                                LogService.Instance.WriteToLog($"{tempItems} Item(s) and {tempGroups} Group(s) moved from Warehouse");

                                // Refresh the Warehouse Tab
                                warehouseWindow.warehouseTab.Refresh(true);
                            }
                        }
                        // The Warehouse was empty, so show the user a message.
                        else
                        {
                            uiManager.ShowPopup(BuildInfo.Name, "No items to move", PopupType.Normal);
                            LogService.Instance.WriteToLog("No items to move");
                        }
                    }
                }
                else
                {
                    LogService.Instance.WriteToLog("Warehouse Window is null");
                }
            }
            // The Warehouse Window is not displayed,
            // so show the user a message.
            else
            {
                uiManager.ShowPopup(BuildInfo.Name, "Please open the Warehouse first.", PopupType.Normal);
                LogService.Instance.WriteToLog("Warehouse is not open");
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
#if DEBUG
            LogService.Instance.WriteToLog($"Called");
#endif
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
                    // Disable QoLmod settings temporarily.
                    ToggleQoLSettings();

                    // Check which Tab is currently being displayed.
                    if (itemsExchangeWindow.currentTab == 0)
                    {
                        // This is the Junk (Found) Tab.
#if DEBUG
                        LogService.Instance.WriteToLog($"Junk (Found) Tab displayed");
#endif
                        // Get a reference to the Junk object.
                        var junk = itemsExchangeWindow.junk;
                        // Get the list of junk items/groups.
                        var junkItems = junk.ItemsInTrash;
                        // Store the number of junk in the Stash.
                        int junkCount = junkItems.Count;
                        // Create a temporary list to hold the body part items.
                        Il2CppSystem.Collections.Generic.List<BaseItem> bodyParts =
                            new Il2CppSystem.Collections.Generic.List<BaseItem>();
                        if (_configFile.TransferPartsOnlyAtBarnOrJunkyard)
                        {
#if DEBUG
                            LogService.Instance.WriteToLog($"TransferPartsOnlyAtBarnOrJunkyard enabled");
#endif
                            // If the user has turned on the setting,
                            // create a list of all the body parts in the stash.
                            bodyParts = UIHelper.GetBodyItems(junkItems);
                            // Subtract the number of body parts from junk parts.
                            junkCount -= bodyParts.Count;
                        }
                        // Check if there is junk to move.
                        if (junkCount > 0)
                        {
                            // Create temporay lists to hold the items and group.
                            List<Item> tempItems = new List<Item>();
                            List<GroupItem> tempGroups = new List<GroupItem>();
                            // We are only storing items, so a ForEach loop works here.
                            foreach (var baseItem in junkItems)
                            {
                                if (_configFile.TransferPartsOnlyAtBarnOrJunkyard)
                                {
                                    if (bodyParts.Contains(baseItem))
                                    {
                                        continue;
                                    }
                                }
                                // Check if the condition of the part is
                                // greater than or equal to the user setting.
                                if ((baseItem.GetCondition() * 100) >= _configFile.MinPartCondition)
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
                                else
                                {
                                    junkCount -= 1;
                                }
                            }
                            // Check that all the items and groups were added to the temporary lists.
                            if ((tempItems.Count + tempGroups.Count) == junkCount)
                            {
                                // Get a reference to the Temp Inventory.
                                var tempInventory = gameManager.TempInventory;
                                // We're using a temporary list, so ForEach loops work here.
                                // We aren't editing the temporary list, just the Junk and Temp Inventory.
                                if (tempItems.Count > 0)
                                {
                                    foreach (var tempItem in tempItems)
                                    {
                                        // Add the Item to the Temp Inventory (Collected Tab).
                                        tempInventory.items.Add(tempItem);
                                        // Remove the Item from the Junk Stash.
                                        junk.ItemsInTrash.Remove(tempItem);
                                    }
                                }
                                // Add the temporary list to the Global Dictionaries.
                                // This allows the user to "undo" the moves to each Junk Stash.
                                // Check if the temporary list is already in the Global Dictionary.
                                if (_tempItems.ContainsKey(junk.Pointer))
                                {
                                    // Add the new items to the list.
                                    var globalItems = _tempItems[junk.Pointer];
                                    globalItems.AddRange(tempItems);
                                    // Replace the Global List with the updated one.
                                    _tempItems[junk.Pointer] = globalItems;
                                }
                                else
                                {
                                    _tempItems.Add(junk.Pointer, tempItems);
                                }

                                // We're using a temporary list, so ForEach loops work here.
                                // We aren't editing the temporary list, just the Junk and Temp Inventory.
                                // *** The Barn and Junkyard don't seem to generate groups, ***
                                // *** but we can't take that for granted in the future. ***
                                if (tempGroups.Count > 0)
                                {
                                    foreach (var tempGroup in tempGroups)
                                    {
                                        // Add the Group to the Temp Inventory (Collected Tab).
                                        tempInventory.items.Add(tempGroup);
                                        // Remove the Group from the Junk Stash.
                                        junk.ItemsInTrash.Remove(tempGroup);
                                    }
                                }
                                // Add the temporary list to the Global Dictionaries.
                                // This allows the user to "undo" the moves to each Junk Stash.
                                // Check if the temporary list is already in the Global Dictionary.
                                if (_tempGroups.ContainsKey(junk.Pointer))
                                {
                                    // Add the new groups to the list.
                                    var globalGroups = _tempGroups[junk.Pointer];
                                    globalGroups.AddRange(tempGroups);
                                    // Replace the Global List with the updated one.
                                    _tempGroups[junk.Pointer] = globalGroups;
                                }
                                else
                                {
                                    _tempGroups.Add(junk.Pointer, tempGroups);
                                }

                                // Show the user the number of items that were moved.
                                uiManager.ShowPopup(BuildInfo.Name, $"Items Moved From Junk: {tempItems.Count}", PopupType.Normal);
                                // Show the user the number of groups that were moved.
                                uiManager.ShowPopup(BuildInfo.Name, $"Groups Moved From Junk: {tempGroups.Count}", PopupType.Normal);
                                LogService.Instance.WriteToLog($"{tempItems.Count} Item(s) and {tempGroups.Count} Group(s) moved from Junk");
                            }
                            // Something went wrong with the temporary moves,
                            // so show the user a message.
                            else
                            {
                                uiManager.ShowPopup(BuildInfo.Name, "Failed to move items", PopupType.Normal);
                                LogService.Instance.WriteToLog("Failed to move items");
                            }

                            // Refresh the Items Exchange Tab
                            itemsExchangeWindow.foundTab.Refresh(true);
                        }
                        // The junk was empty, so show the user a message.
                        else
                        {
                            uiManager.ShowPopup(BuildInfo.Name, "No items to move", PopupType.Normal);
                            LogService.Instance.WriteToLog("No items to move");
                        }
                    }
                    else if (itemsExchangeWindow.currentTab == 1)
                    {
                        // This is the Temp Inventory (Collected) Tab
#if DEBUG
                        LogService.Instance.WriteToLog($"Temp Inventory (Collected) Tab displayed");
#endif
                        // Get a reference to the Temp Inventory.
                        var tempInventory = gameManager.TempInventory;
                        // Get a reference to the Junk object.
                        var junk = itemsExchangeWindow.junk;
                        // We may not be moving body parts,
                        // so get a count of items already in the stash.
                        int junkCount = junk.ItemsInTrash.Count;
                        // Create temporary lists to hold the Global lists.
                        List<Item> tempItems = new List<Item>();
                        List<GroupItem> tempGroups = new List<GroupItem>();

                        // Get the list of temporary items from the Global Dictionary.
                        // We will use these lists to move the items back to the individual Junk Stashes.
                        if (_tempItems.ContainsKey(junk.Pointer))
                        {
                            _tempItems.TryGetValue(junk.Pointer, out tempItems);
                            if (tempItems != null)
                            {
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
                                    LogService.Instance.WriteToLog("No items to move");
                                }
                            }
                        }
                        // Get the list of temporary groups from the Global Dictionary.
                        // We will use these lists to move the groups back to the individual Junk Stashes.
                        if (_tempGroups.ContainsKey(junk.Pointer))
                        {
                            _tempGroups.TryGetValue(junk.Pointer, out tempGroups);
                            if (tempGroups != null)
                            {
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
                                    LogService.Instance.WriteToLog("No groups to move");
                                }
                            }
                        }

                        // Check that all the items and groups were added to the Junk Stash.
                        if ((tempItems.Count + tempGroups.Count + junkCount) == junk.ItemsInTrash.Count)
                        {
                            if (tempItems.Count > 0)
                            {
                                // Loop through the temporary list and remove the items from the Temp Inventory.
                                // A ForEach loop probably would have worked here, also.
                                for (int i = 0; tempItems.Count > i; i++)
                                {
                                    tempInventory.RemoveItem(tempItems[i]);
                                }
                            }
                            if (tempGroups.Count > 0)
                            {
                                // Loop through the temporary list and remove the groups from the Temp Inventory.
                                // A ForEach loop probably would have worked here, also.
                                for (int i = 0; i < tempGroups.Count; i++)
                                {
                                    tempInventory.RemoveItem(tempGroups[i]);
                                }
                            }
                            if (tempItems.Count == 0 && tempGroups.Count == 0)
                            {
                                uiManager.ShowPopup(BuildInfo.Name, "No items to move", PopupType.Normal);
                                LogService.Instance.WriteToLog("No items to move");
                            }

                            // Show the user the number of items that were moved.
                            uiManager.ShowPopup(BuildInfo.Name, $"Items Moved From Junk: {tempItems.Count}", PopupType.Normal);
                            // Show the user the number of groups that were moved.
                            uiManager.ShowPopup(BuildInfo.Name, $"Groups Moved From Junk: {tempGroups.Count}", PopupType.Normal);
                            LogService.Instance.WriteToLog($"{tempItems.Count} Item(s) and {tempGroups.Count} Group(s) moved from Junk");

                            // Remove the temporary list from the Global Dictionaries.
                            _tempItems.Remove(junk.Pointer);
                            _tempGroups.Remove(junk.Pointer);
                        }
                        // Something went wrong with the moves,
                        // so show the user a message.
                        else
                        {
                            uiManager.ShowPopup(BuildInfo.Name, "Failed to move items", PopupType.Normal);
                            LogService.Instance.WriteToLog("Failed to move items");
                        }

                        // Clear the temporary lists for the next Junk Stash.
                        tempItems.Clear();
                        tempGroups.Clear();

                        // Refresh the Items Exchange Tab
                        itemsExchangeWindow.collectedTab.Refresh(true);
                    }

                    // Reset the QoLmod settings.
                    ToggleQoLSettings(reset: true);
                }
            }
            // The Junk Items Window is not displayed,
            // so show the user a Tutorial message.
            else
            {
                uiManager.ShowPopup(BuildInfo.Name, "Please open a junk pile first.", PopupType.Normal);
                LogService.Instance.WriteToLog("Junk window not open");
            }
        }
        /// <summary>
        /// Method to move all items and groups from all the Junk Stashes to the Temp Inventory of the Barn or Junkyard.
        /// </summary>
        private void MoveEntireBarnOrJunkyard()
        {
#if DEBUG
            LogService.Instance.WriteToLog($"Called");
#endif
            var gameManager = Singleton<GameManager>.Instance;
            var windowManager = WindowManager.Instance;
            var uiManager = UIManager.Get();

            // Store a temporary value so the move doesn't happen twice.
            bool cartMove = false;

            // Check if the Junk Items Window (ItemsExchangeWindow) is displayed.
            // This means the user wants to move the junk from the Shopping Cart.
            if (windowManager.activeWindows.count > 0 &&
                windowManager.IsWindowActive(WindowID.ItemsExchange))
            {
                var itemsExchangeWindow = windowManager.GetWindowByID<ItemsExchangeWindow>(WindowID.ItemsExchange);
                if (itemsExchangeWindow != null)
                {
                    // Only work if the Shopping Cart Tab is selected.
                    if (itemsExchangeWindow.currentTab == 1)
                    {
#if DEBUG
                        LogService.Instance.WriteToLog($"Shopping Cart Tab displayed");
#endif
                        // Switch the variable so that the move doesn't happen again below.
                        cartMove = true;
                        // This is the Temp Inventory (Collected) Tab.
                        // Get a reference to the Temp Inventory.
                        var tempInventory = gameManager.TempInventory;
                        // Get a reference to the Junk object.
                        var junkInventory = itemsExchangeWindow.junk;
                        if (tempInventory != null &&
                            junkInventory != null)
                        {
                            // Get a reference to the junk items to move.
                            var invItems = tempInventory.items;
                            // Get a reference to the junk list to move to.
                            var junkItems = junkInventory.ItemsInTrash;
                            // Get a temporary count of the items to move.
                            int junkCount = invItems.Count;
                            for (int i = 0; i < junkCount; i++)
                            {
                                // Always get a reference to the first item.
                                var item = invItems[0];
                                // Add the item to the stash inventory.
                                junkItems.Add(item);
                                // Remove the item from the Temp Inventory.
                                invItems.RemoveAt(0);
                            }
                            if (invItems.Count == 0)
                            {
                                // Show the user the number of items and groups that were moved.
                                uiManager.ShowPopup(BuildInfo.Name, $"Junk items moved: {junkCount}", PopupType.Normal);
                                LogService.Instance.WriteToLog($"Junk items moved: {junkCount} from Shopping Cart");
                            }
                            // Something went wrong with the temporary moves,
                            // so show the user a message.
                            else
                            {
                                uiManager.ShowPopup(BuildInfo.Name, "Failed to move items", PopupType.Normal);
                                LogService.Instance.WriteToLog("Failed to move items from Shopping Cart");
                            }
                            // Clear out the Global Dictionaries whether the move succeeded or not.
                            _tempItems.Clear();
                            _tempGroups.Clear();

                            // Refresh the Items Exchange Tab
                            itemsExchangeWindow.collectedTab.Refresh(true);
                        }
                    }
                }
            }

            // Check that the move didn't happen above.
            if (!cartMove)
            {
#if DEBUG
                LogService.Instance.WriteToLog($"No windows displayed");
#endif
                // This will happen if the user is not on the Shopping Cart tab.
                // Get a reference to all the objects of type Junk.
                var junks = UnityEngine.Object.FindObjectsOfType<Junk>();
                // Setup a temporary count of junk to show the user at the end.
                int junkTotalCount = 0;
                // Use this bool to verify that all the Junk was moved from all the Stashes.
                bool junkEmptied = true;
                // Loop through each Junk object and move the items and groups.
                // We are only getting the Junk lists, so a ForEach loop works here.
                foreach (var junk in junks)
                {
                    // Get the list of junk items/groups.
                    var junkItems = junk.ItemsInTrash;
                    // Store the number of junk in the Stash.
                    int junkCount = junkItems.Count;
                    // Create a temporary list to hold the body part items.
                    Il2CppSystem.Collections.Generic.List<BaseItem> bodyParts =
                        new Il2CppSystem.Collections.Generic.List<BaseItem>();
                    // Check if the user has turned on the setting to
                    // move parts only, but didn't turn on the maps/cases setting.
                    if (_configFile.TransferPartsOnlyAtBarnOrJunkyard &&
                        !_configFile.TransferMapsOrCasesOnlyAtBarnOrJunkyard)
                    {
#if DEBUG
                        LogService.Instance.WriteToLog($"TransferPArtsOnlyAtBarnOrJunkyard enabled");
#endif
                        // If the user has turned on the setting,
                        // create a list of all the body parts in the stash.
                        bodyParts = UIHelper.GetBodyItems(junkItems);
                        // Subtract the number of body parts from junk parts.
                        junkCount -= bodyParts.Count;
                    }
                    // Check if there is junk to move.
                    if (junkCount > 0)
                    {
                        // Create temporay lists to hold the items and group.
                        List<Item> tempItems = new List<Item>();
                        List<GroupItem> tempGroups = new List<GroupItem>();
                        // We are only storing items, so a ForEach loop works here.
                        foreach (var baseItem in junkItems)
                        {
                            // If the user has turned on the setting to move maps/cases,
                            // find the map/case and then override the rest of the move.
                            if (_configFile.TransferMapsOrCasesOnlyAtBarnOrJunkyard)
                            {
                                if (baseItem.ID.ToLower().Contains("specialmap"))
                                {
#if DEBUG
                                    LogService.Instance.WriteToLog($"TransferMapsOrCasesOnlyAtBarnOrJunkyard enabled");
#endif
                                    tempItems.Add(baseItem.TryCast<Item>());
                                    junkTotalCount++;
                                }
                                else if (baseItem.ID.ToLower().Contains("specialcase"))
                                {
#if DEBUG
                                    LogService.Instance.WriteToLog($"TransferMapsOrCasesOnlyAtBarnOrJunkyard enabled");
#endif
                                    tempItems.Add(baseItem.TryCast<Item>());
                                    junkTotalCount++;
                                }
                                continue;
                            }
                            if (_configFile.TransferPartsOnlyAtBarnOrJunkyard &&
                                !_configFile.TransferMapsOrCasesOnlyAtBarnOrJunkyard)
                            {
                                if (bodyParts.Contains(baseItem))
                                {
                                    continue;
                                }
                            }
                            // Check if the condition of the part is
                            // greater than or equal to the user setting.
                            if ((baseItem.GetCondition() * 100) >= _configFile.MinPartCondition)
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
                                junkTotalCount++;
                            }
                            else
                            {
                                junkCount -= 1;
                            }
                        }
                        // Check that all the items and groups were added to the temporary lists.
                        if ((tempItems.Count + tempGroups.Count) == junkCount)
                        {
                            // Get a reference to the Temp Inventory.
                            var tempInventory = gameManager.TempInventory;
                            // We're using a temporary list, so ForEach loops work here.
                            // We aren't editing the temporary list, just the Junk and Temp Inventory.
                            if (tempItems.Count > 0)
                            {
                                foreach (var tempItem in tempItems)
                                {
                                    // Add the Item to the Temp Inventory (Collected Tab).
                                    tempInventory.items.Add(tempItem);
                                    // Remove the Item from the Junk Stash.
                                    junk.ItemsInTrash.Remove(tempItem);
                                }
                            }
                            // Add the temporary list to the Global Dictionaries.
                            // This allows the user to "undo" the moves to each Junk Stash.
                            // Check if the temporary list is already in the Global Dictionary.
                            if (_tempItems.ContainsKey(junk.Pointer))
                            {
                                // Add the new items to the list.
                                var globalItems = _tempItems[junk.Pointer];
                                globalItems.AddRange(tempItems);
                                // Replace the Global List with the updated one.
                                _tempItems[junk.Pointer] = globalItems;
                            }
                            else
                            {
                                _tempItems.Add(junk.Pointer, tempItems);
                            }

                            // We're using a temporary list, so ForEach loops work here.
                            // We aren't editing the temporary list, just the Junk and Temp Inventory.
                            // *** The Barn and Junkyard don't seem to generate groups, ***
                            // *** but we can't take that for granted in the future. ***
                            if (tempGroups.Count > 0)
                            {
                                foreach (var tempGroup in tempGroups)
                                {
                                    // Add the Group to the Temp Inventory (Collected Tab).
                                    tempInventory.items.Add(tempGroup);
                                    // Remove the Group from the Junk Stash.
                                    junk.ItemsInTrash.Remove(tempGroup);
                                }
                            }
                            // Add the temporary list to the Global Dictionaries.
                            // This allows the user to "undo" the moves to each Junk Stash.
                            // Check if the temporary list is already in the Global Dictionary.
                            if (_tempGroups.ContainsKey(junk.Pointer))
                            {
                                // Add the new groups to the list.
                                var globalGroups = _tempGroups[junk.Pointer];
                                globalGroups.AddRange(tempGroups);
                                // Replace the Global List with the updated one.
                                _tempGroups[junk.Pointer] = globalGroups;
                            }
                            else
                            {
                                _tempGroups.Add(junk.Pointer, tempGroups);
                            }
                        }
                        else if (junkTotalCount == 1)
                        {
                            // This means a single map or case was moved.
                            // Move the item to the temp inventory.
                            var tempInventory = gameManager.TempInventory;
                            if (tempItems.Count > 0)
                            {
                                foreach (var item in tempItems)
                                {
                                    // Add the Item to the Temp Inventory (Collected Tab).
                                    tempInventory.items.Add(item);
                                    // Remove the Item from the Junk Stash.
                                    junk.ItemsInTrash.Remove(item);
                                }
                            }
                            // Add the temporary list to the Global Dictionaries.
                            // This allows the user to "undo" the moves to each Junk Stash.
                            // Check if the temporary list is already in the Global Dictionary.
                            if (_tempItems.ContainsKey(junk.Pointer))
                            {
                                // Add the new items to the list.
                                var globalItems = _tempItems[junk.Pointer];
                                globalItems.AddRange(tempItems);
                                // Replace the Global List with the updated one.
                                _tempItems[junk.Pointer] = globalItems;
                            }
                            else
                            {
                                _tempItems.Add(junk.Pointer, tempItems);
                            }
                        }
                        // Something went wrong with the temporary moves,
                        // so show the user a message.
                        else
                        {
                            if (!_configFile.TransferMapsOrCasesOnlyAtBarnOrJunkyard)
                            {
                                junkEmptied = false;
                            }
                        }
                    }
                }
                // One of the Stashes was not emptied, so show the user a message.
                if (!junkEmptied)
                {
                    uiManager.ShowPopup(BuildInfo.Name, "Failed to empty a junk pile.", PopupType.Normal);
                    LogService.Instance.WriteToLog("Failed to empty a junk pile");
                }
                // Show the user the number of items and groups that were moved.
                if (junkTotalCount > 0)
                {
                    uiManager.ShowPopup(BuildInfo.Name, $"Junk items moved: {junkTotalCount}.", PopupType.Normal);
                    LogService.Instance.WriteToLog($"Junk items moved: {junkTotalCount} to Shopping Cart");
                }
                else
                {
                    uiManager.ShowPopup(BuildInfo.Name, "No Junk items to move.", PopupType.Normal);
                    LogService.Instance.WriteToLog("No Junk items to move to Shopping Cart");
                }

                // Refresh the Items Exchange Tab (if it's open).
                if (windowManager.activeWindows.count > 0 &&
                    windowManager.IsWindowActive(WindowID.ItemsExchange))
                {
                    var itemsExchangeWindow = windowManager.GetWindowByID<ItemsExchangeWindow>(WindowID.ItemsExchange);
                    if (itemsExchangeWindow != null)
                    {
#if DEBUG
                        LogService.Instance.WriteToLog($"Junk (Found) Tab displayed");
#endif
                        itemsExchangeWindow.foundTab.Refresh();
                    }
                }

                // Refresh the Inventory window (if it's open).
                if (windowManager.activeWindows.count > 0 &&
                    windowManager.IsWindowActive(WindowID.Inventory))
                {
                    var inventoryWindow = windowManager.GetWindowByID<InventoryWindow>(WindowID.Inventory);
                    if (inventoryWindow != null)
                    {
#if DEBUG
                        LogService.Instance.WriteToLog($"Temp Inventory (Collected) Tab displayed");
#endif
                        inventoryWindow.Refresh();
                    }
                }
            }
        }
    }

    /// <summary>
    /// A service to allow for debug logging.
    /// </summary>
    public class LogService
    {
        // A static reference to the service.
        private static LogService _instance;
        public static LogService Instance => _instance ?? (_instance = new LogService());

        // The path to the log file.
        private string _logFilePath = string.Empty;
        // A list of strings to write to the log file.
        private readonly List<string> _logs = new List<string>();
        // A public reference to the log count.
        // This will be used when the mod closes to write any pending logs.
        public int LogCount => _logs.Count;

        public void Initialize()
        {
            // Create a DateTime string to log.
            string logDate = DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss");
            // Create the log file path string.
            _logFilePath = $"{Directory.GetCurrentDirectory()}\\Mods\\TransferAll.log";
            // Create the log file and write some initial information to it.
            // Example:
            // Transfer All - 1.4.1
            // CMS 2021 - 1.0.34
            // Log Created: 01-01-2024 00:00:01
            File.WriteAllLines(_logFilePath, new List<string> { $"{BuildInfo.Name} - {BuildInfo.Version}", $"CMS 2021 - {GameSettings.BuildVersion}", $"Log Created: {logDate}" });
        }

        /// <summary>
        /// Method to write a string to the log file.
        /// </summary>
        /// <param name="message">A string with the message to log.</param>
        /// <param name="callerName">The method that the log is being created.</param>
        public void WriteToLog(string message, [CallerMemberName] string callerName = "")
        {
            // Create the log string with DateTime, Calling Method and Message string.
            var logString = $"{DateTime.Now:HH:mm:ss}\t{callerName}\t{message}.";
            // Add the string to the list of messsage.
            // This is done in case the file cannot be written to (it is not async).
            _logs.Add(logString);
            // Check that the log string is not empty.
            if (!string.IsNullOrWhiteSpace(_logFilePath))
            {
                // Check that the log file exists.
                if (File.Exists(_logFilePath))
                {
                    // Try to append the log strings to the log file
                    // and then clear the log string list.
                    try
                    {
                        File.AppendAllLines(_logFilePath, _logs);
                        _logs.Clear();
                    }
                    catch (Exception)
                    {
                        // The strings could not be written to the file.
                        // This is usually caused by a lock on the file
                        // (if it is currently being written to).
                        // Add them to the list and they will be written next time.
                        _logs.Add($"{DateTime.Now:HH:mm:ss}\tLogService.WriteToLog: Unable to write to log.");
                        _logs.Add(logString);
                    }
                }
                else
                {
                    // The log file was not found.
                    // This should not happen.
                    _logs.Add($"{DateTime.Now:HH:mm:ss}\tLogService.WriteToLog: Log file not found.");
                    _logs.Add(logString);
                }
            }
            else
            {
                // The log file path was empty.
                // This should not happen.
                _logs.Add($"{DateTime.Now:HH:mm:ss}\tLogService.WriteToLog: Log file not initialized.");
                _logs.Add(logString);
            }
#if DEBUG
            MelonLogger.Msg(message);
#endif
        }
    }
}
