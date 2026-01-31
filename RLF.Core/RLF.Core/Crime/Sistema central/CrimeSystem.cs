using System;
using System.Collections.Generic;
using System.Linq;

namespace RLF.Core.Crime
{
    /// <summary>
    /// Sistema central de gerenciamento de crimes.
    /// Responsável por registrar, classificar e gerenciar casos criminais.
    /// </summary>
    public class CrimeSystem
    {
        private readonly List<CrimeCase> _activeCases;
        private readonly List<CrimeCase> _closedCases;
        private readonly Dictionary<CrimeType, CrimeSeverity> _crimeSeverityMap;
        private float _currentHeat;
        private HeatState _currentHeatState;
        private DateTime _lastHeatUpdate;
        private bool _isInitialized;

        public float CurrentHeat => _currentHeat;
        public HeatState CurrentHeatState => _currentHeatState;
        public int ActiveCaseCount => _activeCases.Count;
        public int TotalCrimeCount => _activeCases.Sum(c => c.GetCrimeCount());

        public CrimeSystem()
        {
            _activeCases = new List<CrimeCase>();
            _closedCases = new List<CrimeCase>();
            _crimeSeverityMap = new Dictionary<CrimeType, CrimeSeverity>();
            _currentHeat = 0f;
            _currentHeatState = HeatState.None;
            _lastHeatUpdate = DateTime.Now;
            _isInitialized = false;

            InitializeCrimeSeverityMap();
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            _activeCases.Clear();
            _closedCases.Clear();
            _currentHeat = 0f;
            _currentHeatState = HeatState.None;
            _lastHeatUpdate = DateTime.Now;
            _isInitialized = true;
        }

        public void Update(float deltaTime)
        {
            if (!_isInitialized) return;

            TimeSpan timeSinceLastUpdate = DateTime.Now - _lastHeatUpdate;
            if (timeSinceLastUpdate.TotalSeconds >= 1.0)
            {
                UpdateHeatDecay(deltaTime);
                CheckCaseExpiration();
                _lastHeatUpdate = DateTime.Now;
            }
        }

        public CrimeRecord RegisterCrime(CrimeType type, float x, float y, float z,
            string locationName = "", string zoneName = "")
        {
            if (!_isInitialized) return null;

            CrimeSeverity severity = GetCrimeSeverity(type);
            CrimeRecord crime = new CrimeRecord(type, severity);
            crime.SetLocation(x, y, z, locationName, zoneName);

            if (severity >= CrimeSeverity.Felony)
            {
                crime.AddFlag(CrimeFlags.EligibleForArrest);
            }

            CrimeCase activeCase = GetOrCreateActiveCase();
            activeCase.AddCrime(crime);

            float crimeHeat = crime.GetHeatContribution();
            AddHeat(crimeHeat);

            CrimeEvents.RaiseCrimeCommitted(crime);

            return crime;
        }

        public void MarkCrimeAsWitnessed(CrimeRecord crime, string witnessId)
        {
            if (crime == null) return;

            crime.AddFlag(CrimeFlags.Witnessed);
            crime.Evidence.AddWitness(witnessId);

            AddHeat(0.05f);
        }

        public void MarkCrimeAsReported(CrimeRecord crime)
        {
            if (crime == null || crime.WasReported()) return;

            crime.AddFlag(CrimeFlags.Reported);

            CrimeEvents.RaiseCrimeReported(crime, true);

            AddHeat(0.15f);
        }

        public void AddCameraEvidence(CrimeRecord crime, string cameraId)
        {
            if (crime == null) return;

            crime.AddFlag(CrimeFlags.CameraRecorded);
            crime.Evidence.AddCameraFootage(cameraId);

            AddHeat(0.10f);
        }

        public void IdentifySuspect(CrimeRecord crime, string characterName, float confidence)
        {
            if (crime == null) return;

            crime.Suspect.UpdateIdentification(characterName, confidence);

            if (confidence >= 0.7f)
            {
                crime.AddFlag(CrimeFlags.SuspectIdentified);
                AddHeat(0.20f);
            }
        }

        public void IdentifySuspectVehicle(CrimeRecord crime, string model, string plate)
        {
            if (crime == null) return;

            crime.Suspect.UpdateVehicle(model, plate);
            crime.AddFlag(CrimeFlags.VehicleIdentified);

            AddHeat(0.15f);
        }

        public void AddHeat(float amount)
        {
            if (amount <= 0f) return;

            _currentHeat = Math.Max(0f, Math.Min(_currentHeat + amount, 1f));

            HeatState newState = CalculateHeatState(_currentHeat);
            if (newState != _currentHeatState)
            {
                _currentHeatState = newState;
                CrimeEvents.RaiseHeatChanged(_currentHeat, _currentHeatState);
            }
        }

        public void ReduceHeat(float amount)
        {
            if (amount <= 0f) return;

            _currentHeat = Math.Max(0f, Math.Min(_currentHeat - amount, 1f));

            HeatState newState = CalculateHeatState(_currentHeat);
            if (newState != _currentHeatState)
            {
                _currentHeatState = newState;
                CrimeEvents.RaiseHeatChanged(_currentHeat, _currentHeatState);
            }
        }

