using System;
using System.Windows.Threading;

namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    // Authoritative progress percent (0..100). Real solver events drive changes; heartbeat fills gaps.
    public int ProgressPercent => _progressPercent;

    // Core progress fields
    private int _progressPercent;
    private bool _progressFinalized;
    // _hasProgressTick exists in Events partial; do not redeclare here.

    // Heartbeat timer fields
    private DispatcherTimer? _progressHeartbeatTimer; // fires synthetic updates during silence
    private DateTime _lastProgressUpdateUtc; // last real or synthetic tick time
    private bool _hadRealProgress; // marks that a real solver event occurred in last interval

    private void ResetProgress()
    {
        _progressFinalized = false;
        _hasProgressTick = false; // defined in Events partial
        _progressPercent = 0;
        ProgressValue = 0.0;
        ProgressLabel = "0%";
        ProgressVisibility = Visibility.Visible;
        ProgressLabelVisibility = Visibility.Visible;
        _lastProgressUpdateUtc = DateTime.UtcNow;
        _hadRealProgress = false;
        StartProgressHeartbeat();
        OnPropertyChanged(nameof(ProgressPercent));
    }

    private void SetProgressPercent(int rawPercent)
    {
        if (_solver.IsSolverCanceled || _progressFinalized) return;

        rawPercent = Math.Clamp(rawPercent, 0, 100);

        // Reserve100 for finalization only
        if (rawPercent >= 100)
            rawPercent = 99;

        // Monotonic guarantee
        if (rawPercent < _progressPercent)
            rawPercent = _progressPercent;

        if (rawPercent == _progressPercent)
        {
            // Real solver activity (even without change) pauses synthetic increments for next heartbeat
            _lastProgressUpdateUtc = DateTime.UtcNow;
            _hadRealProgress = true;
            return;
        }

        _progressPercent = rawPercent;
        ProgressValue = _progressPercent / 100.0;
        ProgressLabel = $"{_progressPercent}%";
        ProgressVisibility = Visibility.Visible;
        ProgressLabelVisibility = Visibility.Visible;

        if (_progressPercent is > 0 and < 100)
            _hasProgressTick = true;

        _lastProgressUpdateUtc = DateTime.UtcNow;
        _hadRealProgress = true;
        OnPropertyChanged(nameof(ProgressPercent));
    }

    private void ForceEarlyProgressIfNeeded()
    {
        if (_hasProgressTick || IsSingleRunning || _progressFinalized) return;
        if (_progressPercent < 1) SetProgressPercent(1);
    }

    private void FinalizeProgressIfApplicable()
    {
        if (_progressFinalized) return;
        _progressFinalized = true;
        _progressPercent = 100;
        ProgressValue = 1.0;
        ProgressLabel = "100%";
        StopProgressHeartbeat();
        OnPropertyChanged(nameof(ProgressPercent));
    }

    // Internal test hook
    internal void InjectProgressForTest(int percent, bool final = false)
    {
        if (final) FinalizeProgressIfApplicable(); else SetProgressPercent(percent);
    }

    // ---------------- Heartbeat Logic ----------------
    // Provides synthetic, minimal forward progress to reassure user during long enumeration phases
    // (e.g., Unique CountOnly on large boards) where the backend may emit sparse or no progress events.
    // Strategy:
    // * Timer interval defined by SimulationSettings.ProgressIntervalInMilliSec.
    // * If no real event since last interval, increment percent by1 (capped at95 to avoid misleading near-completion).
    // * If a real event occurred but did not change percent (solver plateau), pulse label only (no increment).
    // * Real events always take precedence and reset silence tracking.

    private void StartProgressHeartbeat()
    {
        if (_progressHeartbeatTimer != null) return;
        _progressHeartbeatTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(SimulationSettings.ProgressIntervalInMilliSec)
        };
        _progressHeartbeatTimer.Tick += ProgressHeartbeatTimer_Tick;
        _progressHeartbeatTimer.Start();
    }

    private void StopProgressHeartbeat()
    {
        if (_progressHeartbeatTimer == null) return;
        _progressHeartbeatTimer.Stop();
        _progressHeartbeatTimer.Tick -= ProgressHeartbeatTimer_Tick;
        _progressHeartbeatTimer = null;
    }

    private void ProgressHeartbeatTimer_Tick(object? sender, EventArgs e)
    {
        if (!IsSimulating || _progressFinalized || _solver.IsSolverCanceled)
        {
            StopProgressHeartbeat();
            return;
        }
        var silenceMs = (DateTime.UtcNow - _lastProgressUpdateUtc).TotalMilliseconds;
        if (silenceMs < SimulationSettings.ProgressIntervalInMilliSec) return;
        if (_hadRealProgress)
        {
            _hadRealProgress = false;
            ProgressLabel = $"{_progressPercent}%";
            _lastProgressUpdateUtc = DateTime.UtcNow;
            return;
        }
        if (_progressPercent < 95)
        {
            _progressPercent++;
            ProgressValue = _progressPercent / 100.0;
            ProgressLabel = $"{_progressPercent}%";
            ProgressVisibility = Visibility.Visible;
            ProgressLabelVisibility = Visibility.Visible;
            OnPropertyChanged(nameof(ProgressPercent));
        }
        else
        {
            ProgressLabel = $"{_progressPercent}% • working";
        }
        _lastProgressUpdateUtc = DateTime.UtcNow;
    }
}