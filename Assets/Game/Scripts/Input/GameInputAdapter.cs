using UnityEngine;
using Erame.Core;

namespace Game.Scripts.Input{
    public sealed class GameInputAdapter : IInputSource{
        private readonly GameControls _controls;

        public GameInputAdapter(){
            _controls = new GameControls();
            _controls.Enable(); // 开启输入
        }

        public InputFrame GetInputFrame(){
            // 读取你配置的动作
            Vector2 move = _controls.Player.Move.ReadValue<Vector2>();
            bool attack = _controls.Player.Attack.WasPressedThisFrame();

            // 包装成框架的标准输入帧返回
            return new InputFrame(move.x, move.y, attack);
        }

        public void Dispose(){
            _controls.Disable(); // 记得在销毁时关闭
        }
    }
}