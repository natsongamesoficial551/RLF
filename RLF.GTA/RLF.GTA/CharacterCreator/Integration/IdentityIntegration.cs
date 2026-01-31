using System;

namespace RLF.GTA.CharacterCreator.Integration
{
    public static class IdentityIntegration
    {
        private static bool _initialized;
        private static bool _available;
        private static string _currentFirstName;
        private static string _currentLastName;

        // Delegate para integração com sistema de identidade externo
        public static Action<string, string> OnIdentityChanged;

        public static void Initialize()
        {
            try
            {
                _initialized = true;
                _available = true;
                _currentFirstName = "";
                _currentLastName = "";
            }
            catch
            {
                _available = false;
            }
        }

        public static void Shutdown()
        {
            _initialized = false;
            _available = false;
            _currentFirstName = "";
            _currentLastName = "";
            OnIdentityChanged = null;
        }

        public static bool IsAvailable()
        {
            return _initialized && _available;
        }

        public static void SetIdentity(string firstName, string lastName)
        {
            if (!_available) return;

            _currentFirstName = firstName ?? "";
            _currentLastName = lastName ?? "";

            // Notificar sistemas externos
            OnIdentityChanged?.Invoke(_currentFirstName, _currentLastName);

            // Aqui você pode integrar com outros sistemas do seu mod
            // Por exemplo, setar o nome no HUD, salvar em arquivo, etc.
        }

        public static string GetFirstName()
        {
            return _currentFirstName;
        }

        public static string GetLastName()
        {
            return _currentLastName;
        }

        public static string GetFullName()
        {
            if (string.IsNullOrEmpty(_currentLastName))
                return _currentFirstName;

            return _currentFirstName + " " + _currentLastName;
        }
    }
}