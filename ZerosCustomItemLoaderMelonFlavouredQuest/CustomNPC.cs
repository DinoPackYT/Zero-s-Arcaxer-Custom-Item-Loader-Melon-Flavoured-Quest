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
        private const string CustomNPCName = "Merchant_Modded";
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

            // find actual shopkeep (usually reggie)
            NPCOverworld[] allNpcs = FindObjectsOfType<NPCOverworld>();
            NPCOverworld reggieTemplate = null;
            NPCOverworld interactionTemplate = null;

            foreach (var npc in allNpcs)
            {
                if (npc != null)
                {
                    if (reggieTemplate == null && npc.shopkeeper)
                    {
                        reggieTemplate = npc;
                    }
                    // find first non shop npc (usually bored entity)
                    if (interactionTemplate == null && !npc.shopkeeper)
                    {
                        interactionTemplate = npc;
                    }
                }
            }

            // Fallbacks if strict types aren't matched
            if (reggieTemplate == null && allNpcs.Length > 0) reggieTemplate = allNpcs[0];
            if (interactionTemplate == null && allNpcs.Length > 0) interactionTemplate = allNpcs[0];

            if (interactionTemplate == null || reggieTemplate == null)
            {
                MelonLogger.Warning($"[CustomNPCSpawner] Missing required NPC templates in '{TargetSceneName}'.");
                yield break;
            }

            // Prevent duplicate spawning
            if (GameObject.Find(CustomNPCName) != null)
            {
                yield break;
            }

            // clone the first dialogue npc it finds (likely Bored Entity)
            GameObject newNPCObj = Instantiate(interactionTemplate.gameObject, SpawnPosition, SpawnRotation);
            newNPCObj.name = CustomNPCName;

            // config new NPC
            NPCOverworld newNPC = newNPCObj.GetComponent<NPCOverworld>();
            if (newNPC != null)
            {
                newNPC.NPCName = "Modded Merchant";

                // dialogue should never appear but im scared to delete these lines just in case
                // i have no idea what actually made it work (spawning a clone of reggie didnt allow me to interact with him so??)
                if (interactionTemplate.originalDialogue != null)
                {
                    Dialogue clonedDialogue = Instantiate(interactionTemplate.originalDialogue);
                    DontDestroyOnLoad(clonedDialogue);

                    if (clonedDialogue.dialogueTextEnglish != null && clonedDialogue.dialogueTextEnglish.Length > 0)
                    {
                        DialogueLine clonedLine = Instantiate(clonedDialogue.dialogueTextEnglish[0]);
                        DontDestroyOnLoad(clonedLine);

                        clonedLine.dialogueString = "Looking for a modded items?";
                        clonedLine.rotateToPlayer = true;
                        clonedLine.waitForInteraction = true;
                        clonedLine.nameChange = false;

                        clonedDialogue.dialogueTextEnglish[0] = clonedLine;
                    }

                    newNPC.originalDialogue = clonedDialogue;
                    newNPC.npcDialogue = clonedDialogue;
                    newNPC.questCompleteDialogue = clonedDialogue;
                }

                // make custom stock list using all custom items
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
                }

                // inject reggie's npc shop data
                // i probably dont HAVE to do this, but doing it manually either a) didnt work or b) crashed the game
                // this actually WORKED so lets not break it more.
                newNPC.shopkeeper = true;
                newNPC.sideQuestHub = false;
                newNPC.npcStoryEvent = null;
                newNPC.shopInventory = customStock;

                if (reggieTemplate.shopInventories != null && reggieTemplate.shopInventories.Length > 0)
                {
                    ShopInventory customTab = Instantiate(reggieTemplate.shopInventories[0]);
                    DontDestroyOnLoad(customTab);

                    customTab.shopItems = customStock;
                    customTab.reqStoryVal = 0;
                    customTab.storyUnlockString = "";
                    customTab.useTokensForShop = false;

                    newNPC.shopInventories = new ShopInventory[] { customTab };
                }

                if (newNPC.onDialogueEnd != null)
                {
                    newNPC.onDialogueEnd.RemoveAllListeners();
                }
            }

            newNPCObj.SetActive(true);
            MelonLogger.Msg($"[CustomNPCSpawner] Successfully spawned merchant with shop data at {SpawnPosition}");
        }
    }
}