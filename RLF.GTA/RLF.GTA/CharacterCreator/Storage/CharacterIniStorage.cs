using System;
using System.IO;
using System.Text;
using System.Globalization;
using GTA.UI;
using RLF.Core.CharacterCreator.Data;
using RLF.Core.CharacterCreator.Enums;

namespace RLF.GTA.CharacterCreator.Storage
{
    public class CharacterIniStorage
    {
        private readonly string _savePath;

        public CharacterIniStorage(string savePath)
        {
            _savePath = savePath;
            EnsureDirectoryExists();

            System.Diagnostics.Debug.WriteLine($"═══ CharacterIniStorage Criado ═══");
            System.Diagnostics.Debug.WriteLine($"Path: {_savePath}");
        }

        private void EnsureDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_savePath))
                {
                    Directory.CreateDirectory(_savePath);
                    System.Diagnostics.Debug.WriteLine($"📁 DIR CRIADO: {_savePath}");
                    Notification.Show($"~g~[Dir OK]~w~ Criado!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"📁 DIR JÁ EXISTE: {_savePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERRO DIR: {ex.Message}");
                Notification.Show($"~r~[Dir Erro]~w~ {ex.Message}");
            }
        }

        public bool SaveCharacter(CharacterData character, int slotIndex)
        {
            if (character == null || slotIndex < 0)
            {
                Notification.Show("~r~[Save]~w~ Dados inválidos!");
                return false;
            }

            string filePath = Path.Combine(_savePath, $"character_{slotIndex}.ini");

            try
            {
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"💾 SALVANDO SLOT {slotIndex}");
                System.Diagnostics.Debug.WriteLine($"📄 Path: {filePath}");

                Notification.Show($"~b~[SAVE]~w~ Slot {slotIndex}...");

                StringBuilder ini = new StringBuilder();

                // ========== CHARACTER ==========
                ini.AppendLine("[CHARACTER]");
                ini.AppendLine($"Id={character.Id}");
                ini.AppendLine($"Name={character.Name}");
                ini.AppendLine($"Gender={character.Gender}");
                ini.AppendLine($"EyeColor={character.EyeColor}");
                ini.AppendLine($"CreatedAt={character.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                ini.AppendLine($"ModifiedAt={DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                ini.AppendLine();

                System.Diagnostics.Debug.WriteLine($"✓ Name: {character.Name}");

                // ========== POSITION ==========
                ini.AppendLine("[POSITION]");
                ini.AppendLine($"LastPositionX={FloatToString(character.LastPositionX)}");
                ini.AppendLine($"LastPositionY={FloatToString(character.LastPositionY)}");
                ini.AppendLine($"LastPositionZ={FloatToString(character.LastPositionZ)}");
                ini.AppendLine($"LastHeading={FloatToString(character.LastHeading)}");
                ini.AppendLine();

                System.Diagnostics.Debug.WriteLine($"✓ Position: ({character.LastPositionX:F2}, {character.LastPositionY:F2}, {character.LastPositionZ:F2})");

                // ========== GENETICS ==========
                ini.AppendLine("[GENETICS]");
                ini.AppendLine($"ShapeFirst={character.Genetics.ShapeFirst}");
                ini.AppendLine($"ShapeSecond={character.Genetics.ShapeSecond}");
                ini.AppendLine($"ShapeThird={character.Genetics.ShapeThird}");
                ini.AppendLine($"SkinFirst={character.Genetics.SkinFirst}");
                ini.AppendLine($"SkinSecond={character.Genetics.SkinSecond}");
                ini.AppendLine($"SkinThird={character.Genetics.SkinThird}");
                ini.AppendLine($"ShapeMix={FloatToString(character.Genetics.ShapeMix)}");
                ini.AppendLine($"SkinMix={FloatToString(character.Genetics.SkinMix)}");
                ini.AppendLine($"ThirdMix={FloatToString(character.Genetics.ThirdMix)}");
                ini.AppendLine();

                // ========== FACE ==========
                ini.AppendLine("[FACE]");
                for (int i = 0; i < 20; i++)
                {
                    float value = character.FaceFeatures.GetFeature((FaceFeature)i);
                    ini.AppendLine($"Feature{i}={FloatToString(value)}");
                }
                ini.AppendLine();

                // ========== HAIR ==========
                ini.AppendLine("[HAIR]");
                ini.AppendLine($"Style={character.Hair.Style}");
                ini.AppendLine($"PrimaryColor={character.Hair.PrimaryColor}");
                ini.AppendLine($"HighlightColor={character.Hair.HighlightColor}");
                ini.AppendLine();

                System.Diagnostics.Debug.WriteLine($"✓ Hair: {character.Hair.Style}/{character.Hair.PrimaryColor}");

                // ========== OVERLAYS ==========
                ini.AppendLine("[OVERLAYS]");
                int overlayCount = 0;
                foreach (OverlayType type in Enum.GetValues(typeof(OverlayType)))
                {
                    var overlay = character.Overlays.GetOverlay(type);
                    if (overlay != null)
                    {
                        ini.AppendLine($"{type}_Index={overlay.Index}");
                        ini.AppendLine($"{type}_Opacity={FloatToString(overlay.Opacity)}");
                        ini.AppendLine($"{type}_Primary={overlay.PrimaryColor}");
                        ini.AppendLine($"{type}_Secondary={overlay.SecondaryColor}");

                        if (overlay.Index >= 0) overlayCount++;
                    }
                }
                ini.AppendLine();

                System.Diagnostics.Debug.WriteLine($"✓ Overlays: {overlayCount} ativos");

                // ========== CLOTHING ==========
                ini.AppendLine("[CLOTHING]");
                foreach (ComponentType type in Enum.GetValues(typeof(ComponentType)))
                {
                    var component = character.Clothing.GetComponent(type);
                    if (component != null)
                    {
                        ini.AppendLine($"{type}_Drawable={component.DrawableId}");
                        ini.AppendLine($"{type}_Texture={component.TextureId}");
                    }
                }
                ini.AppendLine();

                // ========== PROPS ==========
                ini.AppendLine("[PROPS]");
                foreach (PropType type in Enum.GetValues(typeof(PropType)))
                {
                    var prop = character.Props.GetProp(type);
                    if (prop != null)
                    {
                        ini.AppendLine($"{type}_Drawable={prop.DrawableId}");
                        ini.AppendLine($"{type}_Texture={prop.TextureId}");
                    }
                }
                ini.AppendLine();

                // ========== ⭐ WEAPONS ⭐ ==========
                ini.AppendLine("[WEAPONS]");
                ini.AppendLine($"TotalWeapons={character.Weapons.GetTotalWeaponCount()}");
                ini.AppendLine($"CurrentWeapon={character.Weapons.CurrentWeaponHash}");

                for (int i = 0; i < character.Weapons.Weapons.Count; i++)
                {
                    var weapon = character.Weapons.Weapons[i];
                    ini.AppendLine($"Weapon{i}_Hash={weapon.WeaponHash}");
                    ini.AppendLine($"Weapon{i}_Ammo={weapon.Ammo}");
                    ini.AppendLine($"Weapon{i}_MaxAmmo={weapon.MaxAmmo}");
                    ini.AppendLine($"Weapon{i}_Infinite={weapon.IsInfiniteAmmo}");

                    if (weapon.Components.Count > 0)
                    {
                        ini.AppendLine($"Weapon{i}_Components={string.Join(",", weapon.Components)}");
                    }
                }
                ini.AppendLine();

                System.Diagnostics.Debug.WriteLine($"✓ Weapons: {character.Weapons.GetTotalWeaponCount()}");

                // ========== ⭐ VEHICLE ⭐ ==========
                ini.AppendLine("[VEHICLE]");
                ini.AppendLine($"HasVehicle={character.Vehicle.HasVehicle}");
                ini.AppendLine($"WasInVehicle={character.Vehicle.WasInVehicle}");
                ini.AppendLine($"Model={character.Vehicle.Model}");
                ini.AppendLine($"PositionX={FloatToString(character.Vehicle.PositionX)}");
                ini.AppendLine($"PositionY={FloatToString(character.Vehicle.PositionY)}");
                ini.AppendLine($"PositionZ={FloatToString(character.Vehicle.PositionZ)}");
                ini.AppendLine($"Heading={FloatToString(character.Vehicle.Heading)}");
                ini.AppendLine($"PrimaryColor={character.Vehicle.PrimaryColor}");
                ini.AppendLine($"SecondaryColor={character.Vehicle.SecondaryColor}");
                ini.AppendLine($"PearlescentColor={character.Vehicle.PearlescentColor}");
                ini.AppendLine($"WheelColor={character.Vehicle.WheelColor}");
                ini.AppendLine($"LicensePlate={character.Vehicle.LicensePlate}");
                ini.AppendLine($"LicensePlateStyle={character.Vehicle.LicensePlateStyle}");
                ini.AppendLine();

                System.Diagnostics.Debug.WriteLine($"✓ Vehicle: {(character.Vehicle.HasVehicle ? "Sim" : "Não")}");

                // ========== SALVAR ==========
                File.WriteAllText(filePath, ini.ToString(), Encoding.UTF8);

                bool exists = File.Exists(filePath);
                long size = exists ? new FileInfo(filePath).Length : 0;

                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"✅ INI SALVO!");
                System.Diagnostics.Debug.WriteLine($"📄 {filePath}");
                System.Diagnostics.Debug.WriteLine($"📊 {size} bytes");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");

                Notification.Show($"~g~[SAVE OK]~w~ {character.Name}");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"❌ ERRO AO SALVAR:");
                System.Diagnostics.Debug.WriteLine($"   {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");

                Notification.Show($"~r~[SAVE ERROR]~w~");
                return false;
            }
        }

        public CharacterData LoadCharacter(int slotIndex)
        {
            if (slotIndex < 0)
                return null;

            string filePath = Path.Combine(_savePath, $"character_{slotIndex}.ini");

            try
            {
                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ INI NÃO EXISTE: {filePath}");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"📂 LOAD INI: Slot {slotIndex}");
                System.Diagnostics.Debug.WriteLine($"📄 Path: {filePath}");
                System.Diagnostics.Debug.WriteLine($"📊 Size: {new FileInfo(filePath).Length} bytes");

                Notification.Show($"~b~[LOAD INI]~w~ Slot {slotIndex}...");

                CharacterData character = new CharacterData();
                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                string currentSection = "";

                System.Diagnostics.Debug.WriteLine($"📋 Lines: {lines.Length}");

                foreach (string line in lines)
                {
                    string trimmed = line.Trim();

                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";"))
                        continue;

                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        currentSection = trimmed.Substring(1, trimmed.Length - 2);
                        System.Diagnostics.Debug.WriteLine($"📑 [{currentSection}]");
                        continue;
                    }

                    string[] parts = trimmed.Split(new[] { '=' }, 2);
                    if (parts.Length != 2)
                        continue;

                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    switch (currentSection)
                    {
                        case "CHARACTER":
                            ParseCharacterSection(character, key, value);
                            break;
                        case "POSITION":
                            ParsePositionSection(character, key, value);
                            break;
                        case "GENETICS":
                            ParseGeneticsSection(character.Genetics, key, value);
                            break;
                        case "FACE":
                            ParseFaceSection(character.FaceFeatures, key, value);
                            break;
                        case "HAIR":
                            ParseHairSection(character.Hair, key, value);
                            break;
                        case "OVERLAYS":
                            ParseOverlaysSection(character.Overlays, key, value);
                            break;
                        case "CLOTHING":
                            ParseClothingSection(character.Clothing, key, value);
                            break;
                        case "PROPS":
                            ParsePropsSection(character.Props, key, value);
                            break;
                        case "WEAPONS":
                            ParseWeaponsSection(character.Weapons, key, value);
                            break;
                        case "VEHICLE":
                            ParseVehicleSection(character.Vehicle, key, value);
                            break;
                    }
                }

                System.Diagnostics.Debug.WriteLine("────────────────────────────────────────");
                System.Diagnostics.Debug.WriteLine($"✅ LOADED: {character.Name}");
                System.Diagnostics.Debug.WriteLine($"   Hair: {character.Hair.Style}/{character.Hair.PrimaryColor}");
                System.Diagnostics.Debug.WriteLine($"   Position: ({character.LastPositionX:F2}, {character.LastPositionY:F2}, {character.LastPositionZ:F2})");
                System.Diagnostics.Debug.WriteLine($"   Weapons: {character.Weapons.GetTotalWeaponCount()}");
                System.Diagnostics.Debug.WriteLine($"   Vehicle: {(character.Vehicle.HasVehicle ? "Sim" : "Não")}");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");

                Notification.Show($"~g~[LOAD OK]~w~ {character.Name}");

                return character;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"❌ ERRO LOAD:");
                System.Diagnostics.Debug.WriteLine($"   {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");

                Notification.Show($"~r~[LOAD ERROR]~w~ {ex.Message}");
                return null;
            }
        }

        private void ParseCharacterSection(CharacterData character, string key, string value)
        {
            try
            {
                switch (key)
                {
                    case "Id": character.Id = value; break;
                    case "Name":
                        character.Name = value;
                        System.Diagnostics.Debug.WriteLine($"   ✓ Name: {value}");
                        break;
                    case "Gender":
                        character.Gender = (CharacterGender)Enum.Parse(typeof(CharacterGender), value);
                        break;
                    case "EyeColor": character.EyeColor = int.Parse(value); break;
                    case "CreatedAt": character.CreatedAt = DateTime.Parse(value); break;
                    case "ModifiedAt": character.ModifiedAt = DateTime.Parse(value); break;
                }
            }
            catch { }
        }

        private void ParsePositionSection(CharacterData character, string key, string value)
        {
            try
            {
                switch (key)
                {
                    case "LastPositionX":
                        character.LastPositionX = StringToFloat(value);
                        break;
                    case "LastPositionY":
                        character.LastPositionY = StringToFloat(value);
                        break;
                    case "LastPositionZ":
                        character.LastPositionZ = StringToFloat(value);
                        break;
                    case "LastHeading":
                        character.LastHeading = StringToFloat(value);
                        break;
                }
            }
            catch { }
        }

        private void ParseGeneticsSection(CharacterGenetics genetics, string key, string value)
        {
            try
            {
                switch (key)
                {
                    case "ShapeFirst": genetics.ShapeFirst = int.Parse(value); break;
                    case "ShapeSecond": genetics.ShapeSecond = int.Parse(value); break;
                    case "ShapeThird": genetics.ShapeThird = int.Parse(value); break;
                    case "SkinFirst": genetics.SkinFirst = int.Parse(value); break;
                    case "SkinSecond": genetics.SkinSecond = int.Parse(value); break;
                    case "SkinThird": genetics.SkinThird = int.Parse(value); break;
                    case "ShapeMix": genetics.ShapeMix = StringToFloat(value); break;
                    case "SkinMix": genetics.SkinMix = StringToFloat(value); break;
                    case "ThirdMix": genetics.ThirdMix = StringToFloat(value); break;
                }
            }
            catch { }
        }

        private void ParseFaceSection(CharacterFaceFeatures features, string key, string value)
        {
            try
            {
                if (key.StartsWith("Feature"))
                {
                    int index = int.Parse(key.Substring(7));
                    float fValue = StringToFloat(value);
                    features.SetFeature((FaceFeature)index, fValue);
                }
            }
            catch { }
        }

        private void ParseHairSection(CharacterHair hair, string key, string value)
        {
            try
            {
                switch (key)
                {
                    case "Style":
                        hair.Style = int.Parse(value);
                        System.Diagnostics.Debug.WriteLine($"   ✓ Hair Style: {value}");
                        break;
                    case "PrimaryColor":
                        hair.PrimaryColor = int.Parse(value);
                        System.Diagnostics.Debug.WriteLine($"   ✓ Hair Color: {value}");
                        break;
                    case "HighlightColor": hair.HighlightColor = int.Parse(value); break;
                }
            }
            catch { }
        }

        private void ParseOverlaysSection(CharacterOverlays overlays, string key, string value)
        {
            try
            {
                string[] parts = key.Split('_');
                if (parts.Length != 2)
                    return;

                OverlayType type = (OverlayType)Enum.Parse(typeof(OverlayType), parts[0]);
                var overlay = overlays.GetOverlay(type);

                if (overlay == null)
                {
                    overlay = new CharacterOverlay { Type = type };
                    overlays.SetOverlay(type, -1, 1f, 0, 0);
                    overlay = overlays.GetOverlay(type);
                }

                switch (parts[1])
                {
                    case "Index": overlay.Index = int.Parse(value); break;
                    case "Opacity": overlay.Opacity = StringToFloat(value); break;
                    case "Primary": overlay.PrimaryColor = int.Parse(value); break;
                    case "Secondary": overlay.SecondaryColor = int.Parse(value); break;
                }
            }
            catch { }
        }

        private void ParseClothingSection(CharacterClothing clothing, string key, string value)
        {
            try
            {
                string[] parts = key.Split('_');
                if (parts.Length != 2)
                    return;

                ComponentType type = (ComponentType)Enum.Parse(typeof(ComponentType), parts[0]);
                var component = clothing.GetComponent(type);

                if (component == null)
                {
                    clothing.SetComponent(type, 0, 0);
                    component = clothing.GetComponent(type);
                }

                switch (parts[1])
                {
                    case "Drawable": component.DrawableId = int.Parse(value); break;
                    case "Texture": component.TextureId = int.Parse(value); break;
                }
            }
            catch { }
        }

        private void ParsePropsSection(CharacterProps props, string key, string value)
        {
            try
            {
                string[] parts = key.Split('_');
                if (parts.Length != 2)
                    return;

                PropType type = (PropType)Enum.Parse(typeof(PropType), parts[0]);
                var prop = props.GetProp(type);

                if (prop == null)
                {
                    props.SetProp(type, -1, 0);
                    prop = props.GetProp(type);
                }

                switch (parts[1])
                {
                    case "Drawable": prop.DrawableId = int.Parse(value); break;
                    case "Texture": prop.TextureId = int.Parse(value); break;
                }
            }
            catch { }
        }

        // ⭐ PARSER DE ARMAS
        private void ParseWeaponsSection(CharacterWeapons weapons, string key, string value)
        {
            try
            {
                if (key == "TotalWeapons" || key == "CurrentWeapon")
                {
                    if (key == "CurrentWeapon")
                        weapons.CurrentWeaponHash = value;
                    return;
                }

                if (key.StartsWith("Weapon") && key.Contains("_"))
                {
                    string[] parts = key.Split('_');
                    if (parts.Length != 2)
                        return;

                    int weaponIndex = int.Parse(parts[0].Replace("Weapon", ""));
                    string property = parts[1];

                    while (weapons.Weapons.Count <= weaponIndex)
                    {
                        weapons.Weapons.Add(new WeaponData());
                    }

                    var weapon = weapons.Weapons[weaponIndex];

                    switch (property)
                    {
                        case "Hash":
                            weapon.WeaponHash = value;
                            break;
                        case "Ammo":
                            weapon.Ammo = int.Parse(value);
                            break;
                        case "MaxAmmo":
                            weapon.MaxAmmo = int.Parse(value);
                            break;
                        case "Infinite":
                            weapon.IsInfiniteAmmo = bool.Parse(value);
                            break;
                        case "Components":
                            if (!string.IsNullOrEmpty(value))
                            {
                                weapon.Components.AddRange(value.Split(','));
                            }
                            break;
                    }
                }
            }
            catch { }
        }

        // ⭐ PARSER DE VEÍCULO
        private void ParseVehicleSection(CharacterVehicle vehicle, string key, string value)
        {
            try
            {
                switch (key)
                {
                    case "HasVehicle": vehicle.HasVehicle = bool.Parse(value); break;
                    case "WasInVehicle": vehicle.WasInVehicle = bool.Parse(value); break;
                    case "Model": vehicle.Model = value; break;
                    case "PositionX": vehicle.PositionX = StringToFloat(value); break;
                    case "PositionY": vehicle.PositionY = StringToFloat(value); break;
                    case "PositionZ": vehicle.PositionZ = StringToFloat(value); break;
                    case "Heading": vehicle.Heading = StringToFloat(value); break;
                    case "PrimaryColor": vehicle.PrimaryColor = int.Parse(value); break;
                    case "SecondaryColor": vehicle.SecondaryColor = int.Parse(value); break;
                    case "PearlescentColor": vehicle.PearlescentColor = int.Parse(value); break;
                    case "WheelColor": vehicle.WheelColor = int.Parse(value); break;
                    case "LicensePlate": vehicle.LicensePlate = value; break;
                    case "LicensePlateStyle": vehicle.LicensePlateStyle = int.Parse(value); break;
                }
            }
            catch { }
        }

        public bool DeleteCharacter(int slotIndex)
        {
            try
            {
                string filePath = Path.Combine(_savePath, $"character_{slotIndex}.ini");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    System.Diagnostics.Debug.WriteLine($"✅ DELETED: {filePath}");
                    Notification.Show($"~g~[Delete]~w~ Slot {slotIndex}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DELETE ERROR: {ex.Message}");
                Notification.Show($"~r~[Delete Error]~w~");
                return false;
            }
        }

        public bool CharacterExists(int slotIndex)
        {
            string filePath = Path.Combine(_savePath, $"character_{slotIndex}.ini");
            return File.Exists(filePath);
        }

        private static string FloatToString(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static float StringToFloat(string value)
        {
            value = value.Replace(',', '.');
            return float.Parse(value, CultureInfo.InvariantCulture);
        }
    }
}