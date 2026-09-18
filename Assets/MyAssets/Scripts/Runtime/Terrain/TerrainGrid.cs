using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 월드 타일 그리드 싱글턴 (기획서 8.3). <see cref="TerrainGridSystem"/> 이 소유·생성·해제한다.
    ///
    /// 왜 엔티티가 아니라 통짜 NativeArray 인가:
    /// 타일 1 칸을 엔티티로 만들면 262,144 엔티티가 된다. M2 측정에서 적 10,000 이 메인 스레드 6.2 ms 였으니
    /// 26 배는 논외다. 타일은 **위치가 인덱스로 결정되는 고정 격자**라 엔티티가 주는 이점(개별 이동·수명)이 전혀 없다.
    /// 배열이면 확산 잡이 인덱스 산술만으로 이웃에 접근해 캐시 지역성도 좋다.
    ///
    /// 맵 경계 (2026-09-18 결정 — 기획서 11 장 미결 #1):
    /// **512×512 유한 맵 + 벽.** 토러스 래핑을 택하면 확산·flood fill·타일 조회 전부에 모듈로가 들어가고
    /// 월드 좌표와 타일 좌표가 어긋난다. 현재 병목이 CPU 메인 스레드라 그 비용을 더할 이유가 없다.
    /// 경계 밖 조회는 <see cref="TileAt"/> 가 <see cref="TileType.Rock"/> 을 돌려준다 —
    /// 확산 잡이 따로 경계 분기를 두지 않아도 불이 자연히 멈춘다.
    ///
    /// 잡 핸들 두 개를 드는 이유는 <c>EnemySpatialHash</c> 와 같다:
    /// 컨테이너가 컴포넌트 안에 있어 ECS 의 자동 의존성 추적이 닿지 않는다.
    /// </summary>
    public struct TerrainGrid : IComponentData
    {
        /// <summary>타일 한 변의 월드 크기. 기획서 4.1 — 1 u = 1 타일.</summary>
        public const float TileSize = 1f;

        /// <summary>행 우선(row-major) 타일 배열. 길이 = <see cref="Width"/> × <see cref="Height"/>.</summary>
        public NativeArray<TileData> Tiles;

        /// <summary>
        /// 확산 틱의 쓰기 대상 (기획서 8.3 의 더블 버퍼링). 틱이 끝나면 <see cref="Swap"/> 으로 <see cref="Tiles"/> 와 바꾼다.
        ///
        /// 왜 제자리에서 고치지 않나:
        /// 확산은 이웃을 읽어 자신을 정한다. 제자리로 하면 먼저 처리된 칸의 **이번 틱 결과**를
        /// 옆 칸이 읽어버려서, 불이 한 틱에 여러 칸을 건너뛰고 그 거리가 **잡의 실행 순서에 따라 달라진다**.
        /// 읽기와 쓰기를 갈라 두면 모든 칸이 같은 스냅샷을 보므로 병렬로 돌려도 결과가 하나로 정해진다.
        /// </summary>
        public NativeArray<TileData> Back;

        /// <summary>지금까지 지난 확산 틱 수. 난수 해시의 입력이라 결정성의 일부다 (<see cref="BurnRules.Random01"/>).</summary>
        public uint Tick;

        public int Width;
        public int Height;

        /// <summary>타일 (0,0) 의 **왼쪽 아래 모서리** 월드 좌표.</summary>
        public float2 Origin;

        /// <summary>그리드에 쓰는 잡(연소 틱 등). 읽는 쪽이 이 뒤에 돌도록 의존성으로 건다.</summary>
        public JobHandle WriteHandle;

        /// <summary>그리드를 읽는 잡(렌더 등). 다음 쓰기가 이 뒤에 일어나도록 한다.</summary>
        public JobHandle ReadersHandle;

        public void RegisterReader(JobHandle readerHandle)
        {
            ReadersHandle = JobHandle.CombineDependencies(ReadersHandle, readerHandle);
        }

        /// <summary>
        /// 앞뒤 버퍼를 바꾼다. 잡은 배열 구조체를 **값으로** 복사해 가지므로, 잡을 띄운 직후
        /// 메인 스레드에서 이 필드들을 바꿔도 돌고 있는 잡에는 영향이 없다.
        /// 다음 프레임의 읽기가 틱 결과를 보게 하는 건 <see cref="WriteHandle"/> 의 역할이다.
        /// </summary>
        public void Swap()
        {
            (Tiles, Back) = (Back, Tiles);
        }

        /// <summary>월드 좌표가 속한 타일 좌표. 경계 밖일 수도 있다 — 쓰기 전에 <see cref="InBounds"/> 로 확인할 것.</summary>
        public int2 CellOf(float2 worldPosition)
        {
            // (int) 로 자르면 0 방향으로 잘려 Origin 기준 -0.5 와 0.5 가 같은 칸이 된다. floor 를 거친다.
            return (int2)math.floor((worldPosition - Origin) / TileSize);
        }

        /// <summary>타일의 **중심** 월드 좌표. 렌더 쿼드를 여기에 놓으면 격자가 정확히 맞물린다.</summary>
        public float2 CenterOf(int2 cell)
        {
            return Origin + ((float2)cell + 0.5f) * TileSize;
        }

        public bool InBounds(int2 cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }

        public int IndexOf(int2 cell)
        {
            return cell.y * Width + cell.x;
        }

        /// <summary>경계 밖은 <see cref="TileType.Rock"/> (= 벽). 확산 잡이 경계 분기 없이 멈추게 하는 장치다.</summary>
        public TileData TileAt(int2 cell)
        {
            return InBounds(cell) ? Tiles[IndexOf(cell)] : TileData.Of(TileType.Rock);
        }

        /// <summary>플레이어·적이 나갈 수 없는 월드 영역. 경계 타일 안쪽으로 반 칸 여유를 둬 쿼드가 벽에 박히지 않게 한다.</summary>
        public float2 ClampToBounds(float2 worldPosition, float radius)
        {
            float2 min = Origin + radius;
            float2 max = Origin + new float2(Width, Height) * TileSize - radius;

            // 반경이 맵보다 크면 min > max 가 되어 clamp 가 깨진다. 실제로는 없는 상황이지만 방어해 둔다.
            return math.clamp(worldPosition, min, math.max(min, max));
        }
    }
}
