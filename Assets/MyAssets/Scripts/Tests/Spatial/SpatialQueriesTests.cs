using System.Collections.Generic;
using Assets.MyAssets.Scripts.Runtime.Spatial;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Assets.MyAssets.Scripts.Tests.Spatial
{
    /// <summary>
    /// 공간 해시 조회 정확성 테스트 (CLAUDE.md 5장 — "틀리면 디버깅이 지옥인" 로직).
    ///
    /// 전략: 무작위 배치에서 해시 조회 결과를 **전수 비교(정답)** 와 대조한다.
    /// 공간 해시의 버그는 "가끔 하나 빠짐 / 가끔 두 번 셈" 형태라 눈으로 플레이해서는 거의 안 보인다.
    /// </summary>
    public sealed class SpatialQueriesTests
    {
        private const float WorldHalfExtent = 40f;
        private const float EnemyRadius = 0.25f;

        private NativeParallelMultiHashMap<int, AgentRef> _map;
        private List<AgentRef> _agents;

        [SetUp]
        public void SetUp()
        {
            _map = new NativeParallelMultiHashMap<int, AgentRef>(4096, Allocator.Temp);
            _agents = new List<AgentRef>();
        }

        [TearDown]
        public void TearDown()
        {
            _map.Dispose();
        }

        // ---------------------------------------------------------------- 셀 계산

        [Test]
        public void CellOf_FloorsNegativeCoordinates()
        {
            // (int) 캐스트는 0 쪽으로 잘려 -0.5 와 0.5 가 같은 셀이 된다. floor 여야 한다.
            Assert.AreEqual(new int2(-1, 0), EnemySpatialHash.CellOf(new float2(-0.5f, 0.5f)));
            Assert.AreEqual(new int2(-1, -1), EnemySpatialHash.CellOf(new float2(-1f, -0.0001f)));
            Assert.AreEqual(new int2(0, 1), EnemySpatialHash.CellOf(new float2(0.999f, 1f)));
        }

        [TestCase(0f, 1)]
        [TestCase(0.25f, 1)] // 0.25 + 0.5 = 0.75 → 1 칸
        [TestCase(0.5f, 1)] // 1.0 → 정확히 1 칸
        [TestCase(0.51f, 2)] // 1.01 → 2 칸
        [TestCase(3f, 4)] // 폭발 최대 반경
        public void CellRangeFor_CoversQueryPlusMaxAgentRadius(float queryRadius, int expected)
        {
            Assert.AreEqual(expected, EnemySpatialHash.CellRangeFor(queryRadius));
        }

        // ---------------------------------------------------------------- 가까운 N 마리

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(12)]
        [TestCase(SpatialQueries.MaxNearestCount)]
        public void FindNearest_MatchesBruteForce(int count)
        {
            const float range = 18f;
            FillRandom(seed: 1, agentCount: 3000);

            var random = Random.CreateFromIndex(99);
            for (int trial = 0; trial < 50; trial++)
            {
                float2 origin = random.NextFloat2(-WorldHalfExtent, WorldHalfExtent);

                var result = new FixedList512Bytes<float2>();
                SpatialQueries.FindNearest(_map, origin, range, count, ref result);

                List<float> expected = BruteForceNearestDistances(origin, range, count);
                Assert.AreEqual(expected.Count, result.Length, $"trial {trial}: 개수가 다르다");

                for (int i = 0; i < result.Length; i++)
                {
                    // 같은 거리의 적이 여럿이면 위치는 달라도 되므로 거리로 비교한다.
                    Assert.AreEqual(expected[i], math.distance(origin, result[i]), 1e-4f,
                        $"trial {trial}, {i} 번째 거리가 다르다");
                }
            }
        }

        [Test]
        public void FindNearest_IgnoresEnemiesOutOfRange()
        {
            Add(new float2(5f, 0f));

            var result = new FixedList512Bytes<float2>();
            SpatialQueries.FindNearest(_map, float2.zero, 4f, 3, ref result);

            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void FindNearest_DoesNotDuplicateOnHashKeyCollision()
        {
            // 셀 (0,0) 의 적이 키 충돌로 셀 (1,0) 버킷에도 들어 있는 상황을 흉내 낸다.
            // 방어가 없으면 두 셀을 모두 보는 탐색이 같은 적을 두 번 담는다.
            AgentRef agent = Add(new float2(0.5f, 0.5f));
            _map.Add(EnemySpatialHash.KeyOf(new int2(1, 0)), agent);

            var result = new FixedList512Bytes<float2>();
            SpatialQueries.FindNearest(_map, new float2(0.9f, 0.5f), 5f, 3, ref result);

            Assert.AreEqual(1, result.Length);
        }

        // ---------------------------------------------------------------- 겹침

        [Test]
        public void TryFindOverlap_MatchesBruteForce()
        {
            const float projectileRadius = 0.15f;
            FillRandom(seed: 2, agentCount: 3000);

            var none = new FixedList128Bytes<Entity>();
            var random = Random.CreateFromIndex(7);

            for (int trial = 0; trial < 2000; trial++)
            {
                float2 position = random.NextFloat2(-WorldHalfExtent, WorldHalfExtent);

                bool found = SpatialQueries.TryFindOverlap(_map, position, projectileRadius, ref none, out AgentRef hit);
                bool expected = BruteForceAnyOverlap(position, projectileRadius);

                Assert.AreEqual(expected, found, $"trial {trial} at {position}");
                if (found)
                {
                    float reach = projectileRadius + hit.Radius;
                    Assert.LessOrEqual(math.distancesq(position, hit.Position), reach * reach, "겹치지 않는 적을 돌려줬다");
                }
            }
        }

        [Test]
        public void TryFindOverlap_FindsLargerAgentCenteredInNeighborCell()
        {
            // 반경 0.5 (MaxAgentRadius) 적의 중심이 옆 칸에 있어도, 가장자리가 닿으면 찾아야 한다.
            Add(new float2(1.4f, 0.5f), radius: EnemySpatialHash.MaxAgentRadius);

            var none = new FixedList128Bytes<Entity>();
            bool found = SpatialQueries.TryFindOverlap(_map, new float2(0.95f, 0.5f), 0f, ref none, out _);

            Assert.IsTrue(found);
        }

        [Test]
        public void TryFindOverlap_SkipsExcludedEnemies()
        {
            AgentRef first = Add(new float2(0.5f, 0.5f));
            AgentRef second = Add(new float2(0.6f, 0.5f));
            float2 query = new float2(0.55f, 0.5f);

            var exclude = new FixedList128Bytes<Entity>();
            exclude.Add(first.Entity);
            Assert.IsTrue(SpatialQueries.TryFindOverlap(_map, query, 0.1f, ref exclude, out AgentRef hit));
            Assert.AreEqual(second.Entity, hit.Entity, "제외한 적을 돌려줬다 (관통 탄이 같은 적을 다시 때리게 됨)");

            exclude.Add(second.Entity);
            Assert.IsFalse(SpatialQueries.TryFindOverlap(_map, query, 0.1f, ref exclude, out _));
        }

        // ---------------------------------------------------------------- 도우미

        private AgentRef Add(float2 position, float radius = EnemyRadius)
        {
            var agent = new AgentRef
            {
                // 0 번 엔티티는 Entity.Null 과 헷갈리므로 1 부터 쓴다.
                Entity = new Entity { Index = _agents.Count + 1, Version = 1 },
                Position = position,
                Radius = radius,
            };

            _agents.Add(agent);
            _map.Add(EnemySpatialHash.KeyOf(EnemySpatialHash.CellOf(position)), agent);
            return agent;
        }

        private void FillRandom(uint seed, int agentCount)
        {
            var random = Random.CreateFromIndex(seed);
            for (int i = 0; i < agentCount; i++)
            {
                Add(random.NextFloat2(-WorldHalfExtent, WorldHalfExtent));
            }
        }

        private List<float> BruteForceNearestDistances(float2 origin, float range, int count)
        {
            var distances = new List<float>();
            foreach (AgentRef agent in _agents)
            {
                float distance = math.distance(origin, agent.Position);
                if (distance <= range)
                {
                    distances.Add(distance);
                }
            }

            distances.Sort();
            if (distances.Count > count)
            {
                distances.RemoveRange(count, distances.Count - count);
            }

            return distances;
        }

        private bool BruteForceAnyOverlap(float2 position, float radius)
        {
            foreach (AgentRef agent in _agents)
            {
                float reach = radius + agent.Radius;
                if (math.distancesq(position, agent.Position) <= reach * reach)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
