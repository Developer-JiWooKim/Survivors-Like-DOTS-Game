using Assets.MyAssets.Scripts.Runtime.Terrain;
using NUnit.Framework;

namespace Assets.MyAssets.Scripts.Tests.Terrain
{
    /// <summary>
    /// 연소 확산 규칙 테스트 (CLAUDE.md 5 장 — "틀리면 디버깅이 지옥인" 로직).
    ///
    /// 왜 이걸 테스트하나:
    /// 확산 버그는 "가끔 한 칸 더 번진다 / 소화가 한 틱 늦다" 형태다. 적 1 만 마리가 뛰는 화면에서
    /// 눈으로 잡을 수 있는 종류가 아니고, 잘못되면 지형 콤보(이 게임의 차별화 축)의 체감이 통째로 어긋난다.
    ///
    /// **결정성**이 특히 중요하다 — 병렬 잡에서 실행 순서에 따라 결과가 달라지면
    /// 재현이 안 되어 디버깅 자체가 불가능해진다.
    /// </summary>
    public sealed class BurnRulesTests
    {
        private static TerrainTickSettings Settings(float igniteChance = 0.5f, float oilMultiplier = 3f)
        {
            return new TerrainTickSettings
            {
                TickInterval = 0.1f,
                IgniteChance = igniteChance,
                OilSpreadMultiplier = oilMultiplier,
            };
        }

        private static TileData Burning(TileType type, byte fuel)
        {
            TileData tile = TileData.Of(type, fuel);
            tile.State |= (byte)TileStateFlags.Burning;
            return tile;
        }

        // ---------------------------------------------------------------- 난수 결정성

        [Test]
        public void Random01_IsDeterministic()
        {
            // 같은 (인덱스, 틱) 은 몇 번을 불러도 같은 값이어야 한다. 이게 깨지면 병렬 확산이 재현 불가능해진다.
            for (int index = 0; index < 50; index++)
            {
                for (uint tick = 0; tick < 5; tick++)
                {
                    Assert.AreEqual(BurnRules.Random01(index, tick), BurnRules.Random01(index, tick));
                }
            }
        }

        [Test]
        public void Random01_StaysInUnitRange()
        {
            for (int index = 0; index < 5000; index++)
            {
                float value = BurnRules.Random01(index, (uint)(index % 17));
                Assert.GreaterOrEqual(value, 0f);
                Assert.Less(value, 1f, "1 이 나오면 확률 1.0 인 칸이 불붙지 않는 경우가 생긴다");
            }
        }

        [Test]
        public void Random01_DoesNotCorrelateIndexWithTick()
        {
            // 인덱스와 틱을 더하거나 XOR 하는 해시는 (i+1, t) 와 (i, t+1) 이 같은 값이 되어
            // 확산이 대각선 줄무늬로 보인다. 그 상관이 없어야 한다.
            int collisions = 0;
            for (int index = 0; index < 500; index++)
            {
                for (uint tick = 0; tick < 8; tick++)
                {
                    if (BurnRules.Random01(index + 1, tick) == BurnRules.Random01(index, tick + 1))
                    {
                        collisions++;
                    }
                }
            }

            // 24 비트 난수라 우연한 일치가 아주 드물게 가능하다. 구조적 상관이면 전부 일치한다.
            Assert.Less(collisions, 10, "인덱스와 틱이 상관되어 있다 — 확산이 대각선 패턴으로 보인다");
        }

        [Test]
        public void Random01_IsReasonablyUniform()
        {
            // 해시 품질이 나쁘면 확산 확률이 의도와 달라진다. 10 개 구간에 고르게 떨어지는지만 본다.
            var buckets = new int[10];
            const int Samples = 10000;

            for (int index = 0; index < Samples; index++)
            {
                buckets[(int)(BurnRules.Random01(index, 3) * 10f)]++;
            }

            foreach (int count in buckets)
            {
                // 균등하면 구간당 1000. 편차 40% 까지는 허용한다 (표본 1 만 기준 넉넉한 한계).
                Assert.Greater(count, 600, "난수 분포가 치우쳐 있다");
                Assert.Less(count, 1400, "난수 분포가 치우쳐 있다");
            }
        }

