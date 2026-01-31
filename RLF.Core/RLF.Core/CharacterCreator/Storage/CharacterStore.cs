using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using RLF.Core.CharacterCreator.Data;

namespace RLF.Core.CharacterCreator.Storage
{
    public class CharacterStore
    {
        private readonly string _savePath;
        private readonly int _maxSlots;
        private List<CharacterData> _characters;

        public int MaxSlots => _maxSlots;

        // ✅ Construtor com 1 argumento
        public CharacterStore(string savePath)
        {
            _savePath = savePath;
            _maxSlots = 25;
            _characters = new List<CharacterData>();
            EnsureDirectoryExists();
        }

        // ✅ Construtor com 2 argumentos
        public CharacterStore(string savePath, int maxSlots)
        {
            _savePath = savePath;
            _maxSlots = maxSlots;
            _characters = new List<CharacterData>();
            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_savePath))
                {
                    Directory.CreateDirectory(_savePath);
                }
            }
            catch { }
        }

        public bool SaveCharacter(CharacterData character, int slotIndex)
        {
            if (character == null || slotIndex < 0 || slotIndex >= _maxSlots)
                return false;

            try
            {
                string filePath = GetFilePath(slotIndex);
                character.ModifiedAt = DateTime.Now;

                XmlSerializer serializer = new XmlSerializer(typeof(CharacterData));
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    serializer.Serialize(fs, character);
                }

                // Atualizar cache
                RefreshCache();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public CharacterData LoadCharacter(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _maxSlots)
                return null;

            try
            {
                string filePath = GetFilePath(slotIndex);

                if (!File.Exists(filePath))
                    return null;

                XmlSerializer serializer = new XmlSerializer(typeof(CharacterData));
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    return (CharacterData)serializer.Deserialize(fs);
                }
            }
            catch
            {
                return null;
            }
        }

        public bool DeleteCharacter(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _maxSlots)
                return false;

            try
            {
                string filePath = GetFilePath(slotIndex);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    RefreshCache();
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public List<CharacterData> LoadAllCharacters()
        {
            _characters.Clear();

            for (int i = 0; i < _maxSlots; i++)
            {
                var character = LoadCharacter(i);
                if (character != null)
                {
                    _characters.Add(character);
                }
            }

            return _characters;
        }

        private void RefreshCache()
        {
            LoadAllCharacters();
        }

        public bool CharacterExists(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _maxSlots)
                return false;

            string filePath = GetFilePath(slotIndex);
            return File.Exists(filePath);
        }

        public int GetNextAvailableSlot()
        {
            for (int i = 0; i < _maxSlots; i++)
            {
                if (!CharacterExists(i))
                {
                    return i;
                }
            }
            return -1;
        }

        public int GetUsedSlotCount()
        {
            int count = 0;
            for (int i = 0; i < _maxSlots; i++)
            {
                if (CharacterExists(i))
                {
                    count++;
                }
            }
            return count;
        }

        // ✅ Método HasAnyCharacter
        public bool HasAnyCharacter()
        {
            for (int i = 0; i < _maxSlots; i++)
            {
                if (CharacterExists(i))
                {
                    return true;
                }
            }
            return false;
        }

        // ✅ Método GetMostRecentCharacter
        public CharacterData GetMostRecentCharacter()
        {
            CharacterData mostRecent = null;
            DateTime mostRecentDate = DateTime.MinValue;

            for (int i = 0; i < _maxSlots; i++)
            {
                var character = LoadCharacter(i);
                if (character != null)
                {
                    if (character.ModifiedAt > mostRecentDate)
                    {
                        mostRecentDate = character.ModifiedAt;
                        mostRecent = character;
                    }
                }
            }

            return mostRecent;
        }

        // ✅ Método GetAllCharacters (retorna lista cacheada)
        public List<CharacterData> GetAllCharacters()
        {
            if (_characters == null || _characters.Count == 0)
            {
                LoadAllCharacters();
            }
            return _characters;
        }

        private string GetFilePath(int slotIndex)
        {
            return Path.Combine(_savePath, "character_" + slotIndex + ".xml");
        }
    }
}