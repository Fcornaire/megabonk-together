using Assets.Scripts.Game.MapGeneration.MapEvents;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Menu.Shop;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using MegabonkTogether.Common;
using MegabonkTogether.Common.Models;
using MegabonkTogether.Configuration;
using MegabonkTogether.Helpers;
using MegabonkTogether.Scripts;
using MegabonkTogether.Scripts.Button;
using MegabonkTogether.Scripts.Enemies;
using MegabonkTogether.Scripts.Interactables;
using MegabonkTogether.Scripts.Modal;
using MegabonkTogether.Scripts.NetPlayer;
using MegabonkTogether.Scripts.Snapshot;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MegabonkTogether
{
    public enum DistanceToPlayer
    {
        Close,
        Medium,
        Far
    }

    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        public static Plugin Instance = null!;
        public NetworkHandler NetworkHandler = null;
        public Dictionary<ECharacter, RawImage> CharactersIcon = [];
        public NetPlayersDisplayer NetPlayersDisplayer = null;
        public CameraSwitcher CameraSwitcher = null;
        public PlayTogetherButton PlayTogetherButton = null;
        public AchievementPopup AchievementPopup = null;
        public NotificationQueueManager NotificationQueueManager = null;
        private MainMenu MainMenu = null;
        private MapEventsManager mapEventsManager = null;
        private MapEventsDesert mapEventsDesert = null;
        private LoadingModal modal;
        public NetworkMode Mode = new();

        public static IHost Host = null!;
        public static IServiceProvider Services => Host.Services;

        internal NetworkMenuTab NetworkTab { get; set; }

        internal static new ManualLogSource Log;
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private CancellationToken cancellationToken;
        public static float PLAYER_FEET_OFFSET_Y = 1.50f;

        public static bool CAN_SPAWN_PICKUPS = false;
        public static bool CAN_SPAWN_CHESTS = false;
        public static bool CAN_SEND_MESSAGES = true;
        public static bool CAN_ENEMY_EXPLODE = false;
        public static bool CAN_ENEMY_USE_SPECIAL_ATTACK = false;
        public bool CAN_SPAWN_TORNADOES = false;
        public bool CAN_START_STOP_STORMS = false;
        public bool CAN_DAMAGE_ENEMIES = false;
        public bool IS_HOST_READY = false;
        public bool IS_MANUAL_INVINCIBLE = false;

        public uint? CurrentReviver = null;

        private Vector3 WorldSize = Vector3.zero;
        public Vector3 OriginalWorldSize = Vector3.zero;
        public bool HasDungeonTimerStarted = false;

        private readonly ConcurrentDictionary<string, GameObject> prefabs = new();

        private Il2CppSystem.Action originalDiedAction = null;
        private Il2CppSystem.Action<WeaponBase> originalWeaponAddedAction = null;
        private Il2CppSystem.Action<EStat> originalStatUpdateAction = null;

        public override void Load()
        {
            Instance = this;
            cancellationToken = cancellationTokenSource.Token;

            Log = base.Log;
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            ModConfig.Initialize(Config);
            Log.LogInfo($"Player name set to: {ModConfig.PlayerName.Value}");

            ClassInjector.RegisterTypeInIl2Cpp<NetPlayer>();
            ClassInjector.RegisterTypeInIl2Cpp<CoroutineRunner>();
            ClassInjector.RegisterTypeInIl2Cpp<MainThreadDispatcher>();
            ClassInjector.RegisterTypeInIl2Cpp<NetworkHandler>();
            ClassInjector.RegisterTypeInIl2Cpp<PlayerInterpolator>();
            ClassInjector.RegisterTypeInIl2Cpp<EnemyInterpolator>();
            ClassInjector.RegisterTypeInIl2Cpp<BossOrbInterpolator>();
            ClassInjector.RegisterTypeInIl2Cpp<ProjectileInterpolator>();
            ClassInjector.RegisterTypeInIl2Cpp<TumbleWeedInterpolator>();
            ClassInjector.RegisterTypeInIl2Cpp<NetPlayersDisplayer>();
            ClassInjector.RegisterTypeInIl2Cpp<NetPlayerCard>();
            ClassInjector.RegisterTypeInIl2Cpp<DisplayBar>();
            ClassInjector.RegisterTypeInIl2Cpp<CustomInventoryHud>();
            ClassInjector.RegisterTypeInIl2Cpp<CameraSwitcher>();
            ClassInjector.RegisterTypeInIl2Cpp<PlayTogetherButton>();
            ClassInjector.RegisterTypeInIl2Cpp<CustomButton>();
            ClassInjector.RegisterTypeInIl2Cpp<ModalBase>();
            ClassInjector.RegisterTypeInIl2Cpp<NetworkMenuTab>();
            ClassInjector.RegisterTypeInIl2Cpp<LoadingModal>();
            ClassInjector.RegisterTypeInIl2Cpp<UpdateAvailableModal>();
            ClassInjector.RegisterTypeInIl2Cpp<ChangelogModal>();
            ClassInjector.RegisterTypeInIl2Cpp<TargetSwitcher>();
            ClassInjector.RegisterTypeInIl2Cpp<InteractableReviver>();
            ClassInjector.RegisterTypeInIl2Cpp<NotificationQueueManager>();

            var builder = new HostBuilder();

            string contentRoot = System.IO.Directory.GetCurrentDirectory();
            builder.UseContentRoot(contentRoot);

            builder.ConfigureServices(services =>
            {
                services.AddSingleton(Log);
                services.AddSingleton<IWebsocketClientService, WebsocketClientService>();

                services.AddSingleton<IUdpClientService, UdpClientService>();
                services.AddSingleton<IPlayerManagerService, PlayerManagerService>();
                services.AddSingleton<IEnemyManagerService, EnemyManagerService>();
                services.AddSingleton<IProjectileManagerService, ProjectileManagerService>();
                services.AddSingleton<ISynchronizationService, SynchronizationService>();
                services.AddSingleton<IPickupManagerService, PickupManagerService>();
                services.AddSingleton<IChestManagerService, ChestManagerService>();
                services.AddSingleton<ISpawnedObjectManagerService, SpawnedObjectManagerService>();
                services.AddSingleton<IFinalBossOrbManagerService, FinalBossOrbManagerService>();
                services.AddSingleton<ILocalizationService, LocalizationService>();
                services.AddSingleton<IGameBalanceService, GameBalanceService>();
                services.AddSingleton<IAutoUpdaterService, AutoUpdaterService>();
                services.AddSingleton<IChangelogService, ChangelogService>();
            });

            Host = builder.Build();


            _ = Services.GetRequiredService<ISynchronizationService>(); // Initialize SynchronizationService
            _ = Host.StartAsync(cancellationToken);
            var autoUpdaterService = Services.GetRequiredService<IAutoUpdaterService>();

            if (ModConfig.CheckForUpdates.Value)
            {
                autoUpdaterService.Initialize();

                Task.Run(async () =>
                {
                    try
                    {
                        var updateAvailable = await autoUpdaterService.CheckAndUpdate();
                        if (updateAvailable && !autoUpdaterService.IsThunderstoreBuild())
                        {
                            Log.LogInfo("An update has been downloaded and will be applied when you quit the game.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"Auto-update check failed: {ex.Message}");
                    }
                });
            }
            else
            {
                Log.LogInfo("Auto-update is disabled in configuration.");
            }

            try
            {
                var harmony = new HarmonyLib.Harmony(MyPluginInfo.PLUGIN_GUID);
                harmony.PatchAll();
            }
            catch (Exception ex)
            {
                Log.LogError($"Harmony patching failed: {ex}");
            }

            var go = new GameObject("MainThreadDispatcher");
            GameObject.DontDestroyOnLoad(go);
            go.AddComponent<MainThreadDispatcher>();

            var goNetworkHandler = new GameObject("NetworkHandler");
            GameObject.DontDestroyOnLoad(goNetworkHandler);
            NetworkHandler = goNetworkHandler.AddComponent<NetworkHandler>();

            var goNetPlayersDisplayer = new GameObject("NetPlayersDisplayer");
            GameObject.DontDestroyOnLoad(goNetPlayersDisplayer);
            NetPlayersDisplayer = goNetPlayersDisplayer.AddComponent<NetPlayersDisplayer>();

            var goCameraSwitcher = new GameObject("CameraSwitcher");
            GameObject.DontDestroyOnLoad(goCameraSwitcher);
            CameraSwitcher = goCameraSwitcher.AddComponent<CameraSwitcher>();

            var goNotificationQueueManager = new GameObject("NotificationQueueManager");
            GameObject.DontDestroyOnLoad(goNotificationQueueManager);
            NotificationQueueManager = goNotificationQueueManager.AddComponent<NotificationQueueManager>();
        }

        public void AddPrefab(GameObject prefab)
        {
            prefabs.TryAdd(prefab.name, prefab);
        }

        public GameObject GetPrefab(string name)
        {
            if (prefabs.TryGetValue(name.Trim(), out var prefab))
            {
                return prefab;
            }

            Plugin.Log.LogWarning($"Prefab not found: {name}");

            return null;
        }

        public void ClearPrefabs()
        {
            prefabs.Clear();
        }

        public void ClearCharacterIcons()
        {
            CharactersIcon.Clear();
        }

        public void AddCharacterIcons(Il2CppSystem.Collections.Generic.List<MyButtonCharacter> characterButtons)
        {
            if (CharactersIcon.Count > 0)
            {
                Log.LogWarning("Character icons already added");
                return;
            }

            foreach (var button in characterButtons)
            {
                var iconObj = UnityEngine.Object.Instantiate(button.i_icon);
                UnityEngine.Object.DontDestroyOnLoad(iconObj);
                CharactersIcon.Add(button.characterData.eCharacter, iconObj);
            }

        }

        public void PreventDeath()
        {
            if (originalDiedAction != null)
            {
                Log.LogWarning("Death already prevented");
                return;
            }

            originalDiedAction = PlayerHealth.A_Died;
            PlayerHealth.A_Died = new Action(OnPlayerDied);
        }

        private void OnPlayerDied()
        {
            if (CameraSwitcher == null || CameraSwitcher.IsFollowingTarget)
            {
                return;
            }

            var playerManager = Services.GetService<IPlayerManagerService>();
            var netPlayer = playerManager.GetRandomNetPlayer();

            if (netPlayer != null)
            {
                CameraSwitcher.SwitchToTarget(netPlayer.ConnectionId);
            }
        }

        public void RestoreDeath(bool invokeDeathEvent)
        {
            if (originalDiedAction == null)
            {
                Log.LogWarning("Death not prevented");
                return;
            }

            CameraSwitcher.ResetToLocalPlayer();

            PlayerHealth.A_Died = originalDiedAction;
            originalDiedAction = null;

            if (invokeDeathEvent)
            {
                GameManager.Instance.player.playerRenderer.gameObject.SetActive(true);
                PlayerHealth.A_Died.Invoke();
            }
        }

        public AchievementPopup GetAchievementPopup()
        {
            if (AchievementPopup == null)
            {
                var gameObject = Il2CppFindHelper.FindAllGameObjects()
                    .FirstOrDefault(go => go.GetComponent<AchievementPopup>() != null);
                if (gameObject != null)
                {
                    AchievementPopup = gameObject.GetComponent<AchievementPopup>();

                    if (AchievementPopup != null && NotificationQueueManager != null)
                    {
                        NotificationQueueManager.Initialize();
                    }
                }
            }
            return AchievementPopup;
        }

        public MainMenu GetMainMenu()
        {
            return MainMenu;
        }

        public void SetMainMenu(MainMenu mainMenu)
        {
            MainMenu = mainMenu;
        }

        public static bool StartNotification(
            (string tableReference, string tableEntryReference) localizedName,
            (string tableReference, string tableEntryReference) localizedDescription,
            IEnumerable<string> descriptionArgs,
            RandomSfx sfx = null,
            EItem item = EItem.Key
        )
        {
            if (Instance?.NotificationQueueManager == null)
            {
                Log.LogWarning("NotificationQueueManager is not initialized");
                return false;
            }

            Instance.NotificationQueueManager.EnqueueNotification(
                localizedName,
                localizedDescription,
                descriptionArgs,
                sfx,
                item
            );

            return true;
        }

        public static void GoToMainMenu()
        {
            if (GameManager.Instance == null || GameManager.Instance.player == null)
            {
                if (WindowManager.activeWindow is CharacterMenu menu)
                {
                    menu.b_back.button.onClick.Invoke();
                }

                if (WindowManager.activeWindow.name.Contains("Maps And Stats"))
                {
                    WindowManager.activeWindow.allButtons.ToArray().FirstOrDefault(b => b.name == "B_Back")?.button.onClick.Invoke();
                    (WindowManager.activeWindow as CharacterMenu)?.b_back.button.onClick.Invoke();
                }
            }
            else
            {
                TransitionUI.Instance.LoadMenu();
            }
        }

        public void ShowModal(string message)
        {
            if (modal != null)
            {
                modal.UpdateMessage(message);
                return;
            }
            modal = LoadingModal.Show(message);
        }

        public void HideModal()
        {
            if (modal != null)
            {
                modal.Close();
                modal = null;
            }
        }

        public static void ShowUpdateAvailableModal()
        {
            var go = new GameObject("UpdateAvailableModal");
            go.AddComponent<UpdateAvailableModal>();
        }

        public void SavePlayerInventoryActions()
        {
            originalWeaponAddedAction = WeaponInventory.A_WeaponAdded;
            originalStatUpdateAction = PlayerStatsNew.A_StatUpdate;
            WeaponInventory.A_WeaponAdded = null;
            PlayerStatsNew.A_StatUpdate = null;
        }

        public void RestorePlayerInventoryActions()
        {
            if (originalWeaponAddedAction == null)
            {
                Log.LogWarning("WeaponAdded action not saved");
                return;
            }

            if (originalStatUpdateAction == null)
            {
                Log.LogWarning("StatUpdate action not saved");
                return;
            }

            WeaponInventory.A_WeaponAdded = originalWeaponAddedAction;
            PlayerStatsNew.A_StatUpdate = originalStatUpdateAction;
            originalWeaponAddedAction = null;
            originalStatUpdateAction = null;
        }

        public MapEventsDesert GetMapEventsDesert()
        {
            if (mapEventsDesert == null && mapEventsManager == null)
            {
                mapEventsManager = GetMapEventsManager();
                mapEventsDesert = IL2CPP.PointerToValueGeneric<MapEventsDesert>(mapEventsManager.mapEvents.Pointer, false, false);
            }

            return mapEventsDesert;
        }

        private MapEventsManager GetMapEventsManager()
        {
            if (mapEventsManager == null)
            {
                mapEventsManager = Il2CppFindHelper.FindAllGameObjects()
                    .FirstOrDefault(go => go.GetComponent<MapEventsManager>() != null)
                    .GetComponent<MapEventsManager>();
            }
            return mapEventsManager;
        }

        public void ClearMapEventsManager()
        {
            mapEventsManager = null;
            mapEventsDesert = null;
        }

        public void SetWorldSize(Vector3 size)
        {
            WorldSize = size;
        }

        public Vector3 GetWorldSize()
        {
            return WorldSize;
        }

        public void ResetWorldSize()
        {
            WorldSize = Vector3.zero;
            OriginalWorldSize = Vector3.zero;
        }

        public static DistanceToPlayer GetDistanceToPlayer(Vector3 position)
        {
            var player = GameManager.Instance.player;
            if (player == null)
            {
                return DistanceToPlayer.Far;
            }

            var target = player.transform.position;
            if (player.IsDead())
            {
                target = Instance.CameraSwitcher.GetCurrentTarget().position;
            }

            var distance = Vector3.Distance(position, target);
            if (distance < 25f)
            {
                return DistanceToPlayer.Close;
            }
            else if (distance < 60f)
            {
                return DistanceToPlayer.Medium;
            }
            else
            {
                return DistanceToPlayer.Far;
            }
        }

    }
}
