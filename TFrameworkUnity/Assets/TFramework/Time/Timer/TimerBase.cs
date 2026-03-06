using System;
using R3;
using UnityEngine;

namespace TFramework.Time
{
    public abstract class TimerBase : IDisposable
    {
        protected readonly Subject<Unit> _onCompleteSubject = new();
        protected readonly Subject<float> _onUpdateSubject = new();
        
        protected float _currentTime;
        protected bool _isRunning;
        protected bool _isDisposed;

        public Observable<Unit> OnComplete => _onCompleteSubject;
        public Observable<float> OnUpdate => _onUpdateSubject;
        public bool IsRunning => _isRunning;
        public float CurrentTime => _currentTime;

        public virtual void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _onCompleteSubject.Dispose();
            _onUpdateSubject.Dispose();
        }

        public void Stop()
        {
            _isRunning = false;
        }

        public void Resume()
        {
            if (!_isDisposed)
                _isRunning = true;
        }

        public abstract void Tick(float deltaTime);
    }
}
