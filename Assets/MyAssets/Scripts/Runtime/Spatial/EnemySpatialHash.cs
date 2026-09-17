using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Spatial
{
    /// <summary>
    /// 살아있는 적의 공간 해시 싱글턴 (기획서 8.3). <see cref="BuildEnemySpatialHashSystem"/> 이 소유하고 매 프레임 재구축한다.
    ///
    /// 1차 구현은 NativeParallelMultiHashMap — 병렬 쓰기가 쉽다. 캐시 지역성이 나빠
    /// 최적화 2단계에서 카운팅 소트 그리드로 교체할 예정이며, 교체 전후 측정이 기록 대상이다.
    ///
    /// 잡 핸들 두 개를 함께 드는 이유:
    /// 컨테이너가 컴포넌트 안에 있어 ECS 의 자동 의존성 추적이 닿지 않는다.
    /// - <see cref="BuildHandle"/>: 조회하는 쪽이 재구축 완료 뒤에 돌도록 의존성으로 건다.
    /// - <see cref="ReadersHandle"/>: 다음 프레임 재구축(Clear)이 지난 프레임 조회 잡이 끝난 뒤에 일어나도록 한다.
    /// (DamageEventBus 와 같은 패턴)
    /// </summary>
    public struct EnemySpatialHash : IComponentData
    {
        /// <summary>셀 한 변 (월드 유닛). 기획서 8.3 — 적 반경 0.25 의 4 배.</summary>
        public const float CellSize = 1f;

        public NativeParallelMultiHashMap<int, AgentRef> Map;
        public JobHandle BuildHandle;
        public JobHandle ReadersHandle;

        public void RegisterReader(JobHandle readerHandle)
        {
            ReadersHandle = JobHandle.CombineDependencies(ReadersHandle, readerHandle);
        }

        public static int2 CellOf(float2 position)
        {
            // floor 를 거치지 않고 (int) 로 자르면 0 방향으로 잘려서 -0.5 와 0.5 가 같은 셀(0)이 된다.
            return (int2)math.floor(position / CellSize);
        }

        public static int KeyOf(int2 cell)
        {
            // 서로 다른 셀이 같은 키로 충돌할 수 있지만, 조회 쪽이 거리로 다시 거르므로 정확성에는 영향이 없다.
            return (int)math.hash(cell);
        }
    }
}
