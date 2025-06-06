# TRANSFER ALL - 1.4.7
Mod to automatically transfer all parts from your Inventory to the Warehouse.
Also, works in the Barn and Junkyard; including automatically moving all junk to the shopping cart or inventory.

## REQUIREMENTS
* [MelonLoader 0.5.7](https://github.com/LavaGang/MelonLoader/releases/tag/v0.5.7)

## FEATURES
* This mod allows the user to move all items in their Inventory to the selected Warehouse with one press of a key (K).
* When viewing a Junk stash in the Barn or Junkyard, the same key (K) will move all the Junk to the Shopping Cart/Summary screen or Inventory with option set to true.
* The user can also press a different key (L) and move all the Junk stashes from the Barn or Junkyard to the Shopping Cart/Summary screen.
* ** You still have to pay for the items at the Barn or Junkyard, unless you set the new TransferPartsDirectlyToInventoryAtBarnOrJunkyard option to true. **

## UPDATE 1.4.7
* Added a configuration option to automatically transfer items from Barn/Junkyard to player inventory, skipping the payment option. ** THIS CANNOT BE UNDONE. **
* ** After moving items to the Player Inventory the Barn/Junkyard piles and Shopping Cart will no longer show items.  **
* ** When leaving the Barn/Junkyard, the game will ask if you want to leave without any items. Say yes and the items will be in your inventory when you return to the Garage. **
* Added the option to "undo" the TransferEntireJunkyardOrBarn option by opening the Inventory and pressing the TransferEntireJunkyardOrBarn (L) key.

## DEFAULT KEYS
* K: Move all items from Inventory/Warehouse/Junk/Cart tab to opposite tab in Inventory screens.
* L: ONLY WORKS IN BARN AND JUNKYARD. Move all the Junk from all the stashes to the Shopping Cart/Summary screen. Will move all Junk to Inventory (bypassing cost) if option is configured below.
* -: Press This Key to set the condition percentage lower by 10%. This value is saved to MinPartCondition value below on exit.
* =: Press This Key to set the condition percentage higher by 10%. This value is saved to MinPartCondition value below on exit.

## DEFAULT SETTINGS (TransferAll.cfg)
* TransferAllItemsAndGroups: K
* TransferEntireJunkyardOrBarn: L
* SetPartConditionLower: -
* SetPartConditionHigher: =
* MinPartCondition: 0
* MinNumOfItemsWarning: 500
* TransferByCategory: true
* TransferPartsOnlyAtBarnOrJunkyard: false
* TransferMapsOrCasesOnlyAtBarnOrJunkyard: false
* TransferPartsDirectlyToInventoryAtBarnOrJunkyard: false
- [Keycode Values](https://docs.unity3d.com/ScriptReference/KeyCode.html)

## INSTALLATION (Same as all other MelonLoader mods)
* Install [MelonLoader 0.5.7](https://github.com/LavaGang/MelonLoader/releases/tag/v0.5.7) (MelonLoader.Installer.exe recommended)
* Download .zip from Files page
* Unzip the TransferAll.dll file to Car Mechanic Simulator 2021\Mods folder
- Default Folder: \SteamLibrary\steamapps\common\Car Mechanic Simulator 2021\Mods\

## KNOWN ISSUES
* View [Issues Section](https://github.com/mannly01/TransferAll/issues)

## CREDITS
Thanks to the following developers for sharing their source code (in no particular order):
* [szokocska2](https://www.nexusmods.com/carmechanicsimulator2021/users/64455311)
* [shindouj](https://www.nexusmods.com/carmechanicsimulator2021/users/45606997)
* [snowe2010](https://www.nexusmods.com/carmechanicsimulator2021/users/12298499)

Thanks to the following developer for advice and responses to my questions:
* [MeitziQ](https://www.nexusmods.com/carmechanicsimulator2021/users/151281813)