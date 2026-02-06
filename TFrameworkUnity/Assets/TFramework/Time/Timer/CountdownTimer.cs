using R3;
using UnityEngine;

namespace TFramework.Time
{
    /// <summary>
    /// カウントダウンタイマー
    /// 指定時間から0に向かってカウントする
    /// </summary>
    public class CountdownTimer : TimerBase
    {
        private float _duration;

        public float Duration => _duration;
        public float RemainingTime => Mathf.Max(0f, _duration - _currentTime);
        public float Progress => Mathf.Clamp01(_currentTime / _duration);

        public CountdownTimer(float duration)
        {
            _duration = duration;
            _currentTime = 0f;
            _isRunning = true;
        }

        public override void Tick(float deltaTime)
        {
            if (_isDisposed || !_isRunning || _currentTime >= _duration) return;

            _currentTime += deltaTime;
            _onUpdateSubject.OnNext(RemainingTime);

            if (_currentTime >= _duration)
            {
                _currentTime = _duration;
                _isRunning = false;
                _onCompleteSubject.OnNext(Unit.Default);
            }
        }
        
        public void Reset(float newDuration = -1f)
        {
            if (newDuration > 0) _duration = newDuration;
            _currentTime = 0f;
            _isRunning = true;
        }
    }
}
