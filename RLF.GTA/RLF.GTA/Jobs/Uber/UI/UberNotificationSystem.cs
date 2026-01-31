// ===============================
// UberNotificationSystem.cs
// ===============================
using GTA;
using GTA.Native;

namespace RLF.GTA.Jobs.Uber.UI
{
    public static class UberNotificationSystem
    {
        public static void ShowRideRequest(Ride.RideCategory category, int timeoutSeconds)
        {
            global::GTA.UI.Notification.Show(
                $"📲 Nova corrida {category}\n⏱️ {timeoutSeconds}s para aceitar\nENTER = Aceitar | BACKSPACE = Recusar"
            );

            PlaySound("Menu_Accept");
        }

        public static void ShowRideAccepted()
        {
            global::GTA.UI.Notification.Show("✅ Corrida aceita\n📍 Dirija-se ao ponto de coleta");
            PlaySound("CHALLENGE_UNLOCKED");
        }

        public static void ShowRideCompleted(decimal payment, float rating)
        {
            global::GTA.UI.Notification.Show(
                $"✅ Corrida concluída!\n💰 +${payment:F2}\n⭐ {rating:F1} estrelas"
            );
            PlaySound("PURCHASE");
        }

        public static void ShowPenalty(decimal amount)
        {
            global::GTA.UI.Notification.Show(
                $"❌ Penalidade aplicada\n💸 -${amount:F2}"
            );
            PlaySound("LOSER");
        }

        public static void ShowBanned(string message)
        {
            global::GTA.UI.Notification.Show($"🚫 {message}");
            PlaySound("LOSER");
        }

        private static void PlaySound(string soundName)
        {
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, soundName, "HUD_FRONTEND_DEFAULT_SOUNDSET");
        }
    }
}