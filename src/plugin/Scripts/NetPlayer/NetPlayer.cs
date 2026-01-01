using Assets.Scripts.Camera;
using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Utility;
using MegabonkTogether.Common.Messages;
using MegabonkTogether.Extensions;
using MegabonkTogether.Helpers;
using MegabonkTogether.Scripts.Snapshot;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace MegabonkTogether.Scripts.NetPlayer
{
    internal class SnapshotUpdate
    {
        public double Timestamp;

        public PlayerUpdate PlayerUpdate;
    }

    public class NetPlayer : MonoBehaviour
    {
        public GameObject Model { get; set; }
        private PlayerRenderer playerRenderer => Model.GetComponent<PlayerRenderer>();
        private Animator Animator => Model.GetComponent<Animator>();
        private PlayerInventory inventory { get; set; }

        public Rigidbody Rigidbody { get; internal set; }

        private Dictionary<EWeapon, ConstantAttack> constantAttacks = [];

        private uint connectionId { get; set; }
        public uint ConnectionId => connectionId;

        public PlayerInventory Inventory => inventory;

        private PlayerInterpolator interpolator;

        public StealWeaponWui StealWeaponWui { get; internal set; }
        public ReturnWeaponWui ReturnWeaponWui { get; internal set; }

        private IPlayerManagerService playerManagerService;
        private ISynchronizationService synchronizationService;
        private bool hasInitializedConstantAttacks = false;

        private GameObject minimapIcon;
        private GameObject nameplate;
        private TextMeshProUGUI nameplateText;

        private const float NAMEPLATE_MAX_DISTANCE = 100f;
        private const float NAMEPLATE_Y_OFFSET = 5.0f;

        protected void Awake()
        {
            interpolator = gameObject.AddComponent<PlayerInterpolator>();
            playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();
            synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        }

        protected void Update()
        {
            if (Model == null) return;

            if (GameManager.Instance.player.inventory != null && !hasInitializedConstantAttacks)
            {
                RefreshConstantAttack(new());
                hasInitializedConstantAttacks = true;
            }

            foreach (var constantAttack in constantAttacks.Values) //Needed as the ConstantAttacks to update
            {
                constantAttack.transform.position = this.Model.transform.position;

                if (constantAttack is ProjectileDragonsBreath)
                {
                    constantAttack.transform.rotation = this.Model.transform.rotation;
                }
                else
                {
                    constantAttack.transform.up = this.Model.transform.up;
                    constantAttack.transform.Rotate(
                        this.Model.transform.up,
                        MyTime.time * 20f * constantAttack.GetAuraRotationSpeed(),
                        Space.World
                    );
                }
            }

            UpdateNameplateText();
            UpdateNameplateRotation();
        }

        public void FixedUpdate()
        {
            //RefreshWeaponOwnerId();

            if (GameManager.Instance == null || GameManager.Instance.player == null || GameManager.Instance.player.inventory == null)
            {
                return;
            }

            if (inventory == null)
            {
                return;
            }


            var isHost = synchronizationService.IsServerMode() ?? false;

            if (!isHost)
            {
                inventory.statusEffects.Tick();
                return;
            }

            try
            {
                playerManagerService.AddGetNetplayerPositionRequest(connectionId);
                inventory.PhysicsTick();
                playerManagerService.UnqueueNetplayerPositionRequest();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error in Inventory.PhysicsTick: {ex}");
            }
        }

        private Quaternion GetRotation(PlayerUpdate update)
        {
            var axisInput = Quantizer.Dequantize(update.MovementState.AxisInput);
            if (axisInput.sqrMagnitude > 0.1f)
            {
                var camForward = Quantizer.Dequantize(update.MovementState.CameraForward);
                var camRight = Quantizer.Dequantize(update.MovementState.CameraRight);

                camForward.y = 0;
                camRight.y = 0;
                camForward.Normalize();
                camRight.Normalize();

                var moveDir = (camForward * axisInput.y + camRight * axisInput.x).normalized;

                return Quaternion.LookRotation(moveDir);
            }

            return Quaternion.identity;
        }

        public void AddUpdate(PlayerUpdate update)
        {
            var snapshot = new PlayerSnapshot
            {
                Timestamp = Time.timeAsDouble,
                Position = update.Position.ToUnityVector3(),
                Rotation = GetRotation(update),
                AnimatorState = update.AnimatorState,
            };

            interpolator.AddSnapshot(snapshot);
        }


        public void Initialize(ECharacter eCharacter, uint connectionId, string skin)
        {
            Plugin.Log.LogInfo($"Initializing NetPlayer for character {eCharacter} with ConnectionId {connectionId} and skin {skin}");
            this.connectionId = connectionId;

            var characterData = DataManager.Instance.GetCharacterData(eCharacter);
            this.Model = GameObject.Instantiate(characterData.prefab);
            this.Model.name = $"NetPlayer_{connectionId}_{eCharacter}";
            inventory = playerManagerService.GetPlayerInventory(connectionId);

            if (inventory == null)
            {
                Plugin.Instance.SavePlayerInventoryActions();
                inventory = new PlayerInventory(characterData);
                Plugin.Instance.RestorePlayerInventoryActions();

                playerManagerService.AddPlayerInventory(connectionId, inventory);
            }

            this.Model.transform.position = GameManager.Instance.player.GetFeetPosition() + new Vector3(2, 0, 0);
            this.Model.transform.rotation = Quaternion.LookRotation(GameManager.Instance.player.spawnDir, Vector3.up);

            var playerRenderer = Model.AddComponent<PlayerRenderer>();
            playerRenderer.SetCharacter(characterData, inventory, Vector3.zero);

            if (!string.IsNullOrEmpty(skin))
            {
                var allSkins = DataManager.Instance.GetSkins(eCharacter);
                SkinData currentSkin = null;
                foreach (var sk in allSkins)
                {
                    if (sk.name == skin)
                    {
                        currentSkin = sk;
                        break;
                    }
                }

                if (currentSkin != null)
                {
                    playerRenderer.SetSkin(currentSkin);
                    var smr = Model.GetComponentInChildren<SkinnedMeshRenderer>();
                    Il2CppFindHelper.RuntimeSetSharedMaterials(smr, playerRenderer.activeMaterials);
                }
            }

            playerRenderer.rendererObject.SetActive(false);

            Rigidbody = this.gameObject.AddComponent<Rigidbody>();

            var playerRigidbody = GameManager.Instance.player.GetComponent<Rigidbody>();
            Rigidbody.mass = playerRigidbody.mass;
            Rigidbody.drag = playerRigidbody.drag;
            Rigidbody.angularDrag = playerRigidbody.angularDrag;
            Rigidbody.useGravity = playerRigidbody.useGravity;
            Rigidbody.isKinematic = playerRigidbody.isKinematic;
            Rigidbody.interpolation = playerRigidbody.interpolation;
            Rigidbody.collisionDetectionMode = playerRigidbody.collisionDetectionMode;
            Rigidbody.constraints = playerRigidbody.constraints;
            Rigidbody.gameObject.layer = playerRigidbody.gameObject.layer;

            Rigidbody.transform.position = this.Model.transform.position;

            interpolator.Initialize(this.Model.transform, this.Animator, this.Model.GetComponent<HoverAnimations>());

            CreateMinimapIcon();
            CreateNameplate();
        }

        private void OnDestroy()
        {
            Destroy();
        }

        internal void Destroy()
        {
            if (minimapIcon != null)
            {
                GameObject.Destroy(minimapIcon);
            }

            if (nameplate != null)
            {
                GameObject.Destroy(nameplate);
            }

            if (nameplateText != null)
            {
                GameObject.Destroy(nameplateText);
            }

            if (interpolator != null)
            {
                GameObject.Destroy(interpolator);
            }

            foreach (var constantAttack in constantAttacks.Values)
            {
                if (constantAttack != null)
                {
                    GameObject.Destroy(constantAttack.gameObject);
                }
            }

            constantAttacks.Clear();

            if (StealWeaponWui != null)
            {
                GameObject.Destroy(StealWeaponWui.gameObject);
            }

            if (ReturnWeaponWui != null)
            {
                GameObject.Destroy(ReturnWeaponWui.gameObject);
            }

            inventory.Cleanup(); //Cleanup is important to prevent interference with local player

            GameObject.Destroy(this.Model);
        }

        private bool DoesWeaponNeedConstantAttack(WeaponData weaponData)
        {
            var eWeapon = weaponData.eWeapon;

            switch (eWeapon)
            {
                case EWeapon.Aura:
                case EWeapon.Aegis:
                case EWeapon.Chunkers:
                case EWeapon.DragonsBreath:
                case EWeapon.Frostwalker:
                case EWeapon.SpaceNoodle:
                    return true;
                default:
                    return false;
            }
        }

        //private void RefreshWeaponOwnerId()
        //{
        //    foreach (var weaponKey in inventory.weaponInventory.weapons.Keys)
        //    {
        //        var weapon = inventory.weaponInventory.weapons[weaponKey];
        //        DynamicData.For(weapon).Set("ownerId", connectionId);
        //    }
        //}

        public void RefreshConstantAttack(Il2CppSystem.Collections.Generic.List<StatModifier> upgradeModifiers)
        {
            playerManagerService.AddGetNetplayerPositionRequest(connectionId);

            foreach (var weaponKey in inventory.weaponInventory.weapons.Keys)
            {
                var weapon = inventory.weaponInventory.weapons[weaponKey];

                if (!DoesWeaponNeedConstantAttack(weapon.weaponData))
                {
                    continue;
                }

                if (constantAttacks.ContainsKey(weapon.weaponData.eWeapon))
                {
                    foreach (var upgrade in upgradeModifiers)
                    {
                        constantAttacks[weapon.weaponData.eWeapon].OnStatUpdate(upgrade.stat);
                    }
                    continue;
                }


                var attack = GameObject.Instantiate(weapon.weaponData.attack);

                switch (weapon.weaponData.eWeapon)
                {
                    case EWeapon.Aura:
                        var constantAttack = attack.GetComponent<CombatAura>();
                        constantAttack.Set(weapon);
                        constantAttack.transform.SetParent(this.Model.transform);
                        constantAttacks.Add(weapon.weaponData.eWeapon, constantAttack);
                        DynamicData.For(constantAttack).Set("ownerId", connectionId);
                        break;
                    case EWeapon.Aegis:
                        var aegisAttack = attack.GetComponent<AegisAttack>();
                        aegisAttack.Set(weapon);
                        aegisAttack.currentAmount = 2;
                        aegisAttack.transform.SetParent(this.Model.transform);
                        constantAttacks.Add(weapon.weaponData.eWeapon, aegisAttack);
                        DynamicData.For(aegisAttack).Set("ownerId", connectionId);
                        break;
                    case EWeapon.Chunkers:
                        var chunkersAttack = attack.GetComponent<ChunkersAttack>();
                        chunkersAttack.Set(weapon);
                        chunkersAttack.currentAmount = 2;
                        chunkersAttack.transform.SetParent(this.Model.transform);
                        constantAttacks.Add(weapon.weaponData.eWeapon, chunkersAttack);
                        DynamicData.For(chunkersAttack).Set("ownerId", connectionId);
                        break;
                    case EWeapon.DragonsBreath:
                        var dragonBreathAttack = attack.GetComponent<ProjectileDragonsBreath>();
                        dragonBreathAttack.Set(weapon);
                        dragonBreathAttack.transform.SetParent(this.Model.transform);
                        constantAttacks.Add(weapon.weaponData.eWeapon, dragonBreathAttack);
                        DynamicData.For(dragonBreathAttack).Set("ownerId", connectionId);
                        break;
                    case EWeapon.Frostwalker:
                        var frostwalkerAttack = attack.GetComponent<IceAura>();
                        frostwalkerAttack.Set(weapon);
                        frostwalkerAttack.transform.SetParent(this.Model.transform);
                        constantAttacks.Add(weapon.weaponData.eWeapon, frostwalkerAttack);
                        DynamicData.For(frostwalkerAttack).Set("ownerId", connectionId);
                        break;
                    case EWeapon.SpaceNoodle:
                        var laserBeamAttack = attack.GetComponent<LaserBeamAttack>();
                        laserBeamAttack.Set(weapon);
                        laserBeamAttack.transform.SetParent(this.Model.transform);
                        constantAttacks.Add(weapon.weaponData.eWeapon, laserBeamAttack);
                        DynamicData.For(laserBeamAttack).Set("ownerId", connectionId);
                        break;
                    default:
                        Plugin.Log.LogWarning($"Unhandled constant attack for weapon {weapon.weaponData.eWeapon}");
                        break;
                }
            }

            playerManagerService.UnqueueNetplayerPositionRequest();
        }

        private void CreateMinimapIcon()
        {
            try
            {
                var minimapCamera = GameManager.Instance.player.minimapCamera.GetComponent<MinimapCamera>();
                if (minimapCamera?.playerIcon == null)
                {
                    Plugin.Log.LogWarning("Cannot create minimap icon: playerIcon is null");
                    return;
                }

                var playerIcon = minimapCamera.playerIcon;

                minimapIcon = GameObject.Instantiate(playerIcon.gameObject);
                minimapIcon.name = $"MinimapIcon_NetPlayer_{connectionId}";

                minimapIcon.transform.SetParent(this.Model.transform, false);
                minimapIcon.transform.localPosition = playerIcon.localPosition;
                minimapIcon.transform.localRotation = playerIcon.localRotation;
                minimapIcon.transform.localScale = playerIcon.localScale;

                minimapIcon.layer = playerIcon.gameObject.layer;
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error creating minimap icon for NetPlayer {connectionId}: {ex}");
            }
        }

        private void CreateNameplate()
        {
            try
            {
                var player = playerManagerService.GetPlayer(connectionId);
                if (player == null)
                {
                    Plugin.Log.LogWarning($"Cannot create nameplate: Player not found for connectionId {connectionId}");
                    return;
                }

                if (UiManager.Instance == null)
                {
                    Plugin.Log.LogWarning("Np UiManager.Instance ?");
                    return;
                }

                nameplate = new GameObject($"Nameplate_NetPlayer_{connectionId}");
                UnityEngine.GameObject.DontDestroyOnLoad(nameplate);

                nameplateText = nameplate.AddComponent<TextMeshProUGUI>();

                nameplateText.text = player.Name;
                nameplateText.fontSize = 24;
                nameplateText.alignment = TextAlignmentOptions.Center;
                nameplateText.color = Color.white;
                nameplateText.outlineColor = Color.black;
                nameplateText.outlineWidth = 0.2f;
                nameplateText.fontStyle = FontStyles.Bold;

                nameplate.transform.SetParent(UiManager.Instance.encounterWindows.transform.parent, false);
                nameplateText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                nameplateText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                nameplateText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                nameplateText.rectTransform.sizeDelta = new Vector2(200, 50);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error creating nameplate for NetPlayer {connectionId}: {ex}");
            }
        }

        private void UpdateNameplateText()
        {
            if (nameplateText == null) return;
            var player = playerManagerService.GetPlayer(connectionId);
            if (player == null)
            {
                Plugin.Log.LogWarning($"Cannot update nameplate text: Player not found for connectionId {connectionId}");
                return;
            }
            if (nameplateText.text != player.Name)
            {
                nameplateText.text = player.Name;
            }
        }

        private void UpdateNameplateRotation()
        {
            if (nameplate == null || nameplateText == null || GameManager.Instance?.playerCamera == null || Model == null) return;

            try
            {
                var camera = GameManager.Instance.playerCamera.camera;
                if (camera == null) return;

                var localPlayerPos = GameManager.Instance.player.transform.position;
                var netPlayerPos = Model.transform.position;
                var distance = Vector3.Distance(localPlayerPos, netPlayerPos);

                if (distance > NAMEPLATE_MAX_DISTANCE)
                {
                    nameplate.SetActive(false);
                    return;
                }

                var worldPos = Model.transform.position + new Vector3(0, NAMEPLATE_Y_OFFSET, 0);
                var screenPos = camera.WorldToScreenPoint(worldPos);

                if (screenPos.z > 0)
                {
                    nameplateText.rectTransform.position = screenPos;
                    nameplate.SetActive(true);
                }
                else
                {
                    nameplate.SetActive(false);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error updating nameplate position: {ex}");
            }
        }

        public void UpdateMinimapIconColor()
        {
            if (minimapIcon == null) return;

            var meshRenderer = minimapIcon.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                var uiColor = Plugin.Instance.NetPlayersDisplayer.GetPlayerColor(connectionId);
                var material = new Material(meshRenderer.material);
                material.color = uiColor;

                meshRenderer.material = material;
            }
        }

        public void AddItem(EItem item)
        {
            var original = ItemInventory.A_ItemAdded;
            ItemInventory.A_ItemAdded = null;

            playerManagerService.AddGetNetplayerPositionRequest(connectionId);
            inventory.itemInventory.AddItem(item);
            EffectManager.Instance.OnItemAdded(item);
            playerManagerService.UnqueueNetplayerPositionRequest();

            ItemInventory.A_ItemAdded = original;
        }

        public void RemoveItem(EItem item)
        {
            var original = ItemInventory.A_ItemRemoved;
            ItemInventory.A_ItemRemoved = null;

            playerManagerService.AddGetNetplayerPositionRequest(connectionId);
            inventory.itemInventory.RemoveItem(item, false);
            EffectManager.Instance.OnItemRemoved(item, true);
            playerManagerService.UnqueueNetplayerPositionRequest();

            ItemInventory.A_ItemRemoved = original;
        }

        public void ToggleWeapon(EWeapon eWeapon, bool enabled)
        {
            playerManagerService.AddGetNetplayerPositionRequest(connectionId);

            var weapon = Inventory.weaponInventory.weapons[eWeapon];
            Inventory.weaponInventory.ToggleWeapon(eWeapon, enabled);

            if (!enabled)
            {
                var boss = MusicController.Instance.finalFightController.boss;
                var bossTransform = boss.transform;
                var bossCenter = boss.GetCenterPosition();
                var bossPosition = boss.transform.position;
                var direction = bossCenter - bossPosition;

                var steal = GameObject.Instantiate(EffectManager.Instance.stealItemWui, Model.transform);
                var component = steal.GetComponent<StealWeaponWui>();
                component.Set(weapon.weaponData, bossTransform, direction, 3.0f, 1.0f, 1.5f);
                StealWeaponWui = component;
            }
            else
            {
                var give = GameObject.Instantiate(EffectManager.Instance.giveItemWui, Model.transform);
                var component = give.GetComponent<ReturnWeaponWui>();
                component.Set(weapon.weaponData);
                ReturnWeaponWui = component;
            }

            playerManagerService.UnqueueNetplayerPositionRequest();
        }

        public void SetHat(HatData hatData)
        {
            var head = Il2CppFindHelper.RuntimeGetComponentsInChildren<Transform>(Model, true).FirstOrDefault(t => t.name.Contains("Head") && t.name.Contains("end"));
            if (head == null)
            {
                head = Il2CppFindHelper.RuntimeGetComponentsInChildren<Transform>(Model, true).FirstOrDefault(t => t.name.Contains("Hat"));
            }

            if (head == null)
            {
                Plugin.Log.LogWarning("Need to check all character to find their head bone but zzzzzzz");
                return;
            }

            playerRenderer.rendererObject.SetActive(true);
            playerRenderer.SetHat(hatData);

            playerRenderer.hatTransform.SetParent(head);
            playerRenderer.hatTransform.position = head.position;
            playerRenderer.rendererObject.SetActive(false);
        }

        public void OnDied()
        {
            Model.SetActive(false);
            Inventory.weaponInventory.Cleanup();
            Inventory.itemInventory.Cleanup();
        }
    }
}

