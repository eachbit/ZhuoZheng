using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class AutoPlayIntro : MonoBehaviour
{
    public TextMeshProUGUI introTextMore; // 只保留详细简介文本
    public string[] introLines; // 简介内容数组
    public float displayTime = 2.0f; // 每行显示时间
    public float fadeInTime = 0.5f; // 淡入时间
    public string targetSceneName = "Garden_Main"; // 目标场景名称
    
    private bool isPlaying = false;
    private Coroutine playCoroutine;
    
    void Start()
    {
        Debug.Log("AutoPlayIntro Start called");
        playCoroutine = StartCoroutine(PlayIntro());
    }
    
    public void SkipIntroAndStartGame()
    {
        Debug.Log("SkipIntroAndStartGame called");
        
        // 停止正在播放的intro动画
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }
        
        isPlaying = false;
        
        // 直接加载目标场景
        LoadTargetScene();
    }
    
    private void LoadTargetScene()
    {
        Debug.Log("Loading scene: " + targetSceneName);
        SceneManager.LoadScene(targetSceneName);
    }
    
    IEnumerator PlayIntro()
    {
        isPlaying = true;
        Debug.Log("PlayIntro started");
        
        // 清空文本内容
        if (introTextMore != null)
        {
            introTextMore.text = "";
            introTextMore.color = new Color(introTextMore.color.r, introTextMore.color.g, introTextMore.color.b, 0);
        }
        
        // 逐行显示内容到textIntroMore
        for (int i = 0; i < introLines.Length; i++)
        {
            if (introTextMore != null)
            {
                // 在textIntroMore中逐行添加内容，保持之前的内容
                if (introTextMore.text == "")
                {
                    introTextMore.text = introLines[i];
                }
                else
                {
                    introTextMore.text += "\n" + introLines[i]; // 添加新行并保持之前的内容
                }
                
                yield return FadeInText(introTextMore);
                yield return new WaitForSeconds(displayTime);
            }
        }
        
        isPlaying = false;
        Debug.Log("PlayIntro finished");
        
        // intro播放完成后自动跳转到游戏场景
        LoadTargetScene();
    }
    
    IEnumerator FadeInText(TextMeshProUGUI textComponent)
    {
        if (textComponent == null) yield break;
        
        Color originalColor = textComponent.color;
        Color targetColor = new Color(originalColor.r, originalColor.g, originalColor.b, 1);
        
        float elapsedTime = 0;
        while (elapsedTime < fadeInTime)
        {
            textComponent.color = Color.Lerp(
                new Color(targetColor.r, targetColor.g, targetColor.b, 0), 
                targetColor, 
                elapsedTime / fadeInTime
            );
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        textComponent.color = targetColor;
    }
}