// 役割: 計測 HUD（FPS/ms, チャンク統計, 三角形数, 切削物理）を毎フレーム更新する
using UnityEngine;
using UnityEngine.UI;

namespace MetalCuttingSim
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private Text                  hudText;
        [SerializeField] private MarchingCubesRenderer mcRenderer;
        [SerializeField] private GpuDrillingSystem     drillSystem;

        private float _timeAccum;
        private int   _frameCount;
        private float _fps;

        void Update()
        {
            _timeAccum += Time.unscaledDeltaTime;
            _frameCount++;
            if (_timeAccum >= 0.5f)
            {
                _fps       = _frameCount / _timeAccum;
                _timeAccum = 0f;
                _frameCount = 0;
            }

            if (hudText == null) return;

            int  total = mcRenderer != null ? mcRenderer.TotalChunks : 0;
            int  regen = mcRenderer != null ? mcRenderer.ChunksRegeneratedThisFrame : 0;
            int  dirty = mcRenderer != null ? mcRenderer.DirtyBacklog : 0;
            long tris  = mcRenderer != null ? mcRenderer.TotalActiveTriangles : 0L;
            float vol  = drillSystem != null ? drillSystem.LastRemovedVolume : 0f;
            float force= drillSystem != null ? drillSystem.CuttingForce : 0f;
            float temp = drillSystem != null ? drillSystem.Temperature : 0f;

            hudText.text = string.Format(
                "<b>FPS:</b> {0:F1}  <b>dt:</b> {1:F2}ms\n" +
                "<b>チャンク:</b> {2} 総 / {3} 再生成 / {4} dirty残\n" +
                "<b>三角形:</b> {5:N0}\n" +
                "<b>除去量:</b> {6:F5}\n" +
                "<b>切削抵抗:</b> {7:F0} N  <b>温度:</b> {8:F0}°C",
                _fps,
                Time.deltaTime * 1000f,
                total, regen, dirty,
                tris, vol, force, temp
            );
        }
    }
}
