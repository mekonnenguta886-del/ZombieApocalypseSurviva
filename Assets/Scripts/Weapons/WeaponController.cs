using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.AI;
using ZombieApocalypse.Audio;
using ZombieApocalypse.Player;
using ZombieApocalypse.Zombies;

namespace ZombieApocalypse.Weapons
{
    [Serializable]
    public class WeaponSlotState
    {
        public WeaponData weaponData;
        public int currentMagazineAmmo;
        public int reserveAmmo;

        public WeaponSlotState(WeaponData data)
        {
            weaponData = data;
            if (data != null)
            {
                currentMagazineAmmo = data.magazineSize;
                reserveAmmo = data.startingReserveAmmo;
            }
        }
    }

    /// <summary>
    /// Central weapon manager handling weapon switching (1, 2, 3 keys), over-the-shoulder hitscan raycasting,
    /// shotgun pellet spread, automatic/semi-automatic firing modes, magazine ammo tracking,
    /// reloading sequences (R key), recoil impulse, VFX/SFX, and HUD event broadcasting.
    /// 
    /// ATTACH TO: Player prefab GameObject.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        // Event parameters: (currentMag, reserveAmmo, weaponName, isReloading, isEmpty)
        public event Action<int, int, string, bool, bool> OnWeaponStateChanged;

        [Header("Equipped Weapon Slots")]
        [SerializeField] private List<WeaponData> availableWeapons = new List<WeaponData>();
        [SerializeField] private Transform muzzleTransform;
        [SerializeField] private LayerMask hitScanLayers = ~0;

        // Slot Runtime States
        private List<WeaponSlotState> weaponSlots = new List<WeaponSlotState>();
        private int currentSlotIndex = 0;
        private bool isReloading;
        private float lastFireTime;

        // Components
        private PlayerInputHandler inputHandler;
        private PlayerHealth playerHealth;
        private PlayerCamera playerCamera;
        private Camera mainCamera;

        public WeaponSlotState CurrentSlot => (currentSlotIndex >= 0 && currentSlotIndex < weaponSlots.Count) ? weaponSlots[currentSlotIndex] : null;
        public WeaponData CurrentWeapon => CurrentSlot != null ? CurrentSlot.weaponData : null;
        public bool IsReloading => isReloading;

        public float GetEffectiveDamage()
        {
            if (CurrentWeapon == null) return 20f;
            float baseDamage = WeaponUpgradeSystem.Instance != null
                ? WeaponUpgradeSystem.Instance.GetEffectiveDamage(CurrentWeapon)
                : CurrentWeapon.damage;

            float combatMult = ZombieApocalypse.Progression.PlayerProgressionSystem.Instance != null
                ? ZombieApocalypse.Progression.PlayerProgressionSystem.Instance.GetCombatDamageMultiplier()
                : 1.0f;

            return baseDamage * combatMult;
        }

        public int GetEffectiveMagazineSize()
        {
            if (CurrentWeapon == null) return 12;
            return WeaponUpgradeSystem.Instance != null
                ? WeaponUpgradeSystem.Instance.GetEffectiveMagazineSize(CurrentWeapon)
                : CurrentWeapon.magazineSize;
        }

        public float GetEffectiveFireRate()
        {
            if (CurrentWeapon == null) return 0.25f;
            return WeaponUpgradeSystem.Instance != null
                ? WeaponUpgradeSystem.Instance.GetEffectiveFireRate(CurrentWeapon)
                : CurrentWeapon.fireRate;
        }

        public float GetEffectiveRecoil()
        {
            if (CurrentWeapon == null) return 1.0f;
            return WeaponUpgradeSystem.Instance != null
                ? WeaponUpgradeSystem.Instance.GetEffectiveRecoil(CurrentWeapon)
                : CurrentWeapon.recoilAmount;
        }

