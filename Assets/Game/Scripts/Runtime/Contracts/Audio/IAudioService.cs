using System.Threading.Tasks;

namespace Game.Runtime{
    public enum AudioChannel{
        Master, // 主音量
        Bgm,    // 背景音乐
        Sfx,    // 音效
        UI      // UI音效
    }

    public interface IAudioService{
        // 核心改变：不再传 AudioClip，而是传资源路径（Addressables 的 Key）
        Task PlayBgm(string clipKey, bool loop = true, float fadeTime = 0.5f);
        void StopBgm(float fadeTime = 0.5f);

        // 2D 音效
        Task PlaySfx(string clipKey, AudioChannel channel = AudioChannel.Sfx, float volume = 1f);

        // 3D 音效：坐标用纯 float 传，或者用一个自定义的纯 C# Vector3 结构体
        Task PlaySfx3D(string clipKey, float x, float y, float z, AudioChannel channel = AudioChannel.Sfx, float volume = 1f);

        void SetVolume(AudioChannel channel, float volume);
        float GetVolume(AudioChannel channel);
    }
}
