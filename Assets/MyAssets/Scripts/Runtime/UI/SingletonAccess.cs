using Unity.Collections;
using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.UI
{
    /// <summary>
    /// MonoBehaviour(uGUI) 에서 ECS 싱글턴을 읽고 쓰는 도우미.
    ///
    /// 왜 필요한가:
    /// 이 프로젝트의 UI 용 싱글턴은 **시스템 엔티티**에 붙어 있다 (소유 시스템이 생성·해제를 책임지도록).
    /// SystemAPI 는 시스템 엔티티를 자동으로 포함하지만, 직접 만든 EntityQuery 는
    /// <c>EntityQueryOptions.IncludeSystems</c> 가 없으면 시스템 엔티티를 **못 찾는다**.
    /// 이걸 매번 기억하지 않도록 한 곳에 모았다.
    ///
    /// 월드가 다시 만들어지면(플레이 모드 재진입 등) 쿼리도 다시 만든다.
    /// </summary>
    public sealed class SingletonAccess<T> where T : unmanaged, IComponentData
    {
        private World _world;
        private EntityQuery _query;

        public bool TryRead(out T value)
        {
            value = default;
            return TryGetQuery(out EntityQuery query) && query.TryGetSingleton(out value);
        }

        public bool TryWrite(T value)
        {
            if (!TryGetQuery(out EntityQuery query) || query.CalculateEntityCountWithoutFiltering() != 1)
            {
                return false;
            }

            query.SetSingleton(value);
            return true;
        }

        private bool TryGetQuery(out EntityQuery query)
        {
            query = default;

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                return false;
            }

            if (_world != world)
            {
                _world = world;
                // WithAll 은 읽기 전용 접근이라 SetSingleton 이 InvalidOperationException 을 던진다 (ISSUE-009).
                // 쓰기도 하는 도우미이므로 RW 로 만든다. 읽기(TryGetSingleton)는 RW 쿼리에서도 된다.
                _query = new EntityQueryBuilder(Allocator.Temp)
                    .WithAllRW<T>()
                    .WithOptions(EntityQueryOptions.IncludeSystems)
                    .Build(world.EntityManager);
            }

            query = _query;
            return true;
        }
    }
}
