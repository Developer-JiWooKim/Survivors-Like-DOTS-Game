using Assets.MyAssets.Scripts.Runtime.Leveling;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.MyAssets.Scripts.Runtime.UI
{
    /// <summary>
    /// 레벨업 3지선다 패널 (uGUI).
    ///
    /// <see cref="LevelUpState"/> 를 읽어 열고 닫기만 하고, 클릭하면 고른 번호만 기록한다.
    /// 실제 강화 적용과 재개는 <see cref="LevelUpApplySystem"/> 이 한다 (LevelUpState 주석 참조).
    ///
    /// 라벨이 영어인 이유: TMP 기본 폰트(LiberationSans)에 한글 글리프가 없다.
    /// 한글 폰트 에셋을 추가하면 <see cref="Describe"/> 만 바꾸면 된다.
    /// </summary>
    public sealed class LevelUpPanelView : MonoBehaviour
    {
        [Tooltip("열고 닫을 패널 루트. 이 스크립트가 붙은 오브젝트와 달라야 한다 (자기 자신을 끄면 Update 가 멈춘다)")]
        [SerializeField] private GameObject _panel;

        [Tooltip("선택지 버튼 3 개 (위에서부터 순서대로)")]
        [SerializeField] private Button[] _buttons = new Button[LevelUpState.OptionCount];

        [Tooltip("각 버튼의 라벨 3 개 (버튼과 같은 순서)")]
        [SerializeField] private TMP_Text[] _labels = new TMP_Text[LevelUpState.OptionCount];

        private readonly SingletonAccess<LevelUpState> _state = new SingletonAccess<LevelUpState>();
        private bool _shown;

        private void Awake()
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                int index = i; // 람다가 루프 변수를 캡처하면 전부 마지막 값이 된다.
                if (_buttons[i] != null)
                {
                    _buttons[i].onClick.AddListener(() => Select(index));
                }
            }

            SetShown(false);
        }

        private void Update()
        {
            bool waiting = _state.TryRead(out LevelUpState state)
                           && state.IsOpen
                           && state.SelectedIndex == LevelUpState.NoSelection;

            if (waiting && !_shown)
            {
                // 문자열은 열릴 때 한 번만 채운다.
                for (int i = 0; i < _labels.Length && i < LevelUpState.OptionCount; i++)
                {
                    if (_labels[i] != null)
                    {
                        _labels[i].text = Describe(state.GetOption(i));
                    }
                }
            }

            if (waiting != _shown)
            {
                SetShown(waiting);
            }
        }

        private void Select(int index)
        {
            if (!_state.TryRead(out LevelUpState state) || !state.IsOpen)
            {
                return;
            }

            state.SelectedIndex = index;
            if (_state.TryWrite(state))
            {
                // 적용은 다음 프레임 LevelUpApplySystem 에서 일어난다. 연타 방지를 위해 바로 닫는다.
                SetShown(false);
            }
        }

        private void SetShown(bool shown)
        {
            _shown = shown;
            if (_panel != null)
            {
                _panel.SetActive(shown);
            }
        }

        private static string Describe(UpgradeType upgrade)
        {
            return upgrade switch
            {
                UpgradeType.ShardDamage => "Shard Damage +20%",
                UpgradeType.ShardFireRate => "Shard Fire Rate +18%",
                UpgradeType.ShardProjectileSpeed => "Shard Speed +20%",
                UpgradeType.MoveSpeed => "Move Speed +10%",
                UpgradeType.MaxHealth => "Max HP +20",
                _ => upgrade.ToString(),
            };
        }
    }
}
