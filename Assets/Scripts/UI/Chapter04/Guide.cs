using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Guide : MonoBehaviour
{
    [Header("UI References")]
    public GameObject weatherPromptPanel; // 天气提示面板
    public TMP_Text weatherPromptText; // 天气提示文本
    
    [Header("Settings")]
    public float displayDuration = 5f; // 显示时长（秒）
    public Collider triggerCollider; // 触发碰撞体
    
    // 状态变量
    private bool hasTriggered = false; // 是否已触发过
    
    void Start()
    {
        Debug.Log("=== Guide 脚本初始化 ===");
        
        // 检查必要的引用
        if (weatherPromptPanel == null)
        {
            Debug.LogError("❌ weatherPromptPanel 未赋值！请在Inspector中拖入UI面板");
        }
        else
        {
            Debug.Log("✅ weatherPromptPanel 已赋值");
            weatherPromptPanel.SetActive(false);
        }
        
        if (weatherPromptText == null)
        {
            Debug.LogError("❌ weatherPromptText 未赋值！请在Inspector中拖入TextMeshPro文本");
        }
        else
        {
            Debug.Log("✅ weatherPromptText 已赋值");
        }
        
        if (triggerCollider == null)
        {
            Debug.LogError("❌ triggerCollider 未赋值！请在Inspector中拖入触发碰撞体");
        }
        else
        {
            Debug.Log($"✅ triggerCollider 已赋值: {triggerCollider.name}");
            Debug.Log($"   - IsTrigger: {triggerCollider.isTrigger}");
            
            if (!triggerCollider.isTrigger)
            {
                Debug.LogWarning("⚠️ 警告：碰撞体的 Is Trigger 未勾选！请勾选此选项");
            }
        }
        
        // 自动设置UI位置（顶部居中）
        SetupUIPosition();
        
        Debug.Log("=== Guide 脚本初始化完成 ===");
    }
    
    // 自动设置UI位置
    void SetupUIPosition()
    {
        if (weatherPromptPanel != null)
        {
            RectTransform rect = weatherPromptPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                // 设置在屏幕顶部居中
                rect.anchorMin = new Vector2(0.25f, 0.75f);
                rect.anchorMax = new Vector2(0.75f, 0.85f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }
        }
        
        // 设置文本样式
        if (weatherPromptText != null)
        {
            weatherPromptText.fontSize = 22;
            weatherPromptText.alignment = TextAlignmentOptions.Center;
            weatherPromptText.enableWordWrapping = true;
            weatherPromptText.lineSpacing = 1.2f;
        }
    }
    
    // 碰撞触发
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($" OnTriggerEnter 被调用 - 碰撞对象: {other.name}, 标签: {other.tag}");
        
        if (other.CompareTag("Player"))
        {
            Debug.Log("✅ 检测到Player标签，准备显示天气提示");
            
            // 如果已经触发过，检查面板是否还显示着
            if (hasTriggered)
            {
                if (weatherPromptPanel != null && weatherPromptPanel.activeSelf)
                {
                    Debug.Log("⚠️ 面板已经在显示中，忽略此次触发");
                    return;
                }
                else
                {
                    Debug.Log(" 面板已隐藏，重新显示提示");
                    ShowWeatherPrompt();
                    return;
                }
            }
            
            hasTriggered = true;
            ShowWeatherPrompt();
        }
        else
        {
            Debug.LogWarning($"❌ 碰撞对象标签不是 'Player'，当前标签是: '{other.tag}'");
            Debug.LogWarning("   请确保玩家对象的Tag设置为 'Player'");
        }
    }
    
    // 显示天气提示
    void ShowWeatherPrompt()
    {
        Debug.Log(" 进入ShowWeatherPrompt方法");
        
        if (weatherPromptPanel == null)
        {
            Debug.LogError("❌ weatherPromptPanel为null，无法显示提示！");
            return;
        }
        
        if (weatherPromptText == null)
        {
            Debug.LogError("❌ weatherPromptText为null，无法显示提示！");
            return;
        }
        
        try
        {
            // 设置提示内容
            weatherPromptText.text = "下雨了！\n玩家请前去《与谁同坐轩》避雨";
            Debug.Log("✅ 文本内容已设置");
            
            // 显示面板
            weatherPromptPanel.SetActive(true);
            Debug.Log("✅ 天气提示面板已激活显示");
            
            Debug.Log("天气提示已显示：下雨了，请前去与谁同坐轩避雨");
            
            // 延迟自动隐藏
            StartCoroutine(HidePromptAfterDelay());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 显示天气提示时发生错误: {e.Message}");
            Debug.LogError($"❌ 堆栈跟踪: {e.StackTrace}");
        }
    }
    
    // 延迟隐藏提示
    IEnumerator HidePromptAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        
        if (weatherPromptPanel != null)
        {
            weatherPromptPanel.SetActive(false);
            Debug.Log("✅ 天气提示已自动隐藏");
        }
    }
    
    // 可选：玩家离开触发区域
    void OnTriggerExit(Collider other)
    {
        Debug.Log($"🚪 OnTriggerExit - 对象: {other.name}, 标签: {other.tag}");
        
        if (other.CompareTag("Player"))
        {
            Debug.Log("玩家离开了天气提示区域");
        }
    }
    
    // 添加碰撞检测调试（用于排查问题）
    void OnCollisionEnter(Collision collision)
    {
        // 这个不应该被调用，因为使用的是Trigger
        // 如果被调用，说明Is Trigger没有勾选
        Debug.LogWarning($"⚠️ OnCollisionEnter 被调用！这说明碰撞体不是Trigger模式");
        Debug.LogWarning($"   碰撞对象: {collision.gameObject.name}");
        Debug.LogWarning($"   请检查 triggerCollider 的 Is Trigger 是否已勾选");
    }
}
