using System;
using System.Collections.Generic;

namespace RLF.Core.Safety
{
    /// <summary>
    /// Monitor de atividade de scripts.
    /// Puro C# - sem dependências do GTA.
    /// </summary>
    public sealed class ScriptActivityMonitor
    {
        #region Singleton

        private static ScriptActivityMonitor _instance;
        private static readonly object _lock = new object();

        public static ScriptActivityMonitor Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ScriptActivityMonitor();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        private readonly Dictionary<string, ScriptRecord> _scripts = new Dictionary<string, ScriptRecord>(32);
        private readonly Dictionary<string, ExecContext> _activeExec = new Dictionary<string, ExecContext>(16);
        private readonly List<string> _pendingDisables = new List<string>(8);

        private int _frameCounter;
        private int _lastMonitorFrame;
        private int _globalErrorCount;

        // Thresholds
        private const int LONG_EXEC_FRAMES = 2;
        private const int MAX_CONSECUTIVE_LONG = 8;
        private const int MAX_CONSECUTIVE_ERRORS = 5;
        private const int BASE_COOLDOWN_FRAMES = 150;
        private const int MAX_COOLDOWN_FRAMES = 1800;
        private const int MONITOR_INTERVAL = 15;
        private const int LOOP_THRESHOLD = 60;

        #endregion

        #region Constructor

        private ScriptActivityMonitor() { }

        #endregion

        #region Properties

        public int RegisteredCount => _scripts.Count;
        public int GlobalErrorCount => _globalErrorCount;

        #endregion

        #region Registration

        public void RegisterScript(string scriptId, string scriptName = null)
        {
            if (string.IsNullOrEmpty(scriptId)) return;

            lock (_lock)
            {
                if (_scripts.ContainsKey(scriptId)) return;

                _scripts[scriptId] = new ScriptRecord
                {
                    ScriptId = scriptId,
                    ScriptName = scriptName ?? scriptId,
                    Status = ScriptHealthStatus.Healthy
                };
            }
        }

        public void UnregisterScript(string scriptId)
        {
            lock (_lock)
            {
                _scripts.Remove(scriptId);
                _activeExec.Remove(scriptId);
            }
        }

        #endregion

        #region Execution Tracking

        public ExecContext BeginExecution(string scriptId, string opName = null)
        {
            if (!_scripts.TryGetValue(scriptId, out var record))
            {
                RegisterScript(scriptId);
                record = _scripts[scriptId];
            }

            if (record.IsDisabled)
            {
                if (_frameCounter >= record.DisabledUntilFrame)
                {
                    record.IsDisabled = false;
                    record.ConsecutiveErrors = 0;
                    SafetyLogger.Instance?.Log($"[ScriptActivityMonitor] Recovering: {scriptId}");
                }
                else
                {
                    return null;
                }
            }

            var ctx = new ExecContext
            {
                ScriptId = scriptId,
                StartFrame = _frameCounter,
                Valid = true
            };

            _activeExec[scriptId] = ctx;
            return ctx;
        }

        public void EndExecution(ExecContext ctx)
        {
            if (ctx == null || !ctx.Valid) return;
            ctx.Valid = false;

            if (!_scripts.TryGetValue(ctx.ScriptId, out var record))
                return;

            int execFrames = _frameCounter - ctx.StartFrame;

            record.ExecutionCount++;
            record.LastActivityFrame = _frameCounter;
            record.ConsecutiveErrors = 0;

            if (execFrames > LONG_EXEC_FRAMES)
            {
                record.ConsecutiveLongExecutions++;

                if (record.ConsecutiveLongExecutions >= MAX_CONSECUTIVE_LONG)
                {
                    DisableScript(record, "Consecutive long executions");
                }
            }
            else
            {
                if (record.ConsecutiveLongExecutions > 0)
                    record.ConsecutiveLongExecutions--;
            }

            // Loop detection
            int resultHash = execFrames;
            if (resultHash == record.LastResultHash)
            {
                record.SameResultCount++;
                if (record.SameResultCount >= LOOP_THRESHOLD)
                {
                    record.CurrentRisk = RiskType.RepetitiveLoop;
                }
            }
            else
            {
                record.SameResultCount = 0;
                record.LastResultHash = resultHash;
            }

            _activeExec.Remove(ctx.ScriptId);
            UpdateStatus(record);
        }

