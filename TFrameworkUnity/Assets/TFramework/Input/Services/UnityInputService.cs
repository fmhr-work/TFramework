using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TFramework.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using TFramework.Debug;

namespace TFramework.Input
{
    /// <summary>
    /// UnityInputSystemサービスの実装
    /// </summary>
    public class UnityInputService : IInputService, IInitializable
    {
        private readonly InputModuleSettings _settings;
        private readonly CompositeDisposable _disposables = new();
        private readonly List<IDisposable> _lockHandles = new();
        private bool _isEnabled;
        private bool _isLocked;

        private InputActionMap _gameplayMap;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _submitAction;
        private InputAction _cancelAction;
        private InputAction _jumpAction;
        private InputAction _dashAction;
        private InputAction _attackAction;
        private InputAction _menuAction;
        private InputAction _pauseAction;
        private InputAction _interactAction;
        private InputAction _pointAction;
        private InputAction _holdAction;

        private readonly Subject<InputEvent> _inputSubject = new();

        public Observable<InputEvent> OnInputEvent => _inputSubject;
        public bool IsEnabled => _isEnabled && !_isLocked;

        public UnityInputService(InputModuleSettings settings)
        {
            _settings = settings ?? InputModuleSettings.Instance;
            InitializeActions();
        }

        private void InitializeActions()
        {
            var map = new InputActionMap("Gameplay");

            _moveAction = map.AddAction("Move", InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            _moveAction.AddBinding("<Gamepad>/leftStick");

            _lookAction = map.AddAction("Look", InputActionType.Value);
            _lookAction.AddBinding("<Mouse>/delta");

            _submitAction = map.AddAction("Submit", InputActionType.Button);
            _submitAction.AddBinding("<Keyboard>/enter");
            _submitAction.AddBinding("<Gamepad>/buttonSouth");

            _cancelAction = map.AddAction("Cancel", InputActionType.Button);
            _cancelAction.AddBinding("<Keyboard>/escape");
            _cancelAction.AddBinding("<Gamepad>/buttonEast");

            _jumpAction = map.AddAction("Jump", InputActionType.Button);
            _jumpAction.AddBinding("<Keyboard>/space");
            _jumpAction.AddBinding("<Gamepad>/buttonWest");

            _dashAction = map.AddAction("Dash", InputActionType.Button);
            _dashAction.AddBinding("<Keyboard>/shift");
            _dashAction.AddBinding("<Gamepad>/rightTrigger");

            _attackAction = map.AddAction("Attack", InputActionType.Button);
            _attackAction.AddBinding("<Mouse>/leftButton");
            _attackAction.AddBinding("<Keyboard>/space");
            _attackAction.AddBinding("<Gamepad>/rightShoulder");

            _menuAction = map.AddAction("Menu", InputActionType.Button);
            _menuAction.AddBinding("<Keyboard>/tab");
            _menuAction.AddBinding("<Gamepad>/buttonNorth");

            _pauseAction = map.AddAction("Pause", InputActionType.Button);
            _pauseAction.AddBinding("<Keyboard>/escape");
            _pauseAction.AddBinding("<Gamepad>/start");

            _interactAction = map.AddAction("Interact", InputActionType.Button);
            _interactAction.AddBinding("<Pointer>/press");
            _interactAction.AddBinding("<Keyboard>/e");

            _pointAction = map.AddAction("Point", InputActionType.Value);
            _pointAction.AddBinding("<Pointer>/position");

            _holdAction = map.AddAction("Hold", InputActionType.Button);
            _holdAction.AddBinding("<Pointer>/press", interactions: "hold(duration=0.5)");
            _holdAction.AddBinding("<Keyboard>/space", interactions: "hold(duration=0.5)");

            _gameplayMap = map;

            RegisterActionCallbacks(_moveAction, GameInputAction.Move);
            RegisterActionCallbacks(_lookAction, GameInputAction.Look);
            RegisterActionCallbacks(_submitAction, GameInputAction.Submit);
            RegisterActionCallbacks(_cancelAction, GameInputAction.Cancel);
            RegisterActionCallbacks(_jumpAction, GameInputAction.Jump);
            RegisterActionCallbacks(_dashAction, GameInputAction.Dash);
            RegisterActionCallbacks(_attackAction, GameInputAction.Attack);
            RegisterActionCallbacks(_menuAction, GameInputAction.Menu);
            RegisterActionCallbacks(_pauseAction, GameInputAction.Pause);
            RegisterActionCallbacks(_interactAction, GameInputAction.Interact);
            RegisterActionCallbacks(_pointAction, GameInputAction.Point);
            RegisterActionCallbacks(_holdAction, GameInputAction.Hold);

            _disposables.Add(_inputSubject);
        }

        private void RegisterActionCallbacks(InputAction action, GameInputAction gameAction)
        {
            action.started += _ => OnActionEvent(action, gameAction, InputPhase.Started);
            action.performed += _ => OnActionEvent(action, gameAction, InputPhase.Performed);
            action.canceled += _ => OnActionEvent(action, gameAction, InputPhase.Canceled);
        }

        private void OnActionEvent(InputAction action, GameInputAction gameAction, InputPhase phase)
        {
            if (IsEnabled)
            {
                Vector2 value = Vector2.zero;
                
                // ReadValueAsObjectを使用して安全に値を取得し、型に応じてVector2に変換する
                var rawValue = action.ReadValueAsObject();
                if (rawValue is Vector2 v2)
                {
                    value = v2;
                }
                else if (rawValue is float f)
                {
                    value = new Vector2(f, 0);
                }

                _inputSubject.OnNext(new InputEvent(gameAction, phase, value, Time.time));
            }
            else
            {
                TLogger.Debug($"{gameAction} {phase} ignored. Enabled: {_isEnabled}, Locked: {_isLocked}", "Input");
            }
        }

        public UniTask InitializeAsync(CancellationToken ct)
        {
            _gameplayMap.Enable();
            SetEnabled(_settings.EnableOnStart);
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// デバッグ用：現在の入力状態をログ出力する
        /// </summary>
        public void PrintStatus()
        {
            TLogger.Info($"[InputStatus] Enabled: {_isEnabled}, Locked: {_isLocked}, MapEnabled: {_gameplayMap.enabled}", "Input");
            foreach (var action in _gameplayMap.actions)
            {
                TLogger.Info($"[InputStatus] Action: {action.name}, Enabled: {action.enabled}, Value: {action.ReadValueAsObject()}", "Input");
            }
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }

        public IDisposable LockInput()
        {
            var handle = new LockHandle(this);
            _lockHandles.Add(handle);
            return handle;
        }

        private void ReleaseLock(IDisposable handle)
        {
            _lockHandles.Remove(handle);
        }

        public void Dispose()
        {
            _disposables.Dispose();

            _moveAction.started -= null;
            _lookAction.started -= null;
            _submitAction.started -= null;
            _cancelAction.started -= null;
            _jumpAction.started -= null;
            _dashAction.started -= null;
            _attackAction.started -= null;
            _menuAction.started -= null;
            _pauseAction.started -= null;

            _gameplayMap.Disable();
            _gameplayMap.Dispose();
            _lockHandles.Clear();
        }

        private class LockHandle : IDisposable
        {
            private readonly UnityInputService _service;
            private bool _disposed;

            public LockHandle(UnityInputService service)
            {
                _service = service;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _service.ReleaseLock(this);
            }
        }
    }
}