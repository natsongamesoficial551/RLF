using System;
using GTA;

namespace RLF.GTA
{
    public static class RLFEntry
    {
        public static void Entry()
        {
            try
            {
                global::GTA.UI.Notification.Show("RLF iniciado via ASI");
                // aqui você chama seu bootstrap real
                // RLFCore.Initialize();
            }
            catch (Exception e)
            {
                global::GTA.UI.Notification.Show("RLF erro");
            }
        }
    }
}