        // ---------------------------------------------------------------- 가연 조건 (기획서 4.3)

        [Test]
        public void IsFlammable_OnlyGrassAndOilWithFuel()
        {
            Assert.IsTrue(BurnRules.IsFlammable(TileData.Of(TileType.Grass, 100)));
            Assert.IsTrue(BurnRules.IsFlammable(TileData.Of(TileType.Oil, 100)));

            // 연료가 0 이면 종류가 맞아도 안 탄다.
            Assert.IsFalse(BurnRules.IsFlammable(TileData.Of(TileType.Grass, 0)));

            // 물·바위·흙은 연료를 줘도 안 탄다 (기획서 4.2 — 물은 화염 저항).
            Assert.IsFalse(BurnRules.IsFlammable(TileData.Of(TileType.Water, 100)));
            Assert.IsFalse(BurnRules.IsFlammable(TileData.Of(TileType.Rock, 100)));
            Assert.IsFalse(BurnRules.IsFlammable(TileData.Of(TileType.Dirt, 100)));
        }

        // ---------------------------------------------------------------- 물 위 기름 (ISSUE-012)

        [Test]
        public void OilOnWater_IsFlammableAndSpreadsLikeOil()
        {
            TerrainTickSettings settings = Settings(igniteChance: 0.1f);

            Assert.IsTrue(BurnRules.IsFlammable(TileData.Of(TileType.OilOnWater, 200)));

            // 물 위에 떠 있어도 타는 것은 기름이다 — 확산 확률 3 배가 그대로 붙어야 한다.
            float onWater = BurnRules.IgniteChance(TileData.Of(TileType.OilOnWater, 200), 1, settings);
            float onLand = BurnRules.IgniteChance(TileData.Of(TileType.Oil, 200), 1, settings);
            Assert.AreEqual(onLand, onWater, 1e-5f);
        }

        [Test]
        public void OilOnWater_ReturnsToWaterWhenBurnedOut()
        {
            // ISSUE-012 의 핵심. 흙이 되면 물이 영구히 사라진다.
            TileData next = BurnRules.Step(Burning(TileType.OilOnWater, 1), 0, 0f, Settings());

            Assert.IsFalse(next.HasState(TileStateFlags.Burning));
            Assert.AreEqual(TileType.Water, next.TypeValue, "물 위 기름이 다 타고 물로 돌아가지 않았다");
        }

        [Test]
        public void OilOnWater_LakeSurvivesBeingFullyCovered()
        {
            // 사용자가 관측한 재현 절차 그대로: 물을 **완전히** 덮고 태운다.
            // 다 탄 뒤 물이 한 칸도 빠짐없이 돌아와야 한다.
            const int Size = 5;
            var tiles = new TileData[Size * Size];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = TileData.Of(TileType.OilOnWater, 8);
            }

            tiles[12].State |= (byte)TileStateFlags.Burning;

            TerrainTickSettings settings = Settings(igniteChance: 1f);
            for (uint tick = 1; tick <= 60; tick++)
            {
                tiles = StepGrid(tiles, Size, Size, tick, settings);
            }

            foreach (TileData tile in tiles)
            {
                Assert.IsFalse(tile.HasState(TileStateFlags.Burning));
                Assert.AreEqual(TileType.Water, tile.TypeValue, "호수가 사라졌다 (ISSUE-012 재발)");
            }
        }

        [Test]
        public void OilOnWater_PlainOilStillTurnsToDirt()
        {
            // 땅 위 기름은 기획서 4.3 대로 흙이 되어야 한다. 위 수정이 여기까지 번지면 안 된다.
            TileData next = BurnRules.Step(Burning(TileType.Oil, 1), 0, 0f, Settings());
            Assert.AreEqual(TileType.Dirt, next.TypeValue);
        }

        [Test]
        public void TileTypes_GroupsOilAndWaterCorrectly()
        {
            Assert.IsTrue(TileTypes.IsOil(TileType.Oil));
            Assert.IsTrue(TileTypes.IsOil(TileType.OilOnWater));
            Assert.IsFalse(TileTypes.IsOil(TileType.Water));

            // 감전 flood fill 과 동결이 물 위 기름을 물로 봐야 전격·냉기 콤보가 성립한다.
            Assert.IsTrue(TileTypes.IsWaterLike(TileType.Water));
            Assert.IsTrue(TileTypes.IsWaterLike(TileType.OilOnWater));
            Assert.IsFalse(TileTypes.IsWaterLike(TileType.Oil));
        }

