using System.Text;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.MyAssets.Scripts.Runtime.Diagnostics
{
    /// <summary>
    /// 화면 구석에 성능 수치를 상시 표시한다.
    ///
    /// 왜 필요한가:
    /// CLI 배치모드는 -nographics 라 렌더 비용을 측정할 수 없다(CLAUDE.md 3.2).
    /// 렌더 관련 수치는 사람이 직접 봐야 하므로, Profiler 창을 열지 않아도
    /// 플레이 즉시 보이고 빌드에서도 그대로 동작하는 오버레이를 둔다.
    ///
    /// ProfilerRecorder 를 쓰는 이유:
    /// UnityEditor.UnityStats 는 에디터 전용이라 빌드에서 값을 못 읽는다.
    /// ProfilerRecorder 는 에디터와 개발 빌드 양쪽에서 동작한다.
    /// </summary>
    public sealed class PerfOverlay : MonoBehaviour
    {
        private const float RefreshInterval = 0.25f;

        [SerializeField] private bool _visible = true;

        [Tooltip("오버레이를 켜고 끄는 키.")]
        [SerializeField] private Key _toggleKey = Key.F1;

        private ProfilerRecorder _drawCalls;
        private ProfilerRecorder _batches;
        private ProfilerRecorder _setPassCalls;
        private ProfilerRecorder _mainThreadTime;

        private readonly StringBuilder _text = new StringBuilder(256);
        private GUIStyle _style;

        private float _refreshTimer;
        private float _frameMsAccum;
        private int _frameCount;
        private string _display = string.Empty;

        private void OnEnable()
        {
            _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _mainThreadTime = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
        }

        private void OnDisable()
        {
            _drawCalls.Dispose();
            _batches.Dispose();
            _setPassCalls.Dispose();
            _mainThreadTime.Dispose();
        }

        private void Update()
        {
            // 이 프로젝트는 Active Input Handling 이 "Input System Package (New)" 라
            // 구형 UnityEngine.Input 을 쓰면 런타임에 예외가 난다.
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[_toggleKey].wasPressedThisFrame)
            {
                _visible = !_visible;
            }

            // 프레임 시간은 매 프레임 누적하고, 문자열 생성만 간격을 두고 한다.
            // 매 프레임 문자열을 만들면 오버레이 자체가 측정 대상을 오염시킨다.
            _frameMsAccum += Time.unscaledDeltaTime * 1000f;
            _frameCount++;

            _refreshTimer += Time.unscaledDeltaTime;
            if (_refreshTimer < RefreshInterval)
            {
                return;
            }

            Rebuild();

            _refreshTimer = 0f;
            _frameMsAccum = 0f;
            _frameCount = 0;
        }

        private void Rebuild()
        {
            float avgMs = _frameCount > 0 ? _frameMsAccum / _frameCount : 0f;
            float fps = avgMs > 0f ? 1000f / avgMs : 0f;

            _text.Clear();
            _text.AppendFormat("FPS   {0,7:F1}   ({1:F2} ms)\n", fps, avgMs);

            if (_mainThreadTime.Valid)
            {
                // ProfilerRecorder 의 시간 단위는 나노초다.
                double mainMs = _mainThreadTime.LastValue * 1e-6;
                _text.AppendFormat("Main  {0,7:F2} ms\n", mainMs);
            }

            _text.AppendFormat("Ents  {0,7}\n", CountEntities());

            AppendRenderStats();

            _display = _text.ToString();
        }

        /// <summary>
        /// 렌더 통계를 붙인다.
        ///
        /// 왜 에디터 전용 경로가 따로 있나 (ISSUE-004):
        /// ProfilerRecorder 의 "Draw Calls Count" / "Batches Count" 는 URP(SRP) 에서
        /// 0 만 반환한다("SetPass Calls Count" 는 정상 동작). 그래서 에디터에서는
        /// UnityStats 를 우선 쓴다. UnityStats 는 에디터 전용이라 빌드에서는 못 쓰므로,
        /// 빌드에서는 ProfilerRecorder 값으로 되돌아간다.
        /// </summary>
        private void AppendRenderStats()
        {
#if UNITY_EDITOR
            // UnityStats.batches 는 Unity 6 에서 제거됐다. 실제로 존재하는 멤버만 쓴다.
            //
            // instancedBatchedDrawCalls / dynamicBatchedDrawCalls 는 일부러 표시하지 않는다 (ISSUE-004).
            // 그 카운터들은 구형 GPU Instancing(MeshRenderer 경로)을 세는데,
            // Entities Graphics 는 BatchRendererGroup(BRG)이라는 별개 경로를 쓰므로 항상 0 이 나온다.
            // 0 을 보고 "인스턴싱 실패"로 오해하기 쉬워서 아예 빼둔다.
            //
            // BRG 인스턴싱 판정 기준은 "엔티티 수 대비 드로우콜 수"다. 아래 Ents 와 Draw 를 비교한다.
            // 정확한 인스턴스 수가 필요하면 Game 뷰 Statistics 창의
            // "Draw Calls : N (M instances)" 를 본다.
            _text.AppendFormat("Draw  {0,7}\n", UnityEditor.UnityStats.drawCalls);
            _text.AppendFormat("SetPs {0,7}\n", UnityEditor.UnityStats.setPassCalls);

            // 실제로 화면에 그려지고 있는지 확인하는 값.
            // 쿼드 1만 개면 2만 삼각형이 나와야 한다. 0 이면 렌더 자체가 안 되고 있는 것.
            _text.AppendFormat("Tris  {0,7}\n", UnityEditor.UnityStats.triangles);
#else
            if (_batches.Valid)
            {
                _text.AppendFormat("Batch {0,7}\n", _batches.LastValue);
            }

            if (_drawCalls.Valid)
            {
                _text.AppendFormat("Draw  {0,7}\n", _drawCalls.LastValue);
            }

            if (_setPassCalls.Valid)
            {
                _text.AppendFormat("SetPs {0,7}\n", _setPassCalls.LastValue);
            }
#endif
        }

        private static int CountEntities()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                return 0;
            }

            return world.EntityManager.UniversalQuery.CalculateEntityCount();
        }

        private void OnGUI()
        {
            if (!_visible || _display.Length == 0)
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white },
            };

            const int width = 220;
            const int height = 158;
            var rect = new Rect(10f, 10f, width, height);

            // 배경을 깔지 않으면 밝은 화면에서 글자가 안 보인다.
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(rect.x + 8f, rect.y + 6f, width - 16f, height - 12f), _display, _style);
        }
    }
}
