using Il2Cpp;
using Il2CppInterop.Runtime;
using MelonLoader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ZerosCustomItemLoader
{
    public static class EquipItemLoader
    {
        public static string EquipFolderPath { get; private set; }
        public static List<ItemEquip> CustomEquipList { get; private set; } = new List<ItemEquip>();
        public static Dictionary<string, ItemEquip> LoadedEquipItems { get; private set; } = new Dictionary<string, ItemEquip>(StringComparer.OrdinalIgnoreCase);

        private static bool _equipsRegistered = false;

        #region JSON Data Contract
        [Serializable]
        public class EquipItemData
        {
            public string itemName = "New Armor Piece";
            public string itemDescription = "No description provided.";
            public Item.ItemRarity rarity = Item.ItemRarity.common;
            public int itemValue = 100;
            public bool stackable = false;
            public bool usable = true;
            public bool unSellable = false;

            // Equipment Slot (head, top, bottom, accessory)
            public ItemEquip.EquipType equipType = ItemEquip.EquipType.top;

            // Stat Modifiers
            public int power;
            public int defense;
            public int health;
            public int speed;
            public int crit;
            public int expBoost;
            public int spellVamp;
            public int critDamage;
            public int apBoost;
            public int runSpeed;
            public int summonPower;
            public int glitchChance;
            public bool musicSwap;

            // Passive Ability Name
            public string passiveName;
        }
        #endregion

        public static void Initialize(string basePath)
        {
            EquipFolderPath = Path.Combine(basePath, "equips");

            // Ensure directory structure for non-weapon slots
            Directory.CreateDirectory(EquipFolderPath);
            Directory.CreateDirectory(Path.Combine(EquipFolderPath, "head"));
            Directory.CreateDirectory(Path.Combine(EquipFolderPath, "top"));
            Directory.CreateDirectory(Path.Combine(EquipFolderPath, "bottom"));
            Directory.CreateDirectory(Path.Combine(EquipFolderPath, "accessory"));
        }

        public static void LoadAllEquips()
        {
            if (_equipsRegistered || !Directory.Exists(EquipFolderPath)) return;

            // Excluded weapons from scanning
            string[] subfolders = new string[] { "head", "top", "bottom", "accessory" };

            foreach (string subfolder in subfolders)
            {
                string folderPath = Path.Combine(EquipFolderPath, subfolder);
                if (!Directory.Exists(folderPath)) continue;

                string[] jsonFiles = Directory.GetFiles(folderPath, "*.json", SearchOption.AllDirectories);
                MelonLogger.Msg($"[Zero's Equip Loader] Scanning /equips/{subfolder}... Found {jsonFiles.Length} JSON file(s).");

                foreach (string jsonPath in jsonFiles)
                {
                    string fileName = Path.GetFileName(jsonPath);

                    try
                    {
                        string jsonText = File.ReadAllText(jsonPath);
                        EquipItemData data = JsonConvert.DeserializeObject<EquipItemData>(jsonText);

                        if (data == null || string.IsNullOrEmpty(data.itemName))
                        {
                            MelonLogger.Warning($"[Zero's Equip Loader] Skipped {fileName}: Invalid JSON or missing itemName.");
                            continue;
                        }

                        if (LoadedEquipItems.ContainsKey(data.itemName))
                        {
                            MelonLogger.Msg($"[Zero's Equip Loader] Skipped duplicate equipment item: '{data.itemName}'");
                            continue;
                        }

                        // Auto-assign slot type based on directory
                        data.equipType = subfolder.ToLower() switch
                        {
                            "head" => ItemEquip.EquipType.head,
                            "top" => ItemEquip.EquipType.top,
                            "bottom" => ItemEquip.EquipType.bottom,
                            "accessory" => ItemEquip.EquipType.accessory,
                            _ => data.equipType
                        };

                        ItemEquip equipInstance = CreateEquipInstance(data, jsonPath);
                        if (equipInstance == null) continue;

                        CustomEquipList.Add(equipInstance);
                        LoadedEquipItems[equipInstance.itemName] = equipInstance;

                        // Add to main global dictionary
                        Plugin.LoadedCustomItems[equipInstance.itemName] = equipInstance;

                        MelonLogger.Msg($"[Zero's Equip Loader] Registered equipment [{data.equipType}]: '{equipInstance.itemName}'!");
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error($"[Zero's Equip Loader] Failed loading equipment from {fileName}: {ex.Message}");
                    }
                }
            }

            _equipsRegistered = true;
        }

        private static ItemEquip CreateEquipInstance(EquipItemData data, string jsonPath)
        {
            var rawObj = ScriptableObject.CreateInstance(Il2CppType.Of<ItemEquip>());
            if (rawObj == null) return null;

            var equip = rawObj.TryCast<ItemEquip>();
            if (equip == null) return null;

            equip.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            UnityEngine.Object.DontDestroyOnLoad(equip);

            // Base Item metadata
            equip.itemName = data.itemName;
            equip.name = data.itemName;
            equip.itemDescription = data.itemDescription;
            equip.rarity = data.rarity;
            equip.itemValue = data.itemValue;
            equip.stackable = data.stackable;
            equip.usable = data.usable;
            equip.unSellable = data.unSellable;
            equip.itemType = Item.ItemType.equip;

            // Stat Allocations
            equip.equipType = data.equipType;
            equip.power = data.power;
            equip.defense = data.defense;
            equip.health = data.health;
            equip.speed = data.speed;
            equip.crit = data.crit;
            equip.expBoost = data.expBoost;
            equip.spellVamp = data.spellVamp;
            equip.critDamage = data.critDamage;
            equip.apBoost = data.apBoost;
            equip.runSpeed = data.runSpeed;
            equip.summonPower = data.summonPower;
            equip.glitchChance = data.glitchChance;
            equip.musicSwap = data.musicSwap;

            // Passive Ability Resolution
            if (!string.IsNullOrEmpty(data.passiveName))
            {
                equip.passive = Plugin.ResolvePassive(data.passiveName);
            }

            // Optional Icon Loading (.png with identical name to .json)
            string imagePath = Path.ChangeExtension(jsonPath, ".png");
            if (File.Exists(imagePath))
            {
                Texture2D iconTex = Plugin.LoadTextureFromFile(imagePath);
                if (iconTex != null)
                {
                    equip.icon = Plugin.CreateSpriteFromTexture(iconTex);
                }
            }

            return equip;
        }
    }
}