        // ---------------------------------------------------------------- 확산 확률 (기획서 4.3)

        [Test]
        public void IgniteChance_IsZeroWithoutBurningNeighbor()
        {
            Assert.AreEqual(0f, BurnRules.IgniteChance(TileData.Of(TileType.Grass, 100), 0, Settings()));
        }

        [Test]
        public void IgniteChance_OilIsThreeTimesGrass()
        {
            // 기획서 4.3: 기름은 전파 확률 3 배. 이웃 1 개일 때 배수가 그대로 드러난다.
            TerrainTickSettings settings = Settings(igniteChance: 0.1f);

            float grass = BurnRules.IgniteChance(TileData.Of(TileType.Grass, 100), 1, settings);
            float oil = BurnRules.IgniteChance(TileData.Of(TileType.Oil, 100), 1, settings);

            Assert.AreEqual(0.1f, grass, 1e-5f);
            Assert.AreEqual(0.3f, oil, 1e-5f);
        }

        [Test]
        public void IgniteChance_ChancePerNeighborIsClampedToOne()
        {
            // 확률이 1 을 넘으면 1 - (1-p)^n 이 진동해 이웃이 2 개일 때 확률이 되레 떨어진다.
            TerrainTickSettings settings = Settings(igniteChance: 0.5f, oilMultiplier: 3f);

            Assert.AreEqual(1f, BurnRules.IgniteChancePerNeighbor(TileData.Of(TileType.Oil, 100), settings), 1e-5f);
            Assert.AreEqual(1f, BurnRules.IgniteChance(TileData.Of(TileType.Oil, 100), 2, settings), 1e-5f);
        }

        [Test]
        public void IgniteChance_RisesWithMoreBurningNeighbors()
        {
            TerrainTickSettings settings = Settings(igniteChance: 0.2f);
            TileData grass = TileData.Of(TileType.Grass, 100);

            float previous = 0f;
            for (int neighbors = 1; neighbors <= 4; neighbors++)
            {
                float chance = BurnRules.IgniteChance(grass, neighbors, settings);
                Assert.Greater(chance, previous, "이웃이 늘면 확률도 올라야 한다");
                Assert.LessOrEqual(chance, 1f);
                previous = chance;
            }

            // 독립 시행 4 회: 1 - 0.8^4 = 0.5904
            Assert.AreEqual(0.5904f, previous, 1e-4f);
        }

        // ---------------------------------------------------------------- 한 칸의 전진 (기획서 4.3)

        [Test]
        public void Step_BurningTileLosesOneFuelPerTick()
        {
            TileData tile = Burning(TileType.Grass, 100);
            TileData next = BurnRules.Step(tile, 0, 0f, Settings());

            Assert.AreEqual(99, next.Fuel);
            Assert.IsTrue(next.HasState(TileStateFlags.Burning), "연료가 남았으면 계속 탄다");
            Assert.AreEqual(TileType.Grass, next.TypeValue);
        }

        [Test]
        public void Step_BurningTileBecomesDirtWhenFuelRunsOut()
        {
            // 기획서 4.3: Fuel 이 0 이 되면 소화되고 Type = 흙.
            TileData next = BurnRules.Step(Burning(TileType.Grass, 1), 0, 0f, Settings());

            Assert.AreEqual(0, next.Fuel);
            Assert.IsFalse(next.HasState(TileStateFlags.Burning));
            Assert.AreEqual(TileType.Dirt, next.TypeValue);
        }

        [Test]
        public void Step_BurnsForExactlyFuelTicks()
        {
            // 연료 = 지속 틱 수. 이 대응이 어긋나면 기획서 4.3 의 "풀 12 초" 같은 수치가 전부 틀어진다.
            const byte Fuel = 12;
            TileData tile = Burning(TileType.Grass, Fuel);

            int ticksBurning = 0;
            for (int tick = 0; tick < 100 && tile.HasState(TileStateFlags.Burning); tick++)
            {
                ticksBurning++;
                tile = BurnRules.Step(tile, 0, 0f, Settings());
            }

            Assert.AreEqual(Fuel, ticksBurning);
            Assert.AreEqual(TileType.Dirt, tile.TypeValue);
        }

