# TRANSFER ALL - 1.2.0
Mod to automatically transfer all parts from your Inventory to the Warehouse.
Also, works in the Barn and Junkyard; including automatically moving all junk to the shopping cart.

## UPDATE 1.2.0
* Fixed a bug: When the Search box is active the default keys still enact the mod (this shouldn't happen).
* Fixed a bug: The confirmation box on the Warehouse Tab did not allow the user to cancel the move.
* Added the option (in TransferAll.cfg) to move only non-body part items at the Barn or Junkyard.
* Updated the default TransferAllItemsAndGroups key to 'K' so it doesn't conflict with the Remote Control Lifts mod.
* ** Updated key will not change if TransferAll.cfg already exists. Delete it or manually change the assignment. **
* The TransferAllItemsAndGroups (K) key now moves items back to the Junk stashes if you used the TransferEntireJunkyardOrBarn (L) key.
* ** You must be on the Shopping Cart tab to move items back. This only moves the original items back to the stash. **

## REQUIREMENTS
* [MelonLoader 0.5.7](https://github.com/LavaGang/MelonLoader/releases/tag/v0.5.7)

## FEATURES
* This mod allows the user to move all items in their Inventory to the selected Warehouse with one press of a key (K).
* When viewing a Junk stash in the Barn or Junkyard, the same key (K) will move all the Junk to the Shopping Cart/Summary screen.
* The user can also press a different key (L) and move all the Junk stashes from the Barn or Junkyard to the Shopping Cart/Summary screen.
* You still have to pay for the items at the Barn or Junkyard.

## DEFAULT KEYS
* K: Move all items from Inventory/Warehouse/Junk/Cart tab to opposite tab in Inventory screens.
* L: ONLY WORKS IN BARN AND JUNKYARD. Move all the Junk from all the stashes to the Shopping Cart/Summary screen.

## DEFAULT SETTINGS (TransferAll.cfg)
* TransferAllItemsAndGroups: K
* TransferEntireJunkyardOrBarn: L
* MinNumOfItemsWarning: 500
* TransferByCategory: false
* TransferPartsOnlyAtBarnOrJunkyard: false
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