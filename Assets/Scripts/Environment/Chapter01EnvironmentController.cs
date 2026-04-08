using UnityEngine;

namespace ZhuozhengYuan
{
    public class Chapter01EnvironmentController : MonoBehaviour
    {
        public GameObject[] dormantObjects;
        public GameObject[] flowingObjects;
        public GameObject[] pageRevealObjects;
        public Behaviour[] behavioursEnabledWhenFlowing;
        public ParticleSystem[] particlesPlayWhenFlowing;
        public Animator[] animatorsWithFlowBool;
        public string flowBoolName = "IsFlowing";

        public void SetDormant()
        {
            SetObjectsActive(dormantObjects, true);
            SetObjectsActive(flowingObjects, false);
            SetBehavioursEnabled(behavioursEnabledWhenFlowing, false);
            SetAnimatorFlowState(false);
            StopParticles();
            HidePage();
        }

        public void SetFlowing()
        {
            SetObjectsActive(dormantObjects, false);
            SetObjectsActive(flowingObjects, true);
            SetBehavioursEnabled(behavioursEnabledWhenFlowing, true);
            SetAnimatorFlowState(true);
            PlayParticles();
        }

        public void ShowPage()
        {
            SetObjectsActive(pageRevealObjects, true);
        }

        public void HidePage()
        {
            SetObjectsActive(pageRevealObjects, false);
        }

        private void PlayParticles()
        {
            if (particlesPlayWhenFlowing == null)
            {
                return;
            }

            for (int index = 0; index < particlesPlayWhenFlowing.Length; index++)
            {
                if (particlesPlayWhenFlowing[index] != null)
                {
                    particlesPlayWhenFlowing[index].Play(true);
                }
            }
        }

        private void StopParticles()
        {
            if (particlesPlayWhenFlowing == null)
            {
                return;
            }

            for (int index = 0; index < particlesPlayWhenFlowing.Length; index++)
            {
                if (particlesPlayWhenFlowing[index] != null)
                {
                    particlesPlayWhenFlowing[index].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
