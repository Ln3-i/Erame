using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Erame.Core;

namespace Erame.Runtime.Services{
    // 内部隐藏的协程驱动器
    internal class AudioCoroutineRunner : MonoBehaviour { }

    public class UnityAudioService : IAudioService, IService{
        private const string SfxPoolKey = "AudioSource";

        private readonly ILogService _log;
        private readonly IPoolService _poolService;
        private readonly IAssetService _assetService;
        private readonly ITimeService _timeService;

        private readonly Dictionary<AudioChannel, float> _volumes = new();

        private GameObject _sfxRoot;
        private AudioCoroutineRunner _coroutineRunner;

        // 双 Source 交叉淡入淡出：_activeBgmIndex 指向当前发声的 Source
        private AudioSource[] _bgmSources;
        private int _activeBgmIndex;
        private readonly float[] _bgmMultipliers = { 0f, 0f };

        private AssetHandle<AudioClip> _currentBgmHandle; // 当前 Source 上的资源
        private AssetHandle<AudioClip> _fadingOutHandle;  // 正在淡出的旧资源

        private Coroutine _bgmFadeCoroutine;
        private int _bgmRequestId;
        private bool _disposed;

        public UnityAudioService(ILogService log, IPoolService poolService, IAssetService assetService, ITimeService timeService){
            _log = log;
            _poolService = poolService;
            _assetService = assetService;
            _timeService = timeService;
        }

        public void Initialize(){
            _log.Info("[Audio] 音频服务初始化");
            _volumes[AudioChannel.Master] = 1f;
            _volumes[AudioChannel.Bgm] = 0.6f;
            _volumes[AudioChannel.Sfx] = 0.8f;
            _volumes[AudioChannel.UI] = 1f;

            var runnerGo = new GameObject("~AudioCoroutineRunner");
            Object.DontDestroyOnLoad(runnerGo);
            _coroutineRunner = runnerGo.AddComponent<AudioCoroutineRunner>();

            _bgmSources = new AudioSource[2];
            for (int i = 0; i < _bgmSources.Length; i++){
                var bgmGo = new GameObject($"~BgmSource_{i}");
                Object.DontDestroyOnLoad(bgmGo);
                var source = bgmGo.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.volume = 0f;
                _bgmSources[i] = source;
            }
            _activeBgmIndex = 0;
            _bgmMultipliers[0] = 1f;

            _sfxRoot = new GameObject("~SfxRoot");
            Object.DontDestroyOnLoad(_sfxRoot);
        }

        public void Dispose(){
            _disposed = true;
            _bgmRequestId++;

            StopBgm(0f);

            if (_fadingOutHandle != null){
                _fadingOutHandle.Dispose();
                _fadingOutHandle = null;
            }

            if (_bgmSources != null){
                foreach (var source in _bgmSources){
                    if (source != null) Object.Destroy(source.gameObject);
                }
                _bgmSources = null;
            }
            if (_sfxRoot != null) Object.Destroy(_sfxRoot.gameObject);
            if (_coroutineRunner != null) Object.Destroy(_coroutineRunner.gameObject);
        }

        public async Task PlayBgm(string clipKey, bool loop = true, float fadeTime = 0.5f){
            if (string.IsNullOrEmpty(clipKey) || _disposed) return;

            int requestId = ++_bgmRequestId;
            var handle = await _assetService.LoadAsync<AudioClip>(clipKey);

            // 期间又发起了新的播放请求，或者服务已关闭：丢弃本次结果并释放句柄
            if (requestId != _bgmRequestId || _disposed){
                handle?.Dispose();
                return;
            }
            if (handle == null || handle.Asset == null){
                handle?.Dispose();
                return;
            }

            var current = _bgmSources[_activeBgmIndex];
            if (current.clip == handle.Asset && current.isPlaying){
                handle.Dispose();
                return;
            }

            // 打断上一次淡入淡出，并释放它尚未处理完的旧资源
            if (_bgmFadeCoroutine != null && _coroutineRunner != null){
                _coroutineRunner.StopCoroutine(_bgmFadeCoroutine);
                _bgmFadeCoroutine = null;
            }
            if (_fadingOutHandle != null){
                _fadingOutHandle.Dispose();
                _fadingOutHandle = null;
            }

            int nextIndex = 1 - _activeBgmIndex;
            var next = _bgmSources[nextIndex];

            next.clip = handle.Asset;
            next.loop = loop;
            next.pitch = 1f;
            next.volume = 0f;
            _bgmMultipliers[nextIndex] = 0f;
            next.Play(); // ★ 起播职责明确放在这里，不再依赖淡入协程的边界条件

            _fadingOutHandle = _currentBgmHandle;
            _currentBgmHandle = handle;
            _activeBgmIndex = nextIndex;

            _bgmFadeCoroutine = _coroutineRunner.StartCoroutine(CrossFadeBgm(_activeBgmIndex, fadeTime));
        }

        public void StopBgm(float fadeTime = 0.5f){
            _bgmRequestId++;

            if (_bgmFadeCoroutine != null && _coroutineRunner != null){
                _coroutineRunner.StopCoroutine(_bgmFadeCoroutine);
                _bgmFadeCoroutine = null;
            }
            if (_fadingOutHandle != null){
                _fadingOutHandle.Dispose();
                _fadingOutHandle = null;
            }
            if (_coroutineRunner == null || _bgmSources == null) return;

            _bgmFadeCoroutine = _coroutineRunner.StartCoroutine(FadeOutAllBgm(fadeTime));
        }

