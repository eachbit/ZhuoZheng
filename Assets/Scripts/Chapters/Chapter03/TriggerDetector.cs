using UnityEngine;
using System;

/// <summary>
/// 通用触发器检测组件
/// 挂载到触发器对象上，检测玩家进入/离开
/// </summary>
public class TriggerDetector : MonoBehaviour
{
    [Header("触发器设置")]
    [Tooltip("区域名称，用于调试")]
    public string areaName = "未命名区域";
    
    [Header("回调函数")]
    public Action<string> OnTriggerEnterAction;
    public Action<string> OnTriggerExitAction;
    
    void Start()
    {
        // 验证Collider设置
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"❌ {gameObject.name} 没有Collider组件！");
            enabled = false;
            return;
        }
        
        if (!col.isTrigger)
        {
            Debug.LogError($"❌ {gameObject.name} 的Collider未勾选Is Trigger！");
            enabled = false;
            return;
        }
        
        // 验证Scale
        Transform trans = transform;
        if (trans.localScale.x <= 0 || trans.localScale.y <= 0 || trans.localScale.z <= 0)
        {
            Debug.LogError($"❌ {gameObject.name} 的Scale有负数或零！当前: {trans.localScale}");
            enabled = false;
            return;
        }
        
        Debug.Log($"✅ 触发器已就绪: {areaName} ({gameObject.name})");
    }
    
    void OnTriggerEnter(Collider other)
    {
        // 检查是否是玩家
        bool isPlayer = other.CompareTag("Player") || other.name.Contains("Player");
        
        if (!isPlayer)
            return;
        
        Debug.Log($"📥 [触发器] {areaName} - 玩家进入");
        
        // 触发回调
        OnTriggerEnterAction?.Invoke(areaName);
    }
    
    void OnTriggerExit(Collider other)
    {
        // 检查是否是玩家
        bool isPlayer = other.CompareTag("Player") || other.name.Contains("Player");
        
        if (!isPlayer)
            return;
        
        Debug.Log($"📤 [触发器] {areaName} - 玩家离开");
        
        // 触发回调
        OnTriggerExitAction?.Invoke(areaName);
    }
}
