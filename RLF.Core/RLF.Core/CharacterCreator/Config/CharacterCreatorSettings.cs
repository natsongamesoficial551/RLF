using System;

namespace RLF.Core.CharacterCreator.Config
{
    [Serializable]
    public class CharacterCreatorSettings
    {
        public int MaxCharacterSlots { get; set; }
        public bool AllowRandomization { get; set; }
        public bool AllowPresets { get; set; }
        public bool ShowAdvancedFaceOptions { get; set; }
        public int FadeDuration { get; set; }
        public string StartCreationKey { get; set; }
        public bool AutoSaveDuringCreation { get; set; }
        public int AutoSaveInterval { get; set; }
        public string DefaultCreationLocation { get; set; }
        public string InitialCameraPreset { get; set; }
        public float CameraTransitionSpeed { get; set; }
        public bool AllowCharacterRotation { get; set; }
        public float RotationSensitivity { get; set; }

        public CharacterCreatorSettings()
        {
            MaxCharacterSlots = 10;
            AllowRandomization = true;
            AllowPresets = true;
            ShowAdvancedFaceOptions = true;
            FadeDuration = 1000;
            StartCreationKey = "E";
            AutoSaveDuringCreation = false;
            AutoSaveInterval = 60;
            DefaultCreationLocation = "OnlineCreator";
            InitialCameraPreset = "FullBody";
            CameraTransitionSpeed = 1.0f;
            AllowCharacterRotation = true;
            RotationSensitivity = 1.0f;
        }
    }
}