        private IEnumerator CrossFadeBgm(int toIndex, float fadeTime){
            int fromIndex = 1 - toIndex;
            var from = _bgmSources[fromIndex];
            var to = _bgmSources[toIndex];

            float fromStart = _bgmMultipliers[fromIndex];
            float elapsed = 0f;

            if (fadeTime > 0f){
                while (elapsed < fadeTime){
                    elapsed += _timeService.UnscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeTime);

                    _bgmMultipliers[fromIndex] = Mathf.Lerp(fromStart, 0f, t);
                    _bgmMultipliers[toIndex] = t;
                    ApplyBgmVolume(fromIndex);
                    ApplyBgmVolume(toIndex);
                    yield return null;
                }
            }

            _bgmMultipliers[fromIndex] = 0f;
            _bgmMultipliers[toIndex] = 1f;
            ApplyBgmVolume(fromIndex);
            ApplyBgmVolume(toIndex);

            if (from != null){
                from.Stop();
                from.clip = null;
            }

            if (_fadingOutHandle != null){
                _fadingOutHandle.Dispose();
                _fadingOutHandle = null;
            }
            _bgmFadeCoroutine = null;
        }

        private IEnumerator FadeOutAllBgm(float fadeTime){
            float start0 = _bgmMultipliers[0];
            float start1 = _bgmMultipliers[1];
            float elapsed = 0f;

            if (fadeTime > 0f){
                while (elapsed < fadeTime){
                    elapsed += _timeService.UnscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeTime);
                    _bgmMultipliers[0] = Mathf.Lerp(start0, 0f, t);
                    _bgmMultipliers[1] = Mathf.Lerp(start1, 0f, t);
                    ApplyBgmVolume(0);
                    ApplyBgmVolume(1);
                    yield return null;
                }
            }

            _bgmMultipliers[0] = 0f;
            _bgmMultipliers[1] = 0f;

            foreach (var source in _bgmSources){
                if (source == null) continue;
                source.Stop();
                source.clip = null;
                source.volume = 0f;
            }

            if (_currentBgmHandle != null){
                _currentBgmHandle.Dispose();
                _currentBgmHandle = null;
            }
            if (_fadingOutHandle != null){
                _fadingOutHandle.Dispose();
                _fadingOutHandle = null;
            }

            _bgmFadeCoroutine = null;
        }

        private void ApplyBgmVolume(int index){
            if (_bgmSources == null || _bgmSources[index] == null) return;
            _bgmSources[index].volume = _bgmMultipliers[index] * GetFinalVolume(AudioChannel.Bgm);
        }

        public async Task PlaySfx(string clipKey, AudioChannel channel = AudioChannel.Sfx, float volume = 1f){
            if (string.IsNullOrEmpty(clipKey) || _disposed) return;

            var handle = await _assetService.LoadAsync<AudioClip>(clipKey);
            if (_disposed || handle == null || handle.Asset == null){
                handle?.Dispose();
                return;
            }

            var source = GetPooledAudioSource();
            source.spatialBlend = 0f;
            source.transform.position = Vector3.zero;
            PlayOnSource(source, handle, channel, volume);
        }

        public async Task PlaySfx3D(string clipKey, float x, float y, float z, AudioChannel channel = AudioChannel.Sfx, float volume = 1f){
            if (string.IsNullOrEmpty(clipKey) || _disposed) return;

            var handle = await _assetService.LoadAsync<AudioClip>(clipKey);
            if (_disposed || handle == null || handle.Asset == null){
                handle?.Dispose();
                return;
            }

            var source = GetPooledAudioSource();
            source.spatialBlend = 1f;
            source.transform.position = new Vector3(x, y, z);
            PlayOnSource(source, handle, channel, volume);
        }

        private void PlayOnSource(AudioSource source, AssetHandle<AudioClip> handle, AudioChannel channel, float volume){
            float pitch = Mathf.Abs(source.pitch);
            float playbackLength = handle.Asset.length / Mathf.Max(0.01f, pitch);

            source.clip = handle.Asset;
            source.loop = false;
            source.volume = GetFinalVolume(channel) * volume;
            source.Play();

            _coroutineRunner.StartCoroutine(RecycleSfx(source, handle, playbackLength));
        }

        public void SetVolume(AudioChannel channel, float volume){
            _volumes[channel] = Mathf.Clamp01(volume);

            if (_bgmSources != null && (channel == AudioChannel.Master || channel == AudioChannel.Bgm)){
                ApplyBgmVolume(0);
                ApplyBgmVolume(1);
            }
        }

        public float GetVolume(AudioChannel channel){
            return _volumes[channel];
        }

        private float GetFinalVolume(AudioChannel channel){
            if (channel == AudioChannel.Master) return _volumes[AudioChannel.Master];
            return _volumes[AudioChannel.Master] * _volumes[channel];
        }

        private AudioSource GetPooledAudioSource(){
            var go = _poolService.Spawn(SfxPoolKey, () => {
                var newGo = new GameObject("SfxPlayer");
                newGo.transform.SetParent(_sfxRoot.transform);
                var source = newGo.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                return newGo;
            });
            return go.GetComponent<AudioSource>();
        }

        private IEnumerator RecycleSfx(AudioSource source, AssetHandle<AudioClip> handle, float delay){
            yield return new WaitForSecondsRealtime(delay);

            if (source != null && source.gameObject != null){
                source.Stop();
                source.clip = null;
                source.loop = false;
                source.pitch = 1f;
                _poolService.Despawn(SfxPoolKey, source.gameObject);
            }

            handle?.Dispose();
        }
    }
}