        public void RecordException(string scriptId, Exception ex, string context = null)
        {
            if (!_scripts.TryGetValue(scriptId, out var record))
            {
                RegisterScript(scriptId);
                record = _scripts[scriptId];
            }

            record.ErrorCount++;
            record.ConsecutiveErrors++;
            record.LastErrorFrame = _frameCounter;
            record.LastErrorMessage = ex?.Message ?? "Unknown";
            record.LastActivityFrame = _frameCounter;
            record.CurrentRisk = RiskType.SilentException;

            _globalErrorCount++;

            SafetyLogger.Instance?.LogError($"[ScriptActivityMonitor] Error in {record.ScriptName}: {ex?.Message}");

            if (record.ConsecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
            {
                DisableScript(record, "Consecutive errors");
            }

            _activeExec.Remove(scriptId);
            UpdateStatus(record);
        }

        public void RecordSilentException(string scriptId, string message)
        {
            if (!_scripts.TryGetValue(scriptId, out var record))
                return;

            record.ErrorCount++;
            record.CurrentRisk = RiskType.SilentException;
        }

        #endregion

        #region Monitor

        public void MonitorTick()
        {
            _frameCounter++;

            if (_frameCounter - _lastMonitorFrame < MONITOR_INTERVAL)
                return;
            _lastMonitorFrame = _frameCounter;

            // Process disables
            if (_pendingDisables.Count > 0)
            {
                lock (_lock)
                {
                    foreach (var id in _pendingDisables)
                    {
                        AdaptiveTickController.Instance?.PauseSystem(id, "Disabled by monitor");
                    }
                    _pendingDisables.Clear();
                }
            }

            // Check stuck executions
            var stuckList = new List<string>();
            foreach (var kvp in _activeExec)
            {
                if (!kvp.Value.Valid) continue;

                int stuckFrames = _frameCounter - kvp.Value.StartFrame;
                if (stuckFrames > 60)
                {
                    stuckList.Add(kvp.Key);
                }
            }

            foreach (var scriptId in stuckList)
            {
                if (_activeExec.TryGetValue(scriptId, out var exec))
                {
                    exec.Valid = false;
                }
                if (_scripts.TryGetValue(scriptId, out var record))
                {
                    SafetyLogger.Instance?.LogError($"[ScriptActivityMonitor] Stuck execution: {record.ScriptName}");
                    DisableScript(record, "Stuck execution");
                }
            }
        }

        #endregion

        #region Private Methods

        private void DisableScript(ScriptRecord record, string reason)
        {
            if (record.IsDisabled) return;

            record.IsDisabled = true;
            record.DisableCount++;
            record.Status = ScriptHealthStatus.Disabled;

            int cooldown = BASE_COOLDOWN_FRAMES * (1 << record.DisableCount);
            if (cooldown > MAX_COOLDOWN_FRAMES) cooldown = MAX_COOLDOWN_FRAMES;
            record.DisabledUntilFrame = _frameCounter + cooldown;

            _pendingDisables.Add(record.ScriptId);

            SafetyLogger.Instance?.LogError($"[ScriptActivityMonitor] DISABLED: {record.ScriptName} - {reason} (cooldown: {cooldown} frames)");
        }

        private void UpdateStatus(ScriptRecord record)
        {
            if (record.IsDisabled)
            {
                record.Status = ScriptHealthStatus.Disabled;
            }
            else if (record.ConsecutiveErrors >= 3 || record.CurrentRisk != RiskType.None)
            {
                record.Status = ScriptHealthStatus.Critical;
            }
            else if (record.ConsecutiveLongExecutions >= 3)
            {
                record.Status = ScriptHealthStatus.Warning;
            }
            else
            {
                record.Status = ScriptHealthStatus.Healthy;
                record.CurrentRisk = RiskType.None;
            }
        }

        #endregion

        #region Query

        public bool CanScriptExecute(string scriptId)
        {
            if (!_scripts.TryGetValue(scriptId, out var record))
                return true;

            if (record.IsDisabled)
            {
                if (_frameCounter >= record.DisabledUntilFrame)
                {
                    record.IsDisabled = false;
                    record.Status = ScriptHealthStatus.Healthy;
                    AdaptiveTickController.Instance?.ResumeSystem(scriptId);
                    return true;
                }
                return false;
            }

            return true;
        }

        public ScriptRecord GetScriptStatus(string scriptId)
        {
            _scripts.TryGetValue(scriptId, out var record);
            return record;
        }

        public IEnumerable<ScriptRecord> GetCriticalScripts()
        {
            foreach (var r in _scripts.Values)
            {
                if (r.Status == ScriptHealthStatus.Critical || r.IsDisabled)
                    yield return r;
            }
        }

        public Dictionary<string, object> GetHealthReport()
        {
            int healthy = 0, warning = 0, critical = 0, disabled = 0;

            foreach (var r in _scripts.Values)
            {
                switch (r.Status)
                {
                    case ScriptHealthStatus.Healthy: healthy++; break;
                    case ScriptHealthStatus.Warning: warning++; break;
                    case ScriptHealthStatus.Critical: critical++; break;
                    case ScriptHealthStatus.Disabled: disabled++; break;
                }
            }

            return new Dictionary<string, object>
            {
                ["Total"] = _scripts.Count,
                ["Healthy"] = healthy,
                ["Warning"] = warning,
                ["Critical"] = critical,
                ["Disabled"] = disabled,
                ["GlobalErrors"] = _globalErrorCount
            };
        }

        public bool ForceRecovery(string scriptId)
        {
            if (!_scripts.TryGetValue(scriptId, out var record))
                return false;

            record.IsDisabled = false;
            record.ConsecutiveErrors = 0;
            record.ConsecutiveLongExecutions = 0;
            record.Status = ScriptHealthStatus.Healthy;
            record.CurrentRisk = RiskType.None;

            AdaptiveTickController.Instance?.ResumeSystem(scriptId);
            return true;
        }

        public void ResetStats(string scriptId)
        {
            if (_scripts.TryGetValue(scriptId, out var record))
            {
                record.ErrorCount = 0;
                record.ConsecutiveErrors = 0;
                record.ConsecutiveLongExecutions = 0;
                record.DisableCount = 0;
                record.Status = ScriptHealthStatus.Healthy;
                record.CurrentRisk = RiskType.None;
            }
        }

        #endregion
    }
}