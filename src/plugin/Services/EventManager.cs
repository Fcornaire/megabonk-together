using MegabonkTogether.Common.Messages;
using MegabonkTogether.Common.Messages.GameNetworkMessages;
using MegabonkTogether.Common.Models;
using MegabonkTogether.Scripts;
using System;
using System.Collections.Generic;

namespace MegabonkTogether.Services
{
    public static class EventManager
    {
        private static event Action<SpawnedObject> SpawnedObjectsEvents;
        private static event Action<SpawnedEnemy> SpawnedEnemyEvents;
        private static event Action<PlayerUpdate> PlayerUpdatesEvents;
        private static event Action<SelectedCharacter> SelectedCharacterEvents;
        private static event Action<IEnumerable<EnemyModel>> EnemiesUpdateEvents;
        private static event Action<EnemyDied> EnemyDiedEvents;
        private static event Action<AbstractSpawnedProjectile> SpawnedProjectileEvents;
        private static event Action<ProjectileDone> ProjectileDoneEvents;
        private static event Action<SpawnedPickupOrb> SpawnedPickupOrbEvents;
        private static event Action<SpawnedPickup> SpawnedPickupEvents;
        private static event Action<PickupApplied> PickupAppliedEvents;
        private static event Action<PickupFollowingPlayer> PickupFollowingPlayerEvents;
        private static event Action<SpawnedChest> SpawnedChestEvents;
        private static event Action<ChestOpened> ChestOpenedEvents;
        private static event Action<WeaponAdded> WeaponAddedEvents;
        private static event Action<InteractableUsed> InteractableUsedEvents;
        private static event Action<StartingChargingShrine> StartingChargingShrineEvents;
        private static event Action<StoppingChargingShrine> StoppingChargingShrineEvents;
        private static event Action<EnemyExploder> EnemyExploderEvents;
        private static event Action<EnemyDamaged> EnemyDamagedEvents;
        private static event Action<SpawnedEnemySpecialAttack> SpawnedEnemySpecialAttackEvents;
        private static event Action<StartingChargingPylon> StartingChargingPylonEvents;
        private static event Action<StoppingChargingPylon> StoppingChargingPylonEvents;
        private static event Action<FinalBossOrbSpawned> FinalBossOrbSpawnedEvents;
        private static event Action<IEnumerable<BossOrbModel>> FinalBossOrbsUpdateEvents;
        private static event Action<FinalBossOrbDestroyed> FinalBossOrbDestroyedEvents;
        private static event Action<StartedSwarmEvent> StartedSwarmEventEvents;
        private static event Action<GameOver> GameOverEvents;
        private static event Action<PlayerDied> PlayerDiedEvents;
        private static event Action<RetargetedEnemies> RetargetedEnemiesEvents;
        private static event Action<RunStarted> RunStartedEvents;
        private static event Action<PlayerDisconnected> PlayerDisconnectedEvents;
        private static event Action<IEnumerable<Projectile>> ProjectilesUpdateEvents;
        private static event Action<TomeAdded> TomeAddedEvents;
        private static event Action<LightningStrike> LightningStrikeEvents;
        private static event Action<TornadoesSpawned> TornadoesSpawnedEvents;
        private static event Action<StormStarted> StormStartedEvents;
        private static event Action<StormStopped> StormStoppedEvents;
        private static event Action<TumbleWeedSpawned> TumbleWeedSpawnedEvents;
        private static event Action<IEnumerable<TumbleWeedModel>> TumbleWeedsUpdateEvents;
        private static event Action<TumbleWeedDespawned> TumbleWeedDespawnedEvents;
        private static event Action<InteractableCharacterFightEnemySpawned> InteractableCharacterFightEnemySpawnedEvents;
        private static event Action<WantToStartFollowingPickup> WantToStartFollowingPickupEvents;
        private static event Action<ItemAdded> ItemAddedEvents;
        private static event Action<ItemRemoved> ItemRemovedEvents;
        private static event Action<WeaponToggled> WeaponToggledEvents;
        private static event Action GameStartedEvents;
        private static event Action PortalOpenedEvents;
        private static event Action<SpawnedObjectInCrypt> SpawnedObjectInCryptEvents;
        private static event Action<StartingChargingLamp> StartingChargingLampEvents;
        private static event Action<StoppingChargingLamp> StoppingChargingLampEvents;
        private static event Action<TimerStarted> TimerStartedEvents;
        private static event Action<HatChanged> HatChangedEvents;
        private static event Action<SpawnedReviver> SpawnedReviverEvents;
        private static event Action<PlayerRespawned> PlayerRespawnedEvents;