        private void Awake()
        {
            inputHandler = GetComponent<PlayerInputHandler>();
            playerHealth = GetComponent<PlayerHealth>();
            playerCamera = GetComponent<PlayerCamera>();

            if (playerCamera == null && Camera.main != null)
            {
                playerCamera = Camera.main.GetComponent<PlayerCamera>();
            }
        }

        private void Start()
        {
            mainCamera = Camera.main;

            // Initialize weapon slots
            InitializeWeaponSlots();
        }

        private void InitializeWeaponSlots()
        {
            weaponSlots.Clear();
            foreach (var data in availableWeapons)
            {
                if (data != null)
                {
                    weaponSlots.Add(new WeaponSlotState(data));
                }
            }

            if (weaponSlots.Count > 0)
            {
                SelectWeaponSlot(0);
            }
        }

        private void Update()
        {
            // Player Death Safety Guard: Instantly stop weapon actions, cancel reloading
            if (playerHealth != null && playerHealth.IsDead)
            {
                if (isReloading)
                {
                    isReloading = false;
                    StopAllCoroutines();
                    NotifyHUD();
                }
                return;
            }

            // Inventory Open Guard: Block weapon switching, reloading, and firing while managing inventory UI
            if (inputHandler != null && inputHandler.IsInventoryOpen)
            {
                return;
            }

            HandleWeaponSwitchingInput();
            HandleReloadInput();
            HandleFiringInput();
        }

        private void HandleWeaponSwitchingInput()
        {
            if (isReloading || inputHandler == null) return;

            if (inputHandler.Weapon1Triggered)
            {
                SelectWeaponSlot(0);
                inputHandler.ResetWeaponTriggers();
            }
            else if (inputHandler.Weapon2Triggered)
            {
                SelectWeaponSlot(1);
                inputHandler.ResetWeaponTriggers();
            }
            else if (inputHandler.Weapon3Triggered)
            {
                SelectWeaponSlot(2);
                inputHandler.ResetWeaponTriggers();
            }
        }

        public void SelectWeaponSlot(int index)
        {
            if (index < 0 || index >= weaponSlots.Count) return;
            if (isReloading) return;

            currentSlotIndex = index;
            isReloading = false;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUISound("weapon_switch");
            }

