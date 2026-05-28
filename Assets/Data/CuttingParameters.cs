// 役割: 切削シミュレーション全体の調整可能なパラメータを ScriptableObject で外出し
// 依存: なし
using UnityEngine;

namespace MetalCuttingSim
{
    public enum ToolShape { Sphere = 0, Capsule = 1, FlatEndMill = 2, Cone = 3 }

    [CreateAssetMenu(fileName = "CuttingParameters", menuName = "MetalCuttingSim/CuttingParameters")]
    public class CuttingParameters : ScriptableObject
    {
        [Header("工具形状")]
        public ToolShape toolShape   = ToolShape.Sphere;

        [Header("密度フィールド")]
        public int   fieldResolution = 64;      // 全体解像度（chunkSize の倍数であること）
        public float fieldWorldSize  = 2.0f;    // ワールド空間でのブロックサイズ（m）

        [Header("チャンク")]
        public int chunkSize         = 64;      // Phase A: 64=1チャンク。PhaseB で 16 に変更
        public int maxChunksPerFrame = 8;       // 通常フレームで再生成する上限チャンク数

        [Header("掘削（球状）")]
        public float drillRadius   = 2.5f;      // 掘削半径（ボクセル単位）
        public float drillStrength = 5.0f;      // 1秒あたりの密度減算量

        [Header("掘削（円柱）拡張用）")]
        public float drillLength   = 4.0f;

        [Header("ドリル移動")]
        public float drillMoveSpeed = 1.0f;
        public float feedSpeed      = 0.5f;
        public float spindleRPM     = 1200f;

        [Header("切削物理モデル")]
        public float cuttingForceCoeff = 50f;
        public float thermalCoeff      = 0.3f;
        public float maxTemperature    = 1200f;

        [Header("Marching Cubes")]
        public float isoLevel = 0.5f;

        public int ChunkDim => fieldResolution / chunkSize;
        public int TotalChunks => ChunkDim * ChunkDim * ChunkDim;
    }
}
