using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.UI
{
    /// <summary>
    /// 가로 바를 비율만큼 채운다.
    ///
    /// Image 의 Filled 타입(fillAmount)을 쓰지 않는 이유:
    /// Filled 는 Source Image(스프라이트)가 있어야 동작한다. 단색 도형 프로토타입(기획서 9장)에서는
    /// 스프라이트 없이 색만 있는 Image 를 쓰므로, 앵커의 오른쪽 끝을 비율로 옮기는 방식이 설정 실수가 적다.
    /// </summary>
    public static class UiBar
    {
        public static void SetRatio(RectTransform fill, float ratio)
        {
            if (fill == null)
            {
                return;
            }

            ratio = Mathf.Clamp01(ratio);

            // 값이 같으면 건드리지 않는다. 앵커를 쓰면 레이아웃이 다시 계산된다.
            if (Mathf.Approximately(fill.anchorMax.x, ratio))
            {
                return;
            }

            fill.anchorMin = new Vector2(0f, fill.anchorMin.y);
            fill.anchorMax = new Vector2(ratio, fill.anchorMax.y);
        }
    }
}
