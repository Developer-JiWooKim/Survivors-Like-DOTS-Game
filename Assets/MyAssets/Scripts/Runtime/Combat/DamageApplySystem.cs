using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Assets.MyAssets.Scripts.Runtime.Combat
{
    /// <summary>
    /// <see cref="DamageEventBus"/> 에 등록된 스트림을 드레인해 <see cref="Health"/> 를 깎는다 (기획서 8.2 의 16번).
    /// 사망 판정은 여기서 하지 않고 사망 시스템에 맡긴다 — 적용과 사망을 분리해야
    /// 플레이어 피해 등 대상별 사망 규칙을 따로 붙일 수 있다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(Weapon.ProjectileHitSystem))]
    public partial struct DamageApplySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 버스는 이 시스템이 소유한다. 생성·해제 책임을 한 곳에 둬야 누수가 안 난다.
            state.EntityManager.AddComponentData(state.SystemHandle, new DamageEventBus
            {
                Streams = new NativeList<NativeStream>(4, Allocator.Persistent),
                Dependency = default,
            });
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            DamageEventBus bus = state.EntityManager.GetComponentData<DamageEventBus>(state.SystemHandle);
            bus.Dependency.Complete();
            for (int i = 0; i < bus.Streams.Length; i++)
            {
                bus.Streams[i].Dispose();
            }
            bus.Streams.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            RefRW<DamageEventBus> bus = SystemAPI.GetComponentRW<DamageEventBus>(state.SystemHandle);
            if (bus.ValueRO.Streams.Length == 0)
            {
                return;
            }

            // 스트림마다 적용 잡을 하나씩 만들어 **직렬로** 잇는다.
            // NativeArray<NativeStream> 을 잡 하나에 넘기면 "컨테이너 안의 컨테이너" 라 잡 안전 검사에 걸린다.
            // 직렬 체인이므로 Health 쓰기끼리 겹치지 않는다.
            ComponentLookup<Health> healthLookup = SystemAPI.GetComponentLookup<Health>();
            JobHandle chain = JobHandle.CombineDependencies(state.Dependency, bus.ValueRO.Dependency);

            for (int i = 0; i < bus.ValueRO.Streams.Length; i++)
            {
                NativeStream stream = bus.ValueRO.Streams[i];
                chain = new ApplyDamageJob
                {
                    Reader = stream.AsReader(),
                    HealthLookup = healthLookup,
                }.Schedule(chain);

                // 해제도 적용 뒤에 예약해 메인 스레드를 막지 않는다.
                chain = stream.Dispose(chain);
            }

            // 리스트는 재사용한다. 스트림 핸들 값만 복사해 잡에 넘겼으니 바로 비워도 된다.
            bus.ValueRW.Streams.Clear();
            bus.ValueRW.Dependency = default;

            state.Dependency = chain;
        }
    }

    [BurstCompile]
    internal struct ApplyDamageJob : IJob
    {
        public NativeStream.Reader Reader;
        public ComponentLookup<Health> HealthLookup;

        public void Execute()
        {
            for (int i = 0; i < Reader.ForEachCount; i++)
            {
                int count = Reader.BeginForEachIndex(i);
                for (int e = 0; e < count; e++)
                {
                    DamageEvent damage = Reader.Read<DamageEvent>();

                    // 같은 프레임에 다른 이유로 사라진 대상일 수 있다.
                    if (!HealthLookup.TryGetRefRW(damage.Target, out RefRW<Health> health))
                    {
                        continue;
                    }
                    health.ValueRW.Current -= damage.Amount;
                }
                Reader.EndForEachIndex();
            }
        }
    }
}
