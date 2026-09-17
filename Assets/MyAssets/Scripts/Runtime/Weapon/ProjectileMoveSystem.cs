using Assets.MyAssets.Scripts.Runtime.Pooling;
using Assets.MyAssets.Scripts.Runtime.Run;
using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

namespace Assets.MyAssets.Scripts.Runtime.Weapon
{
    /// <summary>
    /// 살아있는 투사체를 직선으로 움직이고, 수명이 다하면 풀로 돌려보낸다 (기획서 8.2 의 11번).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(ShardFireSystem))]
    public partial struct ProjectileMoveSystem : ISystem
    {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 쿼리를 직접 만들어 넘기는 이유:
            // 잡 파라미터의 EnabledRefRW<T> 가 "켜진 것만" 을 뜻하는지 "있기만 하면" 을 뜻하는지를
            // 코드젠에 맡기지 않고 명시하기 위해서다.
            //   Active           → 켜진 것만 (WithAllRW)
            //   MaterialMeshInfo → 상태 무관 (WithPresentRW). 끄기 위한 쓰기 권한만 필요하다
            _query = SystemAPI.QueryBuilder()
                .WithAllRW<Projectile, LocalTransform>()
                .WithAllRW<Active>()
                .WithPresentRW<MaterialMeshInfo>()
                .Build();

            state.RequireForUpdate(_query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ProjectileMoveJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel(_query);
        }
    }

    [BurstCompile]
    internal partial struct ProjectileMoveJob : IJobEntity
    {
        public float DeltaTime;

        private void Execute(
            ref LocalTransform transform,
            ref Projectile projectile,
            EnabledRefRW<Active> active,
            EnabledRefRW<MaterialMeshInfo> visible)
        {
            projectile.RemainingLifetime -= DeltaTime;
            if (projectile.RemainingLifetime <= 0f)
            {
                // 자기 엔티티의 enabled 비트만 바꾸므로 병렬로 해도 안전하다.
                active.ValueRW = false;
                visible.ValueRW = false;
                return;
            }

            transform.Position.xy += projectile.Velocity * DeltaTime;
        }
    }
}
