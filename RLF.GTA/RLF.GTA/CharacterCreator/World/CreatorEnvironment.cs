using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace RLF.GTA.CharacterCreator.World
{
    public static class SpawnPoints
    {
        public static readonly Vector3 Default = new Vector3(-1037.0f, -2737.0f, 20.17f);
        public static readonly float DefaultHeading = 330f;
    }

    public class CreatorEnvironment
    {
        private CreatorLocation _location;
        private bool _isActive;

        private global::GTA.Weather _originalWeather;
        private int _originalHour;
        private int _originalMinute;

        public bool IsActive { get { return _isActive; } }
        public CreatorLocation CurrentLocation { get { return _location; } }

        public CreatorEnvironment()
        {
            _isActive = false;
        }

        public void Setup(CreatorLocation location)
        {
            if (location == null)
                location = CreatorLocations.Default;

            _location = location;
            SaveOriginalState();
            ApplyCreatorEnvironment();
            _isActive = true;
        }

        private void SaveOriginalState()
        {
            _originalWeather = global::GTA.World.Weather;
            _originalHour = global::GTA.World.CurrentTimeOfDay.Hours;
            _originalMinute = global::GTA.World.CurrentTimeOfDay.Minutes;
        }

        private void ApplyCreatorEnvironment()
        {
            global::GTA.World.Weather = _location.LocationWeather;
            global::GTA.World.CurrentTimeOfDay = new TimeSpan(_location.Hour, _location.Minute, 0);
            Function.Call(Hash.PAUSE_CLOCK, true);
        }

        public void TeleportToLocation()
        {
            if (_location == null) return;

            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            player.Position = _location.Position;
            player.Heading = _location.Heading;

            Function.Call(Hash.LOAD_SCENE, _location.Position.X, _location.Position.Y, _location.Position.Z);
        }

        public void TeleportToSpawn(Vector3 spawnPosition)
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            player.Position = spawnPosition;
            player.Heading = SpawnPoints.DefaultHeading;
        }

        public void Restore()
        {
            if (!_isActive) return;

            global::GTA.World.Weather = _originalWeather;
            global::GTA.World.CurrentTimeOfDay = new TimeSpan(_originalHour, _originalMinute, 0);
            Function.Call(Hash.PAUSE_CLOCK, false);

            _isActive = false;
        }

        public void Update()
        {
            if (!_isActive) return;
            Function.Call(Hash.PAUSE_CLOCK, true);
        }

        public static void DisableMovementControls()
        {
            Game.DisableControlThisFrame(global::GTA.Control.MoveUp);
            Game.DisableControlThisFrame(global::GTA.Control.MoveDown);
            Game.DisableControlThisFrame(global::GTA.Control.MoveLeft);
            Game.DisableControlThisFrame(global::GTA.Control.MoveRight);
            Game.DisableControlThisFrame(global::GTA.Control.Sprint);
            Game.DisableControlThisFrame(global::GTA.Control.Jump);
            Game.DisableControlThisFrame(global::GTA.Control.Enter);
        }

        public static void DisableCombatControls()
        {
            Game.DisableControlThisFrame(global::GTA.Control.Attack);
            Game.DisableControlThisFrame(global::GTA.Control.Aim);
            Game.DisableControlThisFrame(global::GTA.Control.Attack2);
            Game.DisableControlThisFrame(global::GTA.Control.MeleeAttackLight);
            Game.DisableControlThisFrame(global::GTA.Control.MeleeAttackHeavy);
            Game.DisableControlThisFrame(global::GTA.Control.SelectWeapon);
        }
    }
}