namespace Erame.Core.Utils{
    /// <summary>
    /// PCG32 确定性随机数生成
    /// </summary>
    public sealed class PCG32{
        private ulong _state;
        private readonly ulong _inc;

        public PCG32(int seed){
            _inc = 1442695040888963407UL | 1UL;
            _state = 0UL;
            NextUInt(); // 预热
            _state += (ulong)(uint)seed * 6364136223846793005UL + 1442695040888963407UL;
            NextUInt(); // 预热
        }

        public uint NextUInt(){
            ulong old = _state;
            // 第一步：线性同余推进 (LCG)
            _state = old * 6364136223846793005UL + _inc;
            
            // 第二步：XorShift 混淆（算法的灵魂）
            uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            
            // 第三步：循环移位 (Rotation)
            return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
        }
    }
}