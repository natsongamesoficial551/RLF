using RLF.Core.CharacterCreator.Data;
using System.Collections.Generic;

namespace RLF.Core.CharacterCreator.Interfaces
{
    public interface ICharacterStore
    {
        bool SaveCharacter(CharacterData character);
        CharacterData LoadCharacter(string characterId);
        CharacterData LoadCharacterBySlot(int slotIndex);
        List<CharacterData> GetAllCharacters();
        bool DeleteCharacter(string characterId);
        bool HasCharacterInSlot(int slotIndex);
        int GetNextAvailableSlot();
        bool HasAnyCharacter();
    }
}