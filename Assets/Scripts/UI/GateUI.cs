using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

// 必须加这个命名空间，否则找不到你的脚本！
namespace ZhuozhengYuan
{
    public class GateUI : MonoBehaviour
    {
        [Header("绑定你的水闸脚本")]
        public GateInteractable gate;

        [Header("UI 显示")]
        public TextMeshProUGUI angleText;
        public TextMeshProUGUI tipText;
        public UnityEngine.UI.Image progressFill;

        [Header("校准设置")]
        public float targetAngle;
        public float tolerance = 9f;

        void Start()
        {
            // 自动获取目标角度（你脚本里写死的 55 和 -70）
            targetAngle = gate.ResolvedTargetAngle;
        }

        void Update()
        {
            if (gate == null) return;

            ShowCurrentAngle();
            ShowTipAndProgress();
        }

        // 显示当前角度
        void ShowCurrentAngle()
        {
            float angle = gate.CurrentAngle;
            angleText.text = $"{angle:F0}°";
        }

        // 显示提示 + 进度条
        void ShowTipAndProgress()
        {
            bool inRange = gate.IsWithinCalibrationTolerance();

            if (inRange)
            {
                tipText.text = "✅ 校准成功！按 E 确认开启水闸";
                if (progressFill != null)
                    progressFill.fillAmount = 1;
            }
            else
            {
                tipText.text = $"🔧 请调整至 {targetAngle:F0}°";
                if (progressFill != null)
                    progressFill.fillAmount = 0;
            }
        }
    }
}