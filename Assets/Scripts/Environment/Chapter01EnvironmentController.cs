using UnityEngine;

namespace ZhuozhengYuan
{
    public class Chapter01EnvironmentController : MonoBehaviour
    {
        public GameObject[] dormantObjects;
        public GameObject[] westPreviewObjects;
        public GameObject[] southPreviewObjects;
        public GameObject[] flowingObjects;
        public GameObject[] pageRevealObjects;
        public GameObject[] pageRevealAccentObjects;
        public GameObject[] leftGateCalibrationObjects;
        public GameObject[] rightGateCalibrationObjects;
        public GameObject[] leftGateSolvedObjects;
        public GameObject[] rightGateSolvedObjects;
        public Behaviour[] behavioursEnabledWhenFlowing;
        public Behaviour[] behavioursEnabledWhenSouthPreview;
        public ParticleSystem[] particlesPlayWhenFlowing;
        public ParticleSystem[] particlesPlayWhenSouthPreview;
        public Animator[] animatorsWithFlowBool;
        public string flowBoolName = "IsFlowing";

        private string _currentPreviewDirection = string.Empty;

        public void SetDormant()
        {
            _currentPreviewDirection = string.Empty;
            SetObjectsActive(leftGateCalibrationObjects, false);
            SetObjectsActive(rightGateCalibrationObjects, false);
            SetObjectsActive(dormantObjects, true);
            SetObjectsActive(westPreviewObjects, false);
            SetObjectsActive(southPreviewObjects, false);
            SetObjectsActive(flowingObjects, false);
            SetObjectsActive(pageRevealAccentObjects, false);
            SetBehavioursEnabled(behavioursEnabledWhenFlowing, false);
            SetBehavioursEnabled(behavioursEnabledWhenSouthPreview, false);
            SetAnimatorFlowState(false);
            StopParticles(particlesPlayWhenFlowing);
            StopParticles(particlesPlayWhenSouthPreview);
            HidePage();
        }

        public void SetFlowing()
        {
            SetFlowingSolved();
        }

        public void SetDirectionPreview(string direction)
        {
            _currentPreviewDirection = direction ?? string.Empty;

            SetObjectsActive(leftGateCalibrationObjects, false);
            SetObjectsActive(rightGateCalibrationObjects, false);
            SetObjectsActive(dormantObjects, true);
            SetObjectsActive(flowingObjects, false);
            SetObjectsActive(pageRevealAccentObjects, false);
            SetBehavioursEnabled(behavioursEnabledWhenFlowing, false);
            SetAnimatorFlowState(false);
            StopParticles(particlesPlayWhenFlowing);

            bool isWest = string.Equals(_currentPreviewDirection, "西", System.StringComparison.Ordinal);
            bool isSouth = string.Equals(_currentPreviewDirection, "南", System.StringComparison.Ordinal);

            SetObjectsActive(westPreviewObjects, isWest);
            SetObjectsActive(southPreviewObjects, isSouth);
            SetBehavioursEnabled(behavioursEnabledWhenSouthPreview, isSouth);

            if (isSouth)
            {
                PlayParticles(particlesPlayWhenSouthPreview);
            }
            else
            {
                StopParticles(particlesPlayWhenSouthPreview);
            }

            HidePage();
        }

        public void SetFlowingSolved()
        {
            _currentPreviewDirection = "东";
            SetObjectsActive(leftGateCalibrationObjects, false);
            SetObjectsActive(rightGateCalibrationObjects, false);
            SetObjectsActive(dormantObjects, false);
            SetObjectsActive(westPreviewObjects, false);
            SetObjectsActive(southPreviewObjects, false);
            SetObjectsActive(flowingObjects, true);
            SetObjectsActive(pageRevealAccentObjects, true);
            SetBehavioursEnabled(behavioursEnabledWhenFlowing, true);
            SetBehavioursEnabled(behavioursEnabledWhenSouthPreview, false);
            SetAnimatorFlowState(true);
            StopParticles(particlesPlayWhenSouthPreview);
            PlayParticles(particlesPlayWhenFlowing);
        }

        public void OnGateCalibrationStarted(GateId gateId)
        {
            SetObjectsActive(leftGateCalibrationObjects, gateId == GateId.Left);
            SetObjectsActive(rightGateCalibrationObjects, gateId == GateId.Right);
        }

        public void OnGateSolved(GateId gateId)
        {
            if (gateId == GateId.Left)
            {
                SetObjectsActive(leftGateCalibrationObjects, false);
                SetObjectsActive(leftGateSolvedObjects, true);
            }
            else
            {
                SetObjectsActive(rightGateCalibrationObjects, false);
                SetObjectsActive(rightGateSolvedObjects, true);
            }
        }

        public void OnPageRevealed()
        {
            SetObjectsActive(pageRevealAccentObjects, true);
            ShowPage();
        }

        public void ShowPage()
        {
            SetObjectsActive(pageRevealObjects, true);
        }

        public void HidePage()
        {
            SetObjectsActive(pageRevealObjects, false);
        }

        private void PlayParticles(ParticleSystem[] particleSystems)
        {
            if (particleSystems == null)
            {
                return;
            }

            for (int index = 0; index < particleSystems.Length; index++)
            {
                if (particleSystems[index] != null)
                {
                    particleSystems[index].Play(true);
                }
            }
        }

        private void StopParticles(ParticleSystem[] particleSystems)
        {
            if (particleSystems == null)
            {
                return;
            }

            for (int index = 0; index < particleSystems.Length; index++)
            {
                if (particleSystems[index] != null)
                {
                    particleSystems[index].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        private void SetAnimatorFlowState(bool isFlowing)
        {
            if (animatorsWithFlowBool == null || string.IsNullOrEmpty(flowBoolName))
            {
                return;
            }

            for (int index = 0; index < animatorsWithFlowBool.Length; index++)
            {
                Animator animator = animatorsWithFlowBool[index];
                if (animator == null)
                {
                    continue;
                }

                if (HasParameter(animator, flowBoolName))
                {
                    animator.SetBool(flowBoolName, isFlowing);
                }
            }
        }

        private static bool HasParameter(Animator animator, string parameterName)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int index = 0; index < parameters.Length; index++)
            {
                if (parameters[index].name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetObjectsActive(GameObject[] gameObjects, bool active)
        {
            if (gameObjects == null)
            {
                return;
            }

            for (int index = 0; index < gameObjects.Length; index++)
            {
                if (gameObjects[index] != null)
                {
                    gameObjects[index].SetActive(active);
                }
            }
        }

        private static void SetBehavioursEnabled(Behaviour[] behaviours, bool enabled)
        {
            if (behaviours == null)
            {
                return;
            }

            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] != null)
                {
                    behaviours[index].enabled = enabled;
                }
            }
        }
    }
}
