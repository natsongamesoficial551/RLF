// ===============================
// UberHUD.cs
// ===============================
using System.Drawing;
using GTA;

namespace RLF.GTA.Jobs.Uber.UI
{
    public sealed class UberHUD
    {
        private bool _visible;

        public bool IsVisible
        {
            get => _visible;
            set => _visible = value;
        }

        public void Draw(Core.UberAccount account, Ride.RideState ride)
        {
            if (!_visible)
                return;

            float x = 20f;
            float y = 200f;
            float lineHeight = 25f;

            // Header
            DrawText("UBER", x, y, 0.5f, Color.Yellow);
            y += lineHeight * 1.5f;

            // Rating
            DrawText($"⭐ {account.AverageRating:F2}/5.0", x, y, 0.35f, GetRatingColor(account.AverageRating));
            y += lineHeight;

            // Corridas
            DrawText($"🚗 {account.TotalRides} corridas", x, y, 0.35f, Color.White);
            y += lineHeight;

            // Ganhos
            DrawText($"💰 ${account.TotalEarned:F2}", x, y, 0.35f, Color.LimeGreen);
            y += lineHeight;

            // Status da corrida atual
            if (ride.IsActive)
            {
                y += lineHeight * 0.5f;
                DrawText($"📦 {ride.Category}", x, y, 0.35f, Color.Cyan);
                y += lineHeight;
                DrawText($"📏 {ride.DistanceTraveled:F0}m", x, y, 0.35f, Color.White);
            }
        }

        private void DrawText(string text, float x, float y, float scale, Color color)
        {
            new global::GTA.UI.TextElement(
                text,
                new PointF(x, y),
                scale,
                color,
                global::GTA.UI.Font.ChaletLondon,
                global::GTA.UI.Alignment.Left
            ).Draw();
        }

        private Color GetRatingColor(float rating)
        {
            if (rating >= 4.8f)
                return Color.Gold;
            else if (rating >= 4.5f)
                return Color.LimeGreen;
            else if (rating >= 4.0f)
                return Color.Yellow;
            else if (rating >= 3.5f)
                return Color.Orange;
            else
                return Color.Red;
        }
    }
}