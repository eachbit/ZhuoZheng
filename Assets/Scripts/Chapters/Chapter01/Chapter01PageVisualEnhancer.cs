using UnityEngine;

namespace ZhuozhengYuan
{
    public class Chapter01PageVisualEnhancer : MonoBehaviour
    {
        public const string RuntimeRootName = "Chapter01EnhancedPageVisual";

        [Header("Optional downloaded model")]
        public GameObject externalPageModel;

        [Header("Generated visual")]
        public Vector3 visualLocalOffset = new Vector3(0f, 0.48f, 0f);
        public Vector3 visualLocalEuler = new Vector3(8f, 0f, -4f);
        public float visualScale = 0.92f;
        public float bobAmplitude = 0.075f;
        public float bobSpeed = 1.55f;
        public float rotationSpeed = 24f;
        public float swayAngle = 4.5f;
        public Color parchmentColor = new Color(0.78f, 0.62f, 0.39f, 1f);
        public Color inkColor = new Color(0.18f, 0.11f, 0.07f, 1f);
        public Color sealColor = new Color(0.55f, 0.07f, 0.03f, 1f);
        public Color glowColor = new Color(1f, 0.78f, 0.34f, 1f);
        public bool hideLegacyRenderer = true;

        private Transform _visualRoot;
        private Material _pageMaterial;
        private Material _inkMaterial;
        private Material _sealMaterial;
        private Material _particleMaterial;
        private float _phase;

        public bool HasVisual => _visualRoot != null;

        private void Awake()
        {
            EnsureVisual();
        }

        private void OnEnable()
        {
            EnsureVisual();
        }

        private void Update()
        {
            if (_visualRoot == null)
            {
                return;
            }

            float time = Time.time + _phase;
            Vector3 bobbedOffset = visualLocalOffset + (Vector3.up * (Mathf.Sin(time * bobSpeed) * bobAmplitude));
            Quaternion baseRotation = Quaternion.Euler(visualLocalEuler);
            Quaternion spinRotation = Quaternion.Euler(0f, time * rotationSpeed, Mathf.Sin(time * bobSpeed * 0.8f) * swayAngle);

            ApplyWorldVisualTransform(bobbedOffset, baseRotation * spinRotation);
        }

        public void EnsureVisual()
        {
            HideLegacyVisuals();

            if (_visualRoot != null)
            {
                return;
            }

            Transform existingRoot = transform.Find(RuntimeRootName);
            if (existingRoot != null)
            {
                _visualRoot = existingRoot;
                return;
            }

            _phase = Random.Range(0f, 9f);
            _visualRoot = new GameObject(RuntimeRootName).transform;
            _visualRoot.SetParent(transform, false);
            ApplyWorldVisualTransform(visualLocalOffset, Quaternion.Euler(visualLocalEuler));

            if (externalPageModel != null)
            {
                GameObject instance = Instantiate(externalPageModel, _visualRoot);
                instance.name = "DownloadedTornPageModel";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
            }
            else
            {
                CreateGeneratedTornPage(_visualRoot);
            }

            CreateWarmLight(_visualRoot);
            CreateDustParticles(_visualRoot);
        }

        public void SetVisible(bool visible)
        {
            if (visible)
            {
                EnsureVisual();
                HideLegacyVisuals();
            }

            if (_visualRoot != null)
            {
                _visualRoot.gameObject.SetActive(visible);
            }
        }

        private void CreateGeneratedTornPage(Transform parent)
        {
            _pageMaterial = CreateMaterial("Chapter01GeneratedParchment", parchmentColor, glowColor * 0.18f);
            _inkMaterial = CreateMaterial("Chapter01GeneratedInk", inkColor, Color.black);
            _sealMaterial = CreateMaterial("Chapter01GeneratedSeal", sealColor, sealColor * 0.08f);

            GameObject page = new GameObject("GeneratedTornPage");
            page.transform.SetParent(parent, false);
            page.transform.localPosition = Vector3.zero;
            page.transform.localRotation = Quaternion.identity;
            page.transform.localScale = Vector3.one;

            MeshFilter filter = page.AddComponent<MeshFilter>();
            MeshRenderer renderer = page.AddComponent<MeshRenderer>();
            filter.sharedMesh = BuildTornPageMesh();
            renderer.sharedMaterial = _pageMaterial;

            CreateInkStroke(parent, "InkStrokeA", new Vector3(-0.06f, 0.018f, 0.18f), new Vector2(0.38f, 0.018f), _inkMaterial);
            CreateInkStroke(parent, "InkStrokeB", new Vector3(-0.04f, 0.02f, 0.095f), new Vector2(0.46f, 0.016f), _inkMaterial);
            CreateInkStroke(parent, "InkStrokeC", new Vector3(0.02f, 0.021f, 0.008f), new Vector2(0.34f, 0.014f), _inkMaterial);
            CreateInkStroke(parent, "InkStrokeD", new Vector3(-0.02f, 0.019f, -0.085f), new Vector2(0.42f, 0.015f), _inkMaterial);
            CreateInkStroke(parent, "InkStrokeE", new Vector3(0.08f, 0.02f, -0.19f), new Vector2(0.24f, 0.014f), _inkMaterial);
            CreateInkStroke(parent, "RedSeal", new Vector3(0.2f, 0.024f, -0.27f), new Vector2(0.085f, 0.085f), _sealMaterial);
        }

