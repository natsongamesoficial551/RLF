using System;
using RLF.Core.CharacterCreator.Data;

namespace RLF.GTA.CharacterCreator.Storage
{
    public class CharacterSlotManager
    {
        private readonly CharacterIniStorage _iniStorage;
        private readonly int _maxSlots;
        private CharacterData[] _slots;

        public int MaxSlots => _maxSlots;

        public CharacterSlotManager(string savePath, int maxSlots = 25)
        {
            _maxSlots = maxSlots;
            _slots = new CharacterData[maxSlots];
            _iniStorage = new CharacterIniStorage(savePath);

            LoadAllSlots();
        }

        public void LoadAllSlots()
        {
            for (int i = 0; i < _maxSlots; i++)
            {
                _slots[i] = _iniStorage.LoadCharacter(i);
            }
        }

        public CharacterData GetSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _maxSlots)
                return null;

            return _slots[slotIndex];
        }

        public CharacterData[] GetAllSlots()
        {
            return _slots;
        }

        public bool SaveSlot(int slotIndex, CharacterData character)
        {
            if (slotIndex < 0 || slotIndex >= _maxSlots || character == null)
                return false;

            character.ModifiedAt = DateTime.Now;

            bool success = _iniStorage.SaveCharacter(character, slotIndex);

            if (success)
            {
                _slots[slotIndex] = character;
                System.Diagnostics.Debug.WriteLine($"✅ Personagem salvo no slot {slotIndex}: {character.Name}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao salvar no slot {slotIndex}");
            }

            return success;
        }

        // ⭐ CORRIGIDO: Retorna o slot onde foi salvo
        public int SaveToNextAvailableSlot(CharacterData character)
        {
            for (int i = 0; i < _maxSlots; i++)
            {
                if (_slots[i] == null)
                {
                    bool success = SaveSlot(i, character);
                    return success ? i : -1;
                }
            }
            return -1;
        }

        public int GetNextAvailableSlot()
        {
            for (int i = 0; i < _maxSlots; i++)
            {
                if (_slots[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }

        public bool DeleteSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _maxSlots)
                return false;

            bool success = _iniStorage.DeleteCharacter(slotIndex);
            if (success)
            {
                _slots[slotIndex] = null;
                System.Diagnostics.Debug.WriteLine($"✅ Slot {slotIndex} deletado");
            }

            return success;
        }

        public bool HasAnyCharacter()
        {
            for (int i = 0; i < _maxSlots; i++)
            {
                if (_slots[i] != null)
                    return true;
            }
            return false;
        }

        // ⭐ CORRIGIDO: Retorna tanto o personagem quanto o slot
        public (CharacterData character, int slot) GetMostRecentCharacterWithSlot()
        {
            CharacterData mostRecent = null;
            DateTime mostRecentDate = DateTime.MinValue;
            int mostRecentSlot = -1;

            for (int i = 0; i < _maxSlots; i++)
            {
                if (_slots[i] != null)
                {
                    if (_slots[i].ModifiedAt > mostRecentDate)
                    {
                        mostRecentDate = _slots[i].ModifiedAt;
                        mostRecent = _slots[i];
                        mostRecentSlot = i;
                    }
                }
            }

            return (mostRecent, mostRecentSlot);
        }

        public CharacterData GetMostRecentCharacter()
        {
            var (character, _) = GetMostRecentCharacterWithSlot();
            return character;
        }

        public int GetUsedSlotCount()
        {
            int count = 0;
            for (int i = 0; i < _maxSlots; i++)
            {
                if (_slots[i] != null)
                    count++;
            }
            return count;
        }

        public bool IsSlotOccupied(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _maxSlots)
                return false;

            return _slots[slotIndex] != null;
        }

        // ⭐ NOVO: Encontra o slot de um personagem pelo ID
        public int FindSlotByCharacterId(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return -1;

            for (int i = 0; i < _maxSlots; i++)
            {
                if (_slots[i] != null && _slots[i].Id == characterId)
                    return i;
            }
            return -1;
        }
    }
}