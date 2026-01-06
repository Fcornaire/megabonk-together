using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Managers;
using Il2CppInterop.Runtime;
using MegabonkTogether.Helpers;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using UnityEngine;

namespace MegabonkTogether.Scripts.Interactables
{
    public class InteractableReviver : BaseInteractable
    {
        private bool hasInteracted = false;
        private GameObject chargeFx;
        private GameObject explodeFx;
        private Enemy spawned;
        private Material customMaterial;
        private Il2CppSystem.Action<Enemy, DamageContainer> enemyDiedDelegate;
        private ISynchronizationService synchronizationService;
        private IPlayerManagerService playerManagerService;
        private IEnemyManagerService enemyManagerService;
        private uint reviverId;
        private uint ownerId;


        protected void Awake()
        {
            synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
            playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();
            enemyManagerService = Plugin.Services.GetService<IEnemyManagerService>();
        }

        public void SetSpawnedEnemy(Enemy enemy)
        {
            spawned = enemy;

            var renderers = Il2CppFindHelper.RuntimeGetComponentsInChildren<Renderer>(enemy.gameObject, true);

            foreach (var r in renderers)
            {
                if (r.gameObject.name == "Render")
                {
                    Material[] mats = [customMaterial];
                    Il2CppFindHelper.RuntimeSetSharedMaterials(r, mats);

                    enemy.enemyData.material = customMaterial;
                }
            }
        }

        public void Initialize(GameObject chargeFxPrefab, GameObject explodeFxPrefab, Material mat, uint reviverid, uint ownerId)
        {
            reviverId = reviverid;
            this.ownerId = ownerId;

            chargeFx = GameObject.Instantiate(chargeFxPrefab, this.transform);
            chargeFx.SetActive(false);
            explodeFx = GameObject.Instantiate(explodeFxPrefab, this.transform);
            explodeFx.SetActive(false);

            customMaterial = mat;

            enemyDiedDelegate = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<Enemy, DamageContainer>>(OnEnemyDied);
            Enemy.A_EnemyDied += enemyDiedDelegate;
        }

        public string GetFullName()
        {
            return $"{playerManagerService.GetPlayer(ownerId).Name} Ghost";
        }

        public override bool CanInteract()
        {
            return !hasInteracted;
        }

        public override string GetInteractString()
        {
            return "REVIVE!";
        }

        private void OnEnemyDied(Enemy enemy, DamageContainer dc)
        {
            if (spawned == null || enemy != spawned)
            {
                return;
            }

            var isServer = synchronizationService.IsServerMode() ?? false;

            if (!isServer)
            {
                return;
            }

            var respawnPosition = this.gameObject.transform.position + new Vector3(0, 2f, 0);

            synchronizationService.OnRespawn(ownerId, respawnPosition);
            enemyManagerService.RemoveReviverEnemy_Name(spawned);

            Enemy.A_EnemyDied -= enemyDiedDelegate;
            GameObject.Destroy(this.gameObject);
        }

        public override bool Interact()
        {
            if (hasInteracted) return false;
            hasInteracted = true;

            this.gameObject.GetComponent<Collider>().enabled = false;

            var meshRenderers = Il2CppFindHelper.RuntimeGetComponentsInChildren<MeshRenderer>(this.gameObject, true);
            foreach (var mr in meshRenderers)
            {
                mr.enabled = false;
            }

            var transforms = Il2CppFindHelper.RuntimeGetComponentsInChildren<Transform>(this.gameObject, true);

            foreach (var t in transforms)
            {
                if (t.gameObject.name.Contains("Beam"))
                {
                    t.gameObject.SetActive(false);
                }
            }

            chargeFx.SetActive(true);

            var isHost = synchronizationService.IsServerMode() ?? false;

            if (isHost)
            {
                CoroutineRunner.Instance.Run(SpawnEnemy());
            }


            return true;
        }

        private IEnumerator SpawnEnemy()
        {
            yield return new WaitForSeconds(0.5f);

            chargeFx.SetActive(false);

            Plugin.Instance.CurrentReviver = reviverId;
            var enemy = EnemyManager.Instance.SpawnBoss(Actors.Enemies.EEnemy.GhostGrave4, 0, EEnemyFlag.Boss, this.transform.position, 2f);
            enemyManagerService.AddReviverEnemy_Name(enemy, GetFullName());

            Plugin.Instance.CurrentReviver = null;

            var renderers = Il2CppFindHelper.RuntimeGetComponentsInChildren<Renderer>(enemy.gameObject, true);

            foreach (var r in renderers)
            {
                if (r.gameObject.name == "Render")
                {
                    Material[] mats = [customMaterial];
                    Il2CppFindHelper.RuntimeSetSharedMaterials(r, mats);

                    enemy.enemyData.material = customMaterial;
                    enemy.hp = 30;
                }
            }

            spawned = enemy;

            yield return null;
        }
    }
}
