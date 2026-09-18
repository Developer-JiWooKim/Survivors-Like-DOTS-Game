using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 타일을 화면에 칠할 색. 기획서 9 장은 "도형 프로토타입" 이라고만 정했고 색은 지정하지 않았다 —
    /// 아래는 M3 임시값이며 M6 아트 교체에서 스프라이트로 바뀐다.
    ///
    /// 고른 기준:
    /// 1. 지형은 전부 **어둡고 채도가 낮게**. 적(밝은 빨강)·젬(밝은 녹색)·투사체(흰색)가 항상 위로 떠 보여야 한다.
    ///    배경이 튀면 1 만 마리가 깔렸을 때 뭐가 적인지 분간이 안 된다.
    /// 2. 상태(연소·감전·동결)는 **밝기로** 구분한다. 지형 위에서 유일하게 밝은 것이 상태 변화라
    ///    플레이어가 "지금 무슨 일이 벌어지는지" 를 색 학습 없이 알아본다.
    ///
    /// 주의: 연소색(주황)이 자석 아이템과 겹친다. 자석은 움직이고 위에 그려져 실사용에선 헷갈리지 않았지만,
    /// 불바다가 흔해지는 M4 에서 다시 볼 것.
    /// </summary>
    public static class TileColors
    {
        public static float4 Of(in TileData tile)
        {
            float4 baseColor = BaseColorOf(tile.TypeValue);

            TileStateFlags state = tile.StateValue;

            // 여러 상태가 겹칠 때의 우선순위: 연소 > 감전 > 동결 > 오염.
            // 플레이어에게 가장 급한 정보(불은 나를 태운다)를 위에 둔다.
            if ((state & TileStateFlags.Burning) != 0)
            {
                // 연료가 남았을수록 밝다. 꺼져 가는 불과 한창인 불이 눈에 구분된다.
                float intensity = tile.Fuel / 255f;
                return math.lerp(new float4(0.55f, 0.18f, 0.05f, 1f), new float4(1f, 0.62f, 0.15f, 1f), intensity);
            }

            if ((state & TileStateFlags.Shocked) != 0)
            {
                return new float4(0.55f, 0.85f, 1f, 1f);
            }

            if ((state & TileStateFlags.Frozen) != 0)
            {
                return new float4(0.62f, 0.82f, 0.92f, 1f);
            }

            if ((state & TileStateFlags.Poisoned) != 0)
            {
                return new float4(0.42f, 0.18f, 0.50f, 1f);
            }

            return baseColor;
        }

        private static float4 BaseColorOf(TileType type)
        {
            switch (type)
            {
                case TileType.Grass:
                    return new float4(0.16f, 0.30f, 0.14f, 1f);
                case TileType.Water:
                    return new float4(0.10f, 0.22f, 0.42f, 1f);
                case TileType.Oil:
                    return new float4(0.08f, 0.07f, 0.10f, 1f);
                case TileType.OilOnWater:
                    // 기름(거의 검정)에 물의 파랑을 섞었다. 다 타면 물이 돌아온다는 걸
                    // 플레이어가 불을 붙이기 **전에** 알아볼 수 있어야 한다.
                    return new float4(0.09f, 0.11f, 0.20f, 1f);
                case TileType.Ice:
                    return new float4(0.45f, 0.62f, 0.72f, 1f);
                case TileType.Crack:
                    return new float4(0.16f, 0.13f, 0.11f, 1f);
                case TileType.Rock:
                    return new float4(0.32f, 0.32f, 0.34f, 1f);
                default:
                    return new float4(0.26f, 0.21f, 0.15f, 1f);
            }
        }
    }
}
