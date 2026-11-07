using Erame.Core;
using NUnit.Framework;

namespace Erame.Platform.Steam.Tests{
    public class SteamPresenceMapTests{
        [Test]
        public void FromSteamState_KnownValues_ReturnSameState(){
            Assert.AreEqual(PlatformPresenceState.Offline, SteamPresenceMap.FromSteamState(0));
            Assert.AreEqual(PlatformPresenceState.Online, SteamPresenceMap.FromSteamState(1));
            Assert.AreEqual(PlatformPresenceState.Busy, SteamPresenceMap.FromSteamState(2));
            Assert.AreEqual(PlatformPresenceState.Away, SteamPresenceMap.FromSteamState(3));
            Assert.AreEqual(PlatformPresenceState.Snooze, SteamPresenceMap.FromSteamState(4));
            Assert.AreEqual(PlatformPresenceState.LookingToTrade, SteamPresenceMap.FromSteamState(5));
            Assert.AreEqual(PlatformPresenceState.LookingToPlay, SteamPresenceMap.FromSteamState(6));
        }

        [TestCase(-1)]
        [TestCase(7)]
        [TestCase(int.MaxValue)]
        public void FromSteamState_OutOfRange_FallsBackToOffline(int steamState){
            Assert.AreEqual(PlatformPresenceState.Offline, SteamPresenceMap.FromSteamState(steamState));
        }

        [Test]
        public void ToSteamState_ValidStates_ReturnEnumValue(){
            Assert.AreEqual(0, SteamPresenceMap.ToSteamState(PlatformPresenceState.Offline));
            Assert.AreEqual(6, SteamPresenceMap.ToSteamState(PlatformPresenceState.LookingToPlay));
        }

        [Test]
        public void ToSteamState_OutOfRange_ReturnsZero(){
            Assert.AreEqual(0, SteamPresenceMap.ToSteamState((PlatformPresenceState)42));
        }

        [Test]
        public void RoundTrip_AllKnownStates_IsStable(){
            for (int i = 0; i <= 6; i++){
                var state = SteamPresenceMap.FromSteamState(i);
                Assert.AreEqual(i, SteamPresenceMap.ToSteamState(state), $"状态 {i} 往返不一致");
            }
        }
    }
}
