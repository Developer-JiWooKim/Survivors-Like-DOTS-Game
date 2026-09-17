using Unity.Entities;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Spatial
{
    /// <summary>
    /// 공간 해시 한 칸에 들어가는 적 정보 (기획서 8.3).
    ///
    /// 위치와 반경을 **복사해서** 담는 이유:
    /// 조회하는 잡이 ComponentLookup 으로 다른 적의 LocalTransform 을 읽으면, 같은 잡이 자기 LocalTransform 에
    /// 쓰는 것과 충돌해 병렬화가 막힌다. 스냅샷을 해시에 담으면 "이웃은 해시에서 읽고, 자기 것만 쓴다" 가 된다.
    /// 기획서는 Radius 를 half 로 적었지만, 1차 구현은 단순하게 float 으로 간다 (24 byte → 정렬 포함 동일).
    /// </summary>
    public struct AgentRef
    {
        public Entity Entity;
        public float2 Position;
        public float Radius;
    }
}
