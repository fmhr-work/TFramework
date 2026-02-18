using System;
using UnityEngine;
using VContainer;
using TFramework.Debug;
using TFramework.SaveData;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace TFramework.Sample
{
    [Serializable]
    public class PlayerSaveData
    {
        public string PlayerName;
        public int Level;
        public int Gold;
        public string LastLogin;
    }

    public class SaveDataSample : MonoBehaviour
    {
        [Inject] private ISaveDataService _saveDataService;

        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _deleteButton;
        [SerializeField] private Text _outputText;

        private const string SaveKey = "SamplePlayerData";

        private void Start()
        {
            _saveButton.onClick.AddListener(() => SaveTest().Forget());
            _loadButton.onClick.AddListener(() => LoadTest().Forget());
            _deleteButton.onClick.AddListener(() => DeleteTest());
        }

        private async UniTaskVoid SaveTest()
        {
            var data = new PlayerSaveData
            {
                PlayerName = "TestPlayer",
                Level = UnityEngine.Random.Range(1, 100),
                Gold = UnityEngine.Random.Range(100, 10000),
                LastLogin = DateTime.Now.ToString()
            };

            await _saveDataService.SaveAsync(SaveKey, data);
            Log($"Saved: {data.PlayerName}, Lv.{data.Level}, {data.Gold}G");
        }

        private async UniTaskVoid LoadTest()
        {
            if (_saveDataService.Exists(SaveKey))
            {
                var data = await _saveDataService.LoadAsync<PlayerSaveData>(SaveKey);
                Log($"Loaded: {data.PlayerName}, Lv.{data.Level}, {data.Gold}G, Login:{data.LastLogin}");
            }
            else
            {
                Log("Save data not found.");
            }
        }

        private void DeleteTest()
        {
            _saveDataService.Delete(SaveKey);
            Log("Save data deleted.");
        }

        private void Log(string msg)
        {
            TLogger.Info($"[SaveDataSample] {msg}");
            if (_outputText != null) _outputText.text = msg;
        }
    }
}
