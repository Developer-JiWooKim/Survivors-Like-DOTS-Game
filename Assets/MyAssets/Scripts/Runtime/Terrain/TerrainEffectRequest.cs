using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    public enum TerrainEffectKind : byte
    {
        /// <summary>연결된 물 덩어리 전체를 감전시킨다 (기획서 4.3 — flood fill).</summary>
        Shock = 0,

        /// <summary>반경 안의 물을 빙판으로 바꾼다 (기획서 4.2 — 물 + 냉기).</summary>
        Freeze = 1,

        /// <summary>반경 안의 가연 타일에 불을 붙인다 (기획서 5.1 — 화염구가 점화원).</summary>
        Ignite = 2,
    }

    /// <summary>
    /// "지형에 이런 일을 해달라" 는 한 건의 요청. 무기가 지형을 직접 고치지 않고 이걸 큐에 넣는다.
    /// </summary>
    public struct TerrainEffectRequest
    {
        public int2 Cell;

        /// <summary>월드 유닛 반경. <see cref="TerrainEffectKind.Shock"/> 은 연결성으로 퍼지므로 쓰지 않는다.</summary>
        public float Radius;

        public TerrainEffectKind Kind;
    }

    /// <summary>
    /// 지형 효과 요청 큐 싱글턴. <see cref="TerrainEffectSystem"/> 이 소유하고 매 프레임 비운다.
    ///
    /// 왜 무기가 그리드를 직접 고치지 않고 큐를 거치나:
    /// 1. **투사체 충돌은 병렬 잡에서 일어난다.** 거기서 그리드를 직접 고치면 여러 스레드가 같은 칸에
    ///    쓰는 레이스가 된다. 감전은 flood fill 이라 한 요청이 수천 칸을 건드리므로 특히 위험하다.
    ///    (<c>DamageEventBus</c> 가 피해를 스트림에 모았다가 한 곳에서 적용하는 것과 같은 이유다.)
    /// 2. flood fill 은 **메인 스레드 BFS** 로 도는데(2026-09-18 결정), 잡 안에서는 부를 수 없다.
    ///
    /// <see cref="NativeQueue{T}"/> 를 쓰는 이유는 <c>ParallelWriter</c> 가 있어서다 —
    /// 지금은 디버그 키가 메인 스레드에서 넣지만, 화염구·전격 사슬이 들어오면 충돌 잡에서 병렬로 넣는다.
    /// </summary>
    public struct TerrainEffects : IComponentData
    {
        public NativeQueue<TerrainEffectRequest> Queue;

        public void Request(int2 cell, TerrainEffectKind kind, float radius = 0f)
        {
            Queue.Enqueue(new TerrainEffectRequest
            {
                Cell = cell,
                Kind = kind,
                Radius = radius,
            });
        }
    }
}
