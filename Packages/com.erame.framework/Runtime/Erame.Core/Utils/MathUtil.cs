using System;

namespace Erame.Core.Utils{
    public static class MathUtil{
        // 基础运算
        // 1. 钳位
        public static float Clamp(float value, float min, float max){
            return value < min ? min : (value > max ? max : value);
        }

        // 2. 线性插值
        public static float Lerp(float a, float b, float t){
            t = Clamp(t, 0f, 1f);
            return a + (b - a) * t;
        }
    }
}