using Unity.Entities;
using Unity.Mathematics;

namespace SurvivorsLikeDOTS.Flight
{
    /// <summary>
    /// 우주선의 현재 월드 속도 (m/s).
    /// 보조 관성 비행에서는 속도가 프레임을 넘어 이어지는 상태라 LocalTransform 과 따로 들고 있어야 한다.
    /// </summary>
    public struct ShipVelocity : IComponentData
    {
        public float3 Linear;
    }
}
