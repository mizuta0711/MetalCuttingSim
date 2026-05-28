// 役割: Play 中に切削パラメータをリアルタイム調整する uGUI パネル
// 依存: CuttingParameters, MarchingCubesRenderer
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace MetalCuttingSim
{
    public class RuntimeSettingsPanel : MonoBehaviour
    {
        [SerializeField] private CuttingParameters     parameters;
        [SerializeField] private MarchingCubesRenderer mcRenderer;

        [Header("Dropdowns")]
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private Dropdown chunkSizeDropdown;

        [Header("Sliders + Labels")]
        [SerializeField] private Slider drillRadiusSlider;
        [SerializeField] private Text   drillRadiusLabel;
        [SerializeField] private Slider drillStrengthSlider;
        [SerializeField] private Text   drillStrengthLabel;
        [SerializeField] private Slider isoLevelSlider;
        [SerializeField] private Text   isoLevelLabel;
        [SerializeField] private Slider maxChunksSlider;
        [SerializeField] private Text   maxChunksLabel;
        [SerializeField] private Slider feedSpeedSlider;
        [SerializeField] private Text   feedSpeedLabel;

        private static readonly int[] Resolutions = { 32, 64, 96, 128 };
        private static readonly int[] ChunkSizes  = { 8, 16, 32 };

        private bool _visible = true;

        void Start()
        {
            SetupDropdowns();
            SetupSliders();
            UpdateAllLabels();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.tabKey.wasPressedThisFrame)
            {
                _visible = !_visible;
                gameObject.GetComponent<CanvasGroup>().alpha          = _visible ? 1f : 0f;
                gameObject.GetComponent<CanvasGroup>().blocksRaycasts = _visible;
            }
        }

        void SetupDropdowns()
        {
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                var opts = new System.Collections.Generic.List<Dropdown.OptionData>();
                foreach (var r in Resolutions)
                    opts.Add(new Dropdown.OptionData(r.ToString()));
                resolutionDropdown.AddOptions(opts);
                resolutionDropdown.value = System.Array.IndexOf(Resolutions, parameters.fieldResolution);
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }

            if (chunkSizeDropdown != null)
            {
                chunkSizeDropdown.ClearOptions();
                var opts = new System.Collections.Generic.List<Dropdown.OptionData>();
                foreach (var c in ChunkSizes)
                    opts.Add(new Dropdown.OptionData(c.ToString()));
                chunkSizeDropdown.value = System.Array.IndexOf(ChunkSizes, parameters.chunkSize);
                chunkSizeDropdown.AddOptions(opts);
                chunkSizeDropdown.onValueChanged.AddListener(OnChunkSizeChanged);
            }
        }

        void SetupSliders()
        {
            BindSlider(drillRadiusSlider,   0.5f, 20f,  parameters.drillRadius,       v => { parameters.drillRadius   = v; UpdateLabel(drillRadiusLabel,   "掘削半径",     v); });
            BindSlider(drillStrengthSlider, 0.1f, 20f,  parameters.drillStrength,     v => { parameters.drillStrength = v; UpdateLabel(drillStrengthLabel, "掘削強度",     v); });
            BindSlider(isoLevelSlider,      0.1f, 0.9f, parameters.isoLevel,          v => { parameters.isoLevel      = v; mcRenderer.RemeshAll();          UpdateLabel(isoLevelLabel,      "isoLevel",     v); });
            BindSlider(maxChunksSlider,     1f,   64f,  parameters.maxChunksPerFrame, v => { parameters.maxChunksPerFrame = Mathf.RoundToInt(v); UpdateLabel(maxChunksLabel, "最大再生成/f", v); });
            BindSlider(feedSpeedSlider,     0.1f, 5f,   parameters.feedSpeed,         v => { parameters.feedSpeed     = v; UpdateLabel(feedSpeedLabel,     "送り速度",     v); });
        }

        void BindSlider(Slider s, float min, float max, float initial, UnityEngine.Events.UnityAction<float> handler)
        {
            if (s == null) return;
            s.minValue = min;
            s.maxValue = max;
            s.value    = initial;
            s.onValueChanged.AddListener(handler);
        }

        void OnResolutionChanged(int idx)
        {
            int res = Resolutions[idx];
            parameters.fieldResolution = res;
            // chunkSize が res の約数でなければ最小有効値に補正
            if (res % parameters.chunkSize != 0)
            {
                foreach (var cs in ChunkSizes)
                {
                    if (res % cs == 0) { parameters.chunkSize = cs; break; }
                }
            }
            mcRenderer.Rebuild();
        }

        void OnChunkSizeChanged(int idx)
        {
            int cs = ChunkSizes[idx];
            if (parameters.fieldResolution % cs != 0) return; // 不整合はスキップ
            parameters.chunkSize = cs;
            mcRenderer.Rebuild();
        }

        void UpdateLabel(Text label, string name, float val)
        {
            if (label != null) label.text = string.Format("{0}: {1:F1}", name, val);
        }

        void UpdateAllLabels()
        {
            UpdateLabel(drillRadiusLabel,   "掘削半径",     parameters.drillRadius);
            UpdateLabel(drillStrengthLabel, "掘削強度",     parameters.drillStrength);
            UpdateLabel(isoLevelLabel,      "isoLevel",     parameters.isoLevel);
            UpdateLabel(maxChunksLabel,     "最大再生成/f", parameters.maxChunksPerFrame);
            UpdateLabel(feedSpeedLabel,     "送り速度",     parameters.feedSpeed);
        }
    }
}
