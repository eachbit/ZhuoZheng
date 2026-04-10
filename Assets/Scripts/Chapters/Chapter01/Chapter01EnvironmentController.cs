using System.Collections;
using System;
using UnityEngine;

namespace ZhuozhengYuan
{
    public class Chapter01EnvironmentController : MonoBehaviour
    {
        private static readonly int TrailBaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int TrailAccentColorId = Shader.PropertyToID("_AccentColor");
        private static readonly int TrailAlphaScaleId = Shader.PropertyToID("_AlphaScale");
        private static readonly int PulseTintId = Shader.PropertyToID("_Tint");
        private static readonly int PulseCoreColorId = Shader.PropertyToID("_CoreColor");
        private static readonly int PulseRingProgressId = Shader.PropertyToID("_RingProgress");
        private static readonly int PulseAlphaScaleId = Shader.PropertyToID("_AlphaScale");

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
        public float selectionPulseStartRadius = 0.28f;
        public float selectionPulseEndRadius = 4.6f;
        public float selectionPulseThickness = 0.06f;
        public float selectionPulseHeight = 0.09f;
        public float selectionPulseDuration = 0.58f;
        public float selectionPulseInterval = 0.12f;
        public float selectionOrbScale = 0.42f;
        public float selectionTravelHeight = 4.8f;
        public float selectionTravelDuration = 1.05f;
        public float selectionSourceYOffset = 1.35f;
        public float selectionTargetYOffset = 1.55f;
        public float rejectedWestTravelRatio = 0.34f;
        public float rejectedSouthTravelRatio = 0.6f;
        public float rejectedLateralOffset = 1.6f;
        public float selectionTrailWidth = 1.05f;
        public float selectionTrailLifetime = 1.25f;
        public float selectionTrailMinVertexDistance = 0.08f;
        public float selectionHeadGlowScale = 1.75f;
        public float selectionSourceFlareScale = 2.8f;
        public float selectionImpactFlareScale = 4.4f;
        public float selectionFlareDuration = 0.9f;
        public float selectionImpactLift = 0.9f;
        public float selectionRejectedImpactScaleMultiplier = 0.72f;
        public float selectionFlashLightRange = 8.5f;
        public float selectionFlashLightIntensity = 4.4f;
        public float selectionFlashLightDuration = 0.58f;

        private string _currentPreviewDirection = string.Empty;
        private Coroutine _selectionFeedbackRoutine;
        private Transform _runtimeFeedbackRoot;

