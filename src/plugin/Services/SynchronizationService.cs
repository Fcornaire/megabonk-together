using Actors.Enemies;
using Assets.Scripts._Data.Hats;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Camera;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Game.Spawning.New.Timelines;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using BepInEx.Logging;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using MegabonkTogether.Common.Messages;
using MegabonkTogether.Common.Messages.GameNetworkMessages;
using MegabonkTogether.Common.Models;
using MegabonkTogether.Extensions;
using MegabonkTogether.Helpers;
using MegabonkTogether.Patches;
using MegabonkTogether.Scripts.Interactables;
using MegabonkTogether.Scripts.Snapshot;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace MegabonkTogether.Services
{
    public enum GameEvent
    {
        Loading,
        Ready,
        Start,
        PortalOpened,
        FinalPortalOpened,
        GameOver,
    }

    public enum State
    {
        None,
        Loading,
        Ready,
        Started,
        LoadingNextLevel,
        Endgame,
        GameOver,
    }

    public interface ISynchronizationService
    {
        public bool IsLobbyReady();
        public void TransitionToState(GameEvent gameEvent);
        public bool? IsServerMode();
        public void OnSpawnedObject(GameObject obj);
        public void StartGame();

        public bool HasNetplaySessionStarted();
        public bool HasNetplaySessionInitialized();

        public void Reset();

        public bool IsLoading();

        public void OnSpawnedEnemy(Enemy enemy, EEnemy enemyName, Vector3 position, int waveNumber, bool forceSpawn, EEnemyFlag flag, bool canBeElite, float extraSizeMultiplier);
        public void OnSelectedCharacter();
        public void OnEnemyDied(Enemy instance, uint? ownerId = null);
        public void OnSpawnedProjectile(Il2CppObjectBase instance, uint? owner = null);
        public void OnProjectileDone(ProjectileBase instance);
        public void OnPickupOrbSpawned(EPickup ePickup, Vector3 pos);
        public void OnPickupApplied(Pickup instance);
        public void OnSpawnedChest(Vector3 position, Quaternion rotation, UnityEngine.Object obj);
        public void OnChestOpened(OpenChest instance);
        public void OnWeaponAdded(WeaponInventory instance, WeaponData weaponData, Il2CppSystem.Collections.Generic.List<StatModifier> upgradeOffer);
        public void OnInteractableUsed(BaseInteractable instance);
        public bool OnStartingToChargingShrine(uint shrineNetplayId);
        public bool OnStoppingChargingShrine(uint shrineNetplayId);
        public void OnPickupSpawned(Pickup result, EPickup ePickup, Vector3 pos, int value);
        public void OnEnemyExploder(Enemy enemy);
        public void OnEnemyDamaged(Enemy instance, DamageContainer damageContainer);
        public void OnSpawnedEnemySpecialAttack(Enemy enemy, EnemySpecialAttack attack);

        public void PrepareForNextLevel();
        public bool IsLoadingNextLevel();
        public bool OnStartingToChargingPylon(uint pylonNetplayId);
        public bool OnStoppingChargingPylon(uint pylonNetplayId);
        public void OnFinalBossOrbsSpawned(Orb orb);
        public void OnFinalBossOrbDestroyed(uint removed);
        public void OnSwarmEvent(TimelineEvent currentEvent);
        public void OnPlayerDied();
        public void OnRunStarted(RunConfig newRunConfig);
        public void OnTomeAdded(TomeInventory instance, TomeData tomeData, Il2CppSystem.Collections.Generic.List<StatModifier> upgradeOffer, ERarity rarity);
        public void OnLightningStrike(Enemy enemy, int bounces, DamageContainer dc, float bounceRange, float bounceProcCoefficient);
        public void OnTornadoesSpawned(int amount);
        public void OnStormStarted(DesertStorm instance);
        public void OnStormStopped();
        public void OnTumbleWeedSpawned(InteractableTumbleWeed instance);
        public void OnTumbleWeedDespawned(InteractableTumbleWeed instance);
        public void OnInteractableFightEnemySpawned(InteractableCharacterFight instance);
        public void OnWantToStartFollowingPickup(Pickup instance);
        public void SendPickupFollowingPlayer(uint pickupId, uint playerId);
        public void OnItemAdded(EItem item);
        public void OnItemRemoved(EItem item);
        public void OnWeaponToggled(WeaponInventory instance, EWeapon eWeapon, bool enable);
        public void OnSpawnedObjectInCrypt(GameObject obj);
        public bool OnStartingToChargingLamp(uint value);
        public bool OnStoppingChargingLamp(uint value);
        public void OnTimerStarted();
        public void OnHatChanged(EHat eHat);
        public void OnSkinSelected(SkinData skin);
        public void OnRespawn(uint ownerId, Vector3 position);
    }
    internal class SynchronizationService : ISynchronizationService
    {
        private readonly IUdpClientService udpClientService;
        private readonly IPlayerManagerService playerManagerService;
        private readonly IProjectileManagerService projectileManagerService;
        private readonly IEnemyManagerService enemyManagerService;
        private readonly IPickupManagerService pickupManagerService;
        private readonly IChestManagerService chestManagerService;
        private readonly ISpawnedObjectManagerService spawnedObjectManagerService;
        private readonly IFinalBossOrbManagerService finalBossOrbManagerService;
        private readonly IGameBalanceService gameBalanceService;
        private readonly ManualLogSource logger;
        private readonly ConcurrentBag<SpawnedObject> toSpawns = [];
        private readonly ConcurrentBag<SpawnedObjectInCrypt> toUpdate = [];
        private readonly ConcurrentDictionary<uint, ICollection<uint>> shrineChargingPlayers = new();
        private readonly ConcurrentDictionary<uint, ICollection<uint>> pylonChargingPlayers = new();
        private readonly ConcurrentDictionary<uint, ICollection<uint>> lampsChargingPlayers = [];
        private readonly List<GameObject> specificDesertGraves = [];
        private InteractableCoffin currentCoffin = null;

        private CancellationTokenSource cancellationTokenSource = new();
        private CancellationToken cancellationToken = default;

        private State currentState = State.None;

        public SynchronizationService(
            IPlayerManagerService playerManagerService,
            IEnemyManagerService enemyManagerService,
            ManualLogSource logger,
            IUdpClientService udpClientService,
            IProjectileManagerService projectileManagerService,
            IPickupManagerService pickupManagerService,
            IChestManagerService chestManagerService,
            ISpawnedObjectManagerService spawnedObjectManagerService,
            IFinalBossOrbManagerService finalBossOrbManagerService,
            IGameBalanceService gameBalanceService
            )
        {
            this.playerManagerService = playerManagerService;
            this.enemyManagerService = enemyManagerService;
            this.projectileManagerService = projectileManagerService;
            this.pickupManagerService = pickupManagerService;
            this.chestManagerService = chestManagerService;
            this.spawnedObjectManagerService = spawnedObjectManagerService;
            this.finalBossOrbManagerService = finalBossOrbManagerService;
            this.gameBalanceService = gameBalanceService;
            this.logger = logger;

            EventManager.SubscribeSpawnedObjectsEvents(OnNewObjectToSpawn);
            EventManager.SubscribePlayerUpdatesEvents(OnPlayerUpdate);
            EventManager.SubscribeSpawnedEnemyEvents(OnReceivedSpawnedEnemy);
            EventManager.SubscribeSelectedCharacterEvents(OnReceivedSelectedCharacter);
            EventManager.SubscribeEnemiesUpdateEvents(OnReceivedEnemiesUpdate);
            EventManager.SubscribeEnemyDiedEvents(OnReceivedEnemyDied);
            EventManager.SubscribeSpawnedProjectileEvents(OnReceivedSpawnedProjectile);
            EventManager.SubscribeProjectileDoneEvents(OnReceivedProjectileDone);
            EventManager.SubscribeSpawnedPickupOrbEvents(OnReceivedSpawnedOrbPickup);
            EventManager.SubscribeSpawnedPickupEvents(OnReceivedSpawnedPickup);
            EventManager.SubscribePickupAppliedEvents(OnReceivedPickupApplied);
            EventManager.SubscribePickupFollowingPlayerEvents(OnReceivedPickupFollowingPlayer);
            EventManager.SubscribeSpawnedChestEvents(OnReceivedSpawnedChest);
            EventManager.SubscribeChestOpenedEvents(OnReceivedChestOpened);
            EventManager.SubscribeWeaponAddedEvents(OnReceivedWeaponAdded);
            EventManager.SubscribeInteractableUsedEvents(OnReceivedInteractableUsed);
            EventManager.SubscribeStartingChargingShrineEvents(OnReceivedStartingToChargingShrine);
            EventManager.SubscribeStoppingChargingShrineEvents(OnReceivedStoppingChargingShrine);
            EventManager.SubscribeEnemyExploderEvents(OnReceivedEnemyExploder);
            EventManager.SubscribeEnemyDamagedEvents(OnReceivedEnemyDamaged);
            EventManager.SubscribeSpawnedEnemySpecialAttackEvents(OnReceivedSpawnedEnemySpecialAttack);
            EventManager.SubscribeStartingChargingPylonEvents(OnReceivedStartingToChargingPylon);
            EventManager.SubscribeStoppingChargingPylonEvents(OnReceivedStoppingChargingPylon);
            EventManager.SubscribeFinalBossOrbSpawnedEvents(OnReceivedFinalBossOrbsSpawned);
            EventManager.SubscribeFinalBossOrbsUpdateEvents(OnReceivedFinalBossOrbsUpdate);
            EventManager.SubscribeFinalBossOrbDestroyedEvents(OnReceivedFinalBossOrbDestroyed);
            EventManager.SubscribeStartedSwarmEventEvents(OnReceivedSwarmEvent);
            EventManager.SubscribeGameOverEvents(OnReceivedGameOver);
            EventManager.SubscribePlayerDiedEvents(OnReceivedPlayerDied);
            EventManager.SubscribeRetargetedEnemiesEvents(OnReceivedRetargetedEnemies);
            EventManager.SubscribeRunStartedEvents(OnReceivedRunStarted);
            EventManager.SubscribePlayerDisconnectedEvents(OnReceivedPlayerDisconnected);
            EventManager.SubscribeProjectilesUpdateEvents(OnReceivedProjectilesUpdate);
            EventManager.SubscribeTomeAddedEvents(OnReceivedTomeAdded);
            EventManager.SubscribeLightningStrikeEvents(OnReceivedLightningStrike);
            EventManager.SubscribeTornadoesSpawnedEvents(OnReceivedTornadoesSpawned);
            EventManager.SubscribeStormStartedEvents(OnReceivedStormStarted);
            EventManager.SubscribeStormStoppedEvents(OnReceivedStormStopped);
            EventManager.SubscribeTumbleWeedSpawnedEvents(OnReceivedTumbleWeedSpawned);
            EventManager.SubscribeTumbleWeedsUpdateEvents(OnReceivedTumbleWeedsUpdate);
            EventManager.SubscribeTumbleWeedDespawnedEvents(OnReceivedTumbleWeedDespawned);
            EventManager.SubscribeInteractableCharacterFightEnemySpawnedEvents(OnReceivedInteractableFightEnemySpawned);
            EventManager.SubscribeWantToStartFollowingPickupEvents(OnReceivedWantToStartFollowingPickup);
            EventManager.SubscribeItemAddedEvents(OnReceivedItemAdded);
            EventManager.SubscribeItemRemovedEvents(OnReceivedItemRemoved);
            EventManager.SubscribeWeaponToggledEvents(OnReceivedWeaponToggled);
            EventManager.SubscribeSpawnedObjectInCryptEvents(OnReceivedSpawnedObjectInCrypt);
            EventManager.SubscribeStartingChargingLampEvents(OnReceivedStartingToChargingLamp);
            EventManager.SubscribeStoppingChargingLampEvents(OnReceivedStoppingChargingLamp);
            EventManager.SubscribeTimerStartedEvents(OnReceivedTimerStarted);
            EventManager.SubscribeHatChangedEvents(OnReceivedHatChanged);
            EventManager.SubscribeSpawnedReviverEvents(OnReceivedSpawnedReviver);
            EventManager.SubscribePlayerRespawnedEvents(OnReceivedPlayerRespawned);

            cancellationToken = cancellationTokenSource.Token;
            this.udpClientService = udpClientService;
        }

        public bool IsLoading()
        {
            return currentState == State.Loading;
        }

        public bool IsLobbyReady()
        {
            var host = playerManagerService.GetHost();
            if (host == null)
            {
                logger.LogWarning("No host found when checking if lobby is ready.");
                return false;
            }

            var allPlayers = playerManagerService.GetAllPlayers();

            if (allPlayers.Count() < 2)
            {
                logger.LogInfo("Not enough players to start the game.");
                return false;
            }

            return playerManagerService.GetAllPlayers().All(p => p.IsReady) && udpClientService.HasAllPeersConnected();
        }

        public bool? IsServerMode()
        {
            return udpClientService.IsHost();
        }

        public void Reset()
        {
            currentState = State.None;
            toSpawns.Clear();
            toUpdate.Clear();

            shrineChargingPlayers.Clear();
            pylonChargingPlayers.Clear();
            cancellationTokenSource.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            PrepareForNextLevel();
            playerManagerService.Reset();

            Plugin.Instance.HasDungeonTimerStarted = false;
        }

        public void OnSpawnedObject(GameObject obj)
        {
            if (GameManager.Instance == null || GameManager.Instance.player == null)
            {
                return; //Game not started yet, ignore menu interactables !
            }
            var id = spawnedObjectManagerService.AddSpawnedObject(obj);
            SendSpawnedObject(id, obj);
        }

        public void OnNewObjectToSpawn(SpawnedObject toSpawn)
        {
            toSpawns.Add(toSpawn);
        }

        public void OnPlayerUpdate(PlayerUpdate playerUpdate)
        {
            if (currentState < State.Started)
            {
                return;
            }

            if (playerUpdate == null)
            {
                logger.LogWarning("Received null PlayerUpdate.");
                return;
            }

            if (playerUpdate.ConnectionId == playerManagerService.GetLocalPlayer().ConnectionId) // Ignore updates for local player
            {
                return;
            }

            var netplayer = playerManagerService.GetNetPlayerByNetplayId(playerUpdate.ConnectionId);

            if (netplayer == null)
            {
                logger.LogWarning($"NetPlayer not found for ConnectionId: {playerUpdate.ConnectionId}");
                return;
            }

            netplayer.AddUpdate(playerUpdate);

            netplayer.Inventory.playerHealth.hp = (int)playerUpdate.Hp;
            netplayer.Inventory.playerHealth.maxHp = (int)playerUpdate.MaxHp;
            //netplayer.Inventory.playerXp.xp = (int)playerUpdate.Xp;
            netplayer.Inventory.playerHealth.shield = (int)playerUpdate.Shield;
            netplayer.Inventory.playerHealth.maxShield = (int)playerUpdate.MaxShield;

            Plugin.Instance.NetPlayersDisplayer.OnUpdate(playerUpdate);
        }

        private void OnReceivedProjectilesUpdate(IEnumerable<Projectile> projectiles)
        {
            if (currentState < State.Started)
            {
                return;
            }

            if (projectiles == null || !projectiles.Any())
            {
                return;
            }

            var projectileSnapshots = new List<ProjectileSnapshot>();
            foreach (var proj in projectiles)
            {
                projectileSnapshots.Add(new ProjectileSnapshot
                {
                    Timestamp = Time.timeAsDouble,
                    Id = proj.Id,
                    Position = Quantizer.Dequantize(proj.Position),
                    Rotation = Quantizer.Dequantize(proj.FordwardVector)
                });
            }

            projectileManagerService.UpdateProjectileSnapshots(projectileSnapshots);
        }

        private bool SpawnObject(SpawnedObject toSpawn)
        {
            var prefab = Plugin.Instance.GetPrefab(toSpawn.PrefabName);
            if (prefab == null)
            {
                return false;
            }

            var spawned = HandleSpawn(prefab);

            if (spawned == null)
            {
                logger.LogWarning($"Failed to instantiate prefab: {toSpawn.PrefabName}");
                return false;
            }

            spawned.transform.position = toSpawn.Position.ToUnityVector3();
            spawned.transform.rotation = toSpawn.Rotation.ToUnityQuaternion();
            spawned.transform.localScale = toSpawn.Scale.ToUnityVector3();

            spawnedObjectManagerService.SetSpawnedObject(toSpawn.Id, spawned);

            if (toSpawn.SpecificData != null && toSpawn.SpecificData.ShadyGuyRarity >= 0)
            {
                var shadyGuy = spawned.GetComponentInChildren<InteractableShadyGuy>();
                if (shadyGuy != null)
                {
                    DynamicData.For(shadyGuy).Set("rarity", (EItemRarity)toSpawn.SpecificData.ShadyGuyRarity);
                }

                var microWave = spawned.GetComponentInChildren<InteractableMicrowave>();
                if (microWave != null)
                {
                    DynamicData.For(microWave).Set("rarity", (EItemRarity)toSpawn.SpecificData.ShadyGuyRarity);
                }
            }

            DynamicData.For(spawned).Set("netplayId", toSpawn.Id);
            return true;
        }

        private bool UpdateObject(SpawnedObjectInCrypt toUpdate)
        {
            var allPieces = RsgController.Instance.allPieces;
            var position = toUpdate.Position;

            if (toUpdate.IsCryptLeave)
            {
                spawnedObjectManagerService.SetSpawnedObject(toUpdate.NetplayId, RsgController.Instance.rsgEnd.gameObject);
                DynamicData.For(RsgController.Instance.rsgEnd.gameObject).Set("netplayId", toUpdate.NetplayId);
                return true;
            }

            foreach (var piece in allPieces)
            {
                var children = Il2CppFindHelper.RuntimeGetComponentsInChildren<Component>(piece.children);

                foreach (var child in children)
                {
                    var quantizedPos = Quantizer.Quantize(child.transform.position);
                    if (quantizedPos.QuantizedX == position.QuantizedX &&
                        quantizedPos.QuantizedY == position.QuantizedY &&
                        quantizedPos.QuantizedZ == position.QuantizedZ)
                    {
                        spawnedObjectManagerService.SetSpawnedObject(toUpdate.NetplayId, child.gameObject);
                        DynamicData.For(child.gameObject).Set("netplayId", toUpdate.NetplayId);

                        return true;
                    }
                }
            }

            return false;
        }

        private GameObject HandleSpawn(GameObject toSpawn)
        {
            if (!toSpawn.name.Contains("DesertGrave") && !toSpawn.name.Contains("SkeletonKingStatue"))
            {
                return GameObject.Instantiate(toSpawn);
            }

            if (toSpawn.name == "SkeletonKingStatue")
            {
                var skeletonKingStatue = GameObject.Instantiate(toSpawn);
                specificDesertGraves.Add(skeletonKingStatue);
                skeletonKingStatue.gameObject.SetActive(false);
                return skeletonKingStatue;
            }

            if (toSpawn.name.EndsWith("4"))
            {
                var skeletonKingStatue = specificDesertGraves.FirstOrDefault(g => g.name.Contains("SkeletonKingStatue"));
                if (skeletonKingStatue != null)
                {
                    var desertGrave4 = GameObject.Instantiate(toSpawn);
                    desertGrave4.GetComponent<InteractableDesertGrave>().nextShrine = skeletonKingStatue.GetComponent<ShrineSpawnAnimation>();
                    specificDesertGraves.Add(desertGrave4);
                    desertGrave4.gameObject.SetActive(false);
                    return desertGrave4;
                }

                return null;
            }

            if (toSpawn.name.EndsWith("3"))
            {
                var desertGrave4 = specificDesertGraves.FirstOrDefault(g => g.name.Contains("DesertGrave4"));
                if (desertGrave4 != null)
                {
                    var desertGrave3 = GameObject.Instantiate(toSpawn);
                    desertGrave3.GetComponent<InteractableDesertGrave>().nextShrine = desertGrave4.GetComponent<ShrineSpawnAnimation>();
                    specificDesertGraves.Add(desertGrave3);
                    desertGrave3.gameObject.SetActive(false);
                    return desertGrave3;
                }
                return null;
            }

            if (toSpawn.name.EndsWith("2"))
            {
                var desertGrave3 = specificDesertGraves.FirstOrDefault(g => g.name.Contains("DesertGrave3"));
                if (desertGrave3 != null)
                {
                    var desertGrave2 = GameObject.Instantiate(toSpawn);
                    desertGrave2.GetComponent<InteractableDesertGrave>().nextShrine = desertGrave3.GetComponent<ShrineSpawnAnimation>();
                    specificDesertGraves.Add(desertGrave2);
                    desertGrave2.gameObject.SetActive(false);
                    return desertGrave2;
                }
                return null;
            }

            if (toSpawn.name.EndsWith("1"))
            {
                var desertGrave2 = specificDesertGraves.FirstOrDefault(g => g.name.Contains("DesertGrave2"));
                if (desertGrave2 != null)
                {
                    var desertGrave1 = GameObject.Instantiate(toSpawn);
                    desertGrave1.GetComponent<InteractableDesertGrave>().nextShrine = desertGrave2.GetComponent<ShrineSpawnAnimation>();
                    specificDesertGraves.Add(desertGrave1);
                    return desertGrave1;
                }
                return null;
            }

            return null;
        }

        public void StartGame()
        {
            if (currentState >= State.Started)
            {
                logger.LogWarning("Game has already started.");
                return;
            }

            currentState = State.Started;
            EventManager.OnGameStarted();

            var pause = UiManager.Instance.pause; //Disable restart buttons in pause menu
            var buttons = Il2CppFindHelper.RuntimeGetComponentsInChildren<MyButton>(pause);
            foreach (var button in buttons)
            {
                if (button.name == "B_Restart") //If one day we want to disable setting : button.name == "B_Settings" 
                {
                    button.state = MyButton.EButtonState.Inactive;
                    button.RefreshState();
                }
            }

            PickupManager.Instance.xpList.maxObjects = 10000; //Increase max Xp pickup since on netplay, there are more enemies
            PickupManager.Instance.goldList.maxObjects = 10000; //Increase max Gold pickup since on netplay, there are more enemies

            var allNetPlayers = playerManagerService.GetAllPlayersExceptLocal();
            var minimapCamera = GameManager.Instance.player.minimapCamera.GetComponent<MinimapCamera>();
            foreach (var netPlayer in allNetPlayers)
            {
                Plugin.Instance.NetPlayersDisplayer.AddPlayer(netPlayer);
                var spawnedPlayer = playerManagerService.GetNetPlayerByNetplayId(netPlayer.ConnectionId);
                var playerColor = Plugin.Instance.NetPlayersDisplayer.GetPlayerColor(netPlayer.ConnectionId);
                minimapCamera.AddArrow(spawnedPlayer.Model.transform, playerColor);
            }

            gameBalanceService.Initialize();

            Plugin.Instance.PreventDeath();
        }

        private void SendSpawnedObject(uint netplayId, GameObject obj)
        {
            DynamicData.For(obj).Set("netplayId", netplayId);

            var characterFight = obj.GetComponentInChildren<InteractableCharacterFight>();
            if (characterFight != null)
            {
                DynamicData.For(characterFight.gameObject).Set("netplayId", netplayId);
            }

            var shadyGuy = obj.GetComponentInChildren<InteractableShadyGuy>();
            EItemRarity? rarity = null;
            if (shadyGuy != null)
            {
                rarity = shadyGuy.rarity;
            }

            var microWave = obj.GetComponentInChildren<InteractableMicrowave>();
            if (microWave != null)
            {
                rarity = microWave.rarity;
            }

            var prefabName = obj.name.Split('(').FirstOrDefault();

            IGameNetworkMessage message = new SpawnedObject
            {
                Id = netplayId,
                PrefabName = prefabName,
                Position = obj.transform.position.ToNumericsVector3(),
                Rotation = obj.transform.rotation.ToNumericsQuaternion(),
                Scale = obj.transform.localScale.ToNumericsVector3(),
                SpecificData = new Specific
                {
                    ShadyGuyRarity = rarity.HasValue ? (int)rarity.Value : -1
                }
            };

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableUnordered);

        }

        public void TransitionToState(GameEvent gameEvent)
        {
            switch (gameEvent)
            {
                case GameEvent.Loading:
                    PickupManager.maxXpObjects = 10000;
                    PickupManager.maxGoldObjects = 10000;
                    PickupManager.maxPowerupsOnMap = 1000;

                    currentState = State.Loading;
                    break;
                case GameEvent.Ready:
                    if (currentState == State.Ready)
                    {
                        logger.LogWarning("Client is already in Ready state.");
                        break;
                    }
                    currentState = State.Ready;

                    Plugin.Instance.HasDungeonTimerStarted = false;

                    var isServer = IsServerMode() ?? false;
                    Player player = isServer ? playerManagerService.GetHost() : playerManagerService.GetLocalPlayer();

                    logger.LogInfo($"Players in lobby: {string.Join(", ", playerManagerService.GetAllPlayers().Select(p => p.ConnectionId.ToString()))}");

                    if (player == null)
                    {
                        logger.LogWarning("no player not found when transitioning to Ready state.");
                        break;
                    }

                    player.IsReady = true;
                    playerManagerService.UpdatePlayer(player);

                    if (!isServer)
                    {
                        HandleGameEvent(gameEvent);
                    }

                    break;
                case GameEvent.Start:
                    if (currentState < State.Ready)
                    {
                        logger.LogWarning("Cannot start game when not in Ready state.");
                        break;
                    }
                    if (!IsLobbyReady())
                    {
                        logger.LogWarning("Cannot start game when lobby is not ready.");
                        break;
                    }

                    playerManagerService.SpawnPlayers();

                    StartGame();

                    if (IsServerMode() == false)
                    {
                        CoroutineRunner.Instance.Run(NewObjectToSpawnRoutine());
                        EnemyManager.Instance.summonerController.timeline.events.Clear(); //Remove all timelines events from client, will be handled by server
                    }
                    break;
                case GameEvent.PortalOpened:
                    currentState = State.LoadingNextLevel;
                    playerManagerService.ResetForNextLevel();
                    PrepareForNextLevel();
                    Plugin.Instance.ClearPrefabs();
                    Plugin.Instance.RestoreDeath(false);
                    CoroutineRunner.Instance.Stop(LevelUpScreenPatches.CurrentRoutine);
                    //CoroutineRunner.Instance.Stop(EncounterUiPatches.CurrentRoutine);
                    CoroutineRunner.Instance.Stop(ChestWindowUiPatches.CurrentRoutine);

                    EventManager.OnPortalOpened();

                    break;
                case GameEvent.FinalPortalOpened:
                    currentState = State.Endgame;
                    break;
                case GameEvent.GameOver:
                    currentState = State.GameOver;
                    Plugin.Instance.RestoreDeath(true);
                    break;
                default:
                    logger.LogWarning($"Unhandled client event: {gameEvent}");
                    break;
            }
        }

        public void PrepareForNextLevel()
        {
            spawnedObjectManagerService.ResetForNextLevel();
            enemyManagerService.ResetForNextLevel();
            projectileManagerService.ResetForNextLevel();
            pickupManagerService.ResetForNextLevel();
            chestManagerService.ResetForNextLevel();
            specificDesertGraves.Clear();
            currentCoffin = null;

            Plugin.Instance.NetPlayersDisplayer.ResetCards();
            Plugin.Instance.ClearMapEventsManager();
        }

        private void HandleGameEvent(GameEvent gameEvent)
        {
            switch (gameEvent)
            {
                case GameEvent.Ready:
                    IGameNetworkMessage message = new ClientInGameReady
                    {
                        ConnectionId = playerManagerService.GetLocalPlayer().ConnectionId
                    };
                    udpClientService.SendToHost(message);
                    break;
                case GameEvent.Start:
                    break;
                default:
                    logger.LogWarning($"Unhandled game event: {gameEvent}");
                    break;
            }
        }
        private IEnumerator NewObjectToSpawnRoutine() //TODO: add a auto cancel after X seconds
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (currentState == State.Started)
                {
                    var canSpawn = toSpawns.Count > 0;
                    var unspawnedYet = new List<SpawnedObject>();
                    while (canSpawn)
                    {
                        if (toSpawns.TryTake(out var toSpawn))
                        {
                            if (!SpawnObject(toSpawn))
                            {
                                unspawnedYet.Add(toSpawn);
                            }
                        }
                        canSpawn = toSpawns.Count > 0;
                    }

                    foreach (var item in unspawnedYet) //Add back to retry later
                    {
                        logger.LogWarning($"Retrying spawn for object: {item.PrefabName}");
                        toSpawns.Add(item);
                    }

                    var canUpdate = toUpdate.Count > 0;
                    var unupdatedYet = new List<SpawnedObjectInCrypt>();
                    while (canUpdate)
                    {
                        if (toUpdate.TryTake(out var toUpd))
                        {
                            var success = UpdateObject(toUpd);
                            if (!success)
                            {
                                unupdatedYet.Add(toUpd);
                            }
                        }
                        canUpdate = toUpdate.Count > 0;
                    }

                    foreach (var item in unupdatedYet) //Add back to retry later
                    {
                        logger.LogWarning($"Retrying update for object in crypt at position: x:{item.Position.QuantizedX} y:{item.Position.QuantizedY} z:{item.Position.QuantizedZ}");
                        toUpdate.Add(item);
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }
        }

        public bool HasNetplaySessionStarted()
        {
            return currentState == State.Started;
        }

        public bool HasNetplaySessionInitialized()
        {
            return Plugin.Instance.NetworkHandler.HasFoundMatch.HasValue && Plugin.Instance.NetworkHandler.HasFoundMatch.Value;
        }

        public void OnSpawnedEnemy(Enemy enemy, EEnemy enemyName, Vector3 position, int waveNumber, bool forceSpawn, EEnemyFlag flag, bool canBeElite, float extraSizeMultiplier)
        {
            if (enemy == null)
            {
                logger.LogWarning("Enemy is null ?? when processing OnSpawnedEnemy.");
                return;
            }

            var dynEnemy = DynamicData.For(enemy);
            var targetId = dynEnemy.Get<uint?>("targetId");

            if (!targetId.HasValue)
            {
                logger.LogWarning("Enemy targetId not found in DynamicData when processing OnSpawnedEnemy.");
                EnemyManager.Instance.RemoveEnemy(enemy);
            }

            var netplayId = enemyManagerService.AddSpawnedEnemy(enemy);

            IGameNetworkMessage message = new SpawnedEnemy
            {
                Flag = enemy.IsElite() ? (int)EEnemyFlag.Elite : (int)flag,
                Id = netplayId,
                TargetId = targetId.Value,
                Name = (int)enemyName,
                ShouldForce = forceSpawn,
                Position = position.ToNumericsVector3(),
                Wave = waveNumber,
                CanBeElite = enemy.IsElite(),
                Hp = enemy.hp,
                ExtraSizeMultiplier = extraSizeMultiplier,
                ReviverId = Plugin.Instance.CurrentReviver
            };

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableUnordered);
        }

        private void OnReceivedSpawnedEnemy(SpawnedEnemy spawnedEnemy)
        {
            if (!HasNetplaySessionStarted())
            {
                return;
            }

            var enemy = EnemyManager.Instance.SpawnEnemy
                (DataManager.Instance.GetEnemyData((EEnemy)spawnedEnemy.Name),
                spawnedEnemy.Position.ToUnityVector3(),
                spawnedEnemy.Wave,
                true,
                (EEnemyFlag)spawnedEnemy.Flag,
                spawnedEnemy.CanBeElite,
                spawnedEnemy.ExtraSizeMultiplier
            );

            var extraMaterial = spawnedObjectManagerService.GetExtraEnemyMaterial(enemy.enemyData.enemyName);
            if (extraMaterial != null)
            {
                var original = enemy.renderer.sharedMaterial;
                var clone = new Material(extraMaterial);

                Il2CppFindHelper.RuntimeSetSharedMaterials(enemy.renderer, [original, clone]);
            }

            if (spawnedEnemy.ReviverId.HasValue)
            {
                var reviver = spawnedObjectManagerService.GetSpawnedObject(spawnedEnemy.ReviverId.Value).GetComponent<InteractableReviver>();
                reviver?.SetSpawnedEnemy(enemy);
                enemyManagerService.AddReviverEnemy_Name(enemy, reviver.GetFullName());
            }

            enemy.hp = spawnedEnemy.Hp;
            enemy.controlHp = spawnedEnemy.Hp;
            enemy.maxHp = spawnedEnemy.Hp;
            enemy._hp_k__BackingField = spawnedEnemy.Hp;

            if (enemy.IsFinalBoss())
            {
                MusicController.Instance.finalFightController.boss = enemy;
                MusicController.Instance.finalFightController.numWeaponsToTake = GameManager.Instance.player.inventory.weaponInventory.GetNumWeapons();
                MusicController.Instance.finalFightController.takeWeaponAtTime = MyTime.time + 4.0f;
            }

            if (MapController.currentMap.eMap == Assets.Scripts._Data.MapsAndStages.EMap.Desert) //TODO to test
            {

                if (enemy.enemyData.enemyName == EEnemy.Ghost)
                {
                    InteractableDesertGrave grave = specificDesertGraves.FirstOrDefault(go => go.name.Contains("DesertGrave1"))?.GetComponent<InteractableDesertGrave>();
                    grave?.myEnemy = enemy;
                }

                if (enemy.enemyData.enemyName == EEnemy.GreaterGhost)
                {
                    InteractableDesertGrave grave = specificDesertGraves.FirstOrDefault(go => go.name.Contains("DesertGrave2"))?.GetComponent<InteractableDesertGrave>();
                    grave?.myEnemy = enemy;
                }

                if (enemy.enemyData.enemyName == EEnemy.GhostPurple)
                {
                    InteractableDesertGrave grave = specificDesertGraves.FirstOrDefault(go => go.name.Contains("DesertGrave3"))?.GetComponent<InteractableDesertGrave>();
                    grave?.myEnemy = enemy;
                }

                if (enemy.enemyData.enemyName == EEnemy.GhostRed)
                {
                    InteractableDesertGrave grave = specificDesertGraves.FirstOrDefault(go => go.name.Contains("DesertGrave4"))?.GetComponent<InteractableDesertGrave>();
                    grave?.myEnemy = enemy;
                }

                if (enemy.enemyData.enemyName == EEnemy.CalciumDad)
                {
                    InteractableSkeletonKingStatue skeletonStatue = specificDesertGraves.FirstOrDefault(go => go.name.Contains("SkeletonKingStatue"))?.GetComponent<InteractableSkeletonKingStatue>();
                    if (skeletonStatue == null)
                    {
                        logger.LogWarning("SkeletonKingStatue not found for CalciumDad enemy.");
                    }
                    skeletonStatue?.myEnemy = enemy;
                }
            }

            if (MapController.currentMap.eMap == Assets.Scripts._Data.MapsAndStages.EMap.Graveyard) //TODO to test
            {
                if (enemy.enemyData.enemyName == EEnemy.GhostGrave1
                || enemy.enemyData.enemyName == EEnemy.GhostGrave2
                || enemy.enemyData.enemyName == EEnemy.GhostGrave3
                || enemy.enemyData.enemyName == EEnemy.GhostGrave4)
                {
                    currentCoffin.minibossEnemies.Add(enemy);
                }

                if (enemy.enemyData.enemyName == EEnemy.GhostKing)
                {
                    RsgController.Instance.roomBoss.bossEnemy = enemy;
                    RsgController.Instance.roomBoss.RefreshBossArmor();
                    UiManager.Instance.objective.OnBossSpawned();
                }
            }

            var interpolator = enemy.gameObject.AddComponent<EnemyInterpolator>();
            interpolator.Initialize(enemy);
            enemyManagerService.SetSpawnedEnemy(spawnedEnemy.Id, enemy);

            DynamicData.For(enemy).Set("targetId", spawnedEnemy.TargetId);
        }

        public void OnSpawnedProjectile(Il2CppObjectBase proj, uint? owner = null)
        {
            var instance = IL2CPP.PointerToValueGeneric<ProjectileBase>(proj.Pointer, false, false);

            var netplayId = projectileManagerService.AddSpawnedProjectile(instance);
            var ownerId = owner ?? playerManagerService.GetLocalPlayer().ConnectionId;

            IGameNetworkMessage message;

            switch (instance.weaponBase.weaponData.eWeapon)
            {
                case EWeapon.Shotgun:
                    var shotgun = IL2CPP.PointerToValueGeneric<ProjectileShotgun>(proj.Pointer, false, false);
                    var muzzle = shotgun.weaponAttack.muzzle;
                    message = new SpawnedShotgunProjectile
                    {
                        Id = netplayId,
                        OwnerId = ownerId,
                        Rotation = Quantizer.Quantize(instance.transform.eulerAngles),
                        Position = Quantizer.Quantize(instance.transform.position),
                        Weapon = (int)instance.weaponBase.weaponData.eWeapon,
                        MuzzlePosition = Quantizer.Quantize(muzzle.transform.position),
                        MuzzleRotation = Quantizer.Quantize(muzzle.transform.eulerAngles)
                    };
                    break;
                case EWeapon.Revolver:
                    var revolver = IL2CPP.PointerToValueGeneric<ProjectileBasic>(proj.Pointer, false, false);
                    message = new SpawnedRevolverProjectile
                    {
                        Id = netplayId,
                        OwnerId = ownerId,
                        Rotation = Quantizer.Quantize(instance.transform.eulerAngles),
                        Position = Quantizer.Quantize(instance.transform.position),
                        Weapon = (int)instance.weaponBase.weaponData.eWeapon,
                        MuzzlePosition = Quantizer.Quantize(revolver.weaponAttack.muzzle.transform.position),
                        MuzzleRotation = Quantizer.Quantize(revolver.weaponAttack.muzzle.transform.eulerAngles)
                    };
                    break;
                case EWeapon.Axe:
                    var axe = IL2CPP.PointerToValueGeneric<ProjectileAxe>(proj.Pointer, false, false);
                    message = new SpawnedAxeProjectile
                    {
                        Id = netplayId,
                        OwnerId = ownerId,
                        Rotation = Quantizer.Quantize(instance.transform.eulerAngles),
                        Position = Quantizer.Quantize(instance.transform.position),
                        Weapon = (int)instance.weaponBase.weaponData.eWeapon,
                        StartPosition = Quantizer.Quantize(axe.startPosition),
                        DesiredPosition = Quantizer.Quantize(axe.desiredPosition),
                    };
                    break;
                case EWeapon.BlackHole:
                    var blackHole = IL2CPP.PointerToValueGeneric<ProjectileBlackHole>(proj.Pointer, false, false);
                    message = new SpawnedBlackHoleProjectile
                    {
                        Id = netplayId,
                        OwnerId = ownerId,
                        Rotation = Quantizer.Quantize(instance.transform.eulerAngles),
                        Position = Quantizer.Quantize(instance.transform.position),
                        Weapon = (int)instance.weaponBase.weaponData.eWeapon,
                        StartPosition = Quantizer.Quantize(blackHole.startPosition),
                        DesiredPosition = Quantizer.Quantize(blackHole.desiredPosition),
                        StartScaleSize = Quantizer.Quantize(blackHole.startScaleSize)
                    };
                    break;
                case EWeapon.CorruptSword:
                    var cringeSword = IL2CPP.PointerToValueGeneric<ProjectileCringeSword>(proj.Pointer, false, false);
                    message = new SpawnedCringeSwordProjectile
                    {
                        Id = netplayId,
                        OwnerId = ownerId,
                        Rotation = Quantizer.Quantize(instance.transform.eulerAngles),
                        Position = Quantizer.Quantize(instance.transform.position),
                        Weapon = (int)instance.weaponBase.weaponData.eWeapon,
                        MovingProjectilePosition = Quantizer.Quantize(cringeSword.movingProjectilePosition),
                        MovingProjectileRotation = Quantizer.Quantize(cringeSword.movingProjectileRotation)
                    };
                    break;
                case EWeapon.Flamewalker:
                    var flameWalker = IL2CPP.PointerToValueGeneric<ProjectileFirefield>(proj.Pointer, false, false);
                    message = new SpawnedFireFieldProjectile
                    {
                        Id = netplayId,
                        OwnerId = ownerId,
                        Rotation = Quantizer.Quantize(instance.transform.eulerAngles),
                        Position = Quantizer.Quantize(instance.transform.position),
                        Weapon = (int)instance.weaponBase.weaponData.eWeapon,
                        ExpirationTime = flameWalker.expirationTime
                    };
                    break;
                case EWeapon.HeroSword:
                    var heroSword = IL2CPP.PointerToValueGeneric<ProjectileHeroSword>(proj.Pointer, false, false);
                    message = new SpawnedHeroSwordProjectile
                    {
                        Id = netplayId,
                        OwnerId = ownerId,
                        Rotation = Quantizer.Quantize(instance.transform.eulerAngles),
                        Position = Quantizer.Quantize(instance.transform.position),
                        Weapon = (int)instance.weaponBase.weaponData.eWeapon,
                        MovingProjectilePosition = Quantizer.Quantize(heroSword.movingProjectilePosition),
                        MovingProjectileRotation = Quantizer.Quantize(heroSword.movingProjectileRotation)
                    };
                    break;
                case EWeapon.Rockets:
                    var rocketProj = IL2CPP.PointerToValueGeneric<ProjectileRocket>(proj.Pointer, false, false);
                    message = new SpawnedRocketProjectile
                    {
                        Id = netplayId,
                        OwnerId = ownerId,
                        Rotation = Quantizer.Quantize(instance.transform.eulerAngles),
                        Position = Quantizer.Quantize(instance.transform.position),
                        Weapon = (int)instance.weaponBase.weaponData.eWeapon,
                        RocketPosition = Quantizer.Quantize(rocketProj.rocket.transform.position),
                        RocketRotation = Quantizer.Quantize(rocketProj.rocket.transform.eulerAngles)
                    };
                    break;
                case EWeapon.Dexecutioner:
                    var dexecutionerProj = IL2CPP.PointerToValueGeneric<ProjectileDexecutioner>(proj.Pointer, false, false);
                    message = new SpawnedDexecutionerProjectile
                    {
                        Id = netplayId,
                        OwnerId = ownerId,
                        Rotation = Quantizer.Quantize(instance.transform.eulerAngles),
                        Position = Quantizer.Quantize(instance.transform.position),
                        Weapon = (int)instance.weaponBase.weaponData.eWeapon,
                        AttackDir = Quantizer.Quantize(dexecutionerProj.attackDir),
                        Chance = dexecutionerProj.executionChance,
                        ForwardOffset = dexecutionerProj.forwardOffset,
                        ProjectileDistance = dexecutionerProj.projectileDistance,
                        UpOffset = dexecutionerProj.upOffset,
                        UseAudio = dexecutionerProj.useAudio
                    };
                    break;
                case EWeapon.Sniper:
                    var sniperProj = IL2CPP.PointerToValueGeneric<ProjectileSniper>(proj.Pointer, false, false);
                    message = new SpawnedSniperProjectile
                    {
                        Id = netplayId,
                        OwnerId = ownerId,
                        Rotation = Quantizer.Quantize(instance.transform.eulerAngles),
                        Position = Quantizer.Quantize(instance.transform.position),
                        Weapon = (int)instance.weaponBase.weaponData.eWeapon,
                        MuzzlePosition = Quantizer.Quantize(sniperProj.weaponAttack.muzzle.transform.position),
                        MuzzleRotation = Quantizer.Quantize(sniperProj.weaponAttack.muzzle.transform.eulerAngles),
                    };
                    break;
                default:
                    message = new SpawnedProjectile
                    {
                        Id = netplayId,
                        OwnerId = ownerId,
                        Rotation = Quantizer.Quantize(instance.transform.eulerAngles),
                        Position = Quantizer.Quantize(instance.transform.position),
                        Weapon = (int)instance.weaponBase.weaponData.eWeapon
                    };
                    break;
            }

            DynamicData.For(instance).Set("netplayId", netplayId);
            DynamicData.For(instance).Set("ownerId", ownerId);

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableUnordered);
        }

        private void OnReceivedSpawnedProjectile(AbstractSpawnedProjectile projectile)
        {
            try
            {
                PlayerInventory owner =
                    playerManagerService.IsLocalConnectionId(projectile.OwnerId) ?
                        GameManager.Instance.player.inventory :
                        playerManagerService.GetNetPlayerByNetplayId(projectile.OwnerId).Inventory;

                var weapons = owner.weaponInventory.weapons;
                var eweapon = (EWeapon)projectile.Weapon;
                if (weapons.TryGetValue(eweapon, out var weapon))
                {
                    var attack = weapon.weaponData.attack.GetComponent<WeaponAttack>();
                    attack.weaponBase = weapon;
                    var proj = GameObject.Instantiate(attack.prefabProjectile);

                    PoolHelper.EnsureWeaponPoolExists(eweapon);

                    switch ((EWeapon)projectile.Weapon)
                    {
                        case EWeapon.Axe:
                            var axeProjectile = projectile as SpawnedAxeProjectile;
                            var axeProjectileInstance = proj.GetComponent<ProjectileAxe>();
                            axeProjectileInstance.startPosition = Quantizer.Dequantize(axeProjectile.StartPosition);
                            axeProjectileInstance.desiredPosition = Quantizer.Dequantize(axeProjectile.DesiredPosition);
                            axeProjectileInstance.weaponBase = weapon;
                            axeProjectileInstance.weaponAttack = attack;
                            axeProjectileInstance.hitEnemies = new();

                            axeProjectileInstance.transform.position = Quantizer.Dequantize(projectile.Position);
                            axeProjectileInstance.transform.eulerAngles = Quantizer.Dequantize(projectile.Rotation);
                            DynamicData.For(axeProjectileInstance).Set("netplayId", projectile.Id);
                            DynamicData.For(axeProjectileInstance).Set("ownerId", projectile.OwnerId);
                            break;
                        case EWeapon.BlackHole:
                            var blackHoleProjectile = projectile as SpawnedBlackHoleProjectile;
                            var blackHoleProjectileInstance = proj.GetComponent<ProjectileBlackHole>();
                            blackHoleProjectileInstance.startPosition = Quantizer.Dequantize(blackHoleProjectile.StartPosition);
                            blackHoleProjectileInstance.desiredPosition = Quantizer.Dequantize(blackHoleProjectile.DesiredPosition);
                            blackHoleProjectileInstance.weaponBase = weapon;
                            blackHoleProjectileInstance.weaponAttack = attack;
                            blackHoleProjectileInstance.hitEnemies = new();
                            blackHoleProjectileInstance.transform.position = Quantizer.Dequantize(projectile.Position);
                            blackHoleProjectileInstance.transform.eulerAngles = Quantizer.Dequantize(projectile.Rotation);
                            blackHoleProjectileInstance.startScaleSize = Quantizer.Dequantize(blackHoleProjectile.StartScaleSize);

                            DynamicData.For(blackHoleProjectileInstance).Set("netplayId", projectile.Id);
                            DynamicData.For(blackHoleProjectileInstance).Set("ownerId", projectile.OwnerId);
                            break;
                        case EWeapon.CorruptSword:
                            var cringeSwordProjectile = projectile as SpawnedCringeSwordProjectile;
                            var cringeSwordProjectileInstance = proj.GetComponent<ProjectileCringeSword>();
                            cringeSwordProjectileInstance.weaponBase = weapon;
                            cringeSwordProjectileInstance.weaponAttack = attack;
                            cringeSwordProjectileInstance.hitEnemies = new();
                            cringeSwordProjectileInstance.transform.position = Quantizer.Dequantize(projectile.Position);
                            cringeSwordProjectileInstance.transform.eulerAngles = Quantizer.Dequantize(projectile.Rotation);
                            cringeSwordProjectileInstance.movingProjectile.transform.position = Quantizer.Dequantize(cringeSwordProjectile.MovingProjectilePosition);
                            cringeSwordProjectileInstance.movingProjectile.transform.rotation = Quantizer.Dequantize(cringeSwordProjectile.MovingProjectileRotation);
                            DynamicData.For(cringeSwordProjectileInstance).Set("netplayId", projectile.Id);
                            DynamicData.For(cringeSwordProjectileInstance).Set("ownerId", projectile.OwnerId);
                            break;
                        case EWeapon.Flamewalker:
                            var fireFieldProjectile = projectile as SpawnedFireFieldProjectile;
                            var fireFieldProjectileInstance = proj.GetComponent<ProjectileFirefield>();
                            fireFieldProjectileInstance.expirationTime = fireFieldProjectile.ExpirationTime;
                            fireFieldProjectileInstance.weaponBase = weapon;
                            fireFieldProjectileInstance.weaponAttack = attack;
                            fireFieldProjectileInstance.hitEnemies = new();
                            fireFieldProjectileInstance.transform.position = Quantizer.Dequantize(projectile.Position);
                            fireFieldProjectileInstance.transform.eulerAngles = Quantizer.Dequantize(projectile.Rotation);
                            fireFieldProjectileInstance.TryInit(0);
                            DynamicData.For(fireFieldProjectileInstance).Set("netplayId", projectile.Id);
                            DynamicData.For(fireFieldProjectileInstance).Set("ownerId", projectile.OwnerId);
                            break;
                        case EWeapon.HeroSword:
                            var heroSwordProjectile = projectile as SpawnedHeroSwordProjectile;
                            var heroSwordProjectileInstance = proj.GetComponent<ProjectileHeroSword>();
                            heroSwordProjectileInstance.weaponBase = weapon;
                            heroSwordProjectileInstance.weaponAttack = attack;
                            heroSwordProjectileInstance.hitEnemies = new();
                            heroSwordProjectileInstance.transform.position = Quantizer.Dequantize(projectile.Position);
                            heroSwordProjectileInstance.transform.eulerAngles = Quantizer.Dequantize(projectile.Rotation);
                            heroSwordProjectileInstance.movingProjectile.transform.position = Quantizer.Dequantize(heroSwordProjectile.MovingProjectilePosition);
                            heroSwordProjectileInstance.movingProjectile.transform.rotation = Quantizer.Dequantize(heroSwordProjectile.MovingProjectileRotation);
                            DynamicData.For(heroSwordProjectileInstance).Set("netplayId", projectile.Id);
                            DynamicData.For(heroSwordProjectileInstance).Set("ownerId", projectile.OwnerId);
                            break;
                        case EWeapon.Rockets:
                            var rocketProjectile = projectile as SpawnedRocketProjectile;
                            var rocketProjectileInstance = proj.GetComponent<ProjectileRocket>();
                            rocketProjectileInstance.weaponBase = weapon;
                            rocketProjectileInstance.weaponAttack = attack;
                            rocketProjectileInstance.hitEnemies = new();
                            rocketProjectileInstance.transform.position = Quantizer.Dequantize(projectile.Position);
                            rocketProjectileInstance.transform.eulerAngles = Quantizer.Dequantize(projectile.Rotation);
                            rocketProjectileInstance.rocket.transform.position = Quantizer.Dequantize(rocketProjectile.RocketPosition);
                            rocketProjectileInstance.rocket.transform.eulerAngles = Quantizer.Dequantize(rocketProjectile.RocketRotation);
                            DynamicData.For(rocketProjectileInstance).Set("netplayId", projectile.Id);
                            DynamicData.For(rocketProjectileInstance.rocket).Set("ownerId", projectile.OwnerId);
                            break;
                        case EWeapon.Shotgun:
                            var message = projectile as SpawnedShotgunProjectile;
                            var projectileShotgun = proj.GetComponent<ProjectileShotgun>();
                            projectileShotgun.weaponBase = weapon;
                            projectileShotgun.weaponAttack = attack;
                            projectileShotgun.hitEnemies = new();
                            projectileShotgun.TryInit(0);
                            projectileShotgun.psBullets.transform.position = Quantizer.Dequantize(projectile.Position);
                            projectileShotgun.psBullets.transform.eulerAngles = Quantizer.Dequantize(projectile.Rotation);
                            DynamicData.For(projectileShotgun).Set("netplayId", projectile.Id);
                            DynamicData.For(projectileShotgun).Set("ownerId", projectile.OwnerId);

                            if (attack.prefabMuzzle != null)
                            {
                                var muzzle = GameObject.Instantiate(attack.prefabMuzzle);
                                AttackMuzzle attMuzzle = muzzle.GetComponent<AttackMuzzle>();
                                attMuzzle.transform.position = Quantizer.Dequantize(message.MuzzlePosition);
                                attMuzzle.transform.eulerAngles = Quantizer.Dequantize(message.MuzzleRotation);
                                attMuzzle.Set(1, WeaponUtility.GetBurstInterval(weapon));
                                attMuzzle.Play();

                                RandomSfx muzzleSfx = muzzle.GetComponent<RandomSfx>();
                                if (muzzleSfx != null)
                                {
                                    muzzleSfx.Play();
                                }
                            }
                            break;
                        case EWeapon.Sword:
                            var swordProjectileInstance = proj.GetComponent<ProjectileMelee>();
                            swordProjectileInstance.weaponBase = weapon;
                            swordProjectileInstance.weaponAttack = attack;
                            swordProjectileInstance.hitEnemies = new();
                            swordProjectileInstance.transform.eulerAngles = Quantizer.Dequantize(projectile.Rotation);
                            DynamicData.For(swordProjectileInstance).Set("netplayId", projectile.Id);
                            DynamicData.For(swordProjectileInstance).Set("ownerId", projectile.OwnerId);
                            break;
                        case EWeapon.Dexecutioner:
                            var dexecutionerProjectile = projectile as SpawnedDexecutionerProjectile;
                            var dexecutionerProjectileInstance = proj.GetComponent<ProjectileDexecutioner>();
                            dexecutionerProjectileInstance.attackDir = Quantizer.Dequantize(dexecutionerProjectile.AttackDir);
                            dexecutionerProjectileInstance.executionChance = dexecutionerProjectile.Chance;
                            dexecutionerProjectileInstance.forwardOffset = dexecutionerProjectile.ForwardOffset;
                            dexecutionerProjectileInstance.projectileDistance = dexecutionerProjectile.ProjectileDistance;
                            dexecutionerProjectileInstance.useAudio = dexecutionerProjectile.UseAudio;
                            dexecutionerProjectileInstance.weaponBase = weapon;
                            dexecutionerProjectileInstance.weaponAttack = attack;
                            dexecutionerProjectileInstance.hitEnemies = new();
                            dexecutionerProjectileInstance.transform.position = Quantizer.Dequantize(projectile.Position);
                            dexecutionerProjectileInstance.transform.eulerAngles = Quantizer.Dequantize(projectile.Rotation);
                            DynamicData.For(dexecutionerProjectileInstance).Set("netplayId", projectile.Id);
                            DynamicData.For(dexecutionerProjectileInstance).Set("ownerId", projectile.OwnerId);
                            break;
                        case EWeapon.Bananarang:
                            var bananarangProjectileInstance = proj.GetComponent<ProjectileBanana>();
                            bananarangProjectileInstance.weaponBase = weapon;
                            bananarangProjectileInstance.weaponAttack = attack;
                            bananarangProjectileInstance.hitEnemies = new();
                            bananarangProjectileInstance.transform.position = Quantizer.Dequantize(projectile.Position);
                            bananarangProjectileInstance.transform.eulerAngles = Quantizer.Dequantize(projectile.Rotation);
                            bananarangProjectileInstance.rb.velocity = new Vector3(10, 10, 10); //Hack to avoid staying stuck at 0,0,0
                            DynamicData.For(bananarangProjectileInstance).Set("netplayId", projectile.Id);
                            DynamicData.For(bananarangProjectileInstance).Set("ownerId", projectile.OwnerId);
                            break;
                        case EWeapon.Scythe:
                            var scytheProjectileInstance = proj.GetComponent<ProjectileScythe>();
                            scytheProjectileInstance.weaponBase = weapon;
                            scytheProjectileInstance.weaponAttack = attack;
                            scytheProjectileInstance.hitEnemies = new();
                            scytheProjectileInstance.transform.position = Quantizer.Dequantize(projectile.Position);
                            scytheProjectileInstance.transform.eulerAngles = Quantizer.Dequantize(projectile.Rotation);
                            scytheProjectileInstance.expirationTime = 5f; //Hack to avoid staying stuck at 0,0,0
                            DynamicData.For(scytheProjectileInstance).Set("netplayId", projectile.Id);
                            DynamicData.For(scytheProjectileInstance).Set("ownerId", projectile.OwnerId);
                            break;
                        case EWeapon.Revolver:
                            var messageRevolver = projectile as SpawnedRevolverProjectile;
                            var revolverProjectileInstance = proj.GetComponent<ProjectileBasic>();
                            revolverProjectileInstance.weaponBase = weapon;
                            revolverProjectileInstance.weaponAttack = attack;
                            revolverProjectileInstance.hitEnemies = new();
                            revolverProjectileInstance.transform.position = Quantizer.Dequantize(projectile.Position);
                            revolverProjectileInstance.transform.eulerAngles = Quantizer.Dequantize(projectile.Rotation);
                            DynamicData.For(revolverProjectileInstance).Set("netplayId", projectile.Id);
                            DynamicData.For(revolverProjectileInstance).Set("ownerId", projectile.OwnerId);

                            if (attack.prefabMuzzle != null)
                            {
                                var muzzle = GameObject.Instantiate(attack.prefabMuzzle);
                                AttackMuzzle attMuzzle = muzzle.GetComponent<AttackMuzzle>();
                                attMuzzle.transform.position = Quantizer.Dequantize(messageRevolver.MuzzlePosition);
                                attMuzzle.transform.eulerAngles = Quantizer.Dequantize(messageRevolver.MuzzleRotation);
                                attMuzzle.Set(1, WeaponUtility.GetBurstInterval(weapon));
                                attMuzzle.Play();

                                RandomSfx muzzleSfx = muzzle.GetComponent<RandomSfx>();
                                if (muzzleSfx != null)
                                {
                                    muzzleSfx.Play();
                                }
                            }
                            break;
                        case EWeapon.Sniper:
                            var messageSniper = projectile as SpawnedSniperProjectile;
                            var sniperProjectileInstance = proj.GetComponent<ProjectileSniper>();
                            sniperProjectileInstance.weaponBase = weapon;
                            sniperProjectileInstance.weaponAttack = attack;
                            sniperProjectileInstance.hitEnemies = new();
                            sniperProjectileInstance.transform.position = Quantizer.Dequantize(projectile.Position);
                            sniperProjectileInstance.transform.eulerAngles = Quantizer.Dequantize(projectile.Rotation);
                            DynamicData.For(sniperProjectileInstance).Set("netplayId", projectile.Id);
                            DynamicData.For(sniperProjectileInstance).Set("ownerId", projectile.OwnerId);
                            if (attack.prefabMuzzle != null)
                            {
                                var muzzle = GameObject.Instantiate(attack.prefabMuzzle);
                                AttackMuzzle attMuzzle = muzzle.GetComponent<AttackMuzzle>();
                                attMuzzle.transform.position = Quantizer.Dequantize(messageSniper.MuzzlePosition);
                                attMuzzle.transform.eulerAngles = Quantizer.Dequantize(messageSniper.MuzzleRotation);
                                attMuzzle.Set(1, WeaponUtility.GetBurstInterval(weapon));
                                attMuzzle.Play();

                                RandomSfx muzzleSfx = muzzle.GetComponent<RandomSfx>();
                                if (muzzleSfx != null)
                                {
                                    muzzleSfx.Play();
                                }
                            }
                            break;

                        default:
                            var projectileBase = proj.GetComponent<ProjectileBase>();
                            projectileBase.weaponBase = weapon;
                            projectileBase.weaponAttack = attack;
                            projectileBase.hitEnemies = new();

                            projectileBase.transform.position = Quantizer.Dequantize(projectile.Position);
                            projectileBase.transform.eulerAngles = Quantizer.Dequantize(projectile.Rotation);
                            DynamicData.For(projectileBase).Set("netplayId", projectile.Id);
                            DynamicData.For(projectileBase).Set("ownerId", projectile.OwnerId);
                            break;
                    }

                    if (attack.prefabMuzzle == null)
                    {
                        RandomSfx sfx = proj.GetComponentInChildren<RandomSfx>();
                        if (sfx != null)
                        {
                            sfx.Play();
                        }
                    }


                    projectileManagerService.RegisterProjectileForInterpolation(projectile.Id, proj);

                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error in OnReceivedSpawnedProjectile: {ex}");
            }
        }


        public void OnSelectedCharacter()
        {
            ECharacter character = CharacterMenu.selectedCharacter;

            var localPlayer = playerManagerService.GetLocalPlayer();
            localPlayer.Character = (uint)character;
            playerManagerService.UpdatePlayer(localPlayer);

            var isHost = IsServerMode() ?? false;

            IGameNetworkMessage message = new SelectedCharacter
            {
                ConnectionId = localPlayer.ConnectionId,
                Skin = localPlayer.Skin,
                Character = (uint)character
            };

            if (!isHost)
            {
                udpClientService.SendToHost(message);
            }
            else
            {
                playerManagerService.OnSelectedCharacterSet();
                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableUnordered);
            }
        }

        private void OnReceivedSelectedCharacter(SelectedCharacter character)
        {
            var localPlayer = playerManagerService.GetLocalPlayer();
            if (localPlayer.ConnectionId == character.ConnectionId)
            {
                return;
            }

            var player = playerManagerService.GetPlayer(character.ConnectionId);
            if (player == null)
            {
                logger.LogWarning($"Player not found for ConnectionId: {character.ConnectionId}");
                return;
            }

            player.Character = character.Character;
            player.Skin = character.Skin;
            playerManagerService.UpdatePlayer(player);
        }

        private void OnReceivedEnemiesUpdate(IEnumerable<EnemyModel> enemiesModel)
        {
            foreach (var enemyModel in enemiesModel)
            {
                var enemy = enemyManagerService.GetEnemyById(enemyModel.Id);
                if (enemy == null)
                {
                    continue;
                }

                var interpolator = enemy.GetComponent<EnemyInterpolator>();
                if (interpolator == null)
                {
                    continue;
                }

                var snapshot = enemyModel.ToSnapshot(Time.timeAsDouble);

                interpolator.AddSnapshot(snapshot);
            }
        }

        public void OnEnemyDamaged(Enemy instance, DamageContainer damageContainer)
        {
            var enemySpawned = enemyManagerService.GetEnemyByReference(instance);
            if (enemySpawned.Value == null)
            {
                return; //Might already be dead
            }

            IGameNetworkMessage message = new EnemyDamaged
            {
                EnemyId = enemySpawned.Key,
                Damage = damageContainer.damage,
                DamageEffect = (int)damageContainer.damageEffect,
                DamageBlockedByArmor = damageContainer.damageBlockedByArmor,
                DamageSource = damageContainer.damageSource,
                DamageIsCrit = damageContainer.crit,
                DamageProcCoefficient = damageContainer.procCoefficient,
                DamageElement = (int)damageContainer.element,
                DamageFlags = (int)damageContainer.flags,
                DamageKnockback = damageContainer.knockback,
                AttackerId = playerManagerService.GetLocalPlayer().ConnectionId
            };

            var isHost = IsServerMode() ?? false;
            if (isHost)
            {
                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.Unreliable); //TODO: Can be unreliable i think ?
            }
            else
            {
                udpClientService.SendToHost(message);
            }
        }

        private void OnReceivedEnemyDamaged(EnemyDamaged damaged)
        {
            var enemy = enemyManagerService.GetEnemyById(damaged.EnemyId);
            if (enemy == null)
            {
                return; //Might already be dead
            }

            var damageContainer = new DamageContainer(damaged.DamageProcCoefficient, damaged.DamageSource)
            {
                damage = damaged.Damage,
                damageEffect = (EDamageEffect)damaged.DamageEffect,
                damageBlockedByArmor = damaged.DamageBlockedByArmor,
                crit = damaged.DamageIsCrit,
                element = (EElement)damaged.DamageElement,
                flags = (DcFlags)damaged.DamageFlags,
                knockback = damaged.DamageKnockback
            };

            Plugin.Instance.CAN_DAMAGE_ENEMIES = true;
            playerManagerService.AddGetNetplayerPositionRequest(damaged.AttackerId);
            enemy.Damage(damageContainer);
            playerManagerService.UnqueueNetplayerPositionRequest();
            Plugin.Instance.CAN_DAMAGE_ENEMIES = false;
        }


        public void OnEnemyDied(Enemy enemy, uint? diedByOwnerId = null)
        {
            var enemySpawned = enemyManagerService.GetEnemyByReference(enemy);
            if (enemySpawned.Value == null)
            {
                logger.LogWarning("Enemy not found in EnemyManagerService when processing OnEnemyDied.");
                return;
            }

            enemyManagerService.RemoveEnemyById(enemySpawned.Key);

            IGameNetworkMessage message = new EnemyDied
            {
                EnemyId = enemySpawned.Key,
                DiedByOwnerId = diedByOwnerId ?? playerManagerService.GetLocalPlayer().ConnectionId
            };

            var isHost = IsServerMode() ?? false;
            if (isHost)
            {
                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                if (enemy.IsStageBoss() && !enemy.IsFinalBoss()) //Manually invoke boss defeated event client side
                {
                    OnBossDefeated();
                }

                udpClientService.SendToHost(message);
            }
        }

        private void OnReceivedEnemyDied(EnemyDied died)
        {
            var enemy = enemyManagerService.GetEnemyById(died.EnemyId);
            if (enemy == null)
            {
                return;
            }

            var damageContainer = new DamageContainer(0.0f, ""); //TODO track dmgContainer ?
            damageContainer.damage = enemy.hp + 1;

            DynamicData.For(enemy).Set("ownerId", died.DiedByOwnerId);
            DynamicData.For(damageContainer).Set("ownerId", died.DiedByOwnerId);

            Plugin.CAN_SEND_MESSAGES = false;
            enemy.EnemyDied(damageContainer);
            Plugin.CAN_SEND_MESSAGES = true;

            var isHost = IsServerMode() ?? false;
            if (!isHost)
            {
                if (enemy.IsStageBoss() && !enemy.IsFinalBoss()) //Manually invoke boss defeated event client side
                {
                    OnBossDefeated();
                }
            }

            if (enemy.enemyData.enemyName != EEnemy.BoomerSpider) //Will be removed when exploding
            {
                enemyManagerService.RemoveEnemyById(died.EnemyId);
            }
        }

        /// <summary>
        /// Manually invoke boss defeated event client side
        /// </summary>
        private void OnBossDefeated()
        {
            logger.LogInfo("Boss defeated, activating portal.");
            var cam = GameManager.Instance.player.minimapCamera.GetComponent<MinimapCamera>();
            cam.arrowDict.Clear(); //Prevent sometimes double add for portal arrow

            var bossSpawner = spawnedObjectManagerService.GetSpecific<InteractableBossSpawner>();
            if (bossSpawner != null)
            {
                bossSpawner.portal.SetActive(true);
            }
            InteractableBossSpawner.A_BossDefeated?.Invoke(true);
        }

        public void OnProjectileDone(ProjectileBase instance)
        {
            var projectileSpawned = projectileManagerService.GetProjectileByReference(instance);
            if (projectileSpawned.Value == null)
            {
                return;
            }

            projectileManagerService.RemoveProjectileById(projectileSpawned.Key);

            IGameNetworkMessage message = new ProjectileDone
            {
                ProjectileId = projectileSpawned.Key,
                SenderConnectionId = playerManagerService.GetLocalPlayer().ConnectionId
            };

            var isHost = IsServerMode() ?? false;
            if (isHost)
            {
                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                udpClientService.SendToHost(message);
            }
        }

        private void OnReceivedProjectileDone(ProjectileDone done)
        {
            projectileManagerService.UnregisterProjectileFromInterpolation(done.ProjectileId);
        }

        public void OnPickupSpawned(Pickup pickup, EPickup ePickup, Vector3 pos, int value)
        {
            if (IsServerMode() == false)
            {
                return;
            }

            if (ePickup == EPickup.Xp)
            {
                pickup.value = gameBalanceService.GetPickupXpValue();
            }

            pickup.readyForPickupTime += 2.0f; //Attempt to compensate for network delay ?

            var netplayId = pickupManagerService.AddSpawnedPickup(pickup);

            DynamicData.For(pickup).Set("pickupId", netplayId);

            IGameNetworkMessage message = new SpawnedPickup
            {
                Id = netplayId,
                Pickup = (int)ePickup,
                Position = pos.ToNumericsVector3(),
                Value = value
            };

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        private void OnReceivedSpawnedPickup(SpawnedPickup pickup)
        {
            Plugin.CAN_SPAWN_PICKUPS = true;
            var spawnedPickup = PickupManager.Instance.SpawnPickup((EPickup)pickup.Pickup, pickup.Position.ToUnityVector3(), pickup.Value, false);
            Plugin.CAN_SPAWN_PICKUPS = false;

            if (spawnedPickup.ePickup == EPickup.Xp)
            {
                spawnedPickup.value = gameBalanceService.GetPickupXpValue();
            }

            var dynP = DynamicData.For(spawnedPickup);
            dynP.Data.Clear();

            pickupManagerService.SetSpawnedPickup(pickup.Id, spawnedPickup);
            dynP.Set("pickupId", pickup.Id);
        }

        public void OnPickupOrbSpawned(EPickup ePickup, Vector3 pos)
        {
            IGameNetworkMessage message = new SpawnedPickupOrb
            {
                Pickup = (int)ePickup,
                Position = pos.ToNumericsVector3(),
            };

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        private void OnReceivedSpawnedOrbPickup(SpawnedPickupOrb pickup)
        {
            Plugin.CAN_SPAWN_PICKUPS = true;
            EffectManager.Instance.SpawnPickupOrb((EPickup)pickup.Pickup, pickup.Position.ToUnityVector3());
            Plugin.CAN_SPAWN_PICKUPS = false;
        }

        public void OnPickupApplied(Pickup instance)
        {
            (var pickupId, var pickupSpawned) = pickupManagerService.GetPickupByReference(instance);
            if (pickupSpawned == null)
            {
                logger.LogWarning($"Pickup {pickupId} not found in PickupManagerService when processing OnPickupApplied. Deleting");
                DynamicData.For(instance).Data.Clear();
                PickupManager.Instance.DespawnPickup(instance);

                return;
            }

            pickupManagerService.RemoveSpawnedPickupById(pickupId);
            DynamicData.For(instance).Data.Clear();

            IGameNetworkMessage message = new PickupApplied
            {
                PickupId = pickupId,
                OwnerId = playerManagerService.GetLocalPlayer().ConnectionId
            };

            var isHost = IsServerMode() ?? false;
            if (isHost)
            {
                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                udpClientService.SendToHost(message);
            }
        }

        private void OnReceivedPickupApplied(PickupApplied applied)
        {
            var pickup = pickupManagerService.GetSpawnedPickupById(applied.PickupId);
            if (pickup == null)
            {
                logger.LogWarning($"Pickup {applied.PickupId} for owner {applied.OwnerId} not found in PickupManagerService when processing OnReceivedPickupApplied.");
                return;
            }

            if (pickup.ePickup == EPickup.Time || pickup.ePickup == EPickup.Magnet) //Apply for all clients
            {
                Plugin.CAN_SEND_MESSAGES = false;
                pickup.ApplyPickup();
                Plugin.CAN_SEND_MESSAGES = true;
            }
            else
            {
                var isServer = IsServerMode() ?? false;
                var netPlayer = playerManagerService.GetNetPlayerByNetplayId(applied.OwnerId);

                if (isServer && pickup.ePickup == EPickup.Rage) //Apply rage on server since projectiles are spawned server side
                {
                    playerManagerService.AddGetNetplayerPositionRequest(applied.OwnerId);
                    netPlayer.Inventory.statusEffects.OnPickupTriggered(pickup);
                    playerManagerService.UnqueueNetplayerPositionRequest();
                }

                try
                {
                    playerManagerService.AddGetNetplayerPositionRequest(applied.OwnerId);
                    switch (pickup.ePickup)
                    {
                        case EPickup.Rage:
                            var rage = EffectManager.Instance.ragePickup;
                            var copiedPickup = GameObject.Instantiate(rage).GetComponent<StatusEffectPickup>();
                            copiedPickup.useFeetPosition = false;
                            copiedPickup.Set();
                            break;
                        case EPickup.Shield:
                            var shield = EffectManager.Instance.shieldPickup;
                            var copiedShieldPickup = GameObject.Instantiate(shield).GetComponent<StatusEffectPickup>();
                            copiedShieldPickup.useFeetPosition = false;
                            copiedShieldPickup.Set();
                            var y = copiedShieldPickup.transform.position.y;
                            y += Plugin.PLAYER_FEET_OFFSET_Y;
                            copiedShieldPickup.transform.position = new Vector3(copiedShieldPickup.transform.position.x, y, copiedShieldPickup.transform.position.z);
                            break;
                        case EPickup.Haste:
                            var haste = EffectManager.Instance.hastePickup;
                            var copiedHastePickup = GameObject.Instantiate(haste).GetComponent<StatusEffectPickup>();
                            copiedHastePickup.useFeetPosition = false;
                            copiedHastePickup.Set();
                            break;
                        case EPickup.Nuke:
                            var nuke = EffectManager.Instance.nukePickup;
                            var explosion = GameObject.Instantiate(nuke).GetComponent<Explosion>();
                            explosion.transform.position = netPlayer.Model.transform.position;
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error applying pickup {pickup.ePickup} for player {applied.OwnerId}: {ex}");
                }
                finally
                {
                    playerManagerService.UnqueueNetplayerPositionRequest();
                }
            }

            DynamicData.For(pickup).Data.Clear();

            PickupManager.Instance.DespawnPickup(pickup);
            pickupManagerService.RemoveSpawnedPickupById(applied.PickupId);
        }

        private void OnReceivedPickupFollowingPlayer(PickupFollowingPlayer player)
        {
            var pickup = pickupManagerService.GetSpawnedPickupById(player.PickupId);
            if (pickup == null)
            {
                logger.LogWarning($"Pickup {player.PickupId} not found in PickupManagerService when processing OnReceivedPickupFollowingPlayer by player {player.PlayerId}.");
                return;
            }

            Transform target;
            var netPlayer = playerManagerService.GetNetPlayerByNetplayId(player.PlayerId);
            if (netPlayer == null)
            {
                if (player.PlayerId == playerManagerService.GetLocalPlayer().ConnectionId)
                {
                    target = GameManager.Instance.player.transform;
                }
                else
                {
                    logger.LogWarning("NetPlayer not found in PlayerManager when processing OnReceivedPickupFollowingPlayer.");
                    return;
                }
            }
            else
            {
                target = netPlayer.Model.transform;
            }

            var dynPickup = DynamicData.For(pickup);
            dynPickup.Set("ownerId", player.PlayerId);

            Plugin.CAN_SEND_MESSAGES = false;
            pickup.StartFollowingPlayer(target);
            Plugin.CAN_SEND_MESSAGES = true;
        }

        public void OnWantToStartFollowingPickup(Pickup instance)
        {
            var isServer = IsServerMode() ?? false;

            if (!isServer)
            {
                DynamicData.For(instance).Set("hasSentAlready", true);

                IGameNetworkMessage message = new WantToStartFollowingPickup
                {
                    PickupId = pickupManagerService.GetPickupByReference(instance).Key,
                    OwnerId = playerManagerService.GetLocalPlayer().ConnectionId
                };

                udpClientService.SendToHost(message);
            }
            else
            {
                HandleWantToStartFollowingPickup(playerManagerService.GetLocalPlayer().ConnectionId, pickupManagerService.GetPickupByReference(instance).Key);
            }
        }

        private void HandleWantToStartFollowingPickup(uint ownerId, uint pickupId)
        {
            var pickup = pickupManagerService.GetSpawnedPickupById(pickupId);
            if (pickup == null)
            {
                logger.LogWarning($"Pickup {pickupId} not found in PickupManagerService when processing HandleWantToStartFollowingPickup.");
                return;
            }

            var currentOwnerId = DynamicData.For(pickup).Get<uint?>("ownerId");
            if (currentOwnerId.HasValue)
            {
                if (currentOwnerId.Value != ownerId)
                {
                    IGameNetworkMessage msg = new PickupFollowingPlayer
                    {
                        PickupId = pickupId,
                        PlayerId = currentOwnerId.Value
                    };
                    udpClientService.SendToAllClients(msg, LiteNetLib.DeliveryMethod.ReliableOrdered);
                }

                return;
            }

            DynamicData.For(pickup).Set("ownerId", ownerId);

            IGameNetworkMessage message = new PickupFollowingPlayer
            {
                PickupId = pickupId,
                PlayerId = ownerId
            };

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
            OnReceivedPickupFollowingPlayer((PickupFollowingPlayer)message);
        }

        private void OnReceivedWantToStartFollowingPickup(WantToStartFollowingPickup pickupMessage)
        {
            HandleWantToStartFollowingPickup(pickupMessage.OwnerId, pickupMessage.PickupId);
        }

        public void SendPickupFollowingPlayer(uint ownerId, uint pickupId)
        {
            IGameNetworkMessage message = new PickupFollowingPlayer
            {
                PickupId = pickupId,
                PlayerId = ownerId
            };
            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        public void OnSpawnedChest(Vector3 position, Quaternion rotation, UnityEngine.Object obj)
        {
            var chestId = chestManagerService.AddChest(obj);

            IGameNetworkMessage message = new SpawnedChest
            {
                Position = position.ToNumericsVector3(),
                Rotation = rotation.ToNumericsQuaternion(),
                ChestId = chestId
            };

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableUnordered);
        }

        private void OnReceivedSpawnedChest(SpawnedChest chest)
        {
            chestManagerService.PushNextChestId(chest.ChestId);
            Plugin.CAN_SPAWN_CHESTS = true;
            EffectManager.Instance.SpawnChest(EffectManager.Instance.openChestNormal, chest.Position.ToUnityVector3());
            Plugin.CAN_SPAWN_CHESTS = false;
        }

        public void OnChestOpened(OpenChest instance)
        {
            var chestSpawned = chestManagerService.GetChestByReference(instance);

            if (chestSpawned.Value == null)
            {
                logger.LogWarning("Chest not found in ChestManagerService when processing OnChestOpened.");
                return;
            }
            IGameNetworkMessage message = new ChestOpened
            {
                ChestId = chestSpawned.Key,
                OwnerId = playerManagerService.GetLocalPlayer().ConnectionId
            };
            var isHost = IsServerMode() ?? false;

            if (isHost)
            {
                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                udpClientService.SendToHost(message);
            }
        }

        private void OnReceivedChestOpened(ChestOpened opened)
        {
            var chestObject = chestManagerService.GetChest(opened.ChestId);
            if (chestObject == null)
            {
                logger.LogWarning("Chest object not found in ChestManagerService when processing OnReceivedChestOpened.");
                return;
            }

            GameObject.DestroyImmediate(chestObject);
            chestManagerService.RemoveChest(opened.ChestId);
        }

        public void OnWeaponAdded(WeaponInventory instance, WeaponData weaponData, Il2CppSystem.Collections.Generic.List<StatModifier> upgradeOffer)
        {
            var upgrades = new List<StatModifierModel>();

            if (upgradeOffer != null)
            {
                foreach (var modifier in upgradeOffer)
                {
                    upgrades.Add(new StatModifierModel
                    {
                        StatType = (int)modifier.stat,
                        Value = modifier.modification,
                        ModificationType = (int)modifier.modifyType
                    });
                }
            }

            IGameNetworkMessage msg = new WeaponAdded
            {
                Weapon = (int)weaponData.eWeapon,
                OwnerId = playerManagerService.GetLocalPlayer().ConnectionId,
                Upgrades = upgrades
            };

            var isHost = IsServerMode() ?? false;
            if (isHost)
            {
                udpClientService.SendToAllClients(msg, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                udpClientService.SendToHost(msg);
            }
        }

        private void OnReceivedWeaponAdded(WeaponAdded added)
        {
            var player = playerManagerService.GetNetPlayerByNetplayId(added.OwnerId);
            if (player == null)
            {
                logger.LogWarning($"Player not found for ConnectionId: {added.OwnerId} when processing OnReceivedWeaponAdded.");
                return;
            }

            if (DataManager.Instance.weapons.TryGetValue((EWeapon)added.Weapon, out var weaponData))
            {
                var upgradeModifiers = new Il2CppSystem.Collections.Generic.List<StatModifier>();
                foreach (var upgrade in added.Upgrades)
                {
                    var modifier = new StatModifier
                    {
                        stat = (EStat)upgrade.StatType,
                        modification = upgrade.Value,
                        modifyType = (EStatModifyType)upgrade.ModificationType
                    };
                    upgradeModifiers.Add(modifier);
                }

                Plugin.CAN_SEND_MESSAGES = false;
                Plugin.Instance.SavePlayerInventoryActions();
                player.Inventory.weaponInventory.AddWeapon(weaponData, upgradeModifiers);
                player.RefreshConstantAttack(upgradeModifiers);
                Plugin.Instance.RestorePlayerInventoryActions();
                Plugin.CAN_SEND_MESSAGES = true;
            }
        }

        public void OnTomeAdded(TomeInventory instance, TomeData tomeData, Il2CppSystem.Collections.Generic.List<StatModifier> upgradeOffer, ERarity rarity)
        {
            var upgrades = new List<StatModifierModel>();
            foreach (var modifier in upgradeOffer)
            {
                upgrades.Add(new StatModifierModel
                {
                    StatType = (int)modifier.stat,
                    Value = modifier.modification,
                    ModificationType = (int)modifier.modifyType
                });
            }

            IGameNetworkMessage msg = new TomeAdded
            {
                Tome = (int)tomeData.eTome,
                OwnerId = playerManagerService.GetLocalPlayer().ConnectionId,
                Upgrades = upgrades,
                Rarity = (int)rarity
            };
            var isHost = IsServerMode() ?? false;
            if (isHost)
            {
                udpClientService.SendToAllClients(msg, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                udpClientService.SendToHost(msg);
            }
        }

        private void OnReceivedTomeAdded(TomeAdded added)
        {
            var player = playerManagerService.GetNetPlayerByNetplayId(added.OwnerId);
            if (player == null)
            {
                logger.LogWarning($"Player not found for ConnectionId: {added.OwnerId} when processing OnReceivedTomeAdded.");
                return;
            }

            if (DataManager.Instance.tomeData.TryGetValue((ETome)added.Tome, out var tomeData))
            {
                var upgradeModifiers = new Il2CppSystem.Collections.Generic.List<StatModifier>();
                foreach (var upgrade in added.Upgrades)
                {
                    var modifier = new StatModifier
                    {
                        stat = (EStat)upgrade.StatType,
                        modification = upgrade.Value,
                        modifyType = (EStatModifyType)upgrade.ModificationType
                    };
                    upgradeModifiers.Add(modifier);
                }
                Plugin.CAN_SEND_MESSAGES = false;
                player.Inventory.tomeInventory.AddTome(tomeData, upgradeModifiers, (ERarity)added.Rarity);
                player.RefreshConstantAttack(upgradeModifiers);
                Plugin.CAN_SEND_MESSAGES = true;
            }
        }

        public void OnInteractableUsed(BaseInteractable instance)
        {
            var netplayId = DynamicData.For(instance.gameObject).Get<uint?>("netplayId");

            if (!netplayId.HasValue)
            {
                try
                {
                    var id = spawnedObjectManagerService.GetByReferenceInChildren<InteractableCharacterFight>(instance.gameObject);
                    if (id.HasValue)
                    {
                        netplayId = id;
                    }

                    var boomBox = spawnedObjectManagerService.GetByReferenceInChildren<InteractableBoombox>(instance.gameObject);
                    if (boomBox.HasValue)
                    {
                        netplayId = boomBox;
                    }

                    var coffin = spawnedObjectManagerService.GetByReferenceInChildren<InteractableCoffin>(instance.gameObject);
                    if (coffin.HasValue)
                    {
                        netplayId = coffin;
                    }

                    var present = spawnedObjectManagerService.GetByReferenceInChildren<InteractableGift>(instance.gameObject);
                    if (present.HasValue)
                    {
                        netplayId = present;
                    }

                    var crypt = spawnedObjectManagerService.GetByReferenceInChildren<InteractableCrypt>(instance.gameObject);
                    if (crypt.HasValue)
                    {
                        netplayId = crypt;
                    }

                    var tumbleWeed = spawnedObjectManagerService.GetByReferenceInChildren<InteractableTumbleWeed>(instance.gameObject);
                    if (tumbleWeed.HasValue)
                    {
                        netplayId = tumbleWeed;
                    }

                    var reviver = spawnedObjectManagerService.GetByReferenceInChildren<InteractableReviver>(instance.gameObject);
                    if (reviver.HasValue)
                    {
                        netplayId = reviver;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Error while getting netplayId for InteractableCharacterFight: {ex}");
                }
            }

            var isPortal = instance.GetComponentInChildren<InteractablePortal>() != null;
            var isFinalPortal = instance.GetComponentInChildren<InteractablePortalFinal>() != null;
            var isCryptKey = instance.GetComponentInChildren<InteractableCageKey>() != null && instance.name.Contains("CryptKeyPickup");

            if (!isPortal && !isFinalPortal && !isCryptKey)
            {
                if (!netplayId.HasValue)
                {
                    if (!instance.name.Contains("ShadyGuy") && !instance.name.Contains("Microwave")) //TODO: those guys are so shady that they don't work on client side for some reason ¯\_(ツ)_/¯ (edit, they might work like interactable character fight ?)
                    {
                        logger.LogWarning("Interactable does not have a netplayId when processing OnInteractableUsed.");
                    }
                    return;
                }

                var interactable = spawnedObjectManagerService.GetSpawnedObject(netplayId.Value);

                if (interactable == null)
                {
                    logger.LogWarning("Interactable not found in SpawnedObjectManagerService when processing OnInteractableUsed.");
                    return;
                }
            }

            IGameNetworkMessage message = new InteractableUsed
            {
                NetplayId = netplayId.HasValue ? netplayId.Value : 0,
                Action = GetActionByInteractable(instance),
                IsPortal = isPortal,
                IsFinalPortal = isFinalPortal,
                IsCryptKey = isCryptKey,
                OwnerId = playerManagerService.GetLocalPlayer().ConnectionId
            };

            var isHost = IsServerMode() ?? false;

            if (instance.GetComponentInChildren<InteractableCoffin>() != null)
            {
                currentCoffin = instance.GetComponentInChildren<InteractableCoffin>();
            }

            if (isHost)
            {
                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                if (instance.GetComponentInChildren<InteractableCoffin>() != null)
                {
                    currentCoffin.minibossEnemies = new Il2CppSystem.Collections.Generic.HashSet<Enemy>();
                }

                udpClientService.SendToHost(message);
            }

            if (isPortal || instance.GetComponentInChildren<InteractableBossSpawnerFinal>() != null)
            {
                TransitionToState(GameEvent.PortalOpened);
            }

            if (isFinalPortal)
            {
                TransitionToState(GameEvent.FinalPortalOpened);
            }
        }

        private static InteractableAction GetActionByInteractable(BaseInteractable interactable)
        {
            if (interactable == null) return InteractableAction.Used;

            if (interactable.GetComponentInChildren<InteractableCharacterFight>() != null)
            {
                return InteractableAction.Interact;
            }

            if (interactable.GetComponentInChildren<InteractableChest>() != null)
            {
                return InteractableAction.Destroy;
            }

            if (
                interactable.GetComponentInChildren<InteractableShrineCursed>() != null ||
                interactable.GetComponentInChildren<InteractableShrineGreed>() != null ||
                interactable.GetComponentInChildren<InteractableShrineChallenge>() != null ||
                interactable.GetComponentInChildren<InteractableShrineMagnet>() != null ||
                interactable.GetComponentInChildren<InteractableBossSpawner>() != null ||
                interactable.GetComponentInChildren<InteractablePortal>() != null ||
                interactable.GetComponentInChildren<InteractableBossSpawnerFinal>() != null ||
                interactable.GetComponentInChildren<InteractablePortalFinal>() != null ||
                interactable.GetComponentInChildren<InteractableTumbleWeed>() != null ||
                interactable.GetComponentInChildren<InteractablePot>() != null ||
                interactable.GetComponentInChildren<InteractableBoombox>() != null ||
                interactable.GetComponentInChildren<InteractableDesertGrave>() != null ||
                interactable.GetComponentInChildren<InteractableSkeletonKingStatue>() != null ||
                interactable.GetComponentInChildren<InteractableCryptLeave>() != null ||
                interactable.GetComponentInChildren<InteractableCoffin>() != null ||
                interactable.GetComponentInChildren<InteractableCageKey>() != null ||
                interactable.GetComponentInChildren<InteractableCrypt>() != null ||
                interactable.GetComponentInChildren<InteractableGhostBossLeave>() != null ||
                interactable.GetComponentInChildren<InteractableGift>() != null ||
                interactable.GetComponentInChildren<InteractableGravestone>() != null ||
                interactable.GetComponentInChildren<InteractableReviver>() != null
            )
            {
                return InteractableAction.Interact;
            }

            return InteractableAction.Used;
        }

        private void OnReceivedInteractableUsed(InteractableUsed used)
        {
            if (used.IsPortal)
            {
                var bossSpawner = spawnedObjectManagerService.GetSpecific<InteractableBossSpawner>();
                if (bossSpawner != null)
                {
                    if (!bossSpawner.portal.activeSelf || !bossSpawner.portal.activeInHierarchy)
                    {
                        bossSpawner.portal.SetActive(true); //We might not received boss death yet, so...
                    }

                    var portal = bossSpawner.portal.GetComponent<InteractablePortal>();
                    if (portal != null)
                    {
                        Plugin.CAN_SEND_MESSAGES = false;
                        portal.Interact();
                        TransitionToState(GameEvent.PortalOpened);
                        Plugin.CAN_SEND_MESSAGES = true;
                    }
                }

                return;
            }

            if (used.IsFinalPortal)
            {
                var finalPortal = Il2CppFindHelper.FindAllGameObjects().FirstOrDefault(go => go.GetComponent<InteractablePortalFinal>() != null).GetComponent<InteractablePortalFinal>(); //Find the componnt somehow instead of whole gameobject search
                if (finalPortal != null)
                {
                    Plugin.CAN_SEND_MESSAGES = false;
                    finalPortal.Interact();
                    TransitionToState(GameEvent.FinalPortalOpened);
                    Plugin.CAN_SEND_MESSAGES = true;
                }
                else
                {
                    logger.LogWarning("Final Portal not found ?");
                    return;
                }
            }

            if (used.IsCryptKey)
            {
                var key = currentCoffin.keyPickup.GetComponentInChildren<InteractableCageKey>();
                if (key != null)
                {
                    currentCoffin.OnInteracted(key, key.Interact());
                }
                else
                {
                    logger.LogWarning("Crypt Key not found ?");
                }
                return;
            }

            var interactableObj = spawnedObjectManagerService.GetSpawnedObject(used.NetplayId);

            if (interactableObj == null)
            {
                logger.LogWarning("Interactable object not found in SpawnedObjectManagerService when processing OnReceivedInteractableUsed.");
                return;
            }

            Plugin.CAN_SEND_MESSAGES = false;

            switch (used.Action)
            {
                case InteractableAction.Destroy:
                    //var chest = interactableObj.GetComponent<InteractableChest>();
                    GameObject.DestroyImmediate(interactableObj);
                    break;
                case InteractableAction.Used:
                    logger.LogInfo($"Net player used interactable with ID: {used.NetplayId}");
                    break;
                case InteractableAction.Interact:
                    var shrineCursed = interactableObj.GetComponent<InteractableShrineCursed>();
                    if (shrineCursed != null)
                    {
                        shrineCursed.Interact();
                        break;
                    }

                    var shrineGreed = interactableObj.GetComponent<InteractableShrineGreed>();
                    if (shrineGreed != null)
                    {
                        shrineGreed.Interact();
                        break;
                    }

                    var shrineChallenge = interactableObj.GetComponent<InteractableShrineChallenge>();
                    if (shrineChallenge != null)
                    {
                        shrineChallenge.Interact();
                        break;
                    }

                    var shrineMagnet = interactableObj.GetComponent<InteractableShrineMagnet>();
                    if (shrineMagnet != null)
                    {
                        shrineMagnet.Interact();
                        break;
                    }

                    var bossSpawner = interactableObj.GetComponent<InteractableBossSpawner>();
                    if (bossSpawner != null)
                    {
                        bossSpawner.Interact();
                        break;
                    }

                    var finalBossSpawner = interactableObj.GetComponent<InteractableBossSpawnerFinal>();
                    if (finalBossSpawner != null)
                    {
                        finalBossSpawner.Interact();
                        TransitionToState(GameEvent.PortalOpened);
                        break;
                    }

                    var characterFight = interactableObj.GetComponentInChildren<InteractableCharacterFight>();
                    if (characterFight != null)
                    {
                        characterFight.chargeFx.SetActive(true);
                        interactableObj.SetActive(false);
                        break;
                    }

                    var interactableTumbleWeed = interactableObj.GetComponent<InteractableTumbleWeed>();
                    if (interactableTumbleWeed != null)
                    {
                        playerManagerService.AddGetNetplayerPositionRequest(used.OwnerId);
                        interactableTumbleWeed.Interact();
                        playerManagerService.UnqueueNetplayerPositionRequest();
                        break;
                    }

                    var interactablePot = interactableObj.GetComponent<InteractablePot>();
                    if (interactablePot != null)
                    {
                        playerManagerService.AddGetNetplayerPositionRequest(used.OwnerId);
                        interactablePot.Interact();
                        playerManagerService.UnqueueNetplayerPositionRequest();
                        break;
                    }

                    var interactableBoombox = interactableObj.GetComponentInChildren<InteractableBoombox>();
                    if (interactableBoombox != null)
                    {
                        interactableBoombox.Interact();
                        break;
                    }

                    var interactableDesertGrave = interactableObj.GetComponentInChildren<InteractableDesertGrave>();
                    if (interactableDesertGrave != null)
                    {
                        interactableDesertGrave.Interact();
                        interactableObj.SetActive(false);
                        break;
                    }

                    var interactableSkeletonKingStatue = interactableObj.GetComponentInChildren<InteractableSkeletonKingStatue>();
                    if (interactableSkeletonKingStatue != null)
                    {
                        interactableSkeletonKingStatue.Interact();
                        interactableObj.SetActive(false);
                        break;
                    }

                    var interactableCryptLeave = interactableObj.GetComponentInChildren<InteractableCryptLeave>();
                    if (interactableCryptLeave != null)
                    {
                        interactableCryptLeave.Interact();
                        break;
                    }

                    var interactableCoffin = interactableObj.GetComponentInChildren<InteractableCoffin>();
                    if (interactableCoffin != null)
                    {
                        interactableCoffin.Interact();
                        currentCoffin = interactableCoffin;

                        if (IsServerMode() == false)
                        {
                            currentCoffin.minibossEnemies = new Il2CppSystem.Collections.Generic.HashSet<Enemy>();
                        }
                        break;
                    }

                    var interactableCrypt = interactableObj.GetComponentInChildren<InteractableCrypt>();
                    if (interactableCrypt != null)
                    {
                        interactableCrypt.Interact();
                        break;
                    }

                    var interactableGhostBossLeave = interactableObj.GetComponentInChildren<InteractableGhostBossLeave>();
                    if (interactableGhostBossLeave != null)
                    {
                        interactableGhostBossLeave.Interact();
                        break;
                    }

                    var interactableGift = interactableObj.GetComponentInChildren<InteractableGift>();
                    if (interactableGift != null)
                    {
                        interactableGift.Interact();
                        break;
                    }

                    var interactableGravestone = interactableObj.GetComponentInChildren<InteractableGravestone>();
                    if (interactableGravestone != null)
                    {
                        interactableGravestone.Interact();
                        break;
                    }

                    var interactableReviver = interactableObj.GetComponentInChildren<InteractableReviver>();
                    if (interactableReviver != null)
                    {
                        interactableReviver.Interact();
                        break;
                    }

                    logger.LogWarning("Interactable type for Interact action not recognized.");

                    break;
            }

            Plugin.CAN_SEND_MESSAGES = true;
        }

        public bool OnStartingToChargingShrine(uint shrineNetplayId)
        {
            var isHost = IsServerMode() ?? false;

            IGameNetworkMessage message = new StartingChargingShrine
            {
                ShrineNetplayId = shrineNetplayId,
                PlayerChargingId = playerManagerService.GetLocalPlayer().ConnectionId
            };

            if (!isHost)
            {
                udpClientService.SendToHost(message);
                return false;
            }

            var players = playerManagerService.GetAllPlayers();
            var chargers = shrineChargingPlayers.FirstOrDefault(p => p.Key == shrineNetplayId).Value;

            shrineChargingPlayers[shrineNetplayId] = [playerManagerService.GetLocalPlayer().ConnectionId];

            if (chargers != null && chargers.Any())
            {
                logger.LogInfo("Another player is already charging this shrine. Preventing re trigger.");
                return false;
            }

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);

            return true;
        }

        private void OnReceivedStartingToChargingShrine(StartingChargingShrine shrine)
        {
            var isHost = IsServerMode() ?? false;
            if (isHost)
            {
                var players = playerManagerService.GetAllPlayers();
                var chargers = shrineChargingPlayers.FirstOrDefault(p => p.Key == shrine.ShrineNetplayId).Value;
                shrineChargingPlayers[shrine.ShrineNetplayId] = [shrine.PlayerChargingId];

                if (chargers != null && chargers.Any())
                {
                    return;
                }

                var spawnedObj = spawnedObjectManagerService.GetSpawnedObject(shrine.ShrineNetplayId);

                if (spawnedObj == null)
                {
                    logger.LogWarning("Spawned object not found in SpawnedObjectManagerService when processing OnReceivedStartingToChargingShrine.");
                    return;
                }

                var shrineObj = spawnedObj.GetComponent<ChargeShrine>();
                if (shrineObj == null)
                {
                    logger.LogWarning("ChargeShrine component not found on spawned object when processing OnReceivedStartingToChargingShrine.");
                    return;
                }


                Plugin.CAN_SEND_MESSAGES = false;
                shrineObj.OnTriggerEnter();
                Plugin.CAN_SEND_MESSAGES = true;

                udpClientService.SendToAllClients(shrine, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {

                var spawnedObj = spawnedObjectManagerService.GetSpawnedObject(shrine.ShrineNetplayId);

                if (spawnedObj == null)
                {
                    logger.LogWarning("Spawned object not found in SpawnedObjectManagerService when processing OnReceivedStartingToChargingShrine.");
                    return;
                }

                var shrineObj = spawnedObj.GetComponent<ChargeShrine>();
                if (shrineObj == null)
                {
                    logger.LogWarning("ChargeShrine component not found on spawned object when processing OnReceivedStartingToChargingShrine.");
                    return;
                }

                Plugin.CAN_SEND_MESSAGES = false;
                shrineObj.OnTriggerEnter();
                Plugin.CAN_SEND_MESSAGES = true;

            }
        }

        public bool OnStoppingChargingShrine(uint shrineNetplayId)
        {
            var isHost = IsServerMode() ?? false;

            IGameNetworkMessage message = new StoppingChargingShrine
            {
                ShrineNetplayId = shrineNetplayId,
                PlayerChargingId = playerManagerService.GetLocalPlayer().ConnectionId
            };

            if (!isHost)
            {
                udpClientService.SendToHost(message);
                return false;
            }

            var players = playerManagerService.GetAllPlayers();
            var chargers = shrineChargingPlayers.FirstOrDefault(p => p.Key == shrineNetplayId).Value;

            shrineChargingPlayers[shrineNetplayId].Remove(playerManagerService.GetLocalPlayer().ConnectionId);

            if (chargers != null && chargers.Any())
            {
                logger.LogInfo("Another player is still charging this shrine. Preventing stop trigger.");
                return false;
            }

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);

            return true;
        }

        private void OnReceivedStoppingChargingShrine(StoppingChargingShrine shrine)
        {
            var isHost = IsServerMode() ?? false;
            if (isHost)
            {
                var players = playerManagerService.GetAllPlayers();
                var chargers = shrineChargingPlayers.FirstOrDefault(p => p.Key == shrine.ShrineNetplayId).Value;
                shrineChargingPlayers[shrine.ShrineNetplayId].Remove(shrine.PlayerChargingId);

                if (chargers != null && chargers.Any())
                {
                    return;
                }

                var spawnedObj = spawnedObjectManagerService.GetSpawnedObject(shrine.ShrineNetplayId);

                if (spawnedObj == null)
                {
                    logger.LogWarning("Spawned object not found in SpawnedObjectManagerService when processing OnReceivedStartingToChargingShrine.");
                    return;
                }

                var shrineObj = spawnedObj.GetComponent<ChargeShrine>();
                if (shrineObj == null)
                {
                    logger.LogWarning("ChargeShrine component not found on spawned object when processing OnReceivedStartingToChargingShrine.");
                    return;
                }


                Plugin.CAN_SEND_MESSAGES = false;
                shrineObj.OnTriggerExit();
                Plugin.CAN_SEND_MESSAGES = true;

                udpClientService.SendToAllClients(shrine, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                var spawnedObj = spawnedObjectManagerService.GetSpawnedObject(shrine.ShrineNetplayId);

                if (spawnedObj == null)
                {
                    logger.LogWarning("Spawned object not found in SpawnedObjectManagerService when processing OnReceivedStartingToChargingShrine.");
                    return;
                }

                var shrineObj = spawnedObj.GetComponent<ChargeShrine>();
                if (shrineObj == null)
                {
                    logger.LogWarning("ChargeShrine component not found on spawned object when processing OnReceivedStartingToChargingShrine.");
                    return;
                }

                Plugin.CAN_SEND_MESSAGES = false;
                shrineObj.OnTriggerExit();
                Plugin.CAN_SEND_MESSAGES = true;

            }
        }

        public void OnEnemyExploder(Enemy enemy)
        {
            var enemySpawned = enemyManagerService.GetEnemyByReference(enemy);
            if (enemySpawned.Value == null)
            {
                logger.LogWarning("Enemy not found in EnemyManagerService when processing OnEnemyExploder.");
                return;
            }

            IGameNetworkMessage message = new EnemyExploder
            {
                EnemyId = enemySpawned.Key,
                SenderId = playerManagerService.GetLocalPlayer().ConnectionId
            };

            var isHost = IsServerMode() ?? false;
            if (isHost)
            {
                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                udpClientService.SendToHost(message);
            }
        }

        private void OnReceivedEnemyExploder(EnemyExploder exploder)
        {
            var enemy = enemyManagerService.GetEnemyById(exploder.EnemyId);
            if (enemy == null)
            {
                logger.LogWarning("Enemy not found when processing OnReceivedEnemyExploder.");
                return;
            }

            Plugin.CAN_ENEMY_EXPLODE = true;
            EffectManager.Instance.ExploderEnemy(enemy);
            Plugin.CAN_ENEMY_EXPLODE = false;
            enemyManagerService.RemoveEnemyById(exploder.EnemyId);
        }

        public void OnSpawnedEnemySpecialAttack(Enemy enemy, EnemySpecialAttack attack)
        {
            var enemySpawned = enemyManagerService.GetEnemyByReference(enemy);
            if (enemySpawned.Value == null)
            {
                return;
            }

            var targetId = DynamicData.For(enemy).Get<uint?>("targetId");
            if (targetId == null)
            {
                logger.LogWarning("Enemy has no targetId when processing OnSpawnedEnemySpecialAttack.");
                return;
            }


            var rand = UnityEngine.Random.Range(0, playerManagerService.GetAllPlayersAlive().Count());
            var randomPlayer = playerManagerService.GetAllPlayersAlive().ElementAt(rand);
            DynamicData.For(enemy).Set("targetId", randomPlayer.ConnectionId); //Random target

            IGameNetworkMessage message = new SpawnedEnemySpecialAttack
            {
                EnemyId = enemySpawned.Key,
                AttackName = attack.attackName,
                TargetId = randomPlayer.ConnectionId
            };

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        private void OnReceivedSpawnedEnemySpecialAttack(SpawnedEnemySpecialAttack attack)
        {
            var enemy = enemyManagerService.GetEnemyById(attack.EnemyId);
            if (enemy == null)
            {
                logger.LogWarning("Enemy not found in EnemyManagerService when processing OnReceivedSpawnedEnemySpecialAttack.");
                return;
            }

            if (enemy.specialAttackController == null)
            {
                logger.LogWarning("EnemySpecialAttackController is null on enemy when processing OnReceivedSpawnedEnemySpecialAttack.");
                return;
            }

            EnemySpecialAttack specialAttack = null;
            foreach (var specialAtt in enemy.specialAttackController.attacks)
            {
                if (specialAtt.attackName == attack.AttackName)
                {
                    specialAttack = specialAtt;
                    break;
                }
            }

            if (specialAttack == null)
            {
                logger.LogWarning("EnemySpecialAttack not found on enemy when processing OnReceivedSpawnedEnemySpecialAttack.");
                return;
            }

            DynamicData.For(enemy).Set("targetId", attack.TargetId); //Target might have changed

            Plugin.CAN_ENEMY_USE_SPECIAL_ATTACK = true;
            enemy.specialAttackController.UseSpecialAttack(specialAttack);
            Plugin.CAN_ENEMY_USE_SPECIAL_ATTACK = false;
        }

        public bool IsLoadingNextLevel()
        {
            return currentState == State.LoadingNextLevel;
        }

        public bool OnStartingToChargingPylon(uint pylonNetplayId)
        {
            var isHost = IsServerMode() ?? false;

            IGameNetworkMessage message = new StartingChargingPylon
            {
                PylonNetplayId = pylonNetplayId,
                PlayerChargingId = playerManagerService.GetLocalPlayer().ConnectionId
            };

            if (!isHost)
            {
                udpClientService.SendToHost(message);
                return false;
            }

            var players = playerManagerService.GetAllPlayers();
            var chargers = pylonChargingPlayers.FirstOrDefault(p => p.Key == pylonNetplayId).Value;

            pylonChargingPlayers[pylonNetplayId] = [playerManagerService.GetLocalPlayer().ConnectionId];

            if (chargers != null && chargers.Any())
            {
                logger.LogInfo("Another player is already charging this shrine. Preventing re trigger.");
                return false;
            }

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);

            return true;
        }

        private void OnReceivedStartingToChargingPylon(StartingChargingPylon pylon)
        {
            var isHost = IsServerMode() ?? false;
            if (isHost)
            {
                var players = playerManagerService.GetAllPlayers();
                var chargers = pylonChargingPlayers.FirstOrDefault(p => p.Key == pylon.PylonNetplayId).Value;
                pylonChargingPlayers[pylon.PylonNetplayId] = [pylon.PlayerChargingId];

                if (chargers != null && chargers.Any())
                {
                    return;
                }

                var spawnedObj = spawnedObjectManagerService.GetSpawnedObject(pylon.PylonNetplayId);

                if (spawnedObj == null)
                {
                    logger.LogWarning("Pylon object not found in SpawnedObjectManagerService when processing OnReceivedStartingToChargingPylon.");
                    return;
                }

                var pylonObj = spawnedObj.GetComponent<BossPylon>();
                if (pylonObj == null)
                {
                    logger.LogWarning("Pylon component not found on spawned object when processing OnReceivedStartingToChargingPylon.");
                    return;
                }


                Plugin.CAN_SEND_MESSAGES = false;
                pylonObj.OnTriggerEnter();
                Plugin.CAN_SEND_MESSAGES = true;

                udpClientService.SendToAllClients(pylon, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {

                var spawnedObj = spawnedObjectManagerService.GetSpawnedObject(pylon.PylonNetplayId);

                if (spawnedObj == null)
                {
                    logger.LogWarning("Pylon object not found in SpawnedObjectManagerService when processing OnReceivedStartingToChargingPylon.");
                    return;
                }

                var pylonObj = spawnedObj.GetComponent<BossPylon>();
                if (pylonObj == null)
                {
                    logger.LogWarning("Pylon component not found on spawned object when processing OnReceivedStartingToChargingPylon.");
                    return;
                }

                Plugin.CAN_SEND_MESSAGES = false;
                pylonObj.OnTriggerEnter();
                Plugin.CAN_SEND_MESSAGES = true;

            }
        }

        public bool OnStartingToChargingLamp(uint lampNetplayId) //TODO: this is ass, pylon and lamp should be refactored to use the same logic, but i'm in holidays and lazy right now zzzz
        {
            var isHost = IsServerMode() ?? false;

            IGameNetworkMessage message = new StartingChargingLamp
            {
                LampNetplayId = lampNetplayId,
                PlayerChargingId = playerManagerService.GetLocalPlayer().ConnectionId
            };

            if (!isHost)
            {
                udpClientService.SendToHost(message);
                return false;
            }

            var players = playerManagerService.GetAllPlayers();
            var chargers = lampsChargingPlayers.FirstOrDefault(p => p.Key == lampNetplayId).Value;

            lampsChargingPlayers[lampNetplayId] = [playerManagerService.GetLocalPlayer().ConnectionId];

            if (chargers != null && chargers.Any())
            {
                logger.LogInfo("Another player is already charging this lamp. Preventing re trigger.");
                return false;
            }

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);

            return true;
        }

        private void OnReceivedStartingToChargingLamp(StartingChargingLamp lamp)
        {
            var isHost = IsServerMode() ?? false;
            if (isHost)
            {
                var players = playerManagerService.GetAllPlayers();
                var chargers = lampsChargingPlayers.FirstOrDefault(p => p.Key == lamp.LampNetplayId).Value;
                lampsChargingPlayers[lamp.LampNetplayId] = [lamp.PlayerChargingId];

                if (chargers != null && chargers.Any())
                {
                    return;
                }

                var spawnedObj = spawnedObjectManagerService.GetSpawnedObject(lamp.LampNetplayId);

                if (spawnedObj == null)
                {
                    logger.LogWarning("Lamp object not found in SpawnedObjectManagerService when processing OnReceivedStartingToChargingLamp.");
                    return;
                }

                var lampObj = spawnedObj.GetComponent<BossLamp>();
                if (lampObj == null)
                {
                    logger.LogWarning("Lamp component not found on spawned object when processing OnReceivedStartingToChargingLamp.");
                    return;
                }


                Plugin.CAN_SEND_MESSAGES = false;
                lampObj.OnTriggerEnter();
                Plugin.CAN_SEND_MESSAGES = true;

                udpClientService.SendToAllClients(lamp, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {

                var spawnedObj = spawnedObjectManagerService.GetSpawnedObject(lamp.LampNetplayId);

                if (spawnedObj == null)
                {
                    logger.LogWarning("Lamp object not found in SpawnedObjectManagerService when processing OnReceivedStartingToChargingLamp.");
                    return;
                }

                var lampObj = spawnedObj.GetComponent<BossLamp>();
                if (lampObj == null)
                {
                    logger.LogWarning("Lamp component not found on spawned object when processing OnReceivedStartingToChargingLamp.");
                    return;
                }

                Plugin.CAN_SEND_MESSAGES = false;
                lampObj.OnTriggerEnter();
                Plugin.CAN_SEND_MESSAGES = true;

            }
        }

        public bool OnStoppingChargingPylon(uint pylonNetplayId)
        {
            var isHost = IsServerMode() ?? false;

            logger.LogInfo($"Player {playerManagerService.GetLocalPlayer().ConnectionId} stopping charging pylon {pylonNetplayId}");

            IGameNetworkMessage message = new StoppingChargingPylon
            {
                PylonNetplayId = pylonNetplayId,
                PlayerChargingId = playerManagerService.GetLocalPlayer().ConnectionId
            };

            if (!isHost)
            {
                udpClientService.SendToHost(message);
                return false;
            }

            var players = playerManagerService.GetAllPlayers();
            var chargers = pylonChargingPlayers.FirstOrDefault(p => p.Key == pylonNetplayId).Value;

            pylonChargingPlayers[pylonNetplayId].Remove(playerManagerService.GetLocalPlayer().ConnectionId);

            if (chargers != null && chargers.Any())
            {
                logger.LogInfo("Another player is still charging this pylon. Preventing stop trigger.");
                return false;
            }

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);

            return true;
        }

        private void OnReceivedStoppingChargingPylon(StoppingChargingPylon pylon)
        {
            var isHost = IsServerMode() ?? false;
            if (isHost)
            {
                var players = playerManagerService.GetAllPlayers();
                var chargers = pylonChargingPlayers.FirstOrDefault(p => p.Key == pylon.PylonNetplayId).Value;
                pylonChargingPlayers[pylon.PylonNetplayId].Remove(pylon.PlayerChargingId);

                if (chargers != null && chargers.Any())
                {
                    return;
                }

                var spawnedObj = spawnedObjectManagerService.GetSpawnedObject(pylon.PylonNetplayId);

                if (spawnedObj == null)
                {
                    logger.LogWarning("Spawned Pylon not found in SpawnedObjectManagerService when processing OnReceivedStoppingChargingPylon.");
                    return;
                }

                var pylonObj = spawnedObj.GetComponent<BossPylon>();
                if (pylonObj == null)
                {
                    logger.LogWarning("Pylon component not found on spawned object when processing OnReceivedStoppingChargingPylon.");
                    return;
                }


                Plugin.CAN_SEND_MESSAGES = false;
                pylonObj.OnTriggerExit();
                Plugin.CAN_SEND_MESSAGES = true;

                udpClientService.SendToAllClients(pylon, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                var spawnedObj = spawnedObjectManagerService.GetSpawnedObject(pylon.PylonNetplayId);

                if (spawnedObj == null)
                {
                    logger.LogWarning("Spawned pylon not found in SpawnedObjectManagerService when processing OnReceivedStoppingChargingPylon.");
                    return;
                }

                var pylonObj = spawnedObj.GetComponent<BossPylon>();
                if (pylonObj == null)
                {
                    logger.LogWarning("Pylon component not found on spawned object when processing OnReceivedStoppingChargingPylon.");
                    return;
                }

                Plugin.CAN_SEND_MESSAGES = false;
                pylonObj.OnTriggerExit();
                Plugin.CAN_SEND_MESSAGES = true;
            }
        }

        public bool OnStoppingChargingLamp(uint lampNetplayId)
        {
            var isHost = IsServerMode() ?? false;

            IGameNetworkMessage message = new StoppingChargingLamp
            {
                LampNetplayId = lampNetplayId,
                PlayerChargingId = playerManagerService.GetLocalPlayer().ConnectionId
            };

            if (!isHost)
            {
                udpClientService.SendToHost(message);
                return false;
            }

            var players = playerManagerService.GetAllPlayers();
            var chargers = lampsChargingPlayers.FirstOrDefault(p => p.Key == lampNetplayId).Value;

            lampsChargingPlayers[lampNetplayId].Remove(playerManagerService.GetLocalPlayer().ConnectionId);

            if (chargers != null && chargers.Any())
            {
                logger.LogInfo("Another player is still charging this lamp. Preventing stop trigger.");
                return false;
            }

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);

            return true;
        }

        private void OnReceivedStoppingChargingLamp(StoppingChargingLamp lamp)
        {
            var isHost = IsServerMode() ?? false;
            if (isHost)
            {
                var players = playerManagerService.GetAllPlayers();
                var chargers = lampsChargingPlayers.FirstOrDefault(p => p.Key == lamp.LampNetplayId).Value;
                lampsChargingPlayers[lamp.LampNetplayId].Remove(lamp.PlayerChargingId);

                if (chargers != null && chargers.Any())
                {
                    return;
                }

                var spawnedObj = spawnedObjectManagerService.GetSpawnedObject(lamp.LampNetplayId);

                if (spawnedObj == null)
                {
                    logger.LogWarning("Spawned Lamp not found in SpawnedObjectManagerService when processing OnReceivedStoppingChargingLamp.");
                    return;
                }

                var lampObj = spawnedObj.GetComponent<BossLamp>();
                if (lampObj == null)
                {
                    logger.LogWarning("Lamp component not found on spawned object when processing OnReceivedStoppingChargingLamp.");
                    return;
                }


                Plugin.CAN_SEND_MESSAGES = false;
                lampObj.OnTriggerExit();
                Plugin.CAN_SEND_MESSAGES = true;

                udpClientService.SendToAllClients(lamp, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                var spawnedObj = spawnedObjectManagerService.GetSpawnedObject(lamp.LampNetplayId);

                if (spawnedObj == null)
                {
                    logger.LogWarning("Spawned lamp not found in SpawnedObjectManagerService when processing OnReceivedStoppingChargingLamp.");
                    return;
                }

                var lampObj = spawnedObj.GetComponent<BossLamp>();
                if (lampObj == null)
                {
                    logger.LogWarning("Lamp component not found on spawned object when processing OnReceivedStoppingChargingLamp.");
                    return;
                }

                Plugin.CAN_SEND_MESSAGES = false;
                lampObj.OnTriggerExit();
                Plugin.CAN_SEND_MESSAGES = true;
            }
        }

        public void OnFinalBossOrbsSpawned(Orb orb)
        {
            var nexts = finalBossOrbManagerService.GetNextTargetAndOrbId();

            if (nexts == null)
            {
                logger.LogWarning("No target found for final boss orb spawn.");
                return;
            }

            while (nexts != null)
            {
                (var nextTarget, var orbId) = nexts;

                IGameNetworkMessage message = new FinalBossOrbSpawned
                {
                    OrbType = orb,
                    Target = nextTarget,
                    OrbId = orbId
                };

                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableUnordered);

                nexts = finalBossOrbManagerService.GetNextTargetAndOrbId();
            }

            finalBossOrbManagerService.ClearQueueNextTarget();
        }

        private void OnReceivedFinalBossOrbsSpawned(FinalBossOrbSpawned spawned)
        {
            var bossPosition = MusicController.Instance.finalFightController.boss.transform.position;

            switch (spawned.OrbType)
            {
                case Orb.Bleed:
                    var gameObject = GameObject.Instantiate(MusicController.Instance.finalFightController.orbBleed);
                    gameObject.transform.position = new Vector3(bossPosition.x, bossPosition.y, bossPosition.z);
                    var orbBleed = gameObject.GetComponent<BossOrbBleed>();
                    orbBleed.isFired = false;
                    orbBleed.Set(MusicController.Instance.finalFightController.boss, MusicController.Instance.finalFightController.currentPhase, MusicController.Instance.finalFightController.currentPhase + 1, 1);

                    var interpolator = gameObject.AddComponent<BossOrbInterpolator>();
                    interpolator.Initialize(gameObject);
                    finalBossOrbManagerService.SetOrbTarget(spawned.Target, gameObject, spawned.OrbId);

                    break;
                case Orb.Following:
                    var gameObjectF = GameObject.Instantiate(MusicController.Instance.finalFightController.orbFollowing);
                    gameObjectF.transform.position = new Vector3(bossPosition.x, bossPosition.y, bossPosition.z);
                    var orbFollowing = gameObjectF.GetComponent<BossOrb>();
                    orbFollowing.isFired = false;
                    orbFollowing.Set(1, MusicController.Instance.finalFightController.currentPhase, MusicController.Instance.finalFightController.boss, 1, 1);

                    var interpolatorF = gameObjectF.AddComponent<BossOrbInterpolator>();
                    interpolatorF.Initialize(gameObjectF);
                    finalBossOrbManagerService.SetOrbTarget(spawned.Target, gameObjectF, spawned.OrbId);

                    break;
                case Orb.Shooty:
                    var gameObjectS = GameObject.Instantiate(MusicController.Instance.finalFightController.orbShooty);
                    gameObjectS.transform.position = new Vector3(bossPosition.x, bossPosition.y, bossPosition.z);
                    var orbShooty = gameObjectS.GetComponent<BossOrbShooty>();
                    orbShooty.isFired = false;
                    orbShooty.Set(MusicController.Instance.finalFightController.boss, MusicController.Instance.finalFightController.currentPhase, MusicController.Instance.finalFightController.currentPhase + 1, 1);

                    var interpolatorS = gameObjectS.AddComponent<BossOrbInterpolator>();
                    interpolatorS.Initialize(gameObjectS);
                    finalBossOrbManagerService.SetOrbTarget(spawned.Target, gameObjectS, spawned.OrbId);

                    break;
            }
        }


        private void OnReceivedFinalBossOrbsUpdate(IEnumerable<BossOrbModel> bossOrbs)
        {
            foreach (var bossOrb in bossOrbs)
            {
                var obj = finalBossOrbManagerService.GetOrbById(bossOrb.Id);
                if (obj == null)
                {
                    continue;
                }

                var interpolator = obj.GetComponent<BossOrbInterpolator>();
                if (interpolator == null)
                {
                    continue;
                }

                var snapshot = new BossOrbSnapshot
                {
                    Timestamp = Time.timeAsDouble,
                    Position = Quantizer.Dequantize(bossOrb.Position),
                };

                interpolator.AddSnapshot(snapshot);
            }
        }

        public void OnFinalBossOrbDestroyed(uint removed)
        {
            IGameNetworkMessage message = new FinalBossOrbDestroyed
            {
                OrbId = removed,
                SenderId = playerManagerService.GetLocalPlayer().ConnectionId
            };

            var isHost = IsServerMode() ?? false;
            if (isHost)
            {
                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableUnordered);
            }
            else
            {
                udpClientService.SendToHost(message);
            }
        }

        private void OnReceivedFinalBossOrbDestroyed(FinalBossOrbDestroyed destroyed)
        {
            var orbObj = finalBossOrbManagerService.GetOrbById(destroyed.OrbId);
            if (orbObj != null)
            {
                finalBossOrbManagerService.RemoveOrbTarget(orbObj);
                GameObject.Destroy(orbObj);
            }
            else
            {
                logger.LogWarning($"Failed to destroy orb with id {destroyed.OrbId} as its not found");
            }
        }

        public void OnSwarmEvent(TimelineEvent currentEvent)
        {
            IGameNetworkMessage message = new StartedSwarmEvent
            {
                Duration = currentEvent.duration,
            };

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        private void OnReceivedSwarmEvent(StartedSwarmEvent startedSwarmEvent)
        {
            var timeline = new TimelineEvent
            {
                duration = startedSwarmEvent.Duration,
                enemies = new Il2CppSystem.Collections.Generic.List<EEnemy>(),
                eTimelineEvent = ETimelineEvent.ESwarm
            };
            EnemyManager.Instance.summonerController.EventSwarm(timeline);
        }

        private void OnReceivedGameOver(GameOver over)
        {
            udpClientService.GameOver();
            TransitionToState(GameEvent.GameOver);
        }

        public void OnPlayerDied()
        {
            var localPlayer = playerManagerService.GetLocalPlayer();

            if (localPlayer.Hp == 0)
            {
                return;
            }

            localPlayer.Hp = 0;
            playerManagerService.UpdatePlayer(localPlayer);
            GameManager.Instance.player.playerRenderer.gameObject.SetActive(false);

            var isHost = IsServerMode() ?? false;
            var playerId = localPlayer.ConnectionId;

            IGameNetworkMessage diedMessage = new PlayerDied
            {
                PlayerId = localPlayer.ConnectionId
            };

            if (!isHost)
            {
                udpClientService.SendToHost(diedMessage);
            }
            else
            {
                udpClientService.SendToAllClients(diedMessage, LiteNetLib.DeliveryMethod.ReliableOrdered);

                var allPlayersAliveIdWithout = playerManagerService.GetAllPlayersAlive().Where(p => p.ConnectionId != playerId).Select(p => p.ConnectionId).ToList();
                var updated = enemyManagerService.ReTargetEnemies(playerId, allPlayersAliveIdWithout);

                IGameNetworkMessage message = new RetargetedEnemies
                {
                    Enemy_NewTargetids = updated
                };

                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);

                SpawnReviver(GameManager.Instance.player.transform.position, GameManager.Instance.player.playerRenderer.activeMaterials, localPlayer.ConnectionId);
            }
        }

        private void OnReceivedPlayerDied(PlayerDied died)
        {
            var isServer = IsServerMode() ?? false;
            if (isServer)
            {
                var diedPlayer = playerManagerService.GetPlayer(died.PlayerId);
                if (diedPlayer == null)
                {
                    logger.LogWarning("Died player not found in PlayerManagerService when processing OnReceivedPlayerDied.");
                    return;
                }

                diedPlayer.Hp = 0;
                playerManagerService.UpdatePlayer(diedPlayer);
                var netPlayer = playerManagerService.GetNetPlayerByNetplayId(died.PlayerId);
                if (netPlayer == null)
                {
                    logger.LogWarning("Died netplayer not found in PlayerManagerService when processing OnReceivedPlayerDied.");
                    return;
                }

                netPlayer.OnDied();

                IGameNetworkMessage diedMessage = new PlayerDied
                {
                    PlayerId = died.PlayerId
                };

                udpClientService.SendToAllClients(diedMessage, LiteNetLib.DeliveryMethod.ReliableOrdered);


                var allPlayersAliveIdWithout = playerManagerService.GetAllPlayersAlive().Where(p => p.ConnectionId != died.PlayerId).Select(p => p.ConnectionId).ToList();
                var updated = enemyManagerService.ReTargetEnemies(died.PlayerId, allPlayersAliveIdWithout);

                IGameNetworkMessage message = new RetargetedEnemies
                {
                    Enemy_NewTargetids = updated
                };

                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);

                SpawnReviver(netPlayer.Model.transform.position, netPlayer.GetActiveMaterials(), diedPlayer.ConnectionId);
            }
            else
            {
                var diedNetPlayer = playerManagerService.GetNetPlayerByNetplayId(died.PlayerId);
                if (diedNetPlayer == null)
                {
                    return;
                }

                diedNetPlayer.OnDied();
            }
        }

        private void SpawnReviver(Vector3 position, Material[] materials, uint ownerConnectionId, uint reviverId = 0)
        {
            var desertGraves = EffectManager.Instance.desertGraves;
            Plugin.CAN_SEND_MESSAGES = false;
            var desertGraveInstance = GameObject.Instantiate(desertGraves[0], position, Quaternion.Euler(-90, 0, 0));
            Plugin.CAN_SEND_MESSAGES = true;
            var interactable = desertGraveInstance.GetComponent<InteractableDesertGrave>();
            var chargeFx = GameObject.Instantiate(interactable.chargeFx, desertGraveInstance.transform);
            var explodeFx = GameObject.Instantiate(interactable.explodeFx, desertGraveInstance.transform);

            var isHost = IsServerMode() ?? false;
            var netplayId = reviverId;

            if (isHost)
            {
                netplayId = spawnedObjectManagerService.AddSpawnedObject(desertGraveInstance);

                IGameNetworkMessage message = new SpawnedReviver
                {
                    Position = position.ToNumericsVector3(),
                    OwnerConnectionId = ownerConnectionId,
                    ReviverId = netplayId
                };

                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                spawnedObjectManagerService.SetSpawnedObject(netplayId, desertGraveInstance);
            }

            var reviver = desertGraveInstance.AddComponent<InteractableReviver>();
            reviver.Initialize(chargeFx, explodeFx, materials[0], netplayId, ownerConnectionId);


            GameObject.Destroy(interactable);

        }

        private void OnReceivedSpawnedReviver(SpawnedReviver reviver)
        {
            var player = playerManagerService.GetNetPlayerByNetplayId(reviver.OwnerConnectionId);
            if (player != null)
            {
                SpawnReviver(reviver.Position.ToUnityVector3(), player.GetActiveMaterials(), reviver.OwnerConnectionId, reviver.ReviverId);

            }
            else if (reviver.OwnerConnectionId == playerManagerService.GetLocalPlayer().ConnectionId)
            {
                SpawnReviver(reviver.Position.ToUnityVector3(), GameManager.Instance.player.playerRenderer.activeMaterials, reviver.OwnerConnectionId, reviver.ReviverId);
            }
            else
            {
                logger.LogWarning("Owner player not found in PlayerManagerService when processing OnReceivedSpawnedRviver.");
            }
        }

        private void OnReceivedRetargetedEnemies(RetargetedEnemies enemies)
        {
            var isServer = IsServerMode() ?? false;
            if (!isServer)
            {
                var playerId_rigidbody = playerManagerService.GetConnectionIdsAndRigidBodies();
                enemyManagerService.ApplyRetargetedEnemies(enemies.Enemy_NewTargetids, playerId_rigidbody);
            }
        }

        public void OnRunStarted(RunConfig runConfig)
        {
            TransitionToState(GameEvent.Loading);

            IGameNetworkMessage message = new RunStarted
            {
                MapData = (int)runConfig.mapData.eMap,
                StageData = runConfig.stageData.name,
                MapTierIndex = runConfig.mapTierIndex,
                MusicTrackIndex = runConfig.musicTrackIndex,
                ChallengeName = runConfig.challenge?.name ?? ""
            };

            var isHost = IsServerMode() ?? false;
            if (isHost)
            {
                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
        }
        private void OnReceivedRunStarted(RunStarted started)
        {
            var mapData = (Assets.Scripts._Data.MapsAndStages.EMap)started.MapData;
            var stageDataName = started.StageData;

            var map = DataManager.Instance.GetMap(mapData);
            var stageData = map.stages.FirstOrDefault(s => s.name == stageDataName);

            if (stageData == null)
            {
                logger.LogWarning($"Stage data {stageDataName} not found in map {mapData} when processing OnReceivedRunStarted.");
                return;
            }

            var runConfig = new RunConfig
            {
                mapData = map,
                stageData = stageData,
                mapTierIndex = started.MapTierIndex,
                musicTrackIndex = started.MusicTrackIndex,
            };

            ChallengeData currentChallenge = stageData.challenges.FirstOrDefault(c => c.name == started.ChallengeName);

            if (currentChallenge != null)
            {
                runConfig.challenge = currentChallenge;
            }

            logger.LogInfo($"Received RunStarted message. Starting new map {mapData} with stage {stageDataName} at index {runConfig.mapTierIndex} with challenge {started.ChallengeName}.");

            TransitionToState(GameEvent.Loading);

            Plugin.Instance.HideModal();
            MapController.StartNewMap(runConfig);
        }

        private void OnReceivedPlayerDisconnected(PlayerDisconnected disconnected)
        {
            var disconnectedPeer = playerManagerService.GetPlayer(disconnected.ConnectionId);
            if (disconnectedPeer == null)
            {
                logger.LogWarning("Disconnected player not found in PlayerManagerService when processing OnReceivedPlayerDisconnected.");
                return;
            }

            Plugin.StartNotification(
                ("MegabonkTogether", "PlayerDisconnected"),
                ("MegabonkTogether", "PlayerDisconnected_Description"),
                [disconnectedPeer.Name],
                AudioManager.Instance.uiAbort,
                item: EItem.BobDead
            );

            playerManagerService.Disconnect(disconnected.ConnectionId);
            projectileManagerService.RemoveProjectilesByOwnerId(disconnected.ConnectionId);

            var isHost = IsServerMode() ?? false;

            if (!isHost)
            {
                return;
            }

            if (GameManager.Instance == null || GameManager.Instance.player == null)
            {
                return;
            }

            var allPlayersAliveIdWithout = playerManagerService.GetAllPlayersAlive().Where(p => p.ConnectionId != disconnected.ConnectionId).Select(p => p.ConnectionId).ToList();
            var updated = enemyManagerService.ReTargetEnemies(disconnected.ConnectionId, allPlayersAliveIdWithout);

            IGameNetworkMessage message = new RetargetedEnemies
            {
                Enemy_NewTargetids = updated
            };

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        public void OnLightningStrike(Enemy enemy, int bounces, DamageContainer dc, float bounceRange, float bounceProcCoefficient)
        {
            var enemySpawned = enemyManagerService.GetEnemyByReference(enemy);
            if (enemySpawned.Value == null)
            {
                //logger.LogWarning("Enemy not found in EnemyManagerService when processing OnLightningStrike.");
                return;
            }

            var ownerId = playerManagerService.GetLocalPlayer().ConnectionId;

            IGameNetworkMessage message = new LightningStrike
            {
                EnemyId = enemySpawned.Key,
                Bounces = bounces,
                Damage = dc.damage,
                DamageEffect = (int)dc.damageEffect,
                DamageBlockedByArmor = dc.damageBlockedByArmor,
                DamageSource = dc.damageSource,
                DamageIsCrit = dc.crit,
                DamageProcCoefficient = dc.procCoefficient,
                DamageElement = (int)dc.element,
                DamageFlags = (int)dc.flags,
                DamageKnockback = dc.knockback,
                BounceRange = bounceRange,
                BounceProcCoefficient = bounceProcCoefficient,
                OwnerId = ownerId
            };

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        private void OnReceivedLightningStrike(LightningStrike lightningStrike)
        {
            var enemy = enemyManagerService.GetEnemyById(lightningStrike.EnemyId);
            if (enemy == null)
            {
                //logger.LogWarning("Enemy not found in EnemyManagerService when processing OnReceivedLightningStrike.");
                return;
            }

            var damageContainer = new DamageContainer(lightningStrike.DamageProcCoefficient, lightningStrike.DamageSource);
            damageContainer.damage = lightningStrike.Damage;
            damageContainer.damageEffect = (EDamageEffect)lightningStrike.DamageEffect;
            damageContainer.damageBlockedByArmor = lightningStrike.DamageBlockedByArmor;
            damageContainer.crit = lightningStrike.DamageIsCrit;
            damageContainer.element = (EElement)lightningStrike.DamageElement;
            damageContainer.flags = (DcFlags)lightningStrike.DamageFlags;
            damageContainer.knockback = lightningStrike.DamageKnockback;
            damageContainer.damageSource = lightningStrike.DamageSource;
            damageContainer.procCoefficient = lightningStrike.DamageProcCoefficient;

            Plugin.CAN_SEND_MESSAGES = false;
            WeaponUtility.LightningStrike(
                enemy,
                lightningStrike.Bounces,
                damageContainer,
                lightningStrike.BounceRange,
                lightningStrike.BounceProcCoefficient
            );
            Plugin.CAN_SEND_MESSAGES = true;
        }

        public void OnTornadoesSpawned(int amount)
        {
            IGameNetworkMessage message = new TornadoesSpawned
            {
                Amount = amount
            };

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        private void OnReceivedTornadoesSpawned(TornadoesSpawned spawned)
        {
            Plugin.Instance.CAN_SPAWN_TORNADOES = true;
            EffectManager.Instance.SpawnTornadoes(spawned.Amount);
            Plugin.Instance.CAN_SPAWN_TORNADOES = false;
        }

        public void OnStormStarted(DesertStorm desertStorm)
        {
            var stormOverAtTime = desertStorm.fadeOverTime;
            IGameNetworkMessage message = new StormStarted
            {
                StormOverAtTime = stormOverAtTime
            };
            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        private void OnReceivedStormStarted(StormStarted started)
        {
            Plugin.Instance.CAN_START_STOP_STORMS = true;
            var desertEvent = Plugin.Instance.GetMapEventsDesert();
            desertEvent.StartStorm();
            desertEvent.stormOverAtTime = started.StormOverAtTime;
            Plugin.Instance.CAN_START_STOP_STORMS = false;
        }

        public void OnStormStopped()
        {
            IGameNetworkMessage message = new StormStopped();
            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        private void OnReceivedStormStopped(StormStopped stopped)
        {
            Plugin.Instance.CAN_START_STOP_STORMS = true;
            Plugin.Instance.GetMapEventsDesert().StopStorm();
            Plugin.Instance.CAN_START_STOP_STORMS = false;
        }

        public void OnTumbleWeedSpawned(InteractableTumbleWeed tumbleWeed)
        {
            var netplayId = spawnedObjectManagerService.AddSpawnedObject(tumbleWeed.gameObject);

            IGameNetworkMessage message = new TumbleWeedSpawned
            {
                NetplayId = netplayId,
                Position = Quantizer.Quantize(tumbleWeed.transform.position),
                Velocity = Quantizer.Quantize(tumbleWeed.rb.velocity),
            };

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);

            DynamicData.For(tumbleWeed).Set("netplayId", netplayId);
        }

        private void OnReceivedTumbleWeedSpawned(TumbleWeedSpawned spawned)
        {
            var tumbleWeedObj = GameObject.Instantiate(EffectManager.Instance.tumbleweed);
            var interactable = tumbleWeedObj.GetComponent<InteractableTumbleWeed>();
            spawnedObjectManagerService.SetSpawnedObject(spawned.NetplayId, tumbleWeedObj);
            interactable.transform.position = Quantizer.Dequantize(spawned.Position);
            interactable.rb.velocity = Quantizer.Dequantize(spawned.Velocity);

            spawnedObjectManagerService.RegisterTumbleWeedForInterpolation(spawned.NetplayId, tumbleWeedObj);
        }

        private void OnReceivedTumbleWeedsUpdate(IEnumerable<TumbleWeedModel> tumbles)
        {
            if (currentState < State.Started)
            {
                return;
            }

            if (tumbles == null || !tumbles.Any())
            {
                return;
            }

            var tumbleWeedSnapshots = new List<TumbleWeedSnapshot>();

            foreach (var model in tumbles)
            {

                var snapshot = new TumbleWeedSnapshot
                {
                    Timestamp = Time.timeAsDouble,
                    Position = Quantizer.Dequantize(model.Position),
                    Id = model.NetplayId
                };
                tumbleWeedSnapshots.Add(snapshot);
            }

            spawnedObjectManagerService.UpdateTumbleWeedSnapshots(tumbleWeedSnapshots);
        }

        public void OnTumbleWeedDespawned(InteractableTumbleWeed instance)
        {
            var netplayId = spawnedObjectManagerService.GetByReference(instance.gameObject);
            if (!netplayId.HasValue)
            {
                netplayId = DynamicData.For(instance).Get<uint?>("netplayId"); //Second attempt
                if (!netplayId.HasValue)
                {
                    logger.LogWarning("TumbleWeed not found in SpawnedObjectManagerService when processing OnTumbleWeedDespawned.");

                    return;
                }
            }

            spawnedObjectManagerService.RemoveSpawnedObject(netplayId.Value, instance.gameObject, false);

            IGameNetworkMessage message = new TumbleWeedDespawned
            {
                NetplayId = netplayId.Value
            };
            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        private void OnReceivedTumbleWeedDespawned(TumbleWeedDespawned despawned)
        {
            var tumbleWeedObj = spawnedObjectManagerService.GetSpawnedObject(despawned.NetplayId);
            if (tumbleWeedObj == null)
            {
                logger.LogWarning("TumbleWeed not found in SpawnedObjectManagerService when processing OnReceivedTumbleWeedDespawned.");
                return;
            }

            spawnedObjectManagerService.UnregisterTumbleWeedFromInterpolation(despawned.NetplayId);
            spawnedObjectManagerService.RemoveSpawnedObject(despawned.NetplayId, tumbleWeedObj);
        }

        public void OnInteractableFightEnemySpawned(InteractableCharacterFight instance)
        {
            var netplayId = spawnedObjectManagerService.GetByReferenceInChildren<InteractableCharacterFight>(instance.gameObject);

            if (!netplayId.HasValue)
            {
                logger.LogWarning("InteractableCharacterFight has no id when processing OnInteractableFightEnemySpawned.");
                return;
            }

            IGameNetworkMessage message = new InteractableCharacterFightEnemySpawned
            {
                NetplayId = netplayId.Value,
            };

            udpClientService.SendToHost(message);
        }

        private void OnReceivedInteractableFightEnemySpawned(InteractableCharacterFightEnemySpawned spawned)
        {
            var spawnedObj = spawnedObjectManagerService.GetSpawnedObject(spawned.NetplayId);
            if (spawnedObj == null)
            {
                logger.LogWarning("InteractableCharacterFight not found in SpawnedObjectManagerService when processing OnReceivedInteractableFightEnemySpawned.");
                return;
            }
            var interactable = spawnedObj.GetComponentInChildren<InteractableCharacterFight>();
            interactable.SpawnEnemy();
        }

        public void OnItemAdded(EItem item)
        {
            IGameNetworkMessage message = new ItemAdded
            {
                EItem = (int)item,
                OwnerId = playerManagerService.GetLocalPlayer().ConnectionId
            };

            var isServer = IsServerMode() ?? false;
            if (isServer)
            {
                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                udpClientService.SendToHost(message);
            }
        }

        private void OnReceivedItemAdded(ItemAdded added)
        {
            var netPlayer = playerManagerService.GetNetPlayerByNetplayId(added.OwnerId);
            if (netPlayer == null)
            {
                logger.LogWarning("NetPlayer not found in PlayerManagerService when processing OnReceivedItemAdded.");
                return;
            }

            var item = (EItem)added.EItem;
            Plugin.CAN_SEND_MESSAGES = false;
            netPlayer.AddItem(item);
            Plugin.CAN_SEND_MESSAGES = true;
        }

        public void OnItemRemoved(EItem item)
        {
            IGameNetworkMessage message = new ItemRemoved
            {
                EItem = (int)item,
                OwnerId = playerManagerService.GetLocalPlayer().ConnectionId
            };

            var isServer = IsServerMode() ?? false;
            if (isServer)
            {
                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                udpClientService.SendToHost(message);
            }
        }

        private void OnReceivedItemRemoved(ItemRemoved removed)
        {
            var netPlayer = playerManagerService.GetNetPlayerByNetplayId(removed.OwnerId);
            if (netPlayer == null)
            {
                logger.LogWarning("NetPlayer not found in PlayerManagerService when processing OnReceivedItemRemoved.");
                return;
            }

            var item = (EItem)removed.EItem;
            Plugin.CAN_SEND_MESSAGES = false;
            netPlayer.RemoveItem(item);
            Plugin.CAN_SEND_MESSAGES = true;
        }

        public void OnWeaponToggled(WeaponInventory instance, EWeapon eWeapon, bool enable)
        {
            IGameNetworkMessage message = new WeaponToggled
            {
                OwnerId = playerManagerService.GetLocalPlayer().ConnectionId,
                EWeapon = (int)eWeapon,
                Enabled = enable
            };

            var isServer = IsServerMode() ?? false;
            if (isServer)
            {
                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                udpClientService.SendToHost(message);
            }
        }

        private void OnReceivedWeaponToggled(WeaponToggled toggled)
        {
            var netPlayer = playerManagerService.GetNetPlayerByNetplayId(toggled.OwnerId);
            if (netPlayer == null)
            {
                logger.LogWarning("NetPlayer not found in PlayerManagerService when processing OnReceivedWeaponToggled.");
                return;
            }
            var weaponInventory = netPlayer.Inventory.weaponInventory;
            if (weaponInventory == null)
            {
                logger.LogWarning("WeaponInventory not found on NetPlayer when processing OnReceivedWeaponToggled.");
                return;
            }
            Plugin.CAN_SEND_MESSAGES = false;

            netPlayer.ToggleWeapon((EWeapon)toggled.EWeapon, toggled.Enabled);

            Plugin.CAN_SEND_MESSAGES = true;
        }

        public void OnSpawnedObjectInCrypt(GameObject obj)
        {
            var exist = spawnedObjectManagerService.GetByReference(obj);
            if (exist.HasValue)
            {
                return; //already registered
            }

            var netplayId = spawnedObjectManagerService.AddSpawnedObject(obj);
            DynamicData.For(obj).Set("netplayId", netplayId);

            var isCryptLeave = obj == RsgController.Instance.rsgEnd.gameObject;

            IGameNetworkMessage message = new SpawnedObjectInCrypt
            {
                NetplayId = netplayId,
                Position = Quantizer.Quantize(obj.transform.position),
                IsCryptLeave = isCryptLeave
            };

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        private void OnReceivedSpawnedObjectInCrypt(SpawnedObjectInCrypt crypt)
        {
            toUpdate.Add(crypt);
        }

        public void OnTimerStarted()
        {
            IGameNetworkMessage message = new TimerStarted
            {
                IsDungeonTimer = true,
                SenderId = playerManagerService.GetLocalPlayer().ConnectionId
            };

            var isHost = IsServerMode() ?? false;
            if (isHost)
            {
                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                udpClientService.SendToHost(message);
            }
        }

        private void OnReceivedTimerStarted(TimerStarted started)
        {
            if (started.IsDungeonTimer)
            {
                Plugin.Instance.HasDungeonTimerStarted = true;
                GameManager.Instance.StartDungeonTimer();
            }
        }

        public void OnHatChanged(EHat eHat)
        {
            IGameNetworkMessage message = new HatChanged
            {
                OwnerId = playerManagerService.GetLocalPlayer().ConnectionId,
                EHat = (int)eHat
            };

            var isServer = IsServerMode() ?? false;
            if (isServer)
            {
                udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            else
            {
                udpClientService.SendToHost(message);
            }
        }

        private void OnReceivedHatChanged(HatChanged changed)
        {
            var netPlayer = playerManagerService.GetNetPlayerByNetplayId(changed.OwnerId);
            if (netPlayer == null)
            {
                logger.LogWarning("NetPlayer not found in PlayerManagerService when processing OnReceivedHatChanged.");
                return;
            }

            var hatData = DataManager.Instance.GetHat((EHat)changed.EHat);

            Plugin.CAN_SEND_MESSAGES = false;
            netPlayer.SetHat(hatData);
            Plugin.CAN_SEND_MESSAGES = true;
        }

        public void OnSkinSelected(SkinData skinData)
        {
            var localPlayer = playerManagerService.GetLocalPlayer();

            if (localPlayer == null) return;

            localPlayer.Skin = skinData.name;
            playerManagerService.UpdatePlayer(localPlayer);
        }

        public void OnRespawn(uint ownerId, Vector3 position)
        {
            var netplayer = playerManagerService.GetNetPlayerByNetplayId(ownerId);
            if (netplayer != null)
            {
                netplayer.Respawn(position);
                var player = playerManagerService.GetPlayer(ownerId);
                player.Hp = player.MaxHp;
                playerManagerService.UpdatePlayer(player);
            }
            else
            {
                GameManager.Instance.player.transform.position = position;
                GameManager.Instance.player.inventory.playerHealth.hp = GameManager.Instance.player.inventory.playerHealth.maxHp;
                var localPlayer = playerManagerService.GetLocalPlayer();
                localPlayer.Hp = localPlayer.MaxHp;
                playerManagerService.UpdatePlayer(localPlayer);
                Plugin.Instance.CameraSwitcher.ResetToLocalPlayer();
                GameManager.Instance.player.playerRenderer.gameObject.SetActive(true);
            }

            IGameNetworkMessage message = new PlayerRespawned
            {
                OwnerId = ownerId,
                Position = Quantizer.Quantize(position)
            };

            udpClientService.SendToAllClients(message, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }


        private void OnReceivedPlayerRespawned(PlayerRespawned respawned)
        {
            var netplayer = playerManagerService.GetNetPlayerByNetplayId(respawned.OwnerId);
            if (netplayer != null)
            {
                netplayer.Respawn(Quantizer.Dequantize(respawned.Position));
            }
            else
            {
                GameManager.Instance.player.transform.position = Quantizer.Dequantize(respawned.Position);
                GameManager.Instance.player.inventory.playerHealth.hp = GameManager.Instance.player.inventory.playerHealth.maxHp;

                var localPlayer = playerManagerService.GetLocalPlayer();
                localPlayer.Hp = localPlayer.MaxHp;
                playerManagerService.UpdatePlayer(localPlayer);

                Plugin.Instance.CameraSwitcher.ResetToLocalPlayer();

                GameManager.Instance.player.playerRenderer.gameObject.SetActive(true);
            }
        }
    }
}
