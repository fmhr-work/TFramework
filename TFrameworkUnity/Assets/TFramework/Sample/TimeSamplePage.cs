using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TFramework.UI;
using TFramework.Debug;
using TFramework.Time;
using R3;
using VContainer;

namespace TFramework.Sample
{
    public class TimeSamplePage : UIPageBase
    {
        #region Serialized Fields
        [Header("UI Components")]
        [SerializeField] private TFTextUGUI _titleText;
        [SerializeField] private TFTextUGUI _timeScaleText;
        [SerializeField] private TFTextUGUI _timerText;
        [SerializeField] private UIButton _backButton;
        
        [Header("Time Control")]
        [SerializeField] private UIButton _pauseButton;
        [SerializeField] private UIButton _resumeButton;
        [SerializeField] private UIButton _slowMotionButton;
        [SerializeField] private UIButton _normalSpeedButton;

        [Header("Timer Control")]
        [SerializeField] private UIButton _startTimerButton;
        [SerializeField] private UIButton _resetTimerButton;
        #endregion

        #region Dependencies
        private IUIService _uiService;
        private ITimeService _timeService;
        #endregion

        private CountdownTimer _countdownTimer;
        private IDisposable _pauseHandle;
        private IDisposable _slowMotionHandle;

        [Inject]
        public void Construct(IUIService uiService, ITimeService timeService)
        {
            _uiService = uiService;
            _timeService = timeService;
        }

        protected override UniTask OnInitializeAsync(CancellationToken ct)
        {
            // Initialize buttons
            _backButton?.Initialize();
            _pauseButton?.Initialize();
            _resumeButton?.Initialize();
            _slowMotionButton?.Initialize();
            _normalSpeedButton?.Initialize();
            _startTimerButton?.Initialize();
            _resetTimerButton?.Initialize();
            
            // Set static text
            _titleText?.SetTextContent("Time Sample");
            
            // Initialize timer (10 seconds)
            _countdownTimer = new CountdownTimer(10f);

            return UniTask.CompletedTask;
        }

        protected override void OnOpened()
        {
            // Back button
            _backButton?.OnClickAsObservable()
                .Subscribe(_ => OnBackClicked())
                .AddTo(PageToken);

            // Time Scale Actions
            _pauseButton?.OnClickAsObservable()
                .Subscribe(_ => 
                {
                    if (_pauseHandle == null)
                    {
                        _pauseHandle = _timeService.Pause("TimeSample");
                        TLogger.Info("Game Paused");
                    }
                })
                .AddTo(PageToken);

            _resumeButton?.OnClickAsObservable()
                .Subscribe(_ => 
                {
                    if (_pauseHandle != null)
                    {
                        _pauseHandle.Dispose();
                        _pauseHandle = null;
                        TLogger.Info("Game Resumed");
                    }
                })
                .AddTo(PageToken);

            _slowMotionButton?.OnClickAsObservable()
                .Subscribe(_ => 
                {
                     if (_slowMotionHandle == null)
                     {
                        _slowMotionHandle = _timeService.SetTimeScale(0.5f, "TimeSampleSlow");
                        TLogger.Info("Slow Motion Enabled");
                     }
                })
                .AddTo(PageToken);

            _normalSpeedButton?.OnClickAsObservable()
                .Subscribe(_ => 
                {
                    if (_slowMotionHandle != null)
                    {
                        _slowMotionHandle.Dispose();
                        _slowMotionHandle = null;
                        TLogger.Info("Normal Speed Restored");
                    }
                })
                .AddTo(PageToken);

            // Timer Actions
             _startTimerButton?.OnClickAsObservable()
                .Subscribe(_ => 
                {
                    if (!_countdownTimer.IsRunning)
                    {
                        if (_countdownTimer.RemainingTime <= 0)
                        {
                            _countdownTimer.Reset();
                            TLogger.Info("Timer Reset and Started");
                        }
                        else
                        {
                             _countdownTimer.Resume();
                             TLogger.Info("Timer Resumed");
                        }
                    }
                    else
                    {
                        _countdownTimer.Stop();
                        TLogger.Info("Timer Stopped");
                    }
                })
                .AddTo(PageToken);

             _resetTimerButton?.OnClickAsObservable()
                .Subscribe(_ => 
                {
                    _countdownTimer.Reset();
                    TLogger.Info("Timer Reset");
                })
                .AddTo(PageToken);

            // Timer Completion
            _countdownTimer.OnComplete
                .Subscribe(_ => 
                {
                    TLogger.Info("Timer Completed!");
                    _timerText?.SetTextContent("Timer: Finished!");
                })
                .AddTo(PageToken);

            // Update Loop
            Observable.EveryUpdate()
                .Subscribe(_ => 
                {
                    UpdateTimerDisplay();
                    UpdateTimeScaleDisplay();
                    
                    // Manually tick the timer
                    if (_countdownTimer != null)
                        _countdownTimer.Tick(UnityEngine.Time.deltaTime);
                })
                .AddTo(PageToken);
        }

        private void UpdateTimeScaleDisplay()
        {
            if (_timeScaleText != null)
                _timeScaleText.SetTextContent($"Time Scale: {_timeService.TimeScale:F2}");
        }

        private void UpdateTimerDisplay()
        {
            // Only update text if timer is running or just reset, to avoid overwriting "Finished!" message immediately in same frame if we wanted to sticky it, 
            // but actually standard display is better.
            if (_timerText != null && _countdownTimer != null)
            {
                if (_countdownTimer.IsRunning || _countdownTimer.RemainingTime > 0)
                {
                    _timerText.SetTextContent($"Timer: {_countdownTimer.RemainingTime:F2}s");
                }
            }
        }

        private async void OnBackClicked()
        {
            CleanupTimeEffects();
            await _uiService.GoBackAsync();
        }
        
        protected override void OnClosed()
        {
            CleanupTimeEffects();
        }

        private void CleanupTimeEffects()
        {
             _pauseHandle?.Dispose();
             _pauseHandle = null;
             
             _slowMotionHandle?.Dispose();
             _slowMotionHandle = null;
        }
    }
}
