using Erame.Core;

namespace Erame.Runtime.Services{
    // 这是 UI 输入上下文，用来拦截底层游戏输入
    public class UIInputContext : IInputContext{
        // 优先级极高，只要有 UI，它就是老大
        public int Priority => 999; 
        
        // 返回 true，代表“吃掉”输入，底层的 WASD 和攻击就无法生效了
        public bool IsBlocking => true; 

        public InputFrame GetInputFrame(){
            // UI 模式下，游戏角色的输入直接返回空
            return InputFrame.Empty;
        }
    }
}