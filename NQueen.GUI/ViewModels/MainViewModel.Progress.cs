namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel
{
    // Authoritative progress percent (0..100). 100 only after finalization.
    // Now raises PropertyChanged so XAML/tests can bind/assert.
    public int ProgressPercent => _progressPercent;

    private int _progressPercent;
    private bool _progressFinalized;

    private void ResetProgress()
    {
        _progressFinalized = false;
        _hasProgressTick = false;
        _progressPercent = 0;
        ProgressValue = 0.0;           // (legacy normalized value; can remove later)
        ProgressLabel = "0%";
        ProgressVisibility = Visibility.Visible;
        ProgressLabelVisibility = Visibility.Visible;
        OnPropertyChanged(nameof(ProgressPercent));
    }

    private void SetProgressPercent(int rawPercent)
    {
        if (_solver.IsSolverCanceled || _progressFinalized)
            return;

        rawPercent = Math.Clamp(rawPercent, 0, 100);

        // Reserve 100% for completion event
        if (rawPercent >= 100)
            rawPercent = 99;

        // Monotonic
        if (rawPercent < _progressPercent)
            rawPercent = _progressPercent;

        if (rawPercent == _progressPercent)
            return;

        _progressPercent = rawPercent;

        // Keep normalized ProgressValue for any legacy binding still present
        ProgressValue = _progressPercent / 100.0;
        ProgressLabel = $"{_progressPercent}%";
        ProgressVisibility = Visibility.Visible;
        ProgressLabelVisibility = Visibility.Visible;

        if (_progressPercent is > 0 and < 100)
            _hasProgressTick = true;

        OnPropertyChanged(nameof(ProgressPercent));
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
        OnPropertyChanged(nameof(ProgressPercent));
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