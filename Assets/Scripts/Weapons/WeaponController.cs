using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
            // Player Death Safety Guard
            if (playerHealth != null && playerHealth.IsDead)
            {
                isReloading = false;
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
            if (CurrentSlot.currentMagazineAmmo >= CurrentWeapon.magazineSize) return; // Mag full
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

            yield return new WaitForSeconds(CurrentWeapon.reloadTime);

            // Transfer ammo from reserve to mag
            int needed = CurrentWeapon.magazineSize - CurrentSlot.currentMagazineAmmo;
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
                }

                inputHandler.ResetFireTrigger();
            }
        }

        public bool CanFire()
        {
            if (isReloading || CurrentSlot == null || CurrentWeapon == null) return false;
            if (CurrentSlot.currentMagazineAmmo <= 0) return false;
            return Time.time >= lastFireTime + CurrentWeapon.fireRate;
        }

        private void FireCurrentWeapon()
        {
            lastFireTime = Time.time;
            CurrentSlot.currentMagazineAmmo--;

            // Calculate Raycast from viewport center (over the shoulder / crosshair)
            Vector3 rayOrigin = mainCamera != null ? mainCamera.transform.position : transform.position + Vector3.up * 1.5f;
            Vector3 rayDirection = mainCamera != null ? mainCamera.transform.forward : transform.forward;

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
                playerCamera.ApplyRecoil(CurrentWeapon.recoilAmount);
            }

            // Play Firing Audio SFX
            if (CurrentWeapon.fireSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(CurrentWeapon.fireSound, transform.position);
            }

            // Spawn Muzzle Flash VFX
            if (CurrentWeapon.muzzleFlashPrefab != null && muzzleTransform != null)
            {
                GameObject vfx = Instantiate(CurrentWeapon.muzzleFlashPrefab, muzzleTransform.position, muzzleTransform.rotation);
                Destroy(vfx, 1.0f);
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
                // Check if hit ZombieHealth component
                ZombieHealth zombieHealth = hit.collider.GetComponent<ZombieHealth>();
                if (zombieHealth == null)
                {
                    zombieHealth = hit.collider.GetComponentInParent<ZombieHealth>();
                }

                if (zombieHealth != null && !zombieHealth.IsDead)
                {
                    float dmg = CurrentWeapon != null ? CurrentWeapon.damage : 20f;
                    zombieHealth.TakeDamage(dmg);
                    Debug.Log($"[WeaponController] Shot hit {hit.collider.name} dealing {dmg} damage.");
                }

                // Spawn Impact VFX if assigned
                if (CurrentWeapon != null && CurrentWeapon.impactVFXPrefab != null)
                {
                    GameObject impact = Instantiate(CurrentWeapon.impactVFXPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(impact, 2.0f);
                }
            }
        }

        private void NotifyHUD()
        {
            if (CurrentSlot == null || CurrentWeapon == null) return;

            bool isEmpty = CurrentSlot.currentMagazineAmmo <= 0 && CurrentSlot.reserveAmmo <= 0;
            OnWeaponStateChanged?.Invoke(CurrentSlot.currentMagazineAmmo, CurrentSlot.reserveAmmo, CurrentWeapon.weaponName, isReloading, isEmpty);
        }
    }
}
