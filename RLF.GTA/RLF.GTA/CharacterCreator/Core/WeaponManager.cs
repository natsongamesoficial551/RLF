using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using RLF.Core.CharacterCreator.Data;

namespace RLF.GTA.CharacterCreator.Core
{
    public static class WeaponManager
    {
        private static readonly List<string> ExcludedWeapons = new List<string>
        {
            "WEAPON_UNARMED"
        };

        public static CharacterWeapons CapturePlayerWeapons()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return new CharacterWeapons();

                var weapons = new CharacterWeapons();

                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine("🔫 CAPTURANDO ARMAS DO PLAYER");

                // Obter arma atual
                int currentWeaponHash = Function.Call<int>(Hash.GET_SELECTED_PED_WEAPON, player.Handle);
                weapons.CurrentWeaponHash = currentWeaponHash.ToString();

                // Percorrer todas as armas possíveis
                foreach (WeaponHash weaponHash in Enum.GetValues(typeof(WeaponHash)))
                {
                    string hash = weaponHash.ToString();

                    if (ExcludedWeapons.Contains(hash))
                        continue;

                    bool hasWeapon = Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, player.Handle, (uint)weaponHash, false);

                    if (hasWeapon)
                    {
                        int ammo = Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON, player.Handle, (uint)weaponHash);

                        // GET_MAX_AMMO usa OutputArgument
                        var maxAmmoOut = new OutputArgument();
                        Function.Call(Hash.GET_MAX_AMMO, player.Handle, (uint)weaponHash, maxAmmoOut);
                        int maxAmmo = maxAmmoOut.GetResult<int>();

                        var weaponData = new WeaponData(hash, ammo, maxAmmo, false);

                        // Capturar componentes (acessórios)
                        foreach (WeaponComponentHash componentHash in Enum.GetValues(typeof(WeaponComponentHash)))
                        {
                            try
                            {
                                bool hasComponent = Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON_COMPONENT,
                                    player.Handle, (uint)weaponHash, (uint)componentHash);

                                if (hasComponent)
                                {
                                    weaponData.Components.Add(componentHash.ToString());
                                }
                            }
                            catch { }
                        }

                        weapons.AddWeapon(weaponData);

                        System.Diagnostics.Debug.WriteLine($"   ✓ {hash}: {ammo}/{maxAmmo} ammo, {weaponData.Components.Count} componentes");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ Total: {weapons.GetTotalWeaponCount()} armas capturadas");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");

                return weapons;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao capturar armas: {ex.Message}");
                return new CharacterWeapons();
            }
        }

        public static void ApplyWeaponsToPlayer(CharacterWeapons weapons)
        {
            if (weapons == null || weapons.GetTotalWeaponCount() == 0)
            {
                System.Diagnostics.Debug.WriteLine("ℹ️ Nenhuma arma para aplicar");
                return;
            }

            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return;

                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"🔫 APLICANDO {weapons.GetTotalWeaponCount()} ARMAS");

                // Remover todas as armas atuais
                Function.Call(Hash.REMOVE_ALL_PED_WEAPONS, player.Handle, true);

                // Adicionar armas salvas
                foreach (var weaponData in weapons.Weapons)
                {
                    try
                    {
                        uint weaponHash = 0;

                        // Tentar converter de string para hash
                        if (weaponData.WeaponHash.StartsWith("WEAPON_"))
                        {
                            weaponHash = Function.Call<uint>(Hash.GET_HASH_KEY, weaponData.WeaponHash);
                        }
                        else
                        {
                            weaponHash = uint.Parse(weaponData.WeaponHash);
                        }

                        // Dar a arma
                        Function.Call(Hash.GIVE_WEAPON_TO_PED, player.Handle, weaponHash, 0, false, true);

                        // Setar munição
                        Function.Call(Hash.SET_PED_AMMO, player.Handle, weaponHash, weaponData.Ammo);

                        // Aplicar componentes
                        foreach (var componentHashStr in weaponData.Components)
                        {
                            try
                            {
                                uint componentHash = 0;

                                if (componentHashStr.StartsWith("COMPONENT_"))
                                {
                                    componentHash = Function.Call<uint>(Hash.GET_HASH_KEY, componentHashStr);
                                }
                                else
                                {
                                    componentHash = uint.Parse(componentHashStr);
                                }

                                Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED, player.Handle, weaponHash, componentHash);
                            }
                            catch { }
                        }

                        System.Diagnostics.Debug.WriteLine($"   ✓ {weaponData.WeaponHash}: {weaponData.Ammo} ammo");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"   ❌ Erro ao adicionar {weaponData.WeaponHash}: {ex.Message}");
                    }
                }

                // Selecionar arma atual
                if (!string.IsNullOrEmpty(weapons.CurrentWeaponHash))
                {
                    try
                    {
                        uint currentHash = 0;

                        if (weapons.CurrentWeaponHash.StartsWith("WEAPON_"))
                        {
                            currentHash = Function.Call<uint>(Hash.GET_HASH_KEY, weapons.CurrentWeaponHash);
                        }
                        else
                        {
                            currentHash = uint.Parse(weapons.CurrentWeaponHash);
                        }

                        Function.Call(Hash.SET_CURRENT_PED_WEAPON, player.Handle, currentHash, true);
                    }
                    catch { }
                }

                System.Diagnostics.Debug.WriteLine("✅ ARMAS APLICADAS COM SUCESSO");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao aplicar armas: {ex.Message}");
            }
        }

        public static void RemoveAllWeapons()
        {
            try
            {
                var player = Game.Player.Character;
                if (player != null && player.Exists())
                {
                    Function.Call(Hash.REMOVE_ALL_PED_WEAPONS, player.Handle, true);
                    System.Diagnostics.Debug.WriteLine("🔫 Todas as armas removidas");
                }
            }
            catch { }
        }
    }
}