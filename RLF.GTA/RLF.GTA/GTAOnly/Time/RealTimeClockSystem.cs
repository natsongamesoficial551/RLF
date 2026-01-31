using GTA;
using GTA.Native;
using System;
using static GTA.Native.Hash;

public class RealTimeClock : Script
{
    private DateTime gameTime;
    private DateTime lastUpdate;

    public RealTimeClock()
    {
        // Começa no horário atual do PC (só na inicialização)
        gameTime = DateTime.Now;
        lastUpdate = DateTime.UtcNow;

        // Impede o GTA de avançar o tempo sozinho
        Function.Call(PAUSE_CLOCK, true);

        Tick += OnTick;
    }

    private void OnTick(object sender, EventArgs e)
    {
        DateTime now = DateTime.UtcNow;
        TimeSpan delta = now - lastUpdate;

        // Só atualiza se pelo menos 1 segundo real passou
        if (delta.TotalSeconds >= 1)
        {
            gameTime = gameTime.AddSeconds(delta.TotalSeconds);
            lastUpdate = now;

            // Aplica o tempo no GTA
            Function.Call(
                SET_CLOCK_TIME,
                gameTime.Hour,
                gameTime.Minute,
                gameTime.Second
            );
        }
    }
}
