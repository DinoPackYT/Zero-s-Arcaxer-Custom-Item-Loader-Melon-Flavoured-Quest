using System;
using System.Collections;
using Il2Cpp;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZerosCustomItemLoader;

namespace ArcaxerCustomNPCMod
{
    public class CustomNPCSpawner : MonoBehaviour
    {
        public CustomNPCSpawner(IntPtr ptr) : base(ptr) { }

        [HideFromIl2Cpp]
        public static CustomNPCSpawner Instance { get; private set; }

        private const string TargetSceneName = "HubWorld";
        private const string CustomNPCName = "Merchant_Mysterious";
        private static readonly Vector3 SpawnPosition = new Vector3(98f, 0f, 78f);
        private static readonly Quaternion SpawnRotation = Quaternion.Euler(0f, 180f, 0f);

        [HideFromIl2Cpp]
        public static void Initialize()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CustomNPCSpawner>();

            var go = new GameObject("CustomNPCSpawner_Host");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<CustomNPCSpawner>();
        }

        private void OnEnable()
        {
            SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)OnSceneLoaded);
        }

        private void OnDisable()
        {
            SceneManager.remove_sceneLoaded((Action<Scene, LoadSceneMode>)OnSceneLoaded);
        }

        [HideFromIl2Cpp]
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == TargetSceneName)
            {
                MelonCoroutines.Start(SpawnRoutine());
            }
        }

        [HideFromIl2Cpp]
        private IEnumerator SpawnRoutine()
        {
            // Wait one frame for default scene setup
            yield return null;

            // 1. Find template NPC
            NPCOverworld templateNPC = FindObjectOfType<NPCOverworld>();

            if (templateNPC == null)
            {
                MelonLogger.Warning($"[CustomNPCSpawner] No NPCOverworld template found in '{TargetSceneName}'.");
                yield break;
            }

            // Prevent duplicate spawning
            if (GameObject.Find(CustomNPCName) != null)
            {
                yield break;
            }

            // 2. Clone the GameObject
            GameObject newNPCObj = Instantiate(templateNPC.gameObject, SpawnPosition, SpawnRotation);
            newNPCObj.name = CustomNPCName;

            // 3. Configure NPCOverworld
            NPCOverworld newNPC = newNPCObj.GetComponent<NPCOverworld>();
            if (newNPC != null)
            {
                // --- SET CUSTOM NPC NAME ---
                newNPC.NPCName = "Mysterious Merchant";

                // --- SET CUSTOM DIALOGUE ---
                if (templateNPC.originalDialogue != null)
                {
                    // Clone and protect the Dialogue ScriptableObject
                    Dialogue clonedDialogue = Instantiate(templateNPC.originalDialogue);
                    DontDestroyOnLoad(clonedDialogue);

                    // Duplicate the first DialogueLine asset
                    if (clonedDialogue.dialogueTextEnglish != null && clonedDialogue.dialogueTextEnglish.Length > 0)
                    {
                        DialogueLine clonedLine = Instantiate(clonedDialogue.dialogueTextEnglish[0]);
                        DontDestroyOnLoad(clonedLine);

                        // Modify line text & behavior
                        clonedLine.dialogueString = "Looking for something out of the ordinary? Check my stock!";
                        clonedLine.rotateToPlayer = true;
                        clonedLine.waitForInteraction = true;
                        clonedLine.nameChange = false;

                        clonedDialogue.dialogueTextEnglish[0] = clonedLine;
                    }

                    newNPC.originalDialogue = clonedDialogue;
                    newNPC.npcDialogue = clonedDialogue;
                    newNPC.questCompleteDialogue = clonedDialogue;
                }

                // --- CONVERT TO SHOPKEEPER ---
                newNPC.shopkeeper = true;
                newNPC.sideQuestHub = false;
                newNPC.npcStoryEvent = null;

                // --- POPULATE ONLY CUSTOM MODDED ITEMS ---
                var customStock = new Il2CppSystem.Collections.Generic.List<Item>();

                if (Plugin.LoadedCustomItems != null && Plugin.LoadedCustomItems.Count > 0)
                {
                    foreach (var customItem in Plugin.LoadedCustomItems.Values)
                    {
                        if (customItem != null)
                        {
                            customStock.Add(customItem);
                        }
                    }
                    MelonLogger.Msg($"[CustomNPCSpawner] Loaded {customStock.Count} custom items into shop stock!");
                }
                else
                {
                    MelonLogger.Warning("[CustomNPCSpawner] Plugin.LoadedCustomItems is empty! Make sure items are loaded before visiting HubWorld.");
                }

                // Set active stock list
                newNPC.shopInventory = customStock;

                // --- CONFIGURE SHOP TAB SO UI DOES NOT OVERWRITE INVENTORY ---
                if (templateNPC.shopInventories != null && templateNPC.shopInventories.Length > 0)
                {
                    ShopInventory customTab = Instantiate(templateNPC.shopInventories[0]);
                    DontDestroyOnLoad(customTab);

                    customTab.shopItems = customStock;
                    customTab.reqStoryVal = 0;
                    customTab.storyUnlockString = "";
                    customTab.useTokensForShop = false;

                    newNPC.shopInventories = new ShopInventory[] { customTab };
                }

                // Clear out end dialogue events
                if (newNPC.onDialogueEnd != null)
                {
                    newNPC.onDialogueEnd.RemoveAllListeners();
                }
            }

            newNPCObj.SetActive(true);
            MelonLogger.Msg($"[CustomNPCSpawner] Successfully spawned {CustomNPCName} ('{newNPC.NPCName}') as Shopkeeper at {SpawnPosition}");
        }
    }
}