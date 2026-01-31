using System;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using RLF.Core.CharacterCreator.Data;
using RLF.Core.CharacterCreator.Enums;
using RLF.Core.Events;

namespace RLF.GTA.CharacterCreator.Core
{
    public class CharacterCreatorManager
    {
        private Ped _creatorPed;
        private CharacterData _characterData;
        private CharacterBuilder _builder;
        private CharacterGender _currentGender;
        private bool _isSessionActive;
        private bool _isEditMode;
        private int _editingSlot;

        public Ped CreatorPed { get { return _creatorPed; } }
        public CharacterData CharacterData { get { return _characterData; } }
        public CharacterBuilder Builder { get { return _builder; } }
        public CharacterGender CurrentGender { get { return _currentGender; } }
        public bool IsSessionActive { get { return _isSessionActive; } }
        public bool IsEditMode { get { return _isEditMode; } }
        public int EditingSlot { get { return _editingSlot; } }

        public CharacterCreatorManager()
        {
            _builder = new CharacterBuilder();
            _isSessionActive = false;
            _isEditMode = false;
            _editingSlot = -1;
            _currentGender = CharacterGender.Male;
        }

        public bool StartNewSession(Vector3 position, float heading)
        {
            try
            {
                if (_isSessionActive)
                    CancelSession();

                _isEditMode = false;
                _editingSlot = -1;
                _currentGender = CharacterGender.Male;

                _characterData = new CharacterData();
                _characterData.Gender = _currentGender;

                Model model = new Model(PedHash.FreemodeMale01);
                model.Request(5000);

                if (!model.IsLoaded)
                    return false;

                _creatorPed = global::GTA.World.CreatePed(model, position, heading);
                model.MarkAsNoLongerNeeded();

                if (_creatorPed == null || !_creatorPed.Exists())
                    return false;

                _creatorPed.IsPositionFrozen = true;
                _creatorPed.IsInvincible = true;
                _creatorPed.BlockPermanentEvents = true;

                _builder.SetPed(_creatorPed);
                _builder.ApplyDefaultAppearance();

                _isSessionActive = true;
                CharacterCreatorEvents.InvokeCreatorStarted();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erro ao iniciar sessão: " + ex.Message);
                return false;
            }
        }

        public bool StartEditSession(CharacterData character, int slotIndex, Vector3 position, float heading)
        {
            try
            {
                if (_isSessionActive)
                    CancelSession();

                if (character == null)
                    return false;

                _isEditMode = true;
                _editingSlot = slotIndex;
                _currentGender = character.Gender;
                _characterData = character.Clone();

                PedHash hash = _currentGender == CharacterGender.Male
                    ? PedHash.FreemodeMale01
                    : PedHash.FreemodeFemale01;

                Model model = new Model(hash);
                model.Request(5000);

                if (!model.IsLoaded)
                    return false;

                _creatorPed = global::GTA.World.CreatePed(model, position, heading);
                model.MarkAsNoLongerNeeded();

                if (_creatorPed == null || !_creatorPed.Exists())
                    return false;

                _creatorPed.IsPositionFrozen = true;
                _creatorPed.IsInvincible = true;
                _creatorPed.BlockPermanentEvents = true;

                _builder.SetPed(_creatorPed);
                _builder.ApplyFullCharacter(_characterData);

                _isSessionActive = true;
                CharacterCreatorEvents.InvokeCreatorStarted();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erro ao iniciar edição: " + ex.Message);
                return false;
            }
        }


        public bool LoadCharacterToPlayer(CharacterData character)
        {
            if (character == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ CharacterData null");
                global::GTA.UI.Notification.Show("~r~[ERRO]~w~ CharacterData null");
                return false;
            }

            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    System.Diagnostics.Debug.WriteLine("❌ Player null/inexistente");
                    global::GTA.UI.Notification.Show("~r~[ERRO]~w~ Player inexistente");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"🎭 LOAD: {character.Name}");
                System.Diagnostics.Debug.WriteLine($"   Hair Style: {character.Hair.Style}");
                System.Diagnostics.Debug.WriteLine($"   Hair Color: {character.Hair.PrimaryColor}");
                System.Diagnostics.Debug.WriteLine($"   Hair Highlight: {character.Hair.HighlightColor}");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");

                Vector3 savedPos = player.Position;
                float savedHead = player.Heading;

                PedHash reqHash = character.Gender == CharacterGender.Male
                    ? PedHash.FreemodeMale01
                    : PedHash.FreemodeFemale01;

                bool needChange = (player.Model.Hash != (int)reqHash);

                // ✅ ETAPA 0: Trocar modelo (SEM Script.Wait)
                if (needChange)
                {
                    System.Diagnostics.Debug.WriteLine($"🔄 Troca modelo: {player.Model.Hash} → {(int)reqHash}");

                    Model model = new Model(reqHash);
                    model.Request(5000);

                    if (!model.IsLoaded)
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Model não carregou");
                        global::GTA.UI.Notification.Show("~r~[ERRO]~w~ Model fail");
                        return false;
                    }

                    Function.Call(Hash.SET_PLAYER_MODEL, Game.Player.Handle, model.Hash);
                    model.MarkAsNoLongerNeeded();

                    // Re-obter player após troca
                    player = Game.Player.Character;
                    if (player == null || !player.Exists())
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Player perdido pós-troca");
                        global::GTA.UI.Notification.Show("~r~[ERRO]~w~ Player lost");
                        return false;
                    }

                    // Restaurar posição
                    player.Position = savedPos;
                    player.Heading = savedHead;

