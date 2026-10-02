using Assets.MyAssets.DOTS_Study.Scripts.Step01;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step02
{
    [BurstCompile]
    public partial struct SpawnSystem : ISystem
    {

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Spawner>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Spawner spawner = SystemAPI.GetSingleton<Spawner>();
            var random = new Random(spawner.Seed);
            NativeArray<Entity> entities = state.EntityManager.Instantiate(spawner.Prefab, spawner.Count, Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                float distance = math.sqrt(random.NextFloat(0f, 1f)) * spawner.Range;
                float2 offset = random.NextFloat2Direction() * distance;

                float rotationSpeed = random.NextFloat(spawner.MinRadiansPerSecond, spawner.MaxRadiansPerSecond);


                state.EntityManager.SetComponentData(entities[i], LocalTransform.FromPosition(new float3(offset.x, offset.y, 0f)));
                state.EntityManager.SetComponentData(entities[i], new RotationSpeed { RadiansPerSecond = rotationSpeed });
            }

            entities.Dispose();
            state.Enabled = false; // 한번 스폰 이후에는 스포너를 꺼야 새롭게 엔티티들이 생성되지 않음
        }

    }
}