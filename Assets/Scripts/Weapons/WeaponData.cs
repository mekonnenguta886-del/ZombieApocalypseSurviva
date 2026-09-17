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
    /// ScriptableObject defining static properties for weapons (Damage, Rate of Fire, Magazine Size, Recoil).
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "Zombie Apocalypse/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName = "Pistol";
        public WeaponType weaponType = WeaponType.Pistol;

        [Header("Combat Stats")]
        public float damage = 25f;
        public float fireRate = 0.2f; // Time between shots
        public float effectiveRange = 50f;
        public int magazineSize = 12;
        public float reloadDuration = 1.8f;

        [Header("Recoil & Accuracy")]
        public float verticalRecoil = 1.5f;
        public float spreadAngle = 0.5f;

        [Header("Assets")]
        public GameObject weaponPrefab;
        public AudioClip fireSound;
        public AudioClip reloadSound;
    }
}
