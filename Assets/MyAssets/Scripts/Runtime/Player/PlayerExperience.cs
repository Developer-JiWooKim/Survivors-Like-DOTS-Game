using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Player
{
    /// <summary>
    /// 플레이어 레벨과 경험치 (기획서 7.1).
    /// </summary>
    public struct PlayerExperience : IComponentData
    {
        public int Level;

        /// <summary>현재 레벨에서 모은 경험치. 레벨업 시 필요량만큼 빠지고 남은 양은 이월된다.</summary>
        public int Xp;

        /// <summary>다음 레벨까지 필요한 경험치.</summary>
        public int XpToNext;

        /// <summary>
        /// 기획서 7.1: <c>5 + level * 8</c>.
        /// "Lv.20 이후 기울기 완만" 은 구체 수치가 없어 아직 반영하지 않았다 — M1 에서 Lv.20 에 도달하지 않는다.
        /// </summary>
        public static int RequiredFor(int level)
        {
            return 5 + level * 8;
        }

        public static PlayerExperience Initial => new PlayerExperience
        {
            Level = 1,
            Xp = 0,
            XpToNext = RequiredFor(1),
        };
    }
}
