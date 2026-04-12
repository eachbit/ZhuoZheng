using System.Collections;
using UnityEngine;

/// <summary>
/// 天气触发控制器 - 当玩家进入碰撞区域时触发天气变化（天色变暗、下雨）
/// </summary>
[RequireComponent(typeof(Collider))]
public class WeatherTriggerController : MonoBehaviour
{
    [Header("触发设置")]
    [Tooltip("是否已经触发过天气变化（防止重复触发）")]
    public bool hasTriggered = false;
    
    [Tooltip("触发后是否自动销毁此组件（节省性能）")]
    public bool destroyAfterTrigger = false;
    
    [Header("天气效果设置")]
    [Tooltip("阴天时的环境光颜色")]
    public Color overcastColor = new Color(0.3f, 0.3f, 0.4f, 1f);
    
    [Tooltip("原始环境光颜色（用于恢复）")]
    public Color originalSkyColor = Color.white;
    
    [Tooltip("颜色过渡时间（秒）")]
    public float transitionDuration = 2f;
    
    [Header("雨效设置")]
    [Tooltip("雨滴预制体（Particle System）")]
    public GameObject rainPrefab;
    
    [Tooltip("雨声 AudioClip（可选）")]
    public AudioClip rainSound;
    
    [Tooltip("雨声音量（0-1）")]
    [Range(0f, 1f)]
    public float rainVolume = 0.5f;
    
    [Header("高级设置")]
    [Tooltip("光照强度最小值（0-1）")]
    [Range(0f, 1f)]
    public float minLightIntensity = 0.6f;
    
    [Tooltip("雾密度最大值")]
    public float maxFogDensity = 0.02f;
    
    [Tooltip("降雨区域大小（当没有预制体时使用）")]
    public Vector3 rainAreaSize = new Vector3(50f, 1f, 50f);
    
    [Tooltip("雨持续时长（秒），0表示不停止")]
    public float rainDuration = 30f;
    
    // 私有变量
    private AudioSource audioSource;
    private Light mainLight;
    private GameObject rainInstance;
    private Color startLightColor;
    private float startLightIntensity;
    private bool isWeatherChanging = false;
    
    /// <summary>
    /// 初始化组件
    /// </summary>
    void Start()
    {
        InitializeComponents();
    }
    
    /// <summary>
    /// 初始化所有需要的组件
    /// </summary>
    private void InitializeComponents()
    {
        // 保存原始天空颜色
        originalSkyColor = RenderSettings.ambientLight;
        
        // 获取主光源
        mainLight = RenderSettings.sun;
        if (mainLight != null)
        {
            startLightColor = mainLight.color;
            startLightIntensity = mainLight.intensity;
            Debug.Log($"[WeatherTrigger] 已获取主光源: {mainLight.name}");
        }
        else
        {
            Debug.LogWarning("[WeatherTrigger] 场景中没有找到Directional Light！请确保场景中有一个标记为Sun的平行光。");
        }
        
        // 添加音频源（如果需要播放雨声）
        if (rainSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.clip = rainSound;
            audioSource.volume = rainVolume;
            audioSource.playOnAwake = false;
            Debug.Log("[WeatherTrigger] 雨声音频已配置");
        }
        
        // 检查Collider是否正确配置
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("[WeatherTrigger] 检测到Collider未设置为Trigger！请在Inspector中勾选Is Trigger选项。");
        }
        
