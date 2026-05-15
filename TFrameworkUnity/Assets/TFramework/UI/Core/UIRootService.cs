using System;
using Cysharp.Threading.Tasks;
using TFramework.Debug;
using TFramework.Resource;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TFramework.UI
{
    /// <summary>
    /// UIRoot管理サービス
    /// </summary>
    internal sealed class UIRootService
    {
        private enum UIRootOrigin
        {
            None,
            ExistingSceneObject,
            AddressableInstance,
            DefaultCreated
        }

        private const string DontDestroyOnLoadSceneName = "DontDestroyOnLoad";

        private readonly IResourceService _resourceService;
        private readonly UISettings _settings;

        private UIRoot _uiRoot;
        private UIRootOrigin _uiRootOrigin = UIRootOrigin.None;

        public UIRootService(IResourceService resourceService, UISettings settings)
        {
            _resourceService = resourceService;
            _settings = settings;
        }

        /// <summary>
        /// 使用中UIRoot
        /// </summary>
        public UIRoot Root => _uiRoot;

        /// <summary>
        /// UIRoot初期化
        /// </summary>
        public async UniTask InitializeAsync(System.Threading.CancellationToken ct)
        {
            await ResolveUIRootAsync(ct);
        }

        /// <summary>
        /// scene読込後のUIRoot正規化
        /// </summary>
        public void NormalizeAfterSceneLoad()
        {
            if (_uiRoot == null || _uiRoot.gameObject == null)
            {
                return;
            }

            CleanupDuplicateUIRoots(_uiRoot);
            EnsureSingleEventSystem(_uiRoot);
        }

        /// <summary>
        /// UIRoot破棄
        /// </summary>
        public void Dispose()
        {
            if (_uiRoot == null || _uiRoot.gameObject == null)
            {
                return;
            }

            switch (_uiRootOrigin)
            {
                case UIRootOrigin.AddressableInstance:
                    _resourceService.ReleaseInstance(_uiRoot.gameObject);
                    break;
                case UIRootOrigin.ExistingSceneObject:
                case UIRootOrigin.DefaultCreated:
                    Object.Destroy(_uiRoot.gameObject);
                    break;
            }

            _uiRoot = null;
            _uiRootOrigin = UIRootOrigin.None;
        }

        /// <summary>
        /// UIRoot解決
        /// </summary>
        private async UniTask ResolveUIRootAsync(System.Threading.CancellationToken ct)
        {
            _uiRoot = FindExistingUIRoot(out int existingCount);
            if (_uiRoot != null)
            {
                _uiRootOrigin = UIRootOrigin.ExistingSceneObject;
                if (existingCount > 1)
                {
                    TLogger.Warning($"[UIRootService] Multiple UIRoot objects were found. Using '{_uiRoot.gameObject.name}' from scene '{_uiRoot.gameObject.scene.name}'.");
                }

                FinalizeResolvedUIRoot(_uiRoot);
                return;
            }

            if (!string.IsNullOrEmpty(_settings.UIRootAddress))
            {
                try
                {
                    _uiRoot = await _resourceService.InstantiateAsync<UIRoot>(_settings.UIRootAddress, null, ct);
                    _uiRootOrigin = UIRootOrigin.AddressableInstance;
                    FinalizeResolvedUIRoot(_uiRoot);
                    return;
                }
                catch (Exception ex)
                {
                    TLogger.Warning($"[UIRootService] Failed to load UIRoot. {ex.Message}");
                }
            }

            CreateDefaultUIRoot();
        }

        /// <summary>
        /// UIRoot確定処理
        /// </summary>
        private void FinalizeResolvedUIRoot(UIRoot uiRoot)
        {
            if (uiRoot == null || uiRoot.gameObject == null)
            {
                return;
            }

            InitializeResolvedUIRoot(uiRoot);
            Object.DontDestroyOnLoad(uiRoot.gameObject);
            CleanupDuplicateUIRoots(uiRoot);
            EnsureSingleEventSystem(uiRoot);
        }

        /// <summary>
        /// UIRoot構成初期化
        /// </summary>
        private void InitializeResolvedUIRoot(UIRoot uiRoot)
        {
            if (uiRoot == null)
            {
                return;
            }

            if (!uiRoot.TryGetComponent(out Canvas canvas))
            {
                TLogger.Warning("[UIRootService] Canvas component not found on UIRoot");
            }

            if (!uiRoot.TryGetComponent(out CanvasScaler canvasScaler))
            {
                TLogger.Warning("[UIRootService] CanvasScaler component not found on UIRoot");
            }

            uiRoot.Initialize(canvas, canvasScaler);
        }

        /// <summary>
        /// 既定UIRoot生成
        /// </summary>
        private void CreateDefaultUIRoot()
        {
            GameObject go = new("UIRoot");
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            _uiRoot = go.AddComponent<UIRoot>();
            _uiRootOrigin = UIRootOrigin.DefaultCreated;
            FinalizeResolvedUIRoot(_uiRoot);
        }

        /// <summary>
        /// 既存UIRoot探索
        /// </summary>
        private UIRoot FindExistingUIRoot(out int count)
        {
            count = 0;
            UIRoot ddolRoot = null;
            UIRoot activeRoot = null;
            UIRoot fallbackRoot = null;

            UIRoot[] roots = Resources.FindObjectsOfTypeAll<UIRoot>();
            for (int i = 0; i < roots.Length; i++)
            {
                UIRoot root = roots[i];
                if (root == null)
                {
                    continue;
                }

                GameObject go = root.gameObject;
                if (go == null || !go.scene.IsValid())
                {
                    continue;
                }

                count++;

                if (string.Equals(go.scene.name, DontDestroyOnLoadSceneName, StringComparison.Ordinal))
                {
                    ddolRoot = root;
                    continue;
                }

                if (activeRoot == null && go.activeInHierarchy)
                {
                    activeRoot = root;
                }

                if (fallbackRoot == null)
                {
                    fallbackRoot = root;
                }
            }

            return ddolRoot ?? activeRoot ?? fallbackRoot;
        }

        /// <summary>
        /// 重複UIRoot除去
        /// </summary>
        private void CleanupDuplicateUIRoots(UIRoot keeper)
        {
            if (keeper == null || keeper.gameObject == null)
            {
                return;
            }

            UIRoot[] roots = Resources.FindObjectsOfTypeAll<UIRoot>();
            int destroyedCount = 0;

            for (int i = 0; i < roots.Length; i++)
            {
                UIRoot root = roots[i];
                if (root == null)
                {
                    continue;
                }

                GameObject go = root.gameObject;
                if (go == null || !go.scene.IsValid() || root == keeper)
                {
                    continue;
                }

                Object.Destroy(go);
                destroyedCount++;
            }

            if (destroyedCount > 0)
            {
                TLogger.Warning($"[UIRootService] Destroyed {destroyedCount} duplicate UIRoot object(s).");
            }
        }

        /// <summary>
        /// EventSystem正規化
        /// </summary>
        private void EnsureSingleEventSystem(UIRoot root)
        {
            if (root == null)
            {
                return;
            }

            EventSystem keepEventSystem = FindEventSystemUnderRoot(root.transform);
            if (keepEventSystem == null)
            {
                keepEventSystem = CreateEventSystem(root.transform);
                TLogger.Warning($"[UIRootService] EventSystem was missing on '{root.gameObject.name}'.");
            }
            else if (!keepEventSystem.gameObject.activeSelf)
            {
                keepEventSystem.gameObject.SetActive(true);
            }

            if (!HasInputSystemUIInputModule(keepEventSystem.gameObject))
            {
                CreateInputSystemUIInputModule(keepEventSystem.gameObject);
            }

            EventSystem[] allEventSystems = Resources.FindObjectsOfTypeAll<EventSystem>();
            int disabledCount = 0;
            for (int i = 0; i < allEventSystems.Length; i++)
            {
                EventSystem eventSystem = allEventSystems[i];
                if (eventSystem == null)
                {
                    continue;
                }

                GameObject go = eventSystem.gameObject;
                if (go == null || !go.scene.IsValid() || eventSystem == keepEventSystem)
                {
                    continue;
                }

                if (go.activeSelf)
                {
                    go.SetActive(false);
                    disabledCount++;
                }
            }

            if (disabledCount > 0)
            {
                TLogger.Warning($"[UIRootService] Disabled {disabledCount} duplicate EventSystem object(s).");
            }
        }

        /// <summary>
        /// root配下EventSystem探索
        /// </summary>
        private EventSystem FindEventSystemUnderRoot(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            EventSystem[] eventSystems = Resources.FindObjectsOfTypeAll<EventSystem>();
            for (int i = 0; i < eventSystems.Length; i++)
            {
                EventSystem eventSystem = eventSystems[i];
                if (eventSystem == null)
                {
                    continue;
                }

                GameObject go = eventSystem.gameObject;
                if (go == null || !go.scene.IsValid())
                {
                    continue;
                }

                if (go.transform.IsChildOf(root))
                {
                    return eventSystem;
                }
            }

            return null;
        }

        /// <summary>
        /// EventSystem生成
        /// </summary>
        private EventSystem CreateEventSystem(Transform parent)
        {
            GameObject go = new("EventSystem");
            go.transform.SetParent(parent, false);

            EventSystem eventSystem = go.AddComponent<EventSystem>();
            CreateInputSystemUIInputModule(go);
            Object.DontDestroyOnLoad(go);
            return eventSystem;
        }

        /// <summary>
        /// InputSystemUIInputModule有無判定
        /// </summary>
        private static bool HasInputSystemUIInputModule(GameObject go)
        {
            if (go == null)
            {
                return false;
            }

            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                {
                    continue;
                }

                if (component.GetType().FullName == "UnityEngine.InputSystem.UI.InputSystemUIInputModule")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// InputSystemUIInputModule生成
        /// </summary>
        private static Component CreateInputSystemUIInputModule(GameObject go)
        {
            if (go == null)
            {
                return null;
            }

            Type type = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (type == null)
            {
                TLogger.Warning("[UIRootService] Unity.InputSystem is not available");
                return null;
            }

            return go.AddComponent(type);
        }
    }
}
