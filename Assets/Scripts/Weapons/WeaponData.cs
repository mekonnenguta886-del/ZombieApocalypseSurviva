using UnityEngine;

namespace ZombieApocalypse.Weapons
{
    public enum WeaponType
    {
        Pistol,
        Shotgun,
        AssaultRifle
    }

    /// <summary>
    /// ScriptableObject data container for weapon properties, damage stats, ammo capacity, fire modes, and VFX/SFX references.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "Zombie Apocalypse/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName = "Pistol";
        public WeaponType weaponType = WeaponType.Pistol;

        [Header("Combat Stats")]
        public float damage = 35f;
        public float fireRate = 0.25f; // Seconds between shots
        public float effectiveRange = 60f;
        public float spreadAngle = 0.5f; // Degree spread cone
        public int pelletsPerShot = 1;   // 1 for Pistol/Rifle, 8 for Shotgun
        public bool isAutomatic = false;
        public float recoilAmount = 1.0f;
        public float fireNoiseRadius = 25.0f; // Radius of gunshot noise event for AI perception

        [Header("Ammunition")]
        [Min(1)] public int magazineSize = 12;
        [Min(0)] public int startingReserveAmmo = 48;
        public float reloadTime = 1.5f;

        [Header("Assets & VFX")]
        public GameObject weaponPrefab;
        public GameObject muzzleFlashPrefab;
        public GameObject impactVFXPrefab;

        [Header("Audio SFX")]
        public AudioClip fireSound;
        public AudioClip reloadSound;
        public AudioClip emptySound;
    }
}
