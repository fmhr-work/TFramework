using R3;

namespace TFramework.Time
{
    /// <summary>
    /// ストップウォッチタイマー
    /// 0から時間を計測し続ける
    /// </summary>
    public class StopwatchTimer : TimerBase
    {
        public StopwatchTimer()
        {
            _currentTime = 0f;
            _isRunning = true;
        }

        public override void Tick(float deltaTime)
        {
            if (_isDisposed || !_isRunning) return;

            _currentTime += deltaTime;
            _onUpdateSubject.OnNext(_currentTime);
        }

        public void Reset()
        {
            _currentTime = 0f;
            _isRunning = true;
        }
    }
}
