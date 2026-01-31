using System;
using System.Drawing;
using GTA;
using RLF.Core;
using RLF.Core.Identity.Enums;

namespace RLF.GTA.CoreIntegration.Identity
{
    /// <summary>
    /// Overlay visual de status legal (Documentos / Licenças).
    /// Apenas leitura.
    /// </summary>
    public sealed class IdentityOverlay
    {
        private readonly RLFCore _core;
        private bool _visible;

        public bool IsVisible => _visible;

        public IdentityOverlay()
        {
            _core = RLFCore.Instance;
            _visible = false;
        }

        public void Toggle()
        {
            _visible = !_visible;
        }

        public void Hide()
        {
            _visible = false;
        }

        public void Draw()
        {
            if (!_visible)
                return;

            // 📍 CANTO SUPERIOR DIREITO
            float screenWidth = global::GTA.UI.Screen.Width;
            float x = screenWidth - 420f;
            float y = 40f;
            float lineHeight = 28f;

            DrawTitle("DOCUMENTOS & LICENCAS", x, y);
            y += lineHeight * 1.5f;

            // RG (Identidade)
            DrawDocumentStatus("RG", HasAnyDocument(), x, ref y, lineHeight);

            // CNH (Carro OU Moto)
            DrawDocumentStatus(
                "CNH",
                HasLicense(LicenseType.DriverCar) || HasLicense(LicenseType.DriverMoto),
                x,
                ref y,
                lineHeight
            );

            // CHT (Piloto de Avião)
            DrawDocumentStatus(
                "CHT",
                HasLicense(LicenseType.PilotPlane),
                x,
                ref y,
                lineHeight
            );

            // Porte de Arma
            DrawDocumentStatus(
                "Porte de Arma",
                HasLicense(LicenseType.WeaponPermit),
                x,
                ref y,
                lineHeight
            );
        }

        private void DrawTitle(string text, float x, float y)
        {
            new global::GTA.UI.TextElement(
                text,
                new PointF(x, y),
                0.45f,
                Color.White,
                global::GTA.UI.Font.ChaletLondon,
                global::GTA.UI.Alignment.Left
            ).Draw();
        }

        private void DrawDocumentStatus(
            string label,
            bool valid,
            float x,
            ref float y,
            float lineHeight)
        {
            string statusText = valid ? "VALIDO" : "INVALIDO";
            Color statusColor = valid ? Color.LimeGreen : Color.Red;

            new global::GTA.UI.TextElement(
                $"{label}:",
                new PointF(x, y),
                0.35f,
                Color.LightGray,
                global::GTA.UI.Font.ChaletLondon,
                global::GTA.UI.Alignment.Left
            ).Draw();

            new global::GTA.UI.TextElement(
                statusText,
                new PointF(x + 220f, y),
                0.35f,
                statusColor,
                global::GTA.UI.Font.ChaletLondon,
                global::GTA.UI.Alignment.Left
            ).Draw();

            y += lineHeight;
        }

        // ===============================
        // 🔍 HELPERS
        // ===============================

        private bool HasLicense(LicenseType type)
        {
            try
            {
                var docSystem = _core.Systems.Get("DocumentSystem")
                    as RLF.Core.Identity.DocumentSystem;

                return docSystem != null && docSystem.HasValidLicense(type);
            }
            catch
            {
                return false;
            }
        }

        // RG / Identidade (existência básica)
        private bool HasAnyDocument()
        {
            try
            {
                // Por enquanto, se o DocumentSystem existe, o RG é considerado presente
                // Futuro: checar documento específico (IdentityCard)
                return _core.Systems.Get("DocumentSystem") != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
