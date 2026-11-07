using Erame.Core;

namespace Erame.Platform.Steam{
    /// <summary>
    /// Steam 在线状态（<c>EPersonaState</c>）与框架 <see cref="PlatformPresenceState"/> 的映射。
    /// 两边枚举值都是 0…6，但 Steam 客户端可能给出越界值（未来新增状态），越界一律降级为 Offline。
    /// </summary>
    public static class SteamPresenceMap{
        private static readonly int MaxState = (int)PlatformPresenceState.LookingToPlay;

        /// <summary>Steam 状态整数值 → 框架状态；越界返回 Offline。</summary>
        public static PlatformPresenceState FromSteamState(int steamState){
            return steamState >= 0 && steamState <= MaxState
                ? (PlatformPresenceState)steamState
                : PlatformPresenceState.Offline;
        }

        /// <summary>框架状态 → Steam 状态整数值。</summary>
        public static int ToSteamState(PlatformPresenceState state){
            var value = (int)state;
            return value >= 0 && value <= MaxState ? value : 0;
        }
    }
}
