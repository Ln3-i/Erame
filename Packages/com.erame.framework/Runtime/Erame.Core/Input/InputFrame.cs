using System;

namespace Erame.Core{
    // 这是一个纯数据，不可变。
    // 它可以被序列化，传给服务器，或者用来做本地回放。
    [Serializable]
    public readonly struct InputFrame{
        public readonly float MoveX;
        public readonly float MoveY;
        public readonly bool AttackPressed;

        public InputFrame(float moveX, float moveY, bool attackPressed){
            MoveX = moveX;
            MoveY = moveY;
            AttackPressed = attackPressed;
        }

        // 空输入（没操作时）
        public static InputFrame Empty => new InputFrame(0, 0, false);
    }
}