            NotifyHUD();
            Debug.Log($"[WeaponController] Switched weapon to: {CurrentWeapon?.weaponName}");
        }

        private void HandleReloadInput()
        {
            if (isReloading || CurrentSlot == null || CurrentWeapon == null) return;

            if (inputHandler != null && inputHandler.ReloadTriggered)
            {
                TryReload();
                inputHandler.ResetReloadTrigger();
            }
        }

        public void TryReload()
        {
            if (isReloading || CurrentSlot == null || CurrentWeapon == null) return;
            if (CurrentSlot.currentMagazineAmmo >= GetEffectiveMagazineSize()) return; // Mag full
            if (CurrentSlot.reserveAmmo <= 0) return; // No reserve ammo

            StartCoroutine(PerformReloadSequence());
        }

        private IEnumerator PerformReloadSequence()
        {
            isReloading = true;
            NotifyHUD();

            // Play reload audio SFX if assigned
            if (CurrentWeapon.reloadSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(CurrentWeapon.reloadSound, transform.position);
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayReload(CurrentWeapon.weaponName, transform.position);
            }

            yield return new WaitForSeconds(CurrentWeapon.reloadTime);

            // Transfer ammo from reserve to mag
            int needed = GetEffectiveMagazineSize() - CurrentSlot.currentMagazineAmmo;
            int transferred = Mathf.Min(needed, CurrentSlot.reserveAmmo);

            CurrentSlot.currentMagazineAmmo += transferred;
            CurrentSlot.reserveAmmo -= transferred;

            isReloading = false;
            NotifyHUD();
            Debug.Log($"[WeaponController] Reload complete for {CurrentWeapon.weaponName}. Mag: {CurrentSlot.currentMagazineAmmo}/{CurrentSlot.reserveAmmo}");
        }

        private void HandleFiringInput()
        {
            if (isReloading || CurrentSlot == null || CurrentWeapon == null || inputHandler == null) return;

            bool fireInput = CurrentWeapon.isAutomatic ? inputHandler.FireHeld : inputHandler.FireTriggered;

            if (fireInput)
            {
                if (CanFire())
                {
                    FireCurrentWeapon();
                }
                else if (CurrentSlot.currentMagazineAmmo <= 0 && inputHandler.FireTriggered)
                {
                    // Trigger Auto Reload or Empty SFX
                    if (CurrentSlot.reserveAmmo > 0)
                    {
                        TryReload();
                    }
                    else if (CurrentWeapon.emptySound != null && AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySFX(CurrentWeapon.emptySound, transform.position);
                    }
                    else if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayEmptyClick(transform.position);
                    }
                }

                inputHandler.ResetFireTrigger();
            }
        }

        public bool CanFire()
        {
            if (isReloading || CurrentSlot == null || CurrentWeapon == null) return false;
            if (CurrentSlot.currentMagazineAmmo <= 0) return false;
            return Time.time >= lastFireTime + GetEffectiveFireRate();
        }

        private void FireCurrentWeapon()
        {
            lastFireTime = Time.time;
            CurrentSlot.currentMagazineAmmo--;

            // Calculate Raycast from camera viewport center (0.5, 0.5) so crosshair and shooting direction match perfectly
            Ray cameraRay = mainCamera != null 
                ? mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)) 
                : new Ray(transform.position + Vector3.up * 1.5f, transform.forward);

            Vector3 rayOrigin = cameraRay.origin;
            Vector3 rayDirection = cameraRay.direction;

            // Execute pellets (1 for Pistol/Rifle, multiple for Shotgun)
            int pellets = Mathf.Max(1, CurrentWeapon.pelletsPerShot);
            for (int i = 0; i < pellets; i++)
            {
                Vector3 spreadDir = ApplySpread(rayDirection, CurrentWeapon.spreadAngle);
                ExecuteHitscanRay(rayOrigin, spreadDir);
            }

            // Apply Camera Recoil
            if (playerCamera != null)
            {
                playerCamera.ApplyRecoil(GetEffectiveRecoil());
            }

            // Play Firing Audio SFX
            if (CurrentWeapon.fireSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(CurrentWeapon.fireSound, transform.position);
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWeaponFire(CurrentWeapon.weaponName, transform.position);
            }

            // Broadcast Gunshot Noise Event for AI Perception
            float noiseRad = CurrentWeapon != null ? CurrentWeapon.fireNoiseRadius : 25.0f;
            NoiseManager.EmitNoise(transform.position, noiseRad, NoiseType.Gunshot);

            // Spawn Muzzle Flash VFX via Object Pool
            if (CurrentWeapon.muzzleFlashPrefab != null && muzzleTransform != null)
            {
                if (ZombieApocalypse.Systems.SimpleObjectPool.Instance != null)
                {
                    GameObject vfx = ZombieApocalypse.Systems.SimpleObjectPool.Instance.Spawn(CurrentWeapon.muzzleFlashPrefab, muzzleTransform.position, muzzleTransform.rotation);
                    ZombieApocalypse.Systems.SimpleObjectPool.Instance.Despawn(vfx, 1.0f);
                }
                else
                {
                    GameObject vfx = Instantiate(CurrentWeapon.muzzleFlashPrefab, muzzleTransform.position, muzzleTransform.rotation);
                    Destroy(vfx, 1.0f);
                }
            }

            NotifyHUD();

            // Automatic reload check if mag empties
            if (CurrentSlot.currentMagazineAmmo <= 0 && CurrentSlot.reserveAmmo > 0)
            {
                TryReload();
            }
        }

        private Vector3 ApplySpread(Vector3 baseDirection, float spreadAngleDegrees)
        {
            if (spreadAngleDegrees <= 0.01f) return baseDirection;

            float randomYaw = UnityEngine.Random.Range(-spreadAngleDegrees, spreadAngleDegrees);
            float randomPitch = UnityEngine.Random.Range(-spreadAngleDegrees, spreadAngleDegrees);
            Quaternion spreadRotation = Quaternion.Euler(randomPitch, randomYaw, 0f);

            return spreadRotation * baseDirection;
        }

        private void ExecuteHitscanRay(Vector3 origin, Vector3 direction)
        {
            float maxDist = CurrentWeapon != null ? CurrentWeapon.effectiveRange : 50f;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDist, hitScanLayers, QueryTriggerInteraction.Ignore))
            {
                // Check if hit ZombieHitbox component first (e.g. Headshot)
                ZombieHitbox hitbox = hit.collider.GetComponent<ZombieHitbox>();
                ZombieHealth zombieHealth = hitbox != null ? hitbox.OwnerHealth : hit.collider.GetComponent<ZombieHealth>();

                if (zombieHealth == null)
                {
                    zombieHealth = hit.collider.GetComponentInParent<ZombieHealth>();
                }

                if (zombieHealth != null && !zombieHealth.IsDead)
                {
                    float baseDmg = GetEffectiveDamage();
                    float multiplier = hitbox != null ? hitbox.DamageMultiplier : 1.0f;
                    float finalDmg = baseDmg * multiplier;

                    zombieHealth.TakeDamage(finalDmg);
                    bool isCrit = (hitbox != null && hitbox.Type == HitboxType.Head);

                    HUDController hud = HUDController.Instance != null ? HUDController.Instance : FindObjectOfType<HUDController>();
                    if (hud != null)
                    {
                        hud.ShowHitmarker(isCrit);
                    }

                    Debug.Log($"[WeaponController] Shot hit {hit.collider.name} ({(isCrit ? "CRITICAL HEADSHOT" : "BODY")}) dealing {finalDmg} damage.");
                }

                // Spawn Impact VFX if assigned via Object Pool
                if (CurrentWeapon != null && CurrentWeapon.impactVFXPrefab != null)
                {
                    if (ZombieApocalypse.Systems.SimpleObjectPool.Instance != null)
                    {
                        GameObject impact = ZombieApocalypse.Systems.SimpleObjectPool.Instance.Spawn(CurrentWeapon.impactVFXPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                        ZombieApocalypse.Systems.SimpleObjectPool.Instance.Despawn(impact, 2.0f);
                    }
                    else
                    {
                        GameObject impact = Instantiate(CurrentWeapon.impactVFXPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                        Destroy(impact, 2.0f);
                    }
                }
            }
        }

        private void NotifyHUD()
        {
            if (CurrentSlot == null || CurrentWeapon == null) return;

            bool isEmpty = CurrentSlot.currentMagazineAmmo <= 0 && CurrentSlot.reserveAmmo <= 0;
            OnWeaponStateChanged?.Invoke(CurrentSlot.currentMagazineAmmo, CurrentSlot.reserveAmmo, CurrentWeapon.weaponName, isReloading, isEmpty);
        }

        /// <summary>
        /// Replenishes reserve ammo for the weapon matching the specified AmmoType without resetting current magazine ammo.
        /// </summary>
        public bool AddReserveAmmo(ZombieApocalypse.Inventory.AmmoType ammoType, int amount)
        {
            if (amount <= 0) return false;

            foreach (var slot in weaponSlots)
            {
                if (slot != null && slot.weaponData != null)
                {
                    bool match = false;
                    string nameLower = slot.weaponData.weaponName.ToLower();

                    if (ammoType == ZombieApocalypse.Inventory.AmmoType.Pistol && nameLower.Contains("pistol")) match = true;
                    else if (ammoType == ZombieApocalypse.Inventory.AmmoType.Shotgun && nameLower.Contains("shotgun")) match = true;
                    else if (ammoType == ZombieApocalypse.Inventory.AmmoType.Rifle && (nameLower.Contains("rifle") || nameLower.Contains("assault"))) match = true;

                    if (match)
                    {
                        slot.reserveAmmo += amount;
                        NotifyHUD();
                        Debug.Log($"[WeaponController] Added {amount} reserve ammo for {slot.weaponData.weaponName}. New Reserve: {slot.reserveAmmo}");
                        return true;
                    }
                }
            }

            // Fallback by slot index if name check did not match
            int targetIndex = ammoType == ZombieApocalypse.Inventory.AmmoType.Pistol ? 0 : ammoType == ZombieApocalypse.Inventory.AmmoType.Shotgun ? 1 : ammoType == ZombieApocalypse.Inventory.AmmoType.Rifle ? 2 : -1;
            if (targetIndex >= 0 && targetIndex < weaponSlots.Count)
            {
                weaponSlots[targetIndex].reserveAmmo += amount;
                NotifyHUD();
                Debug.Log($"[WeaponController] Added {amount} reserve ammo to slot {targetIndex}. New Reserve: {weaponSlots[targetIndex].reserveAmmo}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Compiles equipped weapon slots into serializable WeaponSaveData DTO.
        /// </summary>
        public ZombieApocalypse.Save.WeaponSaveData GetWeaponSaveData()
        {
            var saveData = new ZombieApocalypse.Save.WeaponSaveData();
            saveData.currentSlotIndex = currentSlotIndex;

            foreach (var slot in weaponSlots)
            {
                if (slot != null && slot.weaponData != null)
                {
                    var slotSave = new ZombieApocalypse.Save.WeaponSlotSaveData
                    {
                        weaponName = slot.weaponData.weaponName,
                        currentMagazineAmmo = slot.currentMagazineAmmo,
                        reserveAmmo = slot.reserveAmmo
                    };

                    if (WeaponUpgradeSystem.Instance != null)
                    {
                        var state = WeaponUpgradeSystem.Instance.GetUpgradeState(slot.weaponData.weaponName);
                        if (state != null)
                        {
                            slotSave.damageLevel = state.damageLevel;
                            slotSave.magazineLevel = state.magazineLevel;
                            slotSave.fireRateLevel = state.fireRateLevel;
                            slotSave.recoilLevel = state.recoilLevel;
                        }
                    }

                    saveData.slots.Add(slotSave);
                }
            }

            return saveData;
        }

        /// <summary>
        /// Restores magazine ammo, reserve ammo, active weapon slot, and runtime upgrade levels.
        /// </summary>
        public void RestoreWeaponState(int activeSlotIndex, List<ZombieApocalypse.Save.WeaponSlotSaveData> savedSlots)
        {
            if (savedSlots != null)
            {
                for (int i = 0; i < savedSlots.Count && i < weaponSlots.Count; i++)
                {
                    var saved = savedSlots[i];
                    var runtime = weaponSlots[i];

                    if (saved != null && runtime != null)
                    {
                        runtime.currentMagazineAmmo = saved.currentMagazineAmmo;
                        runtime.reserveAmmo = saved.reserveAmmo;

                        if (WeaponUpgradeSystem.Instance != null && !string.IsNullOrEmpty(saved.weaponName))
                        {
                            var state = WeaponUpgradeSystem.Instance.GetUpgradeState(saved.weaponName);
                            state.damageLevel = saved.damageLevel;
                            state.magazineLevel = saved.magazineLevel;
                            state.fireRateLevel = saved.fireRateLevel;
                            state.recoilLevel = saved.recoilLevel;
                        }
                    }
                }
            }

            if (activeSlotIndex >= 0 && activeSlotIndex < weaponSlots.Count)
            {
                currentSlotIndex = activeSlotIndex;
            }

            isReloading = false;
            NotifyHUD();
            Debug.Log($"[WeaponController] Restored weapon states. Active slot: {currentSlotIndex}");
        }
    }
}
