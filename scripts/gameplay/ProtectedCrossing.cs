using System;

/// <summary>Records a left-to-right traversal of an entire authored footprint while protection remains active.
/// Samples outside the footprint arm a new attempt; expiry, retreat, vertical bypass and teleports abandon it.</summary>
public sealed class ProtectedCrossing
{
    private readonly float _left, _right, _top, _bottom, _maximumStep;
    private bool _initialized, _traversing;
    private float _previousX, _previousY;

    /// <summary>Defines body-center entry/exit bounds, valid height and the largest legitimate per-tick motion.</summary>
    public ProtectedCrossing(float left, float right, float top, float bottom, float maximumStep)
    {
        _left = left; _right = right; _top = top; _bottom = bottom; _maximumStep = maximumStep;
    }

    /// <summary>Forgets an abandoned traversal without awarding action evidence.</summary>
    public void Reset() { _initialized = _traversing = false; }

    /// <summary>Returns true only on the sample that finishes a protected crossing; stationary ability use never counts.</summary>
    public bool Sample(float x, float y, bool protectedNow)
    {
        if (!_initialized) { _previousX = x; _previousY = y; _initialized = true; return false; }
        bool valid = protectedNow && y >= _top && y <= _bottom &&
            _previousY >= _top && _previousY <= _bottom && Math.Abs(x - _previousX) <= _maximumStep;
        if (!valid || x < _previousX) _traversing = false;
        if (valid && _previousX <= _left && x > _left) _traversing = true;
        bool completed = valid && _traversing && x >= _right;
        if (completed) _traversing = false;
        _previousX = x; _previousY = y;
        return completed;
    }
}
