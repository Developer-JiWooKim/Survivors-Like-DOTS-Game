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

        /// <summary>
        /// 해시에 들어가는 적 반경의 상한. 조회 쪽이 "몇 칸까지 봐야 하나" 를 정하는 데 쓴다.
        /// 적은 **중심 좌표의 셀**에만 들어가므로, 반경이 큰 적은 옆 칸에서도 닿을 수 있기 때문이다.
        /// 현재 적 반경 0.25 의 2 배로 여유를 뒀다. 큰 적(엘리트·보스)이 들어오는 M4 에서 Blob 스탯의 최대치로 바꾼다.
        /// </summary>
        public const float MaxAgentRadius = 0.5f;

        /// <summary>반경 <paramref name="queryRadius"/> 인 원과 겹칠 수 있는 적을 모두 찾으려면 중심 셀에서 몇 칸까지 봐야 하는지.</summary>
        public static int CellRangeFor(float queryRadius)
        {
            return (int)math.ceil((queryRadius + MaxAgentRadius) / CellSize);
        }

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
            // 서로 다른 셀이 같은 키로 충돌할 수 있다. 한 칸만 볼 때는 거리 검사로 걸러지지만,
            // **여러 칸을 도는 조회에서는 같은 적이 두 번 나온다** → 조회 쪽이 IsInCell 로 걸러야 한다.
            return (int)math.hash(cell);
        }

        /// <summary>
        /// 조회 중인 셀에서 나온 적이 실제로 그 셀에 속하는지. 키 충돌로 섞여 든 다른 셀의 적을 거른다.
        /// 여러 칸을 도는 조회에서 이걸 빼먹으면 같은 적을 중복으로 센다 (폭발 피해 2 번 등).
        /// </summary>
        public static bool IsInCell(in AgentRef agent, int2 cell)
        {
            return CellOf(agent.Position).Equals(cell);
        }
    }
}