        Debug.Log("[WeatherTrigger] 天气触发器初始化完成");
    }

    /// <summary>
    /// 当其他碰撞体进入触发器时调用
    /// </summary>
    /// <param name="other">进入触发器的碰撞体</param>
    private void OnTriggerEnter(Collider other)
    {
        // 检查是否是玩家且尚未触发
        if (other.CompareTag("Player") && !hasTriggered && !isWeatherChanging)
        {
            Debug.Log($"[WeatherTrigger] 玩家 '{other.name}' 进入触发区域，开始天气变化...");
            TriggerWeatherChange();
        }
    }
    
    /// <summary>
    /// 触发天气变化
    /// </summary>
    private void TriggerWeatherChange()
    {
        hasTriggered = true;
        isWeatherChanging = true;
        
        Debug.Log("========================================");
        Debug.Log("[WeatherTrigger] 🌧️ 天气变化已触发！");
        Debug.Log("[WeatherTrigger] - 天色将变暗");
        Debug.Log("[WeatherTrigger] - 开始下雨");
        Debug.Log("========================================");
        
        // 1. 改变天色（环境光和雾效）
        StartCoroutine(ChangeSkyColor());
        
        // 2. 创建下雨效果
        CreateRainEffect();
        
        // 3. 播放雨声（如果有）
        PlayRainSound();
        
        // 4. 如果设置了雨的持续时间，启动停止计时器
        if (rainDuration > 0f)
        {
            StartCoroutine(StopRainAfterDelay(rainDuration));
        }
        
        // 5. 如果设置了自动销毁，延迟销毁此组件（确保在雨停之后才销毁）
        if (destroyAfterTrigger)
        {
            float destroyDelay = rainDuration > 0f ? Mathf.Max(rainDuration + 1f, transitionDuration + 1f) : transitionDuration + 1f;
            StartCoroutine(DestroySelfAfterDelay(destroyDelay));
        }
    }
    
    /// <summary>
    /// 播放雨声
    /// </summary>
    private void PlayRainSound()
    {
        if (audioSource != null && rainSound != null)
        {
            audioSource.Play();
            Debug.Log("[WeatherTrigger] 🎵 雨声开始播放");
        }
    }
    
    /// <summary>
    /// 协程：平滑过渡天空颜色
    /// </summary>
    private IEnumerator ChangeSkyColor()
    {
        float elapsed = 0f;
        
        // 记录起始颜色
        Color currentSkyColor = mainLight != null ? mainLight.color : RenderSettings.ambientLight;
        Color targetColor = overcastColor;
        
        Debug.Log($"[WeatherTrigger] 开始天色过渡，持续时间: {transitionDuration}秒");
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            
            // 使用平滑曲线
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            // 插值颜色
            Color newColor = Color.Lerp(currentSkyColor, targetColor, smoothT);
            
            // 应用颜色变化
            if (mainLight != null)
            {
                mainLight.color = newColor;
                mainLight.intensity = Mathf.Lerp(startLightIntensity, minLightIntensity, smoothT);
            }
            
            // 同时改变环境光和雾的颜色
            RenderSettings.ambientLight = newColor;
            RenderSettings.fogColor = new Color(newColor.r * 0.8f, newColor.g * 0.8f, newColor.b * 0.9f, 1f);
            RenderSettings.fogDensity = Mathf.Lerp(0.001f, maxFogDensity, smoothT);
            
            yield return null;
        }
        
        // 确保最终颜色准确
        if (mainLight != null)
        {
            mainLight.color = targetColor;
            mainLight.intensity = minLightIntensity;
        }
        RenderSettings.ambientLight = targetColor;
        RenderSettings.fogColor = new Color(targetColor.r * 0.8f, targetColor.g * 0.8f, targetColor.b * 0.9f, 1f);
        RenderSettings.fogDensity = maxFogDensity;
        RenderSettings.fog = true; // 启用雾效
        
        isWeatherChanging = false;
        Debug.Log("[WeatherTrigger] ✨ 天色已变为阴天！雾效已启用");
    }
    
    /// <summary>
    /// 创建下雨粒子效果
    /// </summary>
    private void CreateRainEffect()
    {
        if (rainPrefab != null)
        {
            // 实例化雨效预制体
            rainInstance = Instantiate(rainPrefab, transform.position, Quaternion.identity);
            rainInstance.name = "RainEffect_Instance";
            Debug.Log($"[WeatherTrigger] 🌧️ 下雨预制体已实例化: {rainPrefab.name}");
        }
        else
        {
            // 如果没有预制体，创建一个简单的粒子系统
            Debug.Log("[WeatherTrigger] 未提供雨滴预制体，将创建简单粒子系统");
            CreateSimpleRainSystem();
        }
    }
    
    /// <summary>
    /// 创建简单的雨滴粒子系统（备用方案）
    /// </summary>
    private void CreateSimpleRainSystem()
    {
        GameObject rainGO = new GameObject("RainParticleSystem");
        rainGO.transform.position = new Vector3(transform.position.x, transform.position.y + 10f, transform.position.z);
        rainGO.transform.SetParent(transform);
        
        ParticleSystem ps = rainGO.AddComponent<ParticleSystem>();
        
        // 配置主模块
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.maxParticles = 1000;
        main.startSpeed = new ParticleSystem.MinMaxCurve(20f, 30f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        main.gravityModifier = 2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(0.7f, 0.7f, 0.9f, 0.6f);
        
        // 配置发射模块
        var emission = ps.emission;
        emission.rateOverTime = 200f;
        
        // 配置形状模块（创建大面积降雨区域）
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = rainAreaSize;
        
        // 配置渲染器
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        
        // 尝试使用标准着色器
        Shader standardShader = Shader.Find("Standard");
        if (standardShader != null)
        {
            Material mat = new Material(standardShader);
            mat.color = new Color(0.7f, 0.7f, 0.9f, 0.6f);
            mat.SetFloat("_Mode", 3); // 透明模式
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            renderer.material = mat;
        }
        else
        {
            // 如果找不到Standard着色器，使用默认粒子着色器
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.color = new Color(0.7f, 0.7f, 0.9f, 0.6f);
        }
        
        rainInstance = rainGO;
        Debug.Log("[WeatherTrigger] 🌧️ 简单雨滴粒子系统已创建！");
    }
    
    /// <summary>
    /// 延迟销毁自身
    /// </summary>
    private IEnumerator DestroySelfAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("[WeatherTrigger] 天气触发器已自动销毁（优化性能）");
        Destroy(this);
    }
    
    /// <summary>
    /// 协程：在指定时间后停止雨
    /// </summary>
    /// <param name="delay">延迟时间（秒）</param>
    private IEnumerator StopRainAfterDelay(float delay)
    {
        Debug.Log($"[WeatherTrigger] ⏱️ 雨将在 {delay} 秒后停止...");
        yield return new WaitForSeconds(delay);
        
        Debug.Log("[WeatherTrigger] 🌤️ 时间到，雨停了！");
        
        // 停止雨声
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("[WeatherTrigger] 雨声已停止");
        }
        
        // 销毁雨效
        if (rainInstance != null)
        {
            Destroy(rainInstance);
            rainInstance = null;
            Debug.Log("[WeatherTrigger] 雨效已销毁");
        }
        
        // 可选：恢复天色（如果需要的话）
        // RestoreSkyColorGradually();
    }
    
    /// <summary>
    /// （可选）离开触发器时恢复天气
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // 如果需要离开后恢复天气，可以取消下面的注释
        /*
        if (other.CompareTag("Player"))
        {
            RestoreWeather();
        }
        */
    }
    
    /// <summary>
    /// （可选）恢复原始天气
    /// </summary>
    public void RestoreWeather()
    {
        hasTriggered = false;
        isWeatherChanging = true;
        
        Debug.Log("[WeatherTrigger] 🌤️ 开始恢复天气...");
        
        // 停止雨声
        if (audioSource != null)
        {
            audioSource.Stop();
            Debug.Log("[WeatherTrigger] 雨声已停止");
        }
        
        // 销毁雨效
        if (rainInstance != null)
        {
            Destroy(rainInstance);
            Debug.Log("[WeatherTrigger] 雨效已销毁");
        }
        
        // 恢复光照和雾效
        StartCoroutine(RestoreSkyColor());
    }
    
    /// <summary>
    /// 协程：恢复天空颜色
    /// </summary>
    private IEnumerator RestoreSkyColor()
    {
        float elapsed = 0f;
        Color currentColor = RenderSettings.ambientLight;
        Color targetColor = originalSkyColor;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            Color newColor = Color.Lerp(currentColor, targetColor, smoothT);
            
            if (mainLight != null)
            {
                mainLight.color = newColor;
                mainLight.intensity = Mathf.Lerp(minLightIntensity, startLightIntensity, smoothT);
            }
            
            RenderSettings.ambientLight = newColor;
            RenderSettings.fogDensity = Mathf.Lerp(maxFogDensity, 0.001f, smoothT);
            
            yield return null;
        }
        
        // 确保最终状态准确
        if (mainLight != null)
        {
            mainLight.color = startLightColor;
            mainLight.intensity = startLightIntensity;
        }
        RenderSettings.ambientLight = originalSkyColor;
        RenderSettings.fog = false;
        RenderSettings.fogDensity = 0.001f;
        
        isWeatherChanging = false;
        Debug.Log("[WeatherTrigger] ✨ 天气已恢复！");
    }
    
    /// <summary>
    /// 编辑器辅助：在Scene视图中显示触发区域
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            if (col is BoxCollider)
            {
                BoxCollider box = col as BoxCollider;
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider)
            {
                SphereCollider sphere = col as SphereCollider;
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
        }
    }
}
