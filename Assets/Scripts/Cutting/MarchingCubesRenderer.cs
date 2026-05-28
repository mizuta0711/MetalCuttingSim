// 役割: チャンク単位の Marching Cubes 実行とメッシュ描画。dirty チャンクのみ再生成し GPU 描画する。
// 依存: CuttingParameters, VoxelDensityField, MarchingCubes.compute, MetalSurface material
using UnityEngine;
using UnityEngine.Rendering;

namespace MetalCuttingSim
{
    public class MarchingCubesRenderer : MonoBehaviour
    {
        [SerializeField] private CuttingParameters parameters;
        [SerializeField] private VoxelDensityField densityField;
        [SerializeField] private ComputeShader     marchingCubesShader;
        [SerializeField] private Material          metalMaterial;
        [SerializeField] private Vector3           blockCenter = Vector3.zero;
        [SerializeField] private GameObject        placeholderBlock;

        public int  TotalChunks               => densityField != null ? densityField.TotalChunks : 0;
        public int  ChunksRegeneratedThisFrame { get; private set; }
        public int  DirtyBacklog              => densityField != null ? densityField.DirtyCount : 0;
        public long TotalActiveTriangles       { get; private set; }

        private ComputeBuffer[]   _triBuffers;
        private ComputeBuffer[]   _argsBuffers;
        private long[]            _chunkTriCounts;
        private uint[]            _argReadback = new uint[1];
        private MaterialPropertyBlock _mpb;
        private int _kernelMC;
        private int _kernelArgs;
        private Bounds _bounds;
        private bool _startupDone;

        const int TRIANGLE_STRIDE = 36;

        void Awake()
        {
            _kernelMC   = marchingCubesShader.FindKernel("CSMarchingCubes");
            _kernelArgs = marchingCubesShader.FindKernel("CSFillIndirectArgs");
            _mpb        = new MaterialPropertyBlock();

            AllocateBuffers();
            UpdateBounds();

            if (placeholderBlock != null) placeholderBlock.SetActive(false);
        }

        // ─────────── バッファ管理 ───────────

        void AllocateBuffers()
        {
            FreeBuffers();
            int total           = densityField.TotalChunks;
            int cs              = parameters.chunkSize;
            int maxTrisPerChunk = cs * cs * cs * 5;

            _triBuffers     = new ComputeBuffer[total];
            _argsBuffers    = new ComputeBuffer[total];
            _chunkTriCounts = new long[total];

            for (int i = 0; i < total; i++)
            {
                _triBuffers[i]  = new ComputeBuffer(maxTrisPerChunk, TRIANGLE_STRIDE, ComputeBufferType.Append);
                _argsBuffers[i] = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);
                _argsBuffers[i].SetData(new uint[] { 0u, 1u, 0u, 0u });
            }

            _startupDone     = false;
            TotalActiveTriangles = 0;
            Debug.Log($"[MCRenderer] バッファ確保: {total} チャンク, maxTris/chunk={maxTrisPerChunk}");
        }

        void FreeBuffers()
        {
            if (_triBuffers  != null) foreach (var b in _triBuffers)  b?.Release();
            if (_argsBuffers != null) foreach (var b in _argsBuffers) b?.Release();
            _triBuffers = _argsBuffers = null;
        }

        void UpdateBounds()
        {
            _bounds = new Bounds(blockCenter, Vector3.one * parameters.fieldWorldSize * 1.5f);
        }

        // ─────────── メインループ ───────────

        void LateUpdate()
        {
            ProcessDirtyChunks();
            RenderAllChunks();
        }

        void ProcessDirtyChunks()
        {
            int budget = _startupDone ? parameters.maxChunksPerFrame : densityField.TotalChunks;
            var queue  = densityField.DirtyQueue;
            int count  = 0;

            while (queue.Count > 0 && count < budget)
            {
                int idx = queue.Dequeue();
                densityField.ClearDirtyFlag(idx);
                RegenerateChunk(idx);
                count++;
            }

            ChunksRegeneratedThisFrame = count;
            if (!_startupDone && queue.Count == 0)
            {
                _startupDone = true;
                // 全チャンク完了時に三角形総数を確定
                long sum = 0;
                foreach (var c in _chunkTriCounts) sum += c;
                TotalActiveTriangles = sum;
            }
            else if (count > 0)
            {
                long sum = 0;
                foreach (var c in _chunkTriCounts) sum += c;
                TotalActiveTriangles = sum;
            }
        }

        void RegenerateChunk(int chunkIdx)
        {
            Vector3Int origin = densityField.ChunkOriginVoxel(chunkIdx);
            int   cs    = parameters.chunkSize;
            int   res   = densityField.Resolution;
            float ws    = parameters.fieldWorldSize;
            float vSize = ws / (res - 1);
            Vector3 bMin = blockCenter - Vector3.one * ws * 0.5f;

            _triBuffers[chunkIdx].SetCounterValue(0);

            marchingCubesShader.SetBuffer(_kernelMC, "_DensityField", densityField.DensityBuffer);
            marchingCubesShader.SetBuffer(_kernelMC, "_Triangles",    _triBuffers[chunkIdx]);
            marchingCubesShader.SetInt("_Resolution",   res);
            marchingCubesShader.SetInt("_ChunkSize",    cs);
            marchingCubesShader.SetInt("_ChunkOffsetX", origin.x);
            marchingCubesShader.SetInt("_ChunkOffsetY", origin.y);
            marchingCubesShader.SetInt("_ChunkOffsetZ", origin.z);
            marchingCubesShader.SetFloat("_IsoLevel",   parameters.isoLevel);
            marchingCubesShader.SetFloat("_VoxelSize",  vSize);
            marchingCubesShader.SetVector("_BlockMin",  bMin);

            int groups = Mathf.CeilToInt(cs / 8f);
            marchingCubesShader.Dispatch(_kernelMC, groups, groups, groups);

            ComputeBuffer.CopyCount(_triBuffers[chunkIdx], _argsBuffers[chunkIdx], 0);

            // 三角形数を同期読み取り（HUD 用）
            _argsBuffers[chunkIdx].GetData(_argReadback, 0, 0, 1);
            _chunkTriCounts[chunkIdx] = _argReadback[0];

            marchingCubesShader.SetBuffer(_kernelArgs, "_IndirectArgs", _argsBuffers[chunkIdx]);
            marchingCubesShader.Dispatch(_kernelArgs, 1, 1, 1);
        }

        void RenderAllChunks()
        {
            if (_triBuffers == null) return;
            for (int i = 0; i < _triBuffers.Length; i++)
            {
                _mpb.SetBuffer("_Triangles", _triBuffers[i]);
                Graphics.DrawProceduralIndirect(
                    metalMaterial, _bounds, MeshTopology.Triangles,
                    _argsBuffers[i], 0, null, _mpb,
                    ShadowCastingMode.On, true
                );
            }
        }

        // ─────────── 外部から呼ぶ再構築 API ───────────

        /// <summary>density フィールドを含む完全再初期化（解像度・チャンクサイズ変更時）</summary>
        public void Rebuild()
        {
            densityField.Initialize();
            AllocateBuffers();
            UpdateBounds();
        }

        /// <summary>密度はそのまま、MC だけ全チャンク再実行（isoLevel 変更時）</summary>
        public void RemeshAll()
        {
            densityField.MarkAllDirty();
        }

        void OnDestroy() => FreeBuffers();
    }
}
