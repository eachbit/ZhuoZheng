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
        // 初始化：隐藏提示面板
        if (weatherPromptPanel != null)
        {
            weatherPromptPanel.SetActive(false);
        }
        
        // 自动设置UI位置（顶部居中）
        SetupUIPosition();
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
        // 只触发一次
        if (hasTriggered) return;
        
        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            ShowWeatherPrompt();
        }
    }
    
    // 显示天气提示
    void ShowWeatherPrompt()
    {
        if (weatherPromptPanel != null && weatherPromptText != null)
        {
            // 设置提示内容 - 去掉表情符号
            weatherPromptText.text = "下雨了！\n玩家请前去《与谁同坐轩》避雨";
            
            // 显示面板
            weatherPromptPanel.SetActive(true);
            
            Debug.Log("天气提示已显示：下雨了，请前去与谁同坐轩避雨");
            
            // 延迟自动隐藏
            StartCoroutine(HidePromptAfterDelay());
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
        if (other.CompareTag("Player"))
        {
            Debug.Log("玩家离开了天气提示区域");
        }
    }
}
