using GTA.Math;
using System.Collections.Generic;

namespace RLF.GTA.Law.Police
{
    public static class PoliceSpawnConfig
    {
        public const int UrbanUnits = 7;
        public const int RuralUnits = 5;

        public static readonly List<Vector3> UrbanSpawnPoints = new List<Vector3>
        {
            new Vector3(425.1f, -981.5f, 30.7f),
            new Vector3(610.2f, 25.8f, 90.1f),
            new Vector3(-1738.4f, -365.2f, 48.4f),
            new Vector3(-1205.2f, -1565.8f, 4.6f),
            new Vector3(1544.6f, -1850.2f, 68.5f),
            new Vector3(112.5f, -1955.3f, 20.7f),
            new Vector3(-714.8f, -254.1f, 36.8f)
        };

        public static readonly List<Vector3> RuralSpawnPoints = new List<Vector3>
        {
            new Vector3(1854.7f, 3684.3f, 34.3f),
            new Vector3(-447.2f, 6012.6f, 31.7f),
            new Vector3(2542.1f, 4688.6f, 34.0f),
            new Vector3(1692.2f, 2606.0f, 45.6f),
            new Vector3(-1150.9f, 4925.0f, 222.1f)
        };

        public static Vector3 GetSpawnPoint(PoliceUnitType type, int unitId)
        {
            if (type == PoliceUnitType.Urban)
            {
                int idx = (unitId - 1) % UrbanSpawnPoints.Count;
                return UrbanSpawnPoints[idx];
            }
            else
            {
                int idx = (unitId - 1) % RuralSpawnPoints.Count;
                return RuralSpawnPoints[idx];
            }
        }
    }
}