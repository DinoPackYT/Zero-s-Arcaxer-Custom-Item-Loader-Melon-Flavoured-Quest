using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[assembly: MelonInfo(typeof(ZerosCustomItemLoader.Plugin), "Zero's Custom Item Loader (Quest)", "1.0.1", "Zero")]
[assembly: MelonGame(null, null)]

namespace ZerosCustomItemLoader
{
    public class Plugin : MelonMod
    {
        public static string CustomItemsFolderPath { get; private set; }
        public static List<Item> CustomItemsList { get; private set; } = new List<Item>();
        public static Dictionary<string, Item> LoadedCustomItems { get; private set; } = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);

        private static bool _itemsRegistered = false;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("[Zero's Item Loader] Quest Plugin initializing...");

            CustomItemsFolderPath = Path.Combine("/sdcard/Android/data/com.Overrungames.Arcaxer/files", "custom_items");

            Directory.CreateDirectory(CustomItemsFolderPath);
            Directory.CreateDirectory(Path.Combine(CustomItemsFolderPath, "consumables"));
            Directory.CreateDirectory(Path.Combine(CustomItemsFolderPath, "key_items"));

            // Initialize the subfolder structure for non-weapon equips
            EquipItemLoader.Initialize(CustomItemsFolderPath);

            HarmonyInstance.PatchAll(MelonAssembly.Assembly);
            MelonLogger.Msg("[Zero's Item Loader] Harmony patches applied!");
        }

        #region JSON Data Contracts
        [Serializable]
        public class BaseItemData
        {
            public string itemClass = "Item";
            public string itemName = "New Custom Item";
            public string itemDescription = "No description provided.";
            public Item.ItemRarity rarity = Item.ItemRarity.common;
            public int itemValue = 100;
            public bool stackable = true;
            public bool usable = false;
            public bool unSellable = false;

            public string spellName;
            public string passiveName;
            public string particleName;
            public int requiredLevel = 1;
            public int potency;
            public bool usePercentage;
            public string useParticleName;
            public int tokenValue;
            public int expValue;
            public string particleEffectName;
            public string sceneName;
            public bool useOtherScene;
        }
        #endregion

        #region Asset Resolution Helpers
        public static PlayerSpell ResolveSpell(string spellName)
        {
            if (string.IsNullOrEmpty(spellName)) return null;
            try
            {
                var spells = Resources.FindObjectsOfTypeAll<PlayerSpell>();
                return spells?.FirstOrDefault(s => s != null && s.name.Equals(spellName, StringComparison.OrdinalIgnoreCase));
            }
            catch { return null; }
        }

        public static PassiveAbility ResolvePassive(string targetPassiveName)
        {
            if (string.IsNullOrEmpty(targetPassiveName)) return null;

            try
            {
                var passives = Resources.FindObjectsOfTypeAll<PassiveAbility>();
                if (passives != null && passives.Length > 0)
                {
                    foreach (var p in passives)
                    {
                        if (p == null) continue;

                        if (!string.IsNullOrEmpty(p.abilityName) && p.abilityName.Equals(targetPassiveName, StringComparison.OrdinalIgnoreCase))
                            return p;

                        if (!string.IsNullOrEmpty(p.name) && p.name.Equals(targetPassiveName, StringComparison.OrdinalIgnoreCase))
                            return p;

                        try
                        {
                            if (!string.IsNullOrEmpty(p.translatedName) && p.translatedName.Equals(targetPassiveName, StringComparison.OrdinalIgnoreCase))
                                return p;
                        }
                        catch { /* Ignore translation instance errors during resolution */ }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[Zero's Item Loader] Exception in ResolvePassive('{targetPassiveName}'): {ex.Message}");
            }

            return null;
        }

        public static GameObject ResolveParticlePrefab(string particleName)
        {
            if (string.IsNullOrEmpty(particleName)) return null;
            try
            {
                var gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                return gameObjects?.FirstOrDefault(go => go != null && go.name.Equals(particleName, StringComparison.OrdinalIgnoreCase));
            }
            catch { return null; }
        }
        #endregion

        #region Item Factory Helpers
        private static T CreateScriptableInstance<T>() where T : ScriptableObject
        {
            var rawObj = ScriptableObject.CreateInstance(Il2CppType.Of<T>());
            if (rawObj == null) return null;

            var instance = rawObj.TryCast<T>();
            if (instance != null)
            {
                instance.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                UnityEngine.Object.DontDestroyOnLoad(instance);
            }
            return instance;
        }

        public static Item CreateCustomItem(BaseItemData data, Item.ItemType defaultType)
        {
            Item itemInstance = null;

            switch (data.itemClass)
            {
                case "ItemAbilityScroll":
                    var abilityScroll = CreateScriptableInstance<ItemAbilityScroll>();
                    if (abilityScroll != null)
                    {
                        abilityScroll.spell = ResolveSpell(data.spellName);
                        abilityScroll.particle = ResolveParticlePrefab(data.particleName);
                        abilityScroll.requiredLevel = data.requiredLevel;
                    }
                    itemInstance = abilityScroll;
                    break;

                case "ItemPassiveScroll":
                    var passiveScroll = CreateScriptableInstance<ItemPassiveScroll>();
                    if (passiveScroll != null)
                    {
                        passiveScroll.passive = ResolvePassive(data.passiveName);
                        passiveScroll.particle = ResolveParticlePrefab(data.particleName);
                        passiveScroll.requiredLevel = data.requiredLevel;
                    }
                    itemInstance = passiveScroll;
                    break;

                case "ItemPotion":
                    var potion = CreateScriptableInstance<ItemPotion>();
                    if (potion != null)
                    {
                        potion.potency = data.potency;
                        potion.usePercentage = data.usePercentage;
                        potion.useParticle = ResolveParticlePrefab(data.useParticleName);
                    }
                    itemInstance = potion;
                    break;

                case "ItemTokens":
                    var token = CreateScriptableInstance<ItemTokens>();
                    if (token != null)
                    {
                        token.tokenValue = data.tokenValue;
                    }
                    itemInstance = token;
                    break;

                case "ItemExpPotion":
                    var expPotion = CreateScriptableInstance<ItemExpPotion>();
                    if (expPotion != null)
                    {
                        expPotion.expValue = data.expValue;
                        expPotion.particleEffect = ResolveParticlePrefab(data.particleEffectName);
                    }
                    itemInstance = expPotion;
                    break;

                case "ItemMemoryCore":
                    var memoryCore = CreateScriptableInstance<ItemMemoryCore>();
                    if (memoryCore != null)
                    {
                        memoryCore.expValue = data.expValue;
                        memoryCore.particleEffect = ResolveParticlePrefab(data.particleEffectName);
                    }
                    itemInstance = memoryCore;
                    break;

                case "ItemHubTeleport":
                case "ItemHubReturn":
                    var hubTeleport = CreateScriptableInstance<ItemHubReturn>();
                    if (hubTeleport != null)
                    {
                        hubTeleport.sceneName = data.sceneName;
                        hubTeleport.useOtherScene = data.useOtherScene;
                    }
                    itemInstance = hubTeleport;
                    break;

                case "Item":
                default:
                    itemInstance = CreateScriptableInstance<Item>();
                    break;
            }

            if (itemInstance == null)
            {
                itemInstance = CreateScriptableInstance<Item>();
            }

            itemInstance.itemName = data.itemName;
            itemInstance.name = data.itemName;
            itemInstance.itemDescription = data.itemDescription;
            itemInstance.rarity = data.rarity;
            itemInstance.itemValue = data.itemValue;
            itemInstance.stackable = data.stackable;
            itemInstance.usable = data.usable;
            itemInstance.unSellable = data.unSellable;
            itemInstance.itemType = defaultType;

            return itemInstance;
        }
        #endregion

        // load assets during gameLoad with Harmony
        [HarmonyPatch(typeof(PersistentData), nameof(PersistentData.loadGame))]
        public static class RegistrationPatch
        {
            [HarmonyPrefix]
            public static void Prefix(PersistentData __instance)
            {
                if (__instance == null || _itemsRegistered) return;
                if (!Directory.Exists(CustomItemsFolderPath)) return;

                CustomItemsList.Clear();
                LoadedCustomItems.Clear();

                // run equip loader
                EquipItemLoader.LoadAllEquips();

                string[] jsonFiles = Directory.GetFiles(CustomItemsFolderPath, "*.json", SearchOption.AllDirectories);
                MelonLogger.Msg($"[Zero's Item Loader] Scanning custom item folder... Found {jsonFiles.Length} JSON file(s).");

                foreach (string jsonPath in jsonFiles)
                {
                    string fileName = Path.GetFileName(jsonPath);

                    try
                    {
                        string folderName = Path.GetFileName(Path.GetDirectoryName(jsonPath)).ToLower();
                        if (folderName != "consumables" && folderName != "key_items") continue;

                        string jsonText = File.ReadAllText(jsonPath);
                        BaseItemData itemData = JsonConvert.DeserializeObject<BaseItemData>(jsonText);

                        if (itemData == null || string.IsNullOrEmpty(itemData.itemName))
                        {
                            MelonLogger.Warning($"[Zero's Item Loader] Skipped {fileName}: Invalid JSON or missing itemName.");
                            continue;
                        }

                        if (LoadedCustomItems.ContainsKey(itemData.itemName))
                        {
                            MelonLogger.Msg($"[Zero's Item Loader] Skipped duplicate item: '{itemData.itemName}'");
                            continue;
                        }

                        Item.ItemType defaultType = (folderName == "key_items") ? Item.ItemType.key : Item.ItemType.consumable;
                        Item newItem = CreateCustomItem(itemData, defaultType);

                        if (newItem == null)
                        {
                            MelonLogger.Error($"[Zero's Item Loader] Failed to instantiate ScriptableObject for item: '{itemData.itemName}'");
                            continue;
                        }

                        if (folderName == "key_items") newItem.unSellable = true;

                        string imagePath = Path.ChangeExtension(jsonPath, ".png");
                        if (File.Exists(imagePath))
                        {
                            Texture2D iconTex = LoadTextureFromFile(imagePath);
                            if (iconTex != null)
                            {
                                newItem.icon = CreateSpriteFromTexture(iconTex);
                            }
                        }

                        CustomItemsList.Add(newItem);
                        LoadedCustomItems[newItem.itemName] = newItem;

                        MelonLogger.Msg($"[Zero's Item Loader] Registered custom Quest item: '{newItem.itemName}'!");
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error($"[Zero's Item Loader] Failed loading item from {fileName}: {ex.Message}\n{ex.StackTrace}");
                    }
                }

                _itemsRegistered = true;
                MelonLogger.Msg($"[Zero's Item Loader] Successfully cached {LoadedCustomItems.Count} custom items safely in managed memory!");
            }
        }

        // intercept resources.load with harmony
        [HarmonyPatch(typeof(Resources), nameof(Resources.Load), new Type[] { typeof(string), typeof(Il2CppSystem.Type) })]
        public static class ResourcesLoadPatch
        {
            [HarmonyPostfix]
            public static void Postfix(string path, Il2CppSystem.Type systemTypeInstance, ref UnityEngine.Object __result)
            {
                if (__result == null && !string.IsNullOrEmpty(path) && path.StartsWith("Items/", StringComparison.OrdinalIgnoreCase))
                {
                    string itemName = path.Substring(6);

                    if (LoadedCustomItems.TryGetValue(itemName, out Item customItem))
                    {
                        __result = customItem;
                    }
                }
            }
        }

        #region Helper Texture Routines
        public static Texture2D LoadTextureFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                Il2CppStructArray<byte> il2cppBytes = fileData;

                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                UnityEngine.Object.DontDestroyOnLoad(tex);

                if (ImageConversion.LoadImage(tex, il2cppBytes))
                {
                    return tex;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[Zero's Item Loader] Failed to load texture file {Path.GetFileName(filePath)}: {ex.Message}");
            }

            return null;
        }

        public static Sprite CreateSpriteFromTexture(Texture2D spriteTexture)
        {
            if (spriteTexture == null || spriteTexture.width <= 0 || spriteTexture.height <= 0)
                return null;

            try
            {
                Sprite sprite = Sprite.Create(
                    spriteTexture,
                    new Rect(0, 0, spriteTexture.width, spriteTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100.0f
                );

                if (sprite != null)
                {
                    sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                    UnityEngine.Object.DontDestroyOnLoad(sprite);
                }
                return sprite;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[Zero's Item Loader] Failed to create sprite from texture: {ex.Message}");
                return null;
            }
        }
        #endregion
    }
}