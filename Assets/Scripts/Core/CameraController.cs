// 役割: 金属ブロックを注視点としたオービットカメラ（右ドラッグ回転、スクロールズーム）
// 依存: なし（Main Camera にアタッチ）
using UnityEngine;
using UnityEngine.InputSystem;

namespace MetalCuttingSim
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Vector3 orbitTarget = Vector3.zero;
        [SerializeField] private float distance      = 5f;
        // 画面幅を1とした時の感度。1.0 = 画面端まで動かすと360度回転
        [SerializeField] private float orbitSensitivity = 0.25f;
        [SerializeField] private float zoomSpeed         = 0.01f;   // m per scroll unit (120 = 1 notch)
        [SerializeField] private float minDistance   = 0.5f;
        [SerializeField] private float maxDistance   = 15f;

        private float _yaw   = 45f;
        private float _pitch = 30f;

        void Start()
        {
            UpdateTransform();
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                // スクリーン幅で割って 0~1 に正規化 → 360度スケール
                float scale = 360f / Mathf.Max(Screen.width, 1);
                _yaw   += delta.x * scale * orbitSensitivity;
                _pitch -= delta.y * scale * orbitSensitivity;
                _pitch  = Mathf.Clamp(_pitch, -85f, 85f);
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.001f)
                distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);

            UpdateTransform();
        }

        private void UpdateTransform()
        {
            var rot = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.position = orbitTarget + rot * new Vector3(0f, 0f, -distance);
            transform.LookAt(orbitTarget);
        }
    }
}