        public void ClearHeat()
        {
            _currentHeat = 0f;
            HeatState oldState = _currentHeatState;
            _currentHeatState = HeatState.None;

            if (oldState != HeatState.None)
            {
                CrimeEvents.RaiseHeatChanged(_currentHeat, _currentHeatState);
            }
        }

        public List<CrimeCase> GetActiveCases()
        {
            return new List<CrimeCase>(_activeCases);
        }

        public List<CrimeCase> GetEligibleCasesForArrest()
        {
            return _activeCases
                .Where(c => c.IsActive && c.IsEligibleForArrest)
                .OrderByDescending(c => c.MaxSeverity)
                .ToList();
        }

        public CrimeCase GetMostRecentActiveCase()
        {
            return _activeCases
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.LastActivityAt)
                .FirstOrDefault();
        }

        public void CloseCase(CrimeCase crimeCase)
        {
            if (crimeCase == null || !crimeCase.IsActive) return;

            crimeCase.Close();
            _activeCases.Remove(crimeCase);
            _closedCases.Add(crimeCase);

            CrimeEvents.RaiseCaseClosed(crimeCase);
        }

        public void CloseAllCases()
        {
            var casesToClose = _activeCases.Where(c => c.IsActive).ToList();
            foreach (var crimeCase in casesToClose)
            {
                CloseCase(crimeCase);
            }
        }

        private CrimeCase GetOrCreateActiveCase()
        {
            CrimeCase mostRecent = GetMostRecentActiveCase();

            if (mostRecent != null && mostRecent.TimeSinceLastActivity().TotalHours < 6)
            {
                return mostRecent;
            }

            CrimeCase newCase = new CrimeCase();
            _activeCases.Add(newCase);
            CrimeEvents.RaiseCaseOpened(newCase);

            return newCase;
        }

        private HeatState CalculateHeatState(float heat)
        {
            if (heat >= 0.90f) return HeatState.Extreme;
            if (heat >= 0.70f) return HeatState.Critical;
            if (heat >= 0.50f) return HeatState.High;
            if (heat >= 0.25f) return HeatState.Medium;
            if (heat >= 0.05f) return HeatState.Low;
            return HeatState.None;
        }

        private float GetDecayRateForState(HeatState state)
        {
            switch (state)
            {
                case HeatState.Low: return 0.0008f;
                case HeatState.Medium: return 0.0005f;
                case HeatState.High: return 0.0003f;
                case HeatState.Critical: return 0.0002f;
                case HeatState.Extreme: return 0.0001f;
                default: return 0f;
            }
        }

        private void UpdateHeatDecay(float deltaTime)
        {
            if (_currentHeat <= 0f) return;

            float decayRate = GetDecayRateForState(_currentHeatState);
            ReduceHeat(decayRate * deltaTime);
        }

        private void CheckCaseExpiration()
        {
            var expiredCases = _activeCases
                .Where(c => c.IsActive && c.TimeSinceLastActivity().TotalDays >= 7)
                .ToList();

            foreach (var expiredCase in expiredCases)
            {
                CloseCase(expiredCase);
            }
        }

        private CrimeSeverity GetCrimeSeverity(CrimeType type)
        {
            if (_crimeSeverityMap.TryGetValue(type, out CrimeSeverity severity))
            {
                return severity;
            }
            return CrimeSeverity.Misdemeanor;
        }

        private void InitializeCrimeSeverityMap()
        {
            _crimeSeverityMap.Clear();

            _crimeSeverityMap[CrimeType.PedestrianRobbery] = CrimeSeverity.Felony;
            _crimeSeverityMap[CrimeType.WeaponThreat] = CrimeSeverity.Felony;
            _crimeSeverityMap[CrimeType.PhysicalAssault] = CrimeSeverity.Misdemeanor;
            _crimeSeverityMap[CrimeType.PublicGunfire] = CrimeSeverity.Felony;
            _crimeSeverityMap[CrimeType.VehicleTheft] = CrimeSeverity.Felony;
            _crimeSeverityMap[CrimeType.VehicleCarjacking] = CrimeSeverity.ViolentFelony;
            _crimeSeverityMap[CrimeType.StoreRobbery] = CrimeSeverity.Felony;
            _crimeSeverityMap[CrimeType.BankRobbery] = CrimeSeverity.ViolentFelony;
            _crimeSeverityMap[CrimeType.GangActivity] = CrimeSeverity.Felony;
            _crimeSeverityMap[CrimeType.GangTerritory] = CrimeSeverity.Misdemeanor;
            _crimeSeverityMap[CrimeType.DangerousEvasion] = CrimeSeverity.Felony;
            _crimeSeverityMap[CrimeType.Resistance] = CrimeSeverity.Felony;
            _crimeSeverityMap[CrimeType.Murder] = CrimeSeverity.Capital;
            _crimeSeverityMap[CrimeType.MurderWitness] = CrimeSeverity.Capital;
        }

        public void Shutdown()
        {
            CloseAllCases();
            _activeCases.Clear();
            _closedCases.Clear();
            _currentHeat = 0f;
            _currentHeatState = HeatState.None;
            _isInitialized = false;
        }
    }
}
