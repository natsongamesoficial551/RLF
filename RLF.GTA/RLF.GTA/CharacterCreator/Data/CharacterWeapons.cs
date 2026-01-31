using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace RLF.Core.CharacterCreator.Data
{
    [Serializable]
    public class WeaponData
    {
        [XmlElement]
        public string WeaponHash { get; set; }

        [XmlElement]
        public int Ammo { get; set; }

        [XmlElement]
        public int MaxAmmo { get; set; }

        [XmlElement]
        public bool IsInfiniteAmmo { get; set; }

        [XmlArray("Components")]
        [XmlArrayItem("Component")]
        public List<string> Components { get; set; }

        public WeaponData()
        {
            WeaponHash = "";
            Ammo = 0;
            MaxAmmo = 0;
            IsInfiniteAmmo = false;
            Components = new List<string>();
        }

        public WeaponData(string hash, int ammo, int maxAmmo, bool infiniteAmmo)
        {
            WeaponHash = hash;
            Ammo = ammo;
            MaxAmmo = maxAmmo;
            IsInfiniteAmmo = infiniteAmmo;
            Components = new List<string>();
        }

        public WeaponData Clone()
        {
            var clone = new WeaponData(WeaponHash, Ammo, MaxAmmo, IsInfiniteAmmo);
            clone.Components.AddRange(Components);
            return clone;
        }
    }

    [Serializable]
    public class CharacterWeapons
    {
        [XmlArray("Weapons")]
        [XmlArrayItem("Weapon")]
        public List<WeaponData> Weapons { get; set; }

        [XmlElement]
        public string CurrentWeaponHash { get; set; }

        public CharacterWeapons()
        {
            Weapons = new List<WeaponData>();
            CurrentWeaponHash = "";
        }

        public void AddWeapon(WeaponData weapon)
        {
            if (weapon == null || string.IsNullOrEmpty(weapon.WeaponHash))
                return;

            RemoveWeapon(weapon.WeaponHash);
            Weapons.Add(weapon);
        }

        public void RemoveWeapon(string weaponHash)
        {
            Weapons.RemoveAll(w => w.WeaponHash.Equals(weaponHash, StringComparison.OrdinalIgnoreCase));
        }

        public WeaponData GetWeapon(string weaponHash)
        {
            return Weapons.Find(w => w.WeaponHash.Equals(weaponHash, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasWeapon(string weaponHash)
        {
            return GetWeapon(weaponHash) != null;
        }

        public void ClearAll()
        {
            Weapons.Clear();
            CurrentWeaponHash = "";
        }

        public int GetTotalWeaponCount()
        {
            return Weapons.Count;
        }

        public CharacterWeapons Clone()
        {
            var clone = new CharacterWeapons();
            clone.CurrentWeaponHash = CurrentWeaponHash;

            foreach (var weapon in Weapons)
            {
                clone.Weapons.Add(weapon.Clone());
            }

            return clone;
        }
    }
}