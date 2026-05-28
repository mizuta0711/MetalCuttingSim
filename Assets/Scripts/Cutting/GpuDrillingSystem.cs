// 役割: DrillController の位置を受け取り、CSDrill で密度フィールドを掘削し、影響チャンクを dirty にする
// 依存: CuttingParameters, VoxelDensityField, DrillController
using UnityEngine;

namespace MetalCuttingSim
{
    public class GpuDrillingSystem : MonoBehaviour
    {
        [SerializeField] private CuttingParameters parameters;
        [SerializeField] private VoxelDensityField densityField;
        [SerializeField] private DrillController   drillController;
        [SerializeField] private Transform         drillTip;       // 先端の空オブジェクト。未設定時はdrillController中心
        [SerializeField] private ComputeShader     densityShader;
        [SerializeField] private Vector3           blockCenter = Vector3.zero;

        public float LastRemovedVolume { get; private set; }
        public float CuttingForce      { get; private set; }
        public float Temperature       { get; private set; }

        private int _kernelDrill;

        void Awake()
        {
            _kernelDrill = densityShader.FindKernel("CSDrill");
        }

        void Update()
        {
            if (!drillController.IsDrilling)
            {
                LastRemovedVolume = 0f;
                CuttingForce      = 0f;
                // 自然冷却
                Temperature = Mathf.Max(0f, Temperature - parameters.thermalCoeff * 10f * Time.deltaTime);
                return;
            }
            DispatchDrill();
        }

        void DispatchDrill()
        {
            int res      = densityField.Resolution;
            float ws     = parameters.fieldWorldSize;
            Vector3 bMin = blockCenter - Vector3.one * ws * 0.5f;

            Vector3 drillWorld = drillTip != null ? drillTip.position : drillController.transform.position;
            Vector3 drillVoxel = (drillWorld - bMin) / ws * res;

            float radiusVoxel = parameters.drillRadius;
            float strength    = parameters.drillStrength * Time.deltaTime;

            // 先端→胴体方向（isotropic ボクセルなのでワールド方向 = ボクセル方向）
            Vector3 axisWorld = Vector3.back;
            if (drillTip != null)
                axisWorld = (drillController.transform.position - drillTip.position).normalized;

            densityShader.SetBuffer(_kernelDrill, "_DensityField", densityField.DensityBuffer);
            densityShader.SetInt("_Resolution",          res);
            densityShader.SetVector("_DrillPosVoxel",    drillVoxel);
            densityShader.SetVector("_DrillAxisVoxel",   axisWorld);
            densityShader.SetFloat("_DrillRadius",       radiusVoxel);
            densityShader.SetFloat("_DrillLength",       parameters.drillLength);
            densityShader.SetFloat("_DrillStrength",     strength);
            densityShader.SetInt("_ToolShape",           (int)parameters.toolShape);

            int g = Mathf.CeilToInt(res / 8f);
            densityShader.Dispatch(_kernelDrill, g, g, g);

            // 非球形状はドリル長さ分だけ dirty 範囲を拡げる
            float effectiveRadius = radiusVoxel;
            if (parameters.toolShape != ToolShape.Sphere)
                effectiveRadius += parameters.drillLength;
            MarkDirtyChunks(drillVoxel, effectiveRadius);

            // HUD: 除去体積概算（球体積 × strength）
            LastRemovedVolume = (4f / 3f) * Mathf.PI * Mathf.Pow(radiusVoxel, 3f) * strength;

            // HUD: 切削抵抗・温度（簡易物理モデル）
            CuttingForce = parameters.cuttingForceCoeff * LastRemovedVolume / Mathf.Max(Time.deltaTime, 1e-5f);
            Temperature  = Mathf.Min(
                Temperature + parameters.thermalCoeff * CuttingForce * Time.deltaTime,
                parameters.maxTemperature
            );
        }

        void MarkDirtyChunks(Vector3 drillVoxel, float radiusVoxel)
        {
            int cs  = parameters.chunkSize;
            int dim = densityField.ChunkDim;

            for (int cz = 0; cz < dim; cz++)
            for (int cy = 0; cy < dim; cy++)
            for (int cx = 0; cx < dim; cx++)
            {
                float ox = cx * cs, oy = cy * cs, oz = cz * cs;
                float dx = Mathf.Max(ox - drillVoxel.x, 0f, drillVoxel.x - (ox + cs));
                float dy = Mathf.Max(oy - drillVoxel.y, 0f, drillVoxel.y - (oy + cs));
                float dz = Mathf.Max(oz - drillVoxel.z, 0f, drillVoxel.z - (oz + cs));

                if (dx * dx + dy * dy + dz * dz <= radiusVoxel * radiusVoxel)
                    densityField.MarkChunkDirty(densityField.ChunkIndex(cx, cy, cz));
            }
        }
    }
}
