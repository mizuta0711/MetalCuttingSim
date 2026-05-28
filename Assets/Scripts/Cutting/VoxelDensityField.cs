// 役割: GPU 密度フィールド（ComputeBuffer）とチャンク dirty 管理を担当
// 依存: CuttingParameters
using System.Collections.Generic;
using UnityEngine;

namespace MetalCuttingSim
{
    public class VoxelDensityField : MonoBehaviour
    {
        [SerializeField] private CuttingParameters parameters;
        [SerializeField] private ComputeShader densityShader;

        public ComputeBuffer DensityBuffer { get; private set; }
        public int Resolution  => parameters.fieldResolution;
        public int ChunkDim    => parameters.ChunkDim;
        public int TotalChunks => parameters.TotalChunks;

        // dirty キュー（MarchingCubesRenderer が消費する）
        public Queue<int> DirtyQueue => _dirtyQueue;
        public int DirtyCount        => _dirtyQueue.Count;

        private bool[]   _dirtyFlags;
        private Queue<int> _dirtyQueue = new Queue<int>();
        private int _kernelFill;

        void Awake() => Initialize();

        public void Initialize()
        {
            _kernelFill = densityShader.FindKernel("CSFill");

            DensityBuffer?.Release();
            int total = parameters.fieldResolution * parameters.fieldResolution * parameters.fieldResolution;
            DensityBuffer = new ComputeBuffer(total, sizeof(float));

            FillDensity();
            InitChunkTracking();

            Debug.Log($"[VoxelDensityField] 初期化完了。" +
                      $"解像度={parameters.fieldResolution}^3, " +
                      $"チャンク={ChunkDim}^3={TotalChunks}個");
        }

        void FillDensity()
        {
            int res = parameters.fieldResolution;
            densityShader.SetBuffer(_kernelFill, "_DensityField", DensityBuffer);
            densityShader.SetInt("_Resolution", res);
            int g = Mathf.CeilToInt(res / 8f);
            densityShader.Dispatch(_kernelFill, g, g, g);
        }

        void InitChunkTracking()
        {
            _dirtyFlags = new bool[TotalChunks];
            _dirtyQueue = new Queue<int>();
            MarkAllDirty();
        }

        // ────────── チャンク dirty API ──────────

        public void MarkAllDirty()
        {
            _dirtyQueue.Clear();
            for (int i = 0; i < TotalChunks; i++)
            {
                _dirtyFlags[i] = true;
                _dirtyQueue.Enqueue(i);
            }
        }

        public void MarkChunkDirty(int chunkIdx)
        {
            if (chunkIdx < 0 || chunkIdx >= TotalChunks) return;
            if (_dirtyFlags[chunkIdx]) return;
            _dirtyFlags[chunkIdx] = true;
            _dirtyQueue.Enqueue(chunkIdx);
        }

        public void ClearDirtyFlag(int chunkIdx) => _dirtyFlags[chunkIdx] = false;

        // ────────── チャンクインデックス変換 ──────────

        public int ChunkIndex(int cx, int cy, int cz)
            => cx + cy * ChunkDim + cz * ChunkDim * ChunkDim;

        public Vector3Int ChunkOriginVoxel(int chunkIdx)
        {
            int dim = ChunkDim;
            int cz  = chunkIdx / (dim * dim);
            int cy  = (chunkIdx / dim) % dim;
            int cx  = chunkIdx % dim;
            return new Vector3Int(cx, cy, cz) * parameters.chunkSize;
        }

        void OnDestroy() => DensityBuffer?.Release();
    }
}
