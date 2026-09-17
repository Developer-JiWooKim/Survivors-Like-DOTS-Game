using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Assets.MyAssets.Scripts.Runtime.Combat
{
    /// <summary>
    /// 이번 프레임의 데미지 스트림들을 <see cref="DamageApplySystem"/> 에 넘기는 싱글턴.
    ///
    /// 왜 스트림을 모으는 버스가 필요한가:
    /// 피해 발생원은 여러 시스템이다 (투사체, 장판, 지형 — 기획서 8.2 의 12·13·15번).
    /// 각자 자기 NativeStream 을 병렬로 채우고, 여기에 스트림과 잡 핸들을 등록만 한다.
    /// 적용 시스템은 등록된 핸들이 끝나길 기다린 뒤 전부 드레인하고 해제한다.
    ///
    /// 왜 Health 에 병렬로 직접 쓰지 않나 (기획서 8.3):
    /// 같은 적을 두 투사체가 동시에 맞히면 두 워커 스레드가 같은 Health 를 쓰는 레이스가 된다.
    /// 1만 이벤트도 단일 스레드 적용은 0.1ms 수준이라 병렬화할 이유가 없다.
    ///
    /// 등록은 반드시 <see cref="DamageApplySystem"/> 보다 먼저 도는 시스템에서 해야 한다.
    /// </summary>
    public struct DamageEventBus : IComponentData
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
