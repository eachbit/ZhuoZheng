// using System.Collections;
// using UnityEngine;
// using TMPro;
// using UnityEngine.SceneManagement;

// public class MenuIntro : MonoBehaviour
// {
//     public GameObject introText; // 游戏简介文本对象
//     public GameObject introTextMore; // 详细简介文本对象
//     public GameObject startButton; // 开始按钮对象
    
//     void Start()
//     {
//         // 确保所有对象都已正确设置
//         if (introText != null)
//         {
//             introText.SetActive(true);
//         }
        
//         if (introTextMore != null)
//         {
//             introTextMore.SetActive(true);
//         }
        
//         if (startButton != null)
//         {
//             startButton.SetActive(true);
//         }
//     }
    
//     public void OnStartButtonClick()
//     {
//         // 隐藏简介文本
//         if (introText != null)
//         {
//             introText.SetActive(false);
//         }
        
//         if (introTextMore != null)
//         {
//             introTextMore.SetActive(false);
//         }
        
//         // 跳转到游戏主界面
//         SceneManager.LoadScene("Garden_Main");
//     }
// }

using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuIntro : MonoBehaviour
{
    public AutoPlayIntro autoPlayIntro;
    
    public void OnStartButtonClick()
    {
        Debug.Log("Start button clicked - skipping intro");
        
        if (autoPlayIntro != null)
        {
            autoPlayIntro.SkipIntroAndStartGame();
        }
        else
        {
            // 如果没有引用AutoPlayIntro，直接加载场景
            SceneManager.LoadScene("Garden_Main");
        }
    }
}