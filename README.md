# Zero's Arcaxer Custom Item Loader - Melon Flavoured (Quest)
A custom item loader mod for Arcaxer for use with MelonLoader. This build specifically is made for the Quest version of the game.

Please report any bugs either here on GitHub or to Discord user `dino.pack`.

Please note: This mod does not include any custom items by itself. Please use [Zero's Arcaxer Custom Item Creator](https://dino-pack.gitlab.io/Zero-s-Arcaxer-Custom-Item-Creator/) to make your own mods. A database for modapcks will be created... later.

# Installation:
1. Download [LemonLoader](https://github.com/LemonLoader/MelonLoader/releases/latest) and side-load it onto your Quest device. (Developer Mode must be enabled.)
2. Using a file manager (such as SideQuest), back up the following folders and all files:
  - `sdcard/Android/data/com.Overrungames.Arcaxer`
  - `sdcard/Android/obb/com.Overrungames.Arcaxer`
3. Open the MelonLoader Installer app on your Quest 3 device and patch the Arcaxer app with MelonLoader. Follow all on screen instructions and do not restore Arcaxer to the store version.
4. Using SideQuest, restore the two above folders.
5. Launch the game. It may take a while to boot, just wait and close the game when it loads to the title screen.
6. Download the latest release and drop it into the following directory:
  - `sdcard/MelonLoader/com.Overrungames.Arcaxer/Mods`
7. Drop custom items into the following directory: (If it does not exist, either create it or launch the game again to auto-create it.)
  - `sdcard/Android/data/com.Overrungames.Arcaxer/files/custom_items` (in either `consumables` or `key_items` subfolders.)
8. (Optionally) If when trying to enter battle, the game freezes, run the following ADB command in SideQuest:
  - `adb shell "find /sdcard/Android/data/com.Overrungames.Arcaxer/files -name '*.json' -exec chmod 666 {} +"` 

# DLLs required:
- If using the source code to build the Quest version of the mod, the following DLLs are required to be extracted through MelonLoader: (found in sdcard/MelonLoader/com.Overrungames.Arcaxer/MelonLoader)
  - 0Harmony.dll (/net8)
  - Assembly-CSharp.dll (/Il2CppAssemblies)
  - Il2CppInterop.Common.dll (/net8)
  - Il2CppInterop.Generator.dll (/net8)
  - Il2CppInterop.Runtime.dll (/net8)
  - Il2Cppmscorlib.dll (/Il2CppAssemblies)
  - MelonLoader.dll (/net8)
  - Newtonsoft.Json.dll (/net8)
  - UnityEngine.CoreModule.dll (/Il2CppAssemblies)
  - UnityEngine.dll (/Il2CppAssemblies)
  - UnityEngine.ImageConversionModule.dll (/Il2CppAssemblies)
  - UnityEngine.JSONSerializeModule.dll (/Il2CppAssemblies)
  - UnityEngine.TextRenderingModule.dll (/Il2CppAssemblies)
- I will not be providing these files out of respect (and likely legality?) but they are in your copy of the game. They go in the empty ArcaxerDLLS folder in the project folder.

# AI Disclosure:
Due to my inexperience with MelonLoader, Harmony, and C#, AI was used to help support development, mostly by debugging and assisting with parts of the project I was not familiar with or understanding the code from Arcaxer. All code was reviewed by a human. I hope this is acceptable.