        public void SetDormant()
        {
            ClearRuntimeFeedback();
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
            ClearRuntimeFeedback();
            _currentPreviewDirection = Chapter01FlowDirection.Normalize(direction);

            SetObjectsActive(leftGateCalibrationObjects, false);
            SetObjectsActive(rightGateCalibrationObjects, false);
            SetObjectsActive(dormantObjects, true);
            SetObjectsActive(flowingObjects, false);
            SetObjectsActive(pageRevealAccentObjects, false);
            SetBehavioursEnabled(behavioursEnabledWhenFlowing, false);
            SetAnimatorFlowState(false);
            StopParticles(particlesPlayWhenFlowing);

            bool isWest = string.Equals(_currentPreviewDirection, Chapter01FlowDirection.West, StringComparison.Ordinal);
            bool isSouth = string.Equals(_currentPreviewDirection, Chapter01FlowDirection.South, StringComparison.Ordinal);

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
            ClearRuntimeFeedback();
            _currentPreviewDirection = Chapter01FlowDirection.Center;
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

        public void PlayDirectionSelectionFeedback(string direction, bool isCorrect, Vector3 sourcePosition, Vector3 targetPosition)
        {
            ClearRuntimeFeedback();

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            string normalizedDirection = Chapter01FlowDirection.Normalize(direction);
            _selectionFeedbackRoutine = StartCoroutine(
                PlayDirectionSelectionFeedbackRoutine(
                    normalizedDirection,
                    isCorrect,
                    sourcePosition + (Vector3.up * selectionSourceYOffset),
                    targetPosition + (Vector3.up * selectionTargetYOffset)));
        }

        public void ShowPage()
        {
            SetObjectsActive(pageRevealObjects, true);
        }

        public void HidePage()
        {
            SetObjectsActive(pageRevealObjects, false);
        }

        private IEnumerator PlayDirectionSelectionFeedbackRoutine(string direction, bool isCorrect, Vector3 sourcePosition, Vector3 targetPosition)
        {
            Color feedbackColor = GetFeedbackColor(direction, isCorrect);
            Color accentColor = GetFeedbackAccentColor(feedbackColor, isCorrect);
            int sourcePulseCount = isCorrect ? 4 : 3;
            for (int index = 0; index < sourcePulseCount; index++)
            {
                StartCoroutine(AnimatePulseDisc(sourcePosition, feedbackColor, selectionPulseStartRadius, selectionPulseEndRadius));
                yield return new WaitForSeconds(selectionPulseInterval);
            }

            StartCoroutine(AnimateFlareBurst(sourcePosition + (Vector3.up * selectionImpactLift), feedbackColor, accentColor, selectionSourceFlareScale, selectionFlareDuration));
            StartCoroutine(AnimateFeedbackLight(sourcePosition + (Vector3.up * 0.8f), feedbackColor, selectionFlashLightIntensity, selectionFlashLightRange, selectionFlashLightDuration));

            if (isCorrect)
            {
                yield return AnimateTravelOrb(sourcePosition, targetPosition, feedbackColor, accentColor);
                StartCoroutine(AnimateFlareBurst(targetPosition + (Vector3.up * selectionImpactLift), feedbackColor, accentColor, selectionImpactFlareScale, selectionFlareDuration * 1.25f));
                StartCoroutine(AnimateFeedbackLight(targetPosition + (Vector3.up * 1.1f), feedbackColor, selectionFlashLightIntensity * 1.15f, selectionFlashLightRange * 1.1f, selectionFlashLightDuration * 1.2f));
                for (int index = 0; index < 3; index++)
                {
                    StartCoroutine(AnimatePulseDisc(targetPosition, feedbackColor, selectionPulseStartRadius * 0.75f, selectionPulseEndRadius * 0.7f));
                    yield return new WaitForSeconds(selectionPulseInterval * 0.85f);
                }
            }
            else
            {
                Vector3 rejectedTarget = GetRejectedFeedbackTarget(direction, sourcePosition, targetPosition);
                yield return AnimateTravelOrb(sourcePosition, rejectedTarget, feedbackColor, accentColor);
                StartCoroutine(AnimateFlareBurst(
                    rejectedTarget + (Vector3.up * (selectionImpactLift * 0.8f)),
                    feedbackColor,
                    accentColor,
                    selectionImpactFlareScale * selectionRejectedImpactScaleMultiplier,
                    selectionFlareDuration));
                StartCoroutine(AnimateFeedbackLight(
                    rejectedTarget + (Vector3.up * 0.75f),
                    feedbackColor,
                    selectionFlashLightIntensity * 0.85f,
                    selectionFlashLightRange * 0.8f,
                    selectionFlashLightDuration));

                float rejectedEndRadius = string.Equals(direction, Chapter01FlowDirection.West, StringComparison.Ordinal)
                    ? selectionPulseEndRadius * 0.42f
                    : selectionPulseEndRadius * 0.58f;
                int rejectedBurstCount = string.Equals(direction, Chapter01FlowDirection.West, StringComparison.Ordinal) ? 1 : 2;
                for (int index = 0; index < rejectedBurstCount; index++)
                {
                    StartCoroutine(AnimatePulseDisc(rejectedTarget, feedbackColor, selectionPulseStartRadius * 0.55f, rejectedEndRadius));
                    yield return new WaitForSeconds(selectionPulseInterval * 0.75f);
                }
            }

            _selectionFeedbackRoutine = null;
        }

        private IEnumerator AnimatePulseDisc(Vector3 worldPosition, Color color, float startRadius, float endRadius)
        {
            Transform feedbackRoot = EnsureRuntimeFeedbackRoot();
            GameObject pulseObject = CreateRuntimePrimitive(PrimitiveType.Quad, "FlowPulse", feedbackRoot, color, RuntimeVisualStyle.Pulse);
            pulseObject.transform.position = worldPosition + (Vector3.up * selectionPulseHeight);
            pulseObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            Material pulseMaterial = GetRendererMaterial(pulseObject);
            float elapsed = 0f;
            while (elapsed < selectionPulseDuration)
            {
                if (pulseObject == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / selectionPulseDuration);
                float radius = Mathf.Lerp(startRadius, endRadius, t);
                pulseObject.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

                SetPulseMaterial(pulseMaterial, color, GetFeedbackAccentColor(color, false), Mathf.Lerp(0.18f, 1.12f, t), Mathf.Lerp(0.95f, 0f, t));
                yield return null;
            }

            DestroyRuntimeObject(pulseObject, pulseMaterial);
        }

        private IEnumerator AnimateFlareBurst(Vector3 worldPosition, Color color, Color accentColor, float maxScale, float duration)
        {
            Transform feedbackRoot = EnsureRuntimeFeedbackRoot();
            GameObject flareObject = CreateRuntimePrimitive(PrimitiveType.Quad, "FlowFlare", feedbackRoot, color, RuntimeVisualStyle.Pulse);
            flareObject.transform.position = worldPosition;

            Material flareMaterial = GetRendererMaterial(flareObject);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (flareObject == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                flareObject.transform.rotation = GetCameraFacingRotation(flareObject.transform.position);
                float scale = Mathf.Lerp(maxScale * 0.45f, maxScale, t);
                flareObject.transform.localScale = new Vector3(scale, scale, 1f);
                SetPulseMaterial(flareMaterial, color, accentColor, Mathf.Lerp(0.24f, 0.82f, t), Mathf.Lerp(0.8f, 0f, t));
                yield return null;
            }

            DestroyRuntimeObject(flareObject, flareMaterial);
        }

        private IEnumerator AnimateFeedbackLight(Vector3 worldPosition, Color color, float intensity, float range, float duration)
        {
            Transform feedbackRoot = EnsureRuntimeFeedbackRoot();
            GameObject lightObject = new GameObject("FlowFeedbackLight");
            lightObject.transform.SetParent(feedbackRoot, false);
            lightObject.transform.position = worldPosition;

            Light pointLight = lightObject.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = Color.Lerp(color, Color.white, 0.25f);
            pointLight.intensity = 0f;
            pointLight.range = range;
            pointLight.shadows = LightShadows.None;
            pointLight.renderMode = LightRenderMode.ForcePixel;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (pointLight == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float fade = t < 0.24f
                    ? Mathf.SmoothStep(0f, 1f, t / 0.24f)
                    : Mathf.SmoothStep(1f, 0f, (t - 0.24f) / 0.76f);
                pointLight.intensity = intensity * fade;
                yield return null;
            }

            Destroy(lightObject);
        }

        private IEnumerator AnimateTravelOrb(Vector3 sourcePosition, Vector3 targetPosition, Color color, Color accentColor)
        {
            Transform feedbackRoot = EnsureRuntimeFeedbackRoot();
            GameObject orbObject = new GameObject("FlowOrb");
            orbObject.transform.SetParent(feedbackRoot, false);

            TrailRenderer trailRenderer = orbObject.AddComponent<TrailRenderer>();
            trailRenderer.alignment = LineAlignment.View;
            trailRenderer.widthMultiplier = selectionTrailWidth;
            trailRenderer.time = selectionTrailLifetime;
            trailRenderer.minVertexDistance = selectionTrailMinVertexDistance;
            trailRenderer.numCornerVertices = 6;
            trailRenderer.numCapVertices = 6;
            trailRenderer.textureMode = LineTextureMode.Stretch;
            trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trailRenderer.receiveShadows = false;
            trailRenderer.motionVectorGenerationMode = UnityEngine.MotionVectorGenerationMode.ForceNoMotion;
            trailRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.08f, 0.75f),
                new Keyframe(0.45f, 1f),
                new Keyframe(1f, 0f));

