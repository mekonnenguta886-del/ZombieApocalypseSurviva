using System;
using UnityEngine;

namespace ZombieApocalypse.Weapons
{
    /// <summary>
    /// Handles equipped weapon state, firing cooldown, ammo tracking, and reload sequences.
    /// 
    /// ATTACH TO: Weapon Holder or Player Weapon Socket GameObject.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        public event Action<int, int> OnAmmoChanged; // Current Magazine, Reserve

        [Header("Equipped Weapon")]
        [SerializeField] private WeaponData currentWeaponData;

        private int currentAmmoInMag;
        private int reserveAmmo;
        private float lastFireTime;
        private bool isReloading;

        public WeaponData CurrentWeapon => currentWeaponData;
        public int CurrentAmmoInMag => currentAmmoInMag;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => isReloading;

        private void Start()
        {
            if (currentWeaponData != null)
            {
                EquipWeapon(currentWeaponData);
            }
        }

        public void EquipWeapon(WeaponData weaponData)
        {
            currentWeaponData = weaponData;
            currentAmmoInMag = weaponData.magazineSize;
            reserveAmmo = weaponData.magazineSize * 3; // Initial reserve ammo
            OnAmmoChanged?.Invoke(currentAmmoInMag, reserveAmmo);
        }

        public bool CanFire()
        {
            if (currentWeaponData == null || isReloading) return false;
            if (currentAmmoInMag <= 0) return false;
            return Time.time >= lastFireTime + currentWeaponData.fireRate;
        }

        public void Fire()
        {
            if (!CanFire()) return;

            lastFireTime = Time.time;
            currentAmmoInMag--;
            OnAmmoChanged?.Invoke(currentAmmoInMag, reserveAmmo);

            Debug.Log($"[WeaponController] Fired {currentWeaponData.weaponName}. Ammo remaining: {currentAmmoInMag}");
        }

        public void Reload()
        {
            if (isReloading || currentWeaponData == null) return;
            if (currentAmmoInMag >= currentWeaponData.magazineSize || reserveAmmo <= 0) return;

            isReloading = true;
            Debug.Log($"[WeaponController] Reloading {currentWeaponData.weaponName}...");

            // Reload completion timer logic will be hooked up in Phase 2
        }
    }
}