        [Test]
        public void Step_NonFlammableNeverIgnites()
        {
            // random 0 은 "가장 잘 붙는" 뽑기다. 그래도 물·바위·흙은 붙지 않아야 한다.
            foreach (TileType type in new[] { TileType.Water, TileType.Rock, TileType.Dirt })
            {
                TileData next = BurnRules.Step(TileData.Of(type, 200), 4, 0f, Settings(igniteChance: 1f));
                Assert.IsFalse(next.HasState(TileStateFlags.Burning), $"{type} 이 불붙었다");
            }
        }

        [Test]
        public void Step_DoesNotIgniteWithoutBurningNeighbor()
        {
            TileData next = BurnRules.Step(TileData.Of(TileType.Grass, 100), 0, 0f, Settings(igniteChance: 1f));
            Assert.IsFalse(next.HasState(TileStateFlags.Burning), "이웃에 불이 없는데 자연발화했다");
        }

        [Test]
        public void Step_IgnitesWhenRandomIsBelowChance()
        {
            TerrainTickSettings settings = Settings(igniteChance: 0.5f);
            TileData grass = TileData.Of(TileType.Grass, 100);

            Assert.IsTrue(BurnRules.Step(grass, 1, 0.49f, settings).HasState(TileStateFlags.Burning));
            Assert.IsFalse(BurnRules.Step(grass, 1, 0.51f, settings).HasState(TileStateFlags.Burning));
        }

        [Test]
        public void Step_NewlyIgnitedTileKeepsFullFuel()
        {
            // 붙은 틱에 연료까지 깎이면 수명이 한 틱 짧아진다. 소화는 다음 틱부터다.
            TileData next = BurnRules.Step(TileData.Of(TileType.Grass, 100), 1, 0f, Settings(igniteChance: 1f));

            Assert.IsTrue(next.HasState(TileStateFlags.Burning));
            Assert.AreEqual(100, next.Fuel);
        }

        [Test]
        public void Step_PreservesOtherStateBits()
        {
            // 연소 처리가 감전·동결 비트를 지우면 안 된다 (한 타일이 여러 상태에 동시에 걸릴 수 있다).
            TileData tile = Burning(TileType.Grass, 100);
            tile.State |= (byte)TileStateFlags.Poisoned;

            TileData next = BurnRules.Step(tile, 0, 0f, Settings());
            Assert.IsTrue(next.HasState(TileStateFlags.Poisoned));

            // 소화되는 틱에도 마찬가지.
            TileData dying = Burning(TileType.Grass, 1);
            dying.State |= (byte)TileStateFlags.Poisoned;

            TileData extinguished = BurnRules.Step(dying, 0, 0f, Settings());
            Assert.IsFalse(extinguished.HasState(TileStateFlags.Burning));
            Assert.IsTrue(extinguished.HasState(TileStateFlags.Poisoned), "소화가 다른 상태 비트까지 지웠다");
        }

        // ---------------------------------------------------------------- 그리드 전체 확산

        /// <summary>
        /// 테스트용 그리드 1 틱 전진. <c>TerrainTickSystem.BurnSpreadJob</c> 과 같은 규칙을 단일 스레드로 재현한다.
        /// 잡 자체를 테스트에서 돌리지 않는 이유는 EditMode 에서 World 를 세우는 비용을 피하기 위해서다 —
        /// 검증 대상은 규칙이고, 잡은 이 루프를 병렬로 펼친 것뿐이다.
        /// </summary>
        private static TileData[] StepGrid(TileData[] tiles, int width, int height, uint tick, TerrainTickSettings settings)
        {
            var next = new TileData[tiles.Length];

            for (int index = 0; index < tiles.Length; index++)
            {
                int x = index % width;
                int y = index / width;

                int burning = 0;
                burning += IsBurningAt(tiles, width, height, x - 1, y);
                burning += IsBurningAt(tiles, width, height, x + 1, y);
                burning += IsBurningAt(tiles, width, height, x, y - 1);
                burning += IsBurningAt(tiles, width, height, x, y + 1);

                next[index] = BurnRules.Step(tiles[index], burning, BurnRules.Random01(index, tick), settings);
            }

            return next;
        }

