using RLF.Core.CharacterCreator.Data;
using RLF.Core.CharacterCreator.Enums;
using System;

namespace RLF.Core.Events
{
    public static class CharacterCreatorEvents
    {
        // Lifecycle Events
        public static event Action OnCreatorStarted;
        public static event Action OnCreatorCancelled;
        public static event Action<CreatorState> OnStateChanged;

        // Character Events
        public static event Action<CharacterGender> OnGenderChanged;
        public static event Action<CharacterData> OnAppearanceChanged;
        public static event Action<CharacterData> OnCharacterCreated;
        public static event Action<CharacterData> OnCharacterLoaded;
        public static event Action<string> OnCharacterDeleted;

        // UI Events
        public static event Action<string> OnCategoryChanged;
        public static event Action<CharacterPreset> OnPresetApplied;

        // Invoke Methods
        public static void InvokeCreatorStarted()
        {
            if (OnCreatorStarted != null)
                OnCreatorStarted();
        }

        public static void InvokeCreatorCancelled()
        {
            if (OnCreatorCancelled != null)
                OnCreatorCancelled();
        }

        public static void InvokeStateChanged(CreatorState state)
        {
            if (OnStateChanged != null)
                OnStateChanged(state);
        }

        public static void InvokeGenderChanged(CharacterGender gender)
        {
            if (OnGenderChanged != null)
                OnGenderChanged(gender);
        }

        public static void InvokeAppearanceChanged(CharacterData data)
        {
            if (OnAppearanceChanged != null)
                OnAppearanceChanged(data);
        }

        public static void InvokeCharacterCreated(CharacterData data)
        {
            if (OnCharacterCreated != null)
                OnCharacterCreated(data);
        }

        public static void InvokeCharacterLoaded(CharacterData data)
        {
            if (OnCharacterLoaded != null)
                OnCharacterLoaded(data);
        }

        public static void InvokeCharacterDeleted(string characterId)
        {
            if (OnCharacterDeleted != null)
                OnCharacterDeleted(characterId);
        }

        public static void InvokeCategoryChanged(string category)
        {
            if (OnCategoryChanged != null)
                OnCategoryChanged(category);
        }

        public static void InvokePresetApplied(CharacterPreset preset)
        {
            if (OnPresetApplied != null)
                OnPresetApplied(preset);
        }

        public static void ClearAllHandlers()
        {
            OnCreatorStarted = null;
            OnCreatorCancelled = null;
            OnStateChanged = null;
            OnGenderChanged = null;
            OnAppearanceChanged = null;
            OnCharacterCreated = null;
            OnCharacterLoaded = null;
            OnCharacterDeleted = null;
            OnCategoryChanged = null;
            OnPresetApplied = null;
        }
    }
}