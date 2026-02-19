using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace TFramework.Audio
{
    /// <summary>
    /// AudioSourceの再利用を管理
    /// </summary>
    public class AudioSourcePool
    {
        private readonly Stack<AudioSource> _pool = new Stack<AudioSource>();
        private readonly HashSet<AudioSource> _activeSources = new HashSet<AudioSource>();
        private readonly Transform _parent;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="initialCount">初期生成数</param>
        /// <param name="parent">親Transform</param>
        public AudioSourcePool(int initialCount, Transform parent)
        {
            _parent = parent;

            for (int i = 0; i < initialCount; i++)
            {
                var source = CreateNewSource();
                source.gameObject.SetActive(false);
                _pool.Push(source);
            }
        }

        public AudioSource Get()
        {
            AudioSource source = _pool.Count > 0 
                ? _pool.Pop() 
                : CreateNewSource();

            source.gameObject.SetActive(true);
            _activeSources.Add(source);
            return source;
        }

        public void Release(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            if (_activeSources.Contains(source))
            {
                source.Stop();
                source.clip = null;
                source.gameObject.SetActive(false);
                
                _activeSources.Remove(source);
                _pool.Push(source);
            }
        }

        /// <summary>
        /// 再生中の全ソースを停止して解放
        /// </summary>
        public void StopAllActive()
        {
            // コレクション変更エラーを防ぐためコピーして回す
            var sources = new List<AudioSource>(_activeSources);
            foreach (var source in sources)
            {
                Release(source);
            }
            _activeSources.Clear();
        }

        public void Clear()
        {
            StopAllActive(); // まず全停止

            while (_pool.Count > 0)
            {
                var source = _pool.Pop();
                if (source != null && source.gameObject != null)
                {
                    Object.Destroy(source.gameObject);
                }
            }
        }

        private AudioSource CreateNewSource()
        {
            var go = new GameObject("SE_Source");
            if (_parent != null)
            {
                go.transform.SetParent(_parent);
            }
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }
    }
}