        private static int IsBurningAt(TileData[] tiles, int width, int height, int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return 0;
            }

            return tiles[y * width + x].HasState(TileStateFlags.Burning) ? 1 : 0;
        }

        private static TileData[] GrassField(int width, int height, byte fuel = 200)
        {
            var tiles = new TileData[width * height];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = TileData.Of(TileType.Grass, fuel);
            }

            return tiles;
        }

        [Test]
        public void Spread_MovesAtMostOneCellPerTick()
        {
            // 더블 버퍼링이 깨져 제자리에서 고치면 불이 한 틱에 여러 칸을 건너뛴다.
            // 확률 1.0 으로 두면 정확히 한 겹씩 번져야 한다 (맨해튼 거리 = 틱 수).
            const int Size = 9;
            const int Center = (Size / 2) * Size + (Size / 2);

            TileData[] tiles = GrassField(Size, Size);
            tiles[Center].State |= (byte)TileStateFlags.Burning;

            TerrainTickSettings settings = Settings(igniteChance: 1f);

            for (uint tick = 1; tick <= 3; tick++)
            {
                tiles = StepGrid(tiles, Size, Size, tick, settings);

                for (int index = 0; index < tiles.Length; index++)
                {
                    if (!tiles[index].HasState(TileStateFlags.Burning))
                    {
                        continue;
                    }

                    int distance =
                        System.Math.Abs((index % Size) - (Size / 2)) +
                        System.Math.Abs((index / Size) - (Size / 2));

                    Assert.LessOrEqual(distance, (int)tick, $"틱 {tick} 에 거리 {distance} 까지 번졌다 — 한 틱에 두 칸 이상 건너뛰었다");
                }
            }
        }

        [Test]
        public void Spread_DoesNotCrossDiagonally()
        {
            // 기획서 4.3 은 4 방향 확산이다. 대각선으로 번지면 물길·방화선이 무의미해진다.
            // 불 주변을 흙으로 막고 대각선만 풀로 남긴다.
            const int Size = 3;
            var tiles = new TileData[Size * Size];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = TileData.Of(TileType.Dirt);
            }

            tiles[4] = Burning(TileType.Grass, 200);
            tiles[0] = TileData.Of(TileType.Grass, 200);
            tiles[2] = TileData.Of(TileType.Grass, 200);
            tiles[6] = TileData.Of(TileType.Grass, 200);
            tiles[8] = TileData.Of(TileType.Grass, 200);

            TileData[] next = StepGrid(tiles, Size, Size, 1, Settings(igniteChance: 1f));

            foreach (int corner in new[] { 0, 2, 6, 8 })
            {
                Assert.IsFalse(next[corner].HasState(TileStateFlags.Burning), "대각선으로 번졌다");
            }
        }

        [Test]
        public void Spread_StopsAtMapEdge()
        {
            // 맵 경계 = 벽 (2026-09-18 결정). 가장자리에서 인덱스가 감싸 돌면 반대편에 불이 붙는다.
            const int Width = 5;
            const int Height = 5;

            TileData[] tiles = GrassField(Width, Height);
            tiles[0].State |= (byte)TileStateFlags.Burning;   // 좌하단 모서리

            TileData[] next = StepGrid(tiles, Width, Height, 1, Settings(igniteChance: 1f));

            // (0,0) 의 왼쪽은 이전 행의 오른쪽 끝(index Width-1)으로 감싸 돌 수 있는 자리다.
            Assert.IsFalse(next[Width - 1].HasState(TileStateFlags.Burning), "가로로 감싸 돌아 번졌다");

            // 위로 한 칸, 오른쪽 한 칸은 정상 확산.
            Assert.IsTrue(next[1].HasState(TileStateFlags.Burning));
            Assert.IsTrue(next[Width].HasState(TileStateFlags.Burning));
        }

        [Test]
        public void Spread_WaterActsAsFirebreak()
        {
            // 물이 방화선이 되는 게 기획서 4.2 의 "물은 화염 저항". 콤보 설계의 전제다.
            // 가로 한 줄: 불 | 물 | 풀
            const int Width = 3;
            var tiles = new[]
            {
                Burning(TileType.Grass, 200),
                TileData.Of(TileType.Water),
                TileData.Of(TileType.Grass, 200),
            };

            TerrainTickSettings settings = Settings(igniteChance: 1f);
            for (uint tick = 1; tick <= 5; tick++)
            {
                tiles = StepGrid(tiles, Width, 1, tick, settings);
            }

            Assert.IsFalse(tiles[1].HasState(TileStateFlags.Burning), "물이 탔다");
            Assert.IsFalse(tiles[2].HasState(TileStateFlags.Burning), "불이 물을 건넜다");
        }

        [Test]
        public void Spread_BurnedFieldTurnsToDirtAndStops()
        {
            // 연료가 다 떨어지면 맵이 흙이 되고 불이 완전히 꺼져야 한다. 안 꺼지면 영구기관이 된다.
            const int Size = 5;
            TileData[] tiles = GrassField(Size, Size, fuel: 6);
            tiles[12].State |= (byte)TileStateFlags.Burning;

            TerrainTickSettings settings = Settings(igniteChance: 1f);
            for (uint tick = 1; tick <= 40; tick++)
            {
                tiles = StepGrid(tiles, Size, Size, tick, settings);
            }

            foreach (TileData tile in tiles)
            {
                Assert.IsFalse(tile.HasState(TileStateFlags.Burning), "불이 꺼지지 않았다");
                Assert.AreEqual(TileType.Dirt, tile.TypeValue, "다 탄 칸이 흙이 되지 않았다");
            }
        }

        [Test]
        public void Spread_IsDeterministicAcrossRuns()
        {
            // 같은 초기 상태 + 같은 틱 → 항상 같은 결과. 병렬 잡에서도 이게 성립해야 한다.
            const int Size = 12;

            TileData[] RunOnce()
            {
                TileData[] tiles = GrassField(Size, Size, fuel: 30);
                tiles[Size * 2 + 3].State |= (byte)TileStateFlags.Burning;

                TerrainTickSettings settings = Settings(igniteChance: 0.3f);
                for (uint tick = 1; tick <= 20; tick++)
                {
                    tiles = StepGrid(tiles, Size, Size, tick, settings);
                }

                return tiles;
            }

            TileData[] first = RunOnce();
            TileData[] second = RunOnce();

            for (int i = 0; i < first.Length; i++)
            {
                Assert.AreEqual(first[i].Type, second[i].Type, $"[{i}] 타입이 달라졌다");
                Assert.AreEqual(first[i].Fuel, second[i].Fuel, $"[{i}] 연료가 달라졌다");
                Assert.AreEqual(first[i].State, second[i].State, $"[{i}] 상태가 달라졌다");
            }
        }

        [Test]
        public void Spread_OilBurnsFasterThanGrass()
        {
            // 기획서 4.3 의 "기름은 빠른 확산" 이 실제로 드러나는지. 같은 조건에서 기름 줄이 먼저 끝까지 번져야 한다.
            const int Length = 20;

            int TicksToReachEnd(TileType type)
            {
                var tiles = new TileData[Length];
                for (int i = 0; i < Length; i++)
                {
                    tiles[i] = TileData.Of(type, 200);
                }

                tiles[0].State |= (byte)TileStateFlags.Burning;

                TerrainTickSettings settings = Settings(igniteChance: 0.2f);
                for (uint tick = 1; tick <= 500; tick++)
                {
                    tiles = StepGrid(tiles, Length, 1, tick, settings);
                    if (tiles[Length - 1].HasState(TileStateFlags.Burning) || tiles[Length - 1].TypeValue == TileType.Dirt)
                    {
                        return (int)tick;
                    }
                }

                return int.MaxValue;
            }

            Assert.Less(TicksToReachEnd(TileType.Oil), TicksToReachEnd(TileType.Grass));
        }
    }
}
