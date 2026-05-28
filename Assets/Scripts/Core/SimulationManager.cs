// 役割: シミュレーション全体のライフサイクル管理。サブシステムの初期化順序を保証する。
// 依存: CuttingParameters（段階2以降: VoxelDensityField, GpuDrillingSystem, MarchingCubesRenderer を追加）
using UnityEngine;

namespace MetalCuttingSim
{
    public class SimulationManager : MonoBehaviour
    {
        [SerializeField] private CuttingParameters parameters;

        void Awake()
        {
            if (parameters == null)
            {
                Debug.LogError("[SimulationManager] CuttingParameters が未設定です。Inspector で割り当ててください。");
                return;
            }

            Debug.Log($"[SimulationManager] 初期化完了。" +
                      $"解像度={parameters.fieldResolution}^3, " +
                      $"ワールドサイズ={parameters.fieldWorldSize}m");
        }
    }
}