        public static void OnSpawnedObject(SpawnedObject spawnedObject)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                SpawnedObjectsEvents?.Invoke(spawnedObject);
            });
        }

        public static void SubscribeSpawnedObjectsEvents(Action<SpawnedObject> action)
        {
            SpawnedObjectsEvents += action;
        }

        public static void OnPlayerUpdate(PlayerUpdate playerUpdate)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                PlayerUpdatesEvents?.Invoke(playerUpdate);
            });
        }

        public static void SubscribePlayerUpdatesEvents(Action<PlayerUpdate> action)
        {
            PlayerUpdatesEvents += action;
        }

        public static void OnSpawnedEnemy(SpawnedEnemy spawnedEnemy)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                SpawnedEnemyEvents?.Invoke(spawnedEnemy);
            });
        }

        public static void SubscribeSpawnedEnemyEvents(Action<SpawnedEnemy> action)
        {
            SpawnedEnemyEvents += action;
        }

        public static void SubscribeSelectedCharacterEvents(Action<SelectedCharacter> action)
        {
            SelectedCharacterEvents += action;
        }

        public static void OnSelectedCharacter(SelectedCharacter selectedCharacter)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                SelectedCharacterEvents?.Invoke(selectedCharacter);
            });
        }

        public static void SubscribeEnemiesUpdateEvents(Action<IEnumerable<EnemyModel>> action)
        {
            EnemiesUpdateEvents += action;
        }

        public static void OnEnemiesUpdate(IEnumerable<EnemyModel> enemies)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                EnemiesUpdateEvents?.Invoke(enemies);
            });
        }

        public static void SubscribeEnemyDiedEvents(Action<EnemyDied> action)
        {
            EnemyDiedEvents += action;
        }

        public static void OnEnemyDied(EnemyDied enemyDied)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                EnemyDiedEvents?.Invoke(enemyDied);
            });
        }

        public static void SubscribeSpawnedProjectileEvents(Action<AbstractSpawnedProjectile> action)
        {
            SpawnedProjectileEvents += action;
        }
        public static void OnSpawnedProjectile(AbstractSpawnedProjectile spawnedProjectile)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                SpawnedProjectileEvents?.Invoke(spawnedProjectile);
            });
        }

        public static void SubscribeProjectileDoneEvents(Action<ProjectileDone> action)
        {
            ProjectileDoneEvents += action;
        }

        public static void OnProjectileDone(ProjectileDone projectileDone)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                ProjectileDoneEvents?.Invoke(projectileDone);
            });
        }

        public static void SubscribeSpawnedPickupOrbEvents(Action<SpawnedPickupOrb> action)
        {
            SpawnedPickupOrbEvents += action;
        }

        public static void OnSpawnedPickupOrb(SpawnedPickupOrb spawnedPickup)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                SpawnedPickupOrbEvents?.Invoke(spawnedPickup);
            });
        }

        public static void SubscribeSpawnedPickupEvents(Action<SpawnedPickup> action)
        {
            SpawnedPickupEvents += action;
        }

        public static void OnSpawnedPickup(SpawnedPickup spawnedPickup)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                SpawnedPickupEvents?.Invoke(spawnedPickup);
            });
        }


        public static void SubscribePickupAppliedEvents(Action<PickupApplied> action)
        {
            PickupAppliedEvents += action;
        }

        public static void OnPickupApplied(PickupApplied pickupApplied)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                PickupAppliedEvents?.Invoke(pickupApplied);
            });
        }

        public static void SubscribePickupFollowingPlayerEvents(Action<PickupFollowingPlayer> action)
        {
            PickupFollowingPlayerEvents += action;
        }
        public static void OnPickupFollowingPlayer(PickupFollowingPlayer pickupFollowingPlayer)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                PickupFollowingPlayerEvents?.Invoke(pickupFollowingPlayer);
            });
        }

        public static void SubscribeSpawnedChestEvents(Action<SpawnedChest> action)
        {
            SpawnedChestEvents += action;
        }
        public static void OnSpawnedChest(SpawnedChest spawnedChest)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                SpawnedChestEvents?.Invoke(spawnedChest);
            });
        }

        public static void SubscribeChestOpenedEvents(Action<ChestOpened> action)
        {
            ChestOpenedEvents += action;
        }

        public static void OnChestOpened(ChestOpened chestOpened)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                ChestOpenedEvents?.Invoke(chestOpened);
            });
        }

        public static void SubscribeWeaponAddedEvents(Action<WeaponAdded> action)
        {
            WeaponAddedEvents += action;
        }
        public static void OnWeaponAdded(WeaponAdded weaponAdded)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                WeaponAddedEvents?.Invoke(weaponAdded);
            });
        }

        public static void SubscribeInteractableUsedEvents(Action<InteractableUsed> action)
        {
            InteractableUsedEvents += action;
        }

        public static void OnInteractableUsed(InteractableUsed interactableUsed)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                InteractableUsedEvents?.Invoke(interactableUsed);
            });
        }

        public static void SubscribeStartingChargingShrineEvents(Action<StartingChargingShrine> action)
        {
            StartingChargingShrineEvents += action;
        }
        public static void OnStartingChargingShrine(StartingChargingShrine startingChargingShrine)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                StartingChargingShrineEvents?.Invoke(startingChargingShrine);
            });
        }

        public static void SubscribeStoppingChargingShrineEvents(Action<StoppingChargingShrine> action)
        {
            StoppingChargingShrineEvents += action;
        }

        public static void OnStoppingChargingShrine(StoppingChargingShrine stoppingChargingShrine)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                StoppingChargingShrineEvents?.Invoke(stoppingChargingShrine);
            });
        }

        public static void SubscribeEnemyExploderEvents(Action<EnemyExploder> action)
        {
            EnemyExploderEvents += action;
        }
        public static void OnEnemyExploder(EnemyExploder enemyExploder)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                EnemyExploderEvents?.Invoke(enemyExploder);
            });
        }

        public static void SubscribeEnemyDamagedEvents(Action<EnemyDamaged> action)
        {
            EnemyDamagedEvents += action;
        }

        public static void OnEnemyDamaged(EnemyDamaged enemyDamaged)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                EnemyDamagedEvents?.Invoke(enemyDamaged);
            });
        }

        public static void SubscribeSpawnedEnemySpecialAttackEvents(Action<SpawnedEnemySpecialAttack> action)
        {
            SpawnedEnemySpecialAttackEvents += action;
        }

        public static void OnSpawnedEnemySpecialAttack(SpawnedEnemySpecialAttack spawnedEnemySpecialAttack)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                SpawnedEnemySpecialAttackEvents?.Invoke(spawnedEnemySpecialAttack);
            });
        }

        public static void SubscribeStartingChargingPylonEvents(Action<StartingChargingPylon> action)
        {
            StartingChargingPylonEvents += action;
        }

        public static void OnStartingChargingPylon(StartingChargingPylon startingChargingPylon)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                StartingChargingPylonEvents?.Invoke(startingChargingPylon);
            });
        }

        public static void SubscribeStoppingChargingPylonEvents(Action<StoppingChargingPylon> action)
        {
            StoppingChargingPylonEvents += action;
        }

        public static void OnStoppingChargingPylon(StoppingChargingPylon stoppingChargingPylon)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                StoppingChargingPylonEvents?.Invoke(stoppingChargingPylon);
            });
        }

        public static void SubscribeFinalBossOrbSpawnedEvents(Action<FinalBossOrbSpawned> action)
        {
            FinalBossOrbSpawnedEvents += action;
        }

        public static void OnFinalBossOrbSpawned(FinalBossOrbSpawned finalBossOrbSpawned)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                FinalBossOrbSpawnedEvents?.Invoke(finalBossOrbSpawned);
            });
        }

        public static void SubscribeFinalBossOrbsUpdateEvents(Action<IEnumerable<BossOrbModel>> action)
        {
            FinalBossOrbsUpdateEvents += action;
        }

        public static void OnFinalBossOrbsUpdate(IEnumerable<BossOrbModel> bossOrbs)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                FinalBossOrbsUpdateEvents?.Invoke(bossOrbs);
            });
        }

        public static void SubscribeFinalBossOrbDestroyedEvents(Action<FinalBossOrbDestroyed> action)
        {
            FinalBossOrbDestroyedEvents += action;
        }

        public static void OnFinalBossOrbDestroyed(FinalBossOrbDestroyed finalBossOrbDestroyed)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                FinalBossOrbDestroyedEvents?.Invoke(finalBossOrbDestroyed);
            });
        }

        public static void SubscribeStartedSwarmEventEvents(Action<StartedSwarmEvent> action)
        {
            StartedSwarmEventEvents += action;
        }
        public static void OnStartedSwarmEvent(StartedSwarmEvent startedSwarmEvent)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                StartedSwarmEventEvents?.Invoke(startedSwarmEvent);
            });
        }

        public static void SubscribeGameOverEvents(Action<GameOver> action)
        {
            GameOverEvents += action;
        }

        public static void OnGameOver(GameOver gameOver)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                GameOverEvents?.Invoke(gameOver);
            });
        }

        public static void SubscribePlayerDiedEvents(Action<PlayerDied> action)
        {
            PlayerDiedEvents += action;
        }

        public static void OnPlayerDied(PlayerDied playerDied)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                PlayerDiedEvents?.Invoke(playerDied);
            });
        }

        public static void SubscribeRetargetedEnemiesEvents(Action<RetargetedEnemies> action)
        {
            RetargetedEnemiesEvents += action;
        }
        public static void OnRetargetedEnemies(RetargetedEnemies retargetedEnemies)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                RetargetedEnemiesEvents?.Invoke(retargetedEnemies);
            });
        }

        public static void SubscribeRunStartedEvents(Action<RunStarted> action)
        {
            RunStartedEvents += action;
        }
        public static void OnRunStarted(RunStarted runStarted)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                RunStartedEvents?.Invoke(runStarted);
            });
        }

        public static void SubscribePlayerDisconnectedEvents(Action<PlayerDisconnected> action)
        {
            PlayerDisconnectedEvents += action;
        }

        public static void OnPlayerDisconnected(PlayerDisconnected playerDisconnected)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                PlayerDisconnectedEvents?.Invoke(playerDisconnected);
            });
        }

        public static void SubscribeProjectilesUpdateEvents(Action<IEnumerable<Projectile>> action)
        {
            ProjectilesUpdateEvents += action;
        }

        public static void OnProjectilesUpdate(IEnumerable<Projectile> projectiles)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                ProjectilesUpdateEvents?.Invoke(projectiles);
            });
        }

        public static void SubscribeTomeAddedEvents(Action<TomeAdded> action)
        {
            TomeAddedEvents += action;
        }

        public static void OnTomeAdded(TomeAdded tomeAdded)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                TomeAddedEvents?.Invoke(tomeAdded);
            });
        }

        public static void SubscribeLightningStrikeEvents(Action<LightningStrike> action)
        {
            LightningStrikeEvents += action;
        }

        public static void OnLightningStrike(LightningStrike lightningStrike)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                LightningStrikeEvents?.Invoke(lightningStrike);
            });
        }

        public static void SubscribeTornadoesSpawnedEvents(Action<TornadoesSpawned> action)
        {
            TornadoesSpawnedEvents += action;
        }

        public static void OnTornadoesSpawned(TornadoesSpawned tornadoesSpawned)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                TornadoesSpawnedEvents?.Invoke(tornadoesSpawned);
            });
        }

        public static void SubscribeStormStartedEvents(Action<StormStarted> action)
        {
            StormStartedEvents += action;
        }

        public static void OnStormStarted(StormStarted stormStarted)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                StormStartedEvents?.Invoke(stormStarted);
            });
        }

        public static void SubscribeStormStoppedEvents(Action<StormStopped> action)
        {
            StormStoppedEvents += action;
        }

        public static void OnStormStopped(StormStopped stormStopped)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                StormStoppedEvents?.Invoke(stormStopped);
            });
        }

        public static void SubscribeTumbleWeedSpawnedEvents(Action<TumbleWeedSpawned> action)
        {
            TumbleWeedSpawnedEvents += action;
        }

        public static void OnTumbleWeedSpawned(TumbleWeedSpawned tumbleWeedSpawned)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                TumbleWeedSpawnedEvents?.Invoke(tumbleWeedSpawned);
            });
        }

        public static void SubscribeTumbleWeedsUpdateEvents(Action<IEnumerable<TumbleWeedModel>> action)
        {
            TumbleWeedsUpdateEvents += action;
        }

        public static void OnTumbleWeedsUpdate(IEnumerable<TumbleWeedModel> tumbleWeeds)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                TumbleWeedsUpdateEvents?.Invoke(tumbleWeeds);
            });
        }

        public static void SubscribeTumbleWeedDespawnedEvents(Action<TumbleWeedDespawned> action)
        {
            TumbleWeedDespawnedEvents += action;
        }

        public static void OnTumbleWeedDespawned(TumbleWeedDespawned tumbleWeedDespawned)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                TumbleWeedDespawnedEvents?.Invoke(tumbleWeedDespawned);
            });
        }

        public static void SubscribeInteractableCharacterFightEnemySpawnedEvents(Action<InteractableCharacterFightEnemySpawned> action)
        {
            InteractableCharacterFightEnemySpawnedEvents += action;
        }

        public static void OnInteractableCharacterFightEnemySpawned(InteractableCharacterFightEnemySpawned interactableCharacterFightEnemySpawned)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                InteractableCharacterFightEnemySpawnedEvents?.Invoke(interactableCharacterFightEnemySpawned);
            });
        }

        public static void SubscribeWantToStartFollowingPickupEvents(Action<WantToStartFollowingPickup> action)
        {
            WantToStartFollowingPickupEvents += action;
        }

        public static void OnWantToStartFollowingPickup(WantToStartFollowingPickup wantToStartFollowingPickup)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                WantToStartFollowingPickupEvents?.Invoke(wantToStartFollowingPickup);
            });
        }

        public static void SubscribeItemAddedEvents(Action<ItemAdded> action)
        {
            ItemAddedEvents += action;
        }

        public static void OnItemAdded(ItemAdded itemAdded)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                ItemAddedEvents?.Invoke(itemAdded);
            });
        }

        public static void SubscribeItemRemovedEvents(Action<ItemRemoved> action)
        {
            ItemRemovedEvents += action;
        }

        public static void OnItemRemoved(ItemRemoved itemRemoved)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                ItemRemovedEvents?.Invoke(itemRemoved);
            });
        }

        public static void SubscribeWeaponToggledEvents(Action<WeaponToggled> action)
        {
            WeaponToggledEvents += action;
        }

        public static void OnWeaponToggled(WeaponToggled weaponToggled)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                WeaponToggledEvents?.Invoke(weaponToggled);
            });
        }

        public static void SubscribeGameStartedEvents(Action action)
        {
            GameStartedEvents += action;
        }
        public static void OnGameStarted()
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                GameStartedEvents?.Invoke();
            });
        }

        public static void SubscribePortalOpenedEvents(Action action)
        {
            PortalOpenedEvents += action;
        }

        public static void OnPortalOpened()
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                PortalOpenedEvents?.Invoke();
            });
        }

        public static void SubscribeSpawnedObjectInCryptEvents(Action<SpawnedObjectInCrypt> action)
        {
            SpawnedObjectInCryptEvents += action;
        }

        public static void OnSpawnedObjectInCrypt(SpawnedObjectInCrypt spawnedObjectInCrypt)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                SpawnedObjectInCryptEvents?.Invoke(spawnedObjectInCrypt);
            });
        }

        public static void SubscribeStartingChargingLampEvents(Action<StartingChargingLamp> action)
        {
            StartingChargingLampEvents += action;
        }

        public static void OnStartingChargingLamp(StartingChargingLamp startingChargingLamp)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                StartingChargingLampEvents?.Invoke(startingChargingLamp);
            });
        }

        public static void SubscribeStoppingChargingLampEvents(Action<StoppingChargingLamp> action)
        {
            StoppingChargingLampEvents += action;
        }

        public static void OnStoppingChargingLamp(StoppingChargingLamp stoppingChargingLamp)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                StoppingChargingLampEvents?.Invoke(stoppingChargingLamp);
            });
        }

        public static void SubscribeTimerStartedEvents(Action<TimerStarted> action)
        {
            TimerStartedEvents += action;
        }

        public static void OnTimerStarted(TimerStarted timerStarted)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                TimerStartedEvents?.Invoke(timerStarted);
            });
        }

        public static void SubscribeHatChangedEvents(Action<HatChanged> action)
        {
            HatChangedEvents += action;
        }

        public static void OnHatChanged(HatChanged hatChanged)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                HatChangedEvents?.Invoke(hatChanged);
            });
        }

        public static void SubscribeSpawnedReviverEvents(Action<SpawnedReviver> action)
        {
            SpawnedReviverEvents += action;
        }

        public static void OnSpawnedReviver(SpawnedReviver spawnedReviver)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                SpawnedReviverEvents?.Invoke(spawnedReviver);
            });
        }

        public static void SubscribePlayerRespawnedEvents(Action<PlayerRespawned> action)
        {
            PlayerRespawnedEvents += action;
        }

        internal static void OnPlayerRespawned(PlayerRespawned playerRespawned)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                PlayerRespawnedEvents?.Invoke(playerRespawned);
            });
        }
    }
}
