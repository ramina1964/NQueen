namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    // Authoritative progress percent (0..100). 100 is only set at finalization.
    public int ProgressPercent => _progressPercent;

    private int _progressPercent;
    private bool _progressFinalized;

    private void ResetProgress()
    {
        _progressFinalized = false;
        _hasProgressTick = false;
        _progressPercent = 0;
        ProgressValue = 0.0;
        ProgressLabel = "0%";
        ProgressVisibility = Visibility.Visible;
        ProgressLabelVisibility = Visibility.Visible;
    }

    private void SetProgressPercent(int rawPercent)
    {
        if (_solver.IsSolverCanceled || _progressFinalized)
            return;

        rawPercent = Math.Clamp(rawPercent, 0, 100);

        // Reserve 100% for completion
        if (rawPercent >= 100)
            rawPercent = 99;

        // Monotonic
        if (rawPercent < _progressPercent)
            rawPercent = _progressPercent;

        if (rawPercent == _progressPercent)
            return;

        _progressPercent = rawPercent;

        ProgressValue = _progressPercent / 100.0;
        ProgressLabel = $"{_progressPercent}%";
        ProgressVisibility = Visibility.Visible;
        ProgressLabelVisibility = Visibility.Visible;

        if (_progressPercent is > 0 and < 100)
            _hasProgressTick = true;
    }

    private void ForceEarlyProgressIfNeeded()
    {
        if (_hasProgressTick || IsSingleRunning || _progressFinalized)
            return;

        if (_progressPercent < 1)
            SetProgressPercent(1);
    }

    private void FinalizeProgressIfApplicable()
    {
        if (_progressFinalized)
            return;

        _progressFinalized = true;
        _progressPercent = 100;
        ProgressValue = 1.0;
        ProgressLabel = "100%";
    }

    // Internal test hook (optional for unit tests)
    internal void InjectProgressForTest(int percent, bool final = false)
    {
        if (final)
            FinalizeProgressIfApplicable();
        else
            SetProgressPercent(percent);
    }
}