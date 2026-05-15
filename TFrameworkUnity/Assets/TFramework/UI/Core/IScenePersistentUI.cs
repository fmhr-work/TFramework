namespace TFramework.UI
{
    /// <summary>
    /// Scene横断UI保持判定
    /// </summary>
    internal interface IScenePersistentUI
    {
        bool PersistAcrossScenes { get; }
    }
}
