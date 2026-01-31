using RLF.Core.CharacterCreator.Data;
using RLF.Core.CharacterCreator.Enums;
using System;

namespace RLF.Core.CharacterCreator.Interfaces
{
    public interface ICharacterCreatorService
    {
        CreatorState CurrentState { get; }
        CharacterData CurrentCharacter { get; }
        bool IsActive { get; }

        void StartCreation();
        void StartEditing(CharacterData character);
        CharacterData FinishAndSave();
        void Cancel();
        void ApplyPreset(CharacterPreset preset);
        void RandomizeCharacter();
        void ResetToDefault();
        void SetGender(CharacterGender gender);
        void SetName(string name);

        event Action<CreatorState> OnStateChanged;
        event Action<CharacterData> OnAppearanceChanged;
    }
}