using Assets.MyAssets.Scripts.Runtime.Enemy;
using Assets.MyAssets.Scripts.Runtime.Player;
using Assets.MyAssets.Scripts.Runtime.Run;
using Assets.MyAssets.Scripts.Runtime.Weapon;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Assets.MyAssets.Scripts.Runtime.Leveling
{
    /// <summary>
    /// 경험치가 필요량에 도달하면 레벨을 올리고, 3지선다를 띄우며 런을 일시정지한다 (기획서 8.2 의 19번).
    ///
    /// 왜 그룹 맨 앞에서 "지난 프레임" 경험치를 보나: <see cref="PlayerDeathSystem"/> 과 같은 이유 —
    /// 프레임 시작에는 경험치 합산 잡이 끝나 있어 메인 스레드가 기다리지 않는다. 대가는 1 프레임 지연.
    ///
    /// 한 번에 한 레벨만 올린다. 경험치가 두 레벨 분량이면 선택 후 재개된 다음 프레임에 다시 걸린다.
    /// 따로 "남은 레벨업 수" 를 세지 않아도 되는 단순한 방식이다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(PlayerDeathSystem))]
    public partial struct LevelUpSystem : ISystem
    {
        // Burst 밖: 시드에 시계를 쓴다. 고정 시드면 매 판 같은 선택지 순서가 나온다.
        public void OnCreate(ref SystemState state)
        {
            uint seed = (uint)System.Environment.TickCount | 1u;

            state.EntityManager.AddComponentData(state.SystemHandle, new LevelUpState
            {
                IsOpen = false,
                SelectedIndex = LevelUpState.NoSelection,
                Random = Random.CreateFromIndex(seed),
            });

            state.RequireForUpdate<PlayerExperience>();
            state.RequireForUpdate<RunState>();

            // 선택지 후보를 거르려면 무기 상태가 필요하다.
            state.RequireForUpdate<ShardWeapon>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            RefRW<RunState> runState = SystemAPI.GetSingletonRW<RunState>();

            // 같은 프레임에 사망 판정이 먼저 났으면 레벨업을 띄우지 않는다.
            if (runState.ValueRO.Phase != RunPhase.Playing)
            {
                return;
            }

            // 벤치마크 모드: 레벨업 창으로 멈추지 않는다. 경험치는 계속 쌓이기만 한다.
            if (SystemAPI.TryGetSingleton(out SpawnDirector director) && director.IsBenchmark)
            {
                return;
            }

            foreach (RefRW<PlayerExperience> experience in SystemAPI.Query<RefRW<PlayerExperience>>())
            {
                if (experience.ValueRO.Xp < experience.ValueRO.XpToNext)
                {
                    continue;
                }

                // 남은 경험치는 이월한다.
                experience.ValueRW.Xp -= experience.ValueRO.XpToNext;
                experience.ValueRW.Level++;
                experience.ValueRW.XpToNext = PlayerExperience.RequiredFor(experience.ValueRO.Level);

                // ShardWeapon 은 메인 스레드에서만 바뀌므로 읽어도 기다릴 잡이 없다.
                ShardWeapon weapon = SystemAPI.GetSingleton<ShardWeapon>();

                RefRW<LevelUpState> levelUp = SystemAPI.GetComponentRW<LevelUpState>(state.SystemHandle);
                Offer(ref levelUp.ValueRW, weapon);

                runState.ValueRW.Phase = RunPhase.Paused;
                Debug.Log($"[LevelUp] Lv.{experience.ValueRO.Level} — 선택 대기");
                return;
            }
        }

        /// <summary>
        /// 최대치가 아닌 강화 중 서로 다른 3 개를 무작위로 고른다 (부분 Fisher–Yates 셔플).
        /// </summary>
        private static void Offer(ref LevelUpState levelUp, in ShardWeapon weapon)
        {
            var pool = new FixedList32Bytes<byte>();
            for (int i = 0; i < UpgradeTable.Count; i++)
            {
                if (!UpgradeTable.IsMaxed((UpgradeType)i, weapon))
                {
                    pool.Add((byte)i);
                }
            }

            for (int i = 0; i < LevelUpState.OptionCount; i++)
            {
                int pick = levelUp.Random.NextInt(i, pool.Length);
                (pool[i], pool[pick]) = (pool[pick], pool[i]);
            }

            levelUp.Option0 = (UpgradeType)pool[0];
            levelUp.Option1 = (UpgradeType)pool[1];
            levelUp.Option2 = (UpgradeType)pool[2];
            levelUp.SelectedIndex = LevelUpState.NoSelection;
            levelUp.IsOpen = true;
        }
    }
}
