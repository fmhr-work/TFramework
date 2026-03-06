using TFramework.Scene;

namespace TFramework.Sample
{
    /// <summary>
    /// NextSampleSceneへ渡すデータ
    /// </summary>
    public class NextSampleSceneBridgeData : ISceneBridgeData
    {
        public string Message { get; }
        public int SourceId { get; }

        public NextSampleSceneBridgeData(string message, int sourceId)
        {
            Message = message;
            SourceId = sourceId;
        }
    }
}