        private Mesh BuildTornPageMesh()
        {
            const int cols = 4;
            const int rows = 6;
            const float width = 0.72f;
            const float height = 0.98f;
            const float thickness = 0.012f;

            int rowLength = cols + 1;
            int topVertexCount = (cols + 1) * (rows + 1);
            Vector3[] vertices = new Vector3[topVertexCount * 2];
            Vector2[] uvs = new Vector2[vertices.Length];

            for (int row = 0; row <= rows; row++)
            {
                float v = row / (float)rows;
                for (int col = 0; col <= cols; col++)
                {
                    float u = col / (float)cols;
                    float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, u);
                    float z = Mathf.Lerp(-height * 0.5f, height * 0.5f, v);

                    if (col == 0)
                    {
                        x += GetEdgeJitter(row, -0.038f, 0.026f);
                    }
                    else if (col == cols)
                    {
                        x += GetEdgeJitter(row, 0.034f, -0.03f);
                    }

                    if (row == 0)
                    {
                        z += GetEdgeJitter(col, -0.028f, 0.022f);
                    }
                    else if (row == rows)
                    {
                        z += GetEdgeJitter(col, 0.032f, -0.024f);
                    }

                    float curl = Mathf.Sin(u * Mathf.PI) * 0.026f + Mathf.Sin(v * Mathf.PI * 2f) * 0.012f;
                    int topIndex = row * rowLength + col;
                    int bottomIndex = topIndex + topVertexCount;
                    vertices[topIndex] = new Vector3(x, curl + thickness, z);
                    vertices[bottomIndex] = new Vector3(x, curl - thickness, z);
                    uvs[topIndex] = new Vector2(u, v);
                    uvs[bottomIndex] = new Vector2(u, v);
                }
            }

            int cellCount = cols * rows;
            int[] triangles = new int[cellCount * 12];
            int triangleIndex = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int a = row * rowLength + col;
                    int b = a + 1;
                    int c = a + rowLength;
                    int d = c + 1;

                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = d;

                    int backOffset = topVertexCount;
                    triangles[triangleIndex++] = a + backOffset;
                    triangles[triangleIndex++] = b + backOffset;
                    triangles[triangleIndex++] = c + backOffset;
                    triangles[triangleIndex++] = b + backOffset;
                    triangles[triangleIndex++] = d + backOffset;
                    triangles[triangleIndex++] = c + backOffset;
                }
            }

            Mesh mesh = new Mesh
            {
                name = "Chapter01GeneratedTornPageMesh",
                vertices = vertices,
                uv = uvs,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float GetEdgeJitter(int index, float primary, float secondary)
        {
            return (index % 3) switch
            {
                0 => primary,
                1 => secondary,
                _ => primary * -0.45f
            };
        }

        private void CreateInkStroke(Transform parent, string objectName, Vector3 localPosition, Vector2 size, Material material)
        {
            GameObject stroke = new GameObject(objectName);
            stroke.transform.SetParent(parent, false);
            stroke.transform.localPosition = localPosition;
            stroke.transform.localRotation = Quaternion.identity;
            stroke.transform.localScale = Vector3.one;

            MeshFilter filter = stroke.AddComponent<MeshFilter>();
            MeshRenderer renderer = stroke.AddComponent<MeshRenderer>();
            filter.sharedMesh = BuildQuadMesh(size);
            renderer.sharedMaterial = material;
        }

        private static Mesh BuildQuadMesh(Vector2 size)
        {
            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;
            Mesh mesh = new Mesh
            {
                name = "Chapter01GeneratedInkQuad",
                vertices = new[]
                {
                    new Vector3(-halfWidth, 0f, -halfHeight),
                    new Vector3(-halfWidth, 0f, halfHeight),
                    new Vector3(halfWidth, 0f, -halfHeight),
                    new Vector3(halfWidth, 0f, halfHeight)
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f)
                },
                triangles = new[] { 0, 1, 2, 2, 1, 3 }
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void HideLegacyVisuals()
        {
            if (!hideLegacyRenderer)
            {
                return;
            }

            MeshRenderer legacyRenderer = GetComponent<MeshRenderer>();
            if (legacyRenderer != null)
            {
                legacyRenderer.enabled = false;
            }
        }

        private void ApplyWorldVisualTransform(Vector3 worldOffset, Quaternion worldRotation)
        {
            if (_visualRoot == null)
            {
                return;
            }

            _visualRoot.position = transform.position + worldOffset;
            _visualRoot.rotation = worldRotation;
            _visualRoot.localScale = GetParentCompensatedScale();
        }

        private Vector3 GetParentCompensatedScale()
        {
            Vector3 parentScale = transform.lossyScale;
            return new Vector3(
                visualScale / Mathf.Max(Mathf.Abs(parentScale.x), 0.001f),
                visualScale / Mathf.Max(Mathf.Abs(parentScale.y), 0.001f),
                visualScale / Mathf.Max(Mathf.Abs(parentScale.z), 0.001f));
        }

        private void CreateWarmLight(Transform parent)
        {
            GameObject lightObject = new GameObject("WarmPageGlow");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = new Vector3(0f, 0.26f, 0f);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = glowColor;
            light.range = 2.2f;
            light.intensity = 0.95f;
        }

        private void CreateDustParticles(Transform parent)
        {
            GameObject particleObject = new GameObject("PageDustMotes");
            particleObject.transform.SetParent(parent, false);
            particleObject.transform.localPosition = Vector3.zero;

            ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.025f, 0.08f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.014f, 0.035f);
            main.maxParticles = 28;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.78f, 0.34f, 0.2f), new Color(1f, 0.95f, 0.62f, 0.55f));

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 8f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.62f;
            shape.arc = 360f;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            _particleMaterial = CreateMaterial("Chapter01PageDustMaterial", glowColor, glowColor * 0.55f);
            renderer.sharedMaterial = _particleMaterial;
        }

        private static Material CreateMaterial(string materialName, Color color, Color emissionColor)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Diffuse");
            }

            Material material = new Material(shader)
            {
                name = materialName,
                color = color
            };

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
            }

            return material;
        }
    }
}
