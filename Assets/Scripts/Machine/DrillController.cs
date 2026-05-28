// 役割: WASD/矢印キーでドリル工具を3軸移動、Space で掘削フラグを立てる
// 依存: GpuDrillingSystem（掘削フラグを参照）
using UnityEngine;
using UnityEngine.InputSystem;

namespace MetalCuttingSim
{
    public class DrillController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 1f;

        public bool IsDrilling { get; private set; }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            float moveZ = 0f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    moveZ =  1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  moveZ = -1f;

            float moveX = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  moveX = -1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveX =  1f;

            float moveY = 0f;
            if (kb.qKey.isPressed) moveY =  1f;
            if (kb.eKey.isPressed) moveY = -1f;

            transform.Translate(
                new Vector3(moveX, moveY, moveZ) * moveSpeed * Time.deltaTime,
                Space.World
            );

            IsDrilling = kb.spaceKey.isPressed;
        }
    }
}
