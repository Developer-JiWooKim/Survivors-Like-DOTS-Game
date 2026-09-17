using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Experience
{
    public enum XpDropKind : byte
    {
        Gem,
        Magnet,
    }

    /// <summary>
    /// 바닥에 아이템 하나를 떨어뜨리라는 요청. 젬과 자석이 같은 경로(버스 → 스폰 잡)를 쓴다.
    /// </summary>
    public struct XpDrop
    {
        public float2 Position;
        public XpDropKind Kind;

        /// <summary>젬의 경험치. 자석이면 쓰지 않는다.</summary>
        public int Value;
    }

    /// <summary>
    /// 이번 프레임의 젬 드랍 요청 스트림들을 <see cref="XpGemSpawnSystem"/> 에 넘기는 싱글턴.
    ///
    /// 왜 사망 잡이 직접 젬을 켜지 않나:
    /// 사망 잡은 적 청크를 병렬로 돈다. 여러 워커가 "비어 있는 젬" 을 동시에 골라 켜면 같은 젬을 두 번 쓰는 레이스가 난다.
    /// 요청만 모아 두고 한 잡이 순서대로 배정한다. 구조는 DamageEventBus 와 같다.
    /// (분열형 사망 등 드랍 발생원이 늘어도 여기에 등록만 하면 된다.)
    /// </summary>
    public struct XpDropBus : IComponentData
    {
        public NativeList<NativeStream> Streams;
        public JobHandle Dependency;

        public void Register(NativeStream stream, JobHandle writerHandle)
        {
            Streams.Add(stream);
            Dependency = JobHandle.CombineDependencies(Dependency, writerHandle);
        }
    }
}
