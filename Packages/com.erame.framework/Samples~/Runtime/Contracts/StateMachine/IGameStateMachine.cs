namespace __NS__.Runtime{
    // 纯 C# 接口，绝对不碰 UnityEngine 和 MonoBehaviour
    public interface IGameStateMachine{
        void ChangeState<T>() where T : class, IGameState;
        void Update();
    }
}