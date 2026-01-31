using System;
using System.IO;
using GTA;
using GTA.UI;
using RLF.GTA.CharacterCreator.Core;
using RLF.GTA.CharacterCreator.Storage;
using RLF.Core.CharacterCreator.Data;

namespace RLF.GTA.CharacterCreator
{
    public class CharacterCreatorSystem
    {
        private static CharacterCreatorSystem _instance;
        private static readonly object _lock = new object();

        public static CharacterCreatorSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CharacterCreatorSystem();
                        }
                    }
                }
                return _instance;
            }
        }

        public CharacterCreatorManager Manager { get; private set; }
        public CharacterSlotManager SlotManager { get; private set; }
        public bool IsInitialized { get; private set; }

        private string _savePath;
        private CharacterData _currentLoadedCharacter;
        private bool _autoSaveEnabled;

        private CharacterCreatorSystem()
        {
            IsInitialized = false;
            _autoSaveEnabled = true;
        }

        public void Initialize()
        {
            if (IsInitialized) return;

            try
            {
                string gtaPath = AppDomain.CurrentDomain.BaseDirectory;
                _savePath = Path.Combine(gtaPath, "scripts", "RLF", "CharacterData");

                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine("🎮 CHARACTER CREATOR SYSTEM - INIT");
                System.Diagnostics.Debug.WriteLine($"📁 GTA Path: {gtaPath}");
                System.Diagnostics.Debug.WriteLine($"💾 Save Path: {_savePath}");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");

                Notification.Show("~b~[RLF Creator]~w~ Inicializando...");
                Notification.Show($"~y~Save:~w~ {_savePath}");

                Manager = new CharacterCreatorManager();
                SlotManager = new CharacterSlotManager(_savePath, 25);

                LoadLastUsedCharacter();

                IsInitialized = true;

                System.Diagnostics.Debug.WriteLine("✅ Character Creator inicializado");
                Notification.Show($"~g~[RLF Creator]~w~ Inicializado! Slots: {SlotManager.GetUsedSlotCount()}/25");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ ERRO Initialize:");
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);

                Notification.Show("~r~[RLF Creator]~w~ ERRO ao inicializar!");
                IsInitialized = false;
            }
        }

        private void LoadLastUsedCharacter()
        {
            try
            {
                CharacterData mostRecent = null;
                DateTime mostRecentDate = DateTime.MinValue;
                int mostRecentSlot = -1;

                for (int i = 0; i < SlotManager.MaxSlots; i++)
                {
                    var character = SlotManager.GetSlot(i);
                    if (character != null && character.ModifiedAt > mostRecentDate)
                    {
                        mostRecentDate = character.ModifiedAt;
                        mostRecent = character;
                        mostRecentSlot = i;
                    }
                }

                if (mostRecent == null)
                {
                    System.Diagnostics.Debug.WriteLine("ℹ️ Nenhum personagem para auto-load");
                    Notification.Show("~y~[Info]~w~ Nenhum personagem");
                    return;
                }

                _currentLoadedCharacter = mostRecent;

                System.Diagnostics.Debug.WriteLine($"👤 Auto-load: {mostRecent.Name} (Slot {mostRecentSlot})");
                Notification.Show($"~y~[Auto-Load]~w~ {mostRecent.Name}");

                bool success = Manager.LoadCharacterToPlayer(mostRecent);

                System.Diagnostics.Debug.WriteLine(success
                    ? "✅ Auto-load OK"
                    : "❌ Auto-load falhou");

                if (!success)
                    Notification.Show("~r~[Auto-Load]~w~ Falhou!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ Erro em LoadLastUsedCharacter:");
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);

                Notification.Show($"~r~[Auto-Load Erro]~w~ {ex.Message}");
            }
        }

        public bool LoadCharacter(int slotIndex)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"📂 LOAD CHAMADO: Slot {slotIndex}");

                Notification.Show($"~b~[LOAD]~w~ Slot {slotIndex}");

                var character = SlotManager.GetSlot(slotIndex);
                if (character == null)
                {
                    Notification.Show($"~r~[Erro]~w~ Slot {slotIndex} vazio!");
                    return false;
                }

                bool success = Manager.LoadCharacterToPlayer(character);

                if (success)
                {
                    _currentLoadedCharacter = character;
                    Notification.Show($"~g~[OK]~w~ {character.Name}!");
                }
                else
                {
                    Notification.Show($"~r~[FALHOU]~w~ Erro ao aplicar!");
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ EXCEÇÃO em LoadCharacter:");
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);

                Notification.Show($"~r~[EXCEÇÃO]~w~ {ex.Message}");
                return false;
            }
        }

        public void AutoSaveCurrentCharacter()
        {
            if (!_autoSaveEnabled || _currentLoadedCharacter == null)
                return;

            try
            {
                for (int i = 0; i < SlotManager.MaxSlots; i++)
                {
                    var slot = SlotManager.GetSlot(i);
                    if (slot != null && slot.Id == _currentLoadedCharacter.Id)
                    {
                        _currentLoadedCharacter.ModifiedAt = DateTime.Now;
                        SlotManager.SaveSlot(i, _currentLoadedCharacter);

                        System.Diagnostics.Debug.WriteLine($"💾 Auto-save: {_currentLoadedCharacter.Name} (Slot {i})");
                        Notification.Show($"~g~[Auto-Save]~w~ {_currentLoadedCharacter.Name}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ Erro AutoSave:");
                System.Diagnostics.Debug.WriteLine(ex.Message);
                Notification.Show("~r~[Auto-Save]~w~ Falhou!");
            }
        }

        public CharacterData GetCurrentCharacter()
        {
            return _currentLoadedCharacter;
        }

        public void SetAutoSave(bool enabled)
        {
            _autoSaveEnabled = enabled;
            System.Diagnostics.Debug.WriteLine($"⚙️ Auto-save {(enabled ? "ativado" : "desativado")}");
            Notification.Show($"~b~[Auto-Save]~w~ {(enabled ? "ON" : "OFF")}");
        }

        public string GetSavePath()
        {
            return _savePath;
        }

        public void Shutdown()
        {
            System.Diagnostics.Debug.WriteLine("🔄 Desligando Character Creator System...");
            Notification.Show("~r~[RLF Creator]~w~ Desligando...");

            AutoSaveCurrentCharacter();
            Manager = null;
            SlotManager = null;
            _currentLoadedCharacter = null;
            IsInitialized = false;

            System.Diagnostics.Debug.WriteLine("✅ Character Creator System desligado");
        }
    }
}