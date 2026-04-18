using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 类名必须和文件名完全一致：StartMenu.cs → public class StartMenu
public class StartMenu : MonoBehaviour
{
    // 2. 使用Awake()方法进行初始化（如果需要）
    void Awake()
    {
        // 这里可以放置初始化逻辑
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnButtonClick()
    {
        // 🔴 关键：场景名必须和Build Settings里的完全一致（区分大小写）
        // 你的场景文件是Jianjie.unity，所以这里写"Jianjie"
        SceneManager.LoadScene("Jianjie");
    }
}