                    System.Diagnostics.Debug.WriteLine("✅ Model OK (sem wait)");
                }

                // ✅ Aplicar aparência completa
                Builder.SetPed(player);
                Builder.ApplyFullCharacter(character);

                // ✅ Garantia extra: reaplica cor do cabelo no final
                // (o ApplyFullCharacter já faz isso, mas aqui é redundância segura)
                try
                {
                    Builder.SetPed(player);
                    Builder.ApplyHair(character.Hair);
                }
                catch { }

                global::GTA.UI.Notification.Show("~g~[OK]~w~ " + character.Name + "!");
                System.Diagnostics.Debug.WriteLine("✅ LOAD FINALIZADO COM SUCESSO");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ ERRO LoadCharacterToPlayer:");
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);

                global::GTA.UI.Notification.Show("~r~[ERRO]~w~ Falha ao carregar personagem");
                return false;
            }
        }

        private void UpdatePauseMenuIdentity(string characterName)
        {
            try
            {
                if (string.IsNullOrEmpty(characterName))
                    characterName = "Jogador";

                string firstName = characterName;
                if (characterName.Contains(" "))
                    firstName = characterName.Substring(0, characterName.IndexOf(" "));

                string[] statNames = { "MP0_CHAR_NAME", "MP0_PLAYER_NAME", "MPPLY_CHAR_NAME", "PLAYER_NAME" };

                foreach (string statName in statNames)
                {
                    try
                    {
                        int statHash = Function.Call<int>(Hash.GET_HASH_KEY, statName);
                        Function.Call(Hash.STAT_SET_STRING, statHash, firstName, true);
                    }
                    catch { }
                }
            }
            catch { }
        }

        public void StartCreatorSession(Ped ped)
        {
            _creatorPed = ped;
            _characterData = new CharacterData();
            _characterData.Gender = CharacterGender.Male;
            _builder.SetPed(ped);
            _builder.ApplyDefaultAppearance();
            _isSessionActive = true;
            _isEditMode = false;
            _editingSlot = -1;
        }

        public void ApplyCharacterData(CharacterData data)
        {
            if (data == null || _builder == null) return;

            _characterData = data;
            _builder.ApplyFullCharacter(data);
        }

        public void ChangeGender(CharacterGender newGender)
        {
            if (!_isSessionActive || newGender == _currentGender)
                return;

            try
            {
                Vector3 position = _creatorPed.Position;
                float heading = _creatorPed.Heading;

                if (_creatorPed != null && _creatorPed.Exists())
                    _creatorPed.Delete();

                _currentGender = newGender;
                _characterData.Gender = newGender;

                PedHash hash = newGender == CharacterGender.Male ? PedHash.FreemodeMale01 : PedHash.FreemodeFemale01;
                Model model = new Model(hash);
                model.Request(5000);

                if (!model.IsLoaded)
                    return;

                _creatorPed = global::GTA.World.CreatePed(model, position, heading);

                if (_creatorPed == null || !_creatorPed.Exists())
                    return;

                _creatorPed.IsPositionFrozen = true;
                _creatorPed.IsInvincible = true;
                _creatorPed.BlockPermanentEvents = true;

                _builder.SetPed(_creatorPed);
                _builder.ApplyFullCharacter(_characterData);

                CharacterCreatorEvents.InvokeGenderChanged(newGender);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erro ao mudar gênero: " + ex.Message);
            }
        }

        public void Randomize()
        {
            if (!_isSessionActive) return;

            var rand = new Random();

            _characterData.Genetics.ShapeFirst = rand.Next(0, 46);
            _characterData.Genetics.ShapeSecond = rand.Next(0, 46);
            _characterData.Genetics.ShapeThird = 0;
            _characterData.Genetics.SkinFirst = rand.Next(0, 46);
            _characterData.Genetics.SkinSecond = rand.Next(0, 46);
            _characterData.Genetics.SkinThird = 0;
            _characterData.Genetics.ShapeMix = (float)rand.NextDouble();
            _characterData.Genetics.SkinMix = (float)rand.NextDouble();
            _characterData.Genetics.ThirdMix = 0f;

            for (int i = 0; i < 20; i++)
            {
                float value = (float)(rand.NextDouble() * 2 - 1);
                _characterData.FaceFeatures.SetFeature((FaceFeature)i, value);
            }

            _characterData.Hair.Style = rand.Next(0, 50);
            _characterData.Hair.PrimaryColor = rand.Next(0, 64);
            _characterData.Hair.HighlightColor = rand.Next(0, 64);

            _characterData.EyeColor = rand.Next(0, 12);

            _builder.ApplyFullCharacter(_characterData);
        }

        public CharacterData FinishSession()
        {
            if (!_isSessionActive)
                return null;

            try
            {
                var player = Game.Player.Character;
                if (player != null && player.Exists())
                {
                    _builder.SetPed(player);
                    _builder.ApplyFullCharacter(_characterData);

                    UpdatePauseMenuIdentity(_characterData.Name);
                }

                if (_creatorPed != null && _creatorPed.Exists())
                    _creatorPed.Delete();

                _creatorPed = null;
                _isSessionActive = false;

                _characterData.ModifiedAt = DateTime.Now;

                var result = _characterData;
                _characterData = null;

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erro ao finalizar sessão: " + ex.Message);
                return null;
            }
        }

        public void CancelSession()
        {
            if (!_isSessionActive)
                return;

            try
            {
                if (_creatorPed != null && _creatorPed.Exists())
                    _creatorPed.Delete();

                _creatorPed = null;
                _characterData = null;
                _isSessionActive = false;
                _isEditMode = false;
                _editingSlot = -1;

                CharacterCreatorEvents.InvokeCreatorCancelled();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erro ao cancelar sessão: " + ex.Message);
            }
        }
    }
}