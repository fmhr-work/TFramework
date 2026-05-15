using UnityEngine.SceneManagement;

namespace TFramework.UI
{
    /// <summary>
    /// scene切り替え時UIクリーンアップサービス
    /// </summary>
    internal sealed class UISceneCleanupService
    {
        private readonly UIPageService _pageService;
        private readonly UIDialogService _dialogService;

        public UISceneCleanupService(UIPageService pageService, UIDialogService dialogService)
        {
            _pageService = pageService;
            _dialogService = dialogService;
        }

        /// <summary>
        /// scene読込時の旧sceneUIクリーンアップ
        /// </summary>
        public void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single)
            {
                return;
            }

            _pageService.CleanupSceneScopedPages(scene);
            _dialogService.CleanupSceneScopedDialogs(scene);
        }
    }
}
