using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// 도넛 모양 영역에서 균등하게 점을 뽑는다. 최초 스폰과 재스폰이 같은 분포를 쓰도록 한 곳에 둔다.
    /// </summary>
    public static class RingSampler
    {
        public static float2 Sample(ref Random random, float minRadius, float maxRadius)
        {
            float angle = random.NextFloat(0f, 2f * math.PI);

            // sqrt 를 거치는 이유: 반경을 균등 난수로 뽑으면 중심 쪽에 몰린다.
            // 넓이는 반경의 제곱에 비례하므로 sqrt 를 씌워야 링 전체에 고르게 퍼진다.
            float t = math.sqrt(random.NextFloat());
            float radius = math.lerp(minRadius, maxRadius, t);

            return new float2(math.cos(angle), math.sin(angle)) * radius;
        }
    }
}
