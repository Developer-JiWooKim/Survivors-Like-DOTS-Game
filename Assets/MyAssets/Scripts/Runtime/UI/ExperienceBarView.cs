using TMPro;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.UI
{
    /// <summary>
    /// 화면 상단 XP 바 + 레벨 표시 (uGUI).
    /// </summary>
    public sealed class ExperienceBarView : MonoBehaviour
    {
        [Tooltip("채워지는 부분. 배경의 자식으로 두고 Stretch(앵커 0~1, 오프셋 0)로 맞춘다")]
        [SerializeField] private RectTransform _fill;

        [SerializeField] private TMP_Text _levelLabel;

        private readonly SingletonAccess<PlayerStatusView> _status = new SingletonAccess<PlayerStatusView>();
        private int _shownLevel = -1;

        private void Update()
        {
            if (!_status.TryRead(out PlayerStatusView view) || !view.HasPlayer)
            {
                return;
            }

            UiBar.SetRatio(_fill, view.XpToNext > 0 ? (float)view.Xp / view.XpToNext : 0f);

            // 문자열은 레벨이 바뀔 때만 만든다. 매 프레임 만들면 GC 할당이 쌓인다.
            if (_levelLabel != null && view.Level != _shownLevel)
            {
                _shownLevel = view.Level;
                _levelLabel.text = $"Lv {view.Level}";
            }
        }
    }
}
