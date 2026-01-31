using GTA.Math;
using System.Collections.Generic;

namespace RLF.GTA.Identity.FlightSchool
{
    public static class FlightTestRoutes
    {
        // ✈️ TESTE DE AVIÃO – Voo direto LSIA → Sandy Shores (só início e fim)
        public static readonly List<Vector3> PlaneRoute = new List<Vector3>
        {
            // 1. INÍCIO - LSIA (Los Santos International Airport)
            new Vector3(-951.166f, -3171.332f, 14.874f),
            
            // 2. FIM - Sandy Shores Airfield
            new Vector3(1701.282f, 3253.114f, 41.908f)
        };
    }
}