using UnityEngine;

namespace ZhuozhengYuan
{
    [System.Serializable]
    public struct Chapter01GateCalibrationViewData
    {
        public string gateName;
        public float currentAngle;
        public float targetAngle;
        public float validAngleTolerance;
        public bool canConfirm;
        public string rotationHint;
        public KeyCode negativeKey;
        public KeyCode positiveKey;
        public KeyCode confirmKey;
        public KeyCode cancelKey;
    }
}
