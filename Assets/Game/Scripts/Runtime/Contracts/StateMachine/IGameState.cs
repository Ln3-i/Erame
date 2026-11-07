namespace Game.Runtime{
    // 纯 C# 接口，绝对不碰 UnityEngine
    public interface IGameState{
        void OnEnter();
        void OnUpdate();
        void OnExit();
    }
}