            Gradient trailGradient = new Gradient();
            trailGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(accentColor, 0f),
                    new GradientColorKey(color, 0.38f),
                    new GradientColorKey(Color.Lerp(color, Color.white, 0.18f), 0.78f),
                    new GradientColorKey(color * 0.7f, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.12f, 0f),
                    new GradientAlphaKey(color.a, 0.1f),
                    new GradientAlphaKey(color.a * 0.9f, 0.48f),
                    new GradientAlphaKey(color.a * 0.72f, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                });
            trailRenderer.colorGradient = trailGradient;

            Material trailMaterial = CreateRuntimeMaterial(color, RuntimeVisualStyle.Trail);
            if (trailMaterial != null)
            {
                SetTrailMaterial(trailMaterial, color, accentColor, 1f);
                trailRenderer.sharedMaterial = trailMaterial;
            }

            GameObject headGlowObject = CreateRuntimePrimitive(PrimitiveType.Quad, "FlowHeadGlow", orbObject.transform, accentColor, RuntimeVisualStyle.Pulse);
            headGlowObject.transform.localScale = Vector3.one * selectionHeadGlowScale;
            Material headGlowMaterial = GetRendererMaterial(headGlowObject);
            SetPulseMaterial(headGlowMaterial, color, accentColor, 0.4f, 0.95f);

