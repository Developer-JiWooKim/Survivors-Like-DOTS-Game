using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Pooling
{
    /// <summary>
    /// 풀링되는 엔티티(적, 투사체)가 지금 월드에서 "살아있는지" 나타내는 enableable 태그.
    ///
    /// 왜 생성/파괴가 아니라 토글인가 (기획서 8.4):
    /// Instantiate/Destroy 는 구조적 변경이라 메인 스레드 동기화 지점을 만들고 청크를 재배치한다.
    /// enableable 은 청크 안의 비트 하나만 바꾸므로 워커 스레드 잡 안에서도 켜고 끌 수 있다.
    ///
    /// 주의: 이 태그를 끄는 것만으로는 **렌더링이 꺼지지 않는다.** Entities Graphics 는 Active 를 모른다.
    /// 끌 때는 반드시 <c>MaterialMeshInfo</c> (역시 enableable) 도 같이 꺼야 화면에서 사라진다.
    /// </summary>
    public struct Active : IComponentData, IEnableableComponent
    {
    }
}
