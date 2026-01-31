using GTA.Math;
using System.Collections.Generic;

namespace RLF.GTA.Identity.DrivingSchool
{
    public static class DrivingTestRoutes
    {
        // 🚗 TESTE DO CARRO – PÁTIO / ESTACIONAMENTO (loop fechado)
        public static readonly List<Vector3> CarRoute = new List<Vector3>
        {
            new Vector3(-1588.747f, -833.688f, 9.881f), // Início
            new Vector3(-1645.583f, -944.075f, 8.064f),
            new Vector3(-1711.752f, -887.826f, 8.031f),
            new Vector3(-1661.969f, -827.221f, 9.790f),
            new Vector3(-1588.747f, -833.688f, 9.881f)  // Final (volta)
        };
    }
}