            Vector3 midPoint = Vector3.Lerp(sourcePosition, targetPosition, 0.5f) + (Vector3.up * selectionTravelHeight);
            float elapsed = 0f;
            while (elapsed < selectionTravelDuration)
            {
                if (orbObject == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / selectionTravelDuration);
                orbObject.transform.position = EvaluateQuadraticBezier(sourcePosition, midPoint, targetPosition, t);
                headGlowObject.transform.rotation = GetCameraFacingRotation(orbObject.transform.position);
                float headAlpha = Mathf.Lerp(1f, 0.45f, Mathf.Clamp01((t - 0.72f) / 0.28f));
                float headScale = Mathf.Lerp(selectionHeadGlowScale, selectionHeadGlowScale * 0.82f, t);
                headGlowObject.transform.localScale = Vector3.one * headScale;
                SetPulseMaterial(headGlowMaterial, color, accentColor, Mathf.Lerp(0.28f, 0.74f, t), headAlpha);
                yield return null;
            }

            trailRenderer.emitting = false;
            DestroyRuntimeObject(headGlowObject, headGlowMaterial);
            DestroyRuntimeObject(orbObject, trailMaterial, Mathf.Max(0.2f, selectionTrailLifetime));
        }

        private void ClearRuntimeFeedback()
        {
            if (_selectionFeedbackRoutine != null)
            {
                StopCoroutine(_selectionFeedbackRoutine);
                _selectionFeedbackRoutine = null;
            }

            if (_runtimeFeedbackRoot == null)
            {
                return;
            }

            for (int index = _runtimeFeedbackRoot.childCount - 1; index >= 0; index--)
            {
                DestroyRuntimeObject(_runtimeFeedbackRoot.GetChild(index).gameObject, null);
            }
        }

        private Transform EnsureRuntimeFeedbackRoot()
        {
            if (_runtimeFeedbackRoot != null)
            {
                return _runtimeFeedbackRoot;
            }

            GameObject rootObject = new GameObject("Chapter01RuntimeFlowFeedbackRoot");
            rootObject.transform.SetParent(transform, false);
            _runtimeFeedbackRoot = rootObject.transform;
            return _runtimeFeedbackRoot;
        }

        private enum RuntimeVisualStyle
        {
            Solid,
            Trail,
            Pulse
        }

        private GameObject CreateRuntimePrimitive(PrimitiveType primitiveType, string objectName, Transform parent, Color color, RuntimeVisualStyle visualStyle)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = objectName;
            primitive.transform.SetParent(parent, false);

            Collider primitiveCollider = primitive.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                Destroy(primitiveCollider);
            }

            Renderer primitiveRenderer = primitive.GetComponent<Renderer>();
            if (primitiveRenderer != null)
            {
                primitiveRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                primitiveRenderer.receiveShadows = false;
                primitiveRenderer.material = CreateRuntimeMaterial(color, visualStyle);
            }

            return primitive;
        }

        private static Material GetRendererMaterial(GameObject runtimeObject)
        {
            if (runtimeObject == null)
            {
                return null;
            }

            Renderer renderer = runtimeObject.GetComponent<Renderer>();
            return renderer != null ? renderer.sharedMaterial : null;
        }

        private static Material CreateRuntimeMaterial(Color color, RuntimeVisualStyle visualStyle)
        {
            Shader shader = null;
            switch (visualStyle)
            {
                case RuntimeVisualStyle.Trail:
                    shader = Shader.Find("ZhuozhengYuan/Chapter01FlowTrail");
                    break;
                case RuntimeVisualStyle.Pulse:
                    shader = Shader.Find("ZhuozhengYuan/Chapter01FlowPulse");
                    break;
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            SetMaterialColor(material, color);
            return material;
        }

        private static void SetTrailMaterial(Material material, Color baseColor, Color accentColor, float alphaScale)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty(TrailBaseColorId))
            {
                material.SetColor(TrailBaseColorId, baseColor);
            }

            if (material.HasProperty(TrailAccentColorId))
            {
                material.SetColor(TrailAccentColorId, accentColor);
            }

            if (material.HasProperty(TrailAlphaScaleId))
            {
                material.SetFloat(TrailAlphaScaleId, alphaScale);
            }
        }

        private static void SetPulseMaterial(Material material, Color tint, Color coreColor, float ringProgress, float alphaScale)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty(PulseTintId))
            {
                material.SetColor(PulseTintId, tint);
            }

            if (material.HasProperty(PulseCoreColorId))
            {
                material.SetColor(PulseCoreColorId, coreColor);
            }

            if (material.HasProperty(PulseRingProgressId))
            {
                material.SetFloat(PulseRingProgressId, ringProgress);
            }

            if (material.HasProperty(PulseAlphaScaleId))
            {
                material.SetFloat(PulseAlphaScaleId, alphaScale);
            }
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_Color"))
            {
                material.color = color;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
        }

        private static void DestroyRuntimeObject(GameObject runtimeObject, Material runtimeMaterial, float delay = 0f)
        {
            if (runtimeObject != null)
            {
                Renderer renderer = runtimeObject.GetComponent<Renderer>();
                if (renderer != null && runtimeMaterial == null)
                {
                    runtimeMaterial = renderer.sharedMaterial;
                }

                TrailRenderer trailRenderer = runtimeObject.GetComponent<TrailRenderer>();
                if (trailRenderer != null && runtimeMaterial == null)
                {
                    runtimeMaterial = trailRenderer.sharedMaterial;
                }

                Destroy(runtimeObject, delay);
            }

            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial, delay);
            }
        }

        private static Vector3 EvaluateQuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
        {
            float oneMinusT = 1f - t;
            return (oneMinusT * oneMinusT * start)
                + (2f * oneMinusT * t * control)
                + (t * t * end);
        }

        private static Color GetFeedbackColor(string direction, bool isCorrect)
        {
            if (isCorrect)
            {
                return new Color(0.45f, 0.93f, 1f, 0.82f);
            }

            switch (Chapter01FlowDirection.Normalize(direction))
            {
                case Chapter01FlowDirection.West:
                    return new Color(1f, 0.77f, 0.35f, 0.78f);
                case Chapter01FlowDirection.South:
                    return new Color(0.56f, 0.92f, 0.66f, 0.78f);
                default:
                    return new Color(1f, 0.68f, 0.68f, 0.78f);
            }
        }

        private static Color GetFeedbackAccentColor(Color baseColor, bool isCorrect)
        {
            float blend = isCorrect ? 0.7f : 0.55f;
            Color accentColor = Color.Lerp(baseColor, Color.white, blend);
            accentColor.a = Mathf.Clamp01(baseColor.a + 0.15f);
            return accentColor;
        }

        private static Quaternion GetCameraFacingRotation(Vector3 worldPosition)
        {
            Camera targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindObjectOfType<Camera>();
            }

            if (targetCamera == null)
            {
                return Quaternion.identity;
            }

            Vector3 direction = targetCamera.transform.position - worldPosition;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return targetCamera.transform.rotation;
            }

            return Quaternion.LookRotation(-direction.normalized, targetCamera.transform.up);
        }

        private Vector3 GetRejectedFeedbackTarget(string direction, Vector3 sourcePosition, Vector3 intendedTargetPosition)
        {
            string normalizedDirection = Chapter01FlowDirection.Normalize(direction);
            float travelRatio = string.Equals(normalizedDirection, Chapter01FlowDirection.West, StringComparison.Ordinal)
                ? rejectedWestTravelRatio
                : rejectedSouthTravelRatio;

            Vector3 towardTarget = Vector3.Lerp(sourcePosition, intendedTargetPosition, Mathf.Clamp01(travelRatio));
            Vector3 lateralOffset = Vector3.zero;

            if (string.Equals(normalizedDirection, Chapter01FlowDirection.West, StringComparison.Ordinal))
            {
                lateralOffset = Vector3.left * rejectedLateralOffset;
            }
            else if (string.Equals(normalizedDirection, Chapter01FlowDirection.South, StringComparison.Ordinal))
            {
                lateralOffset = Vector3.right * (rejectedLateralOffset * 0.65f);
            }

            return towardTarget + lateralOffset;
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
