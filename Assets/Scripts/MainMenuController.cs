using UnityEngine;
using UnityEngine.SceneManagement; // 必须引入这个命名空间才能切场景！
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("UI 按钮绑定")]
    public Button btnGamePlayTest2;
    public Button btnSoulDecay;

    private void Start()
    {
        // 给按钮绑定点击事件（Lambda 表达式语法）
        if (btnGamePlayTest2 != null)
        {
            btnGamePlayTest2.onClick.AddListener(() => LoadLevel("GamePlayTest2Scene"));
        }

        if (btnSoulDecay != null)
        {
            btnSoulDecay.onClick.AddListener(() => LoadLevel("SoulDecayScene"));
        }
    }

    private void LoadLevel(string sceneName)
    {
        Debug.Log($"<color=green>[MainMenu] 准备载入关卡: {sceneName}</color>");

        // 核心：使用 Unity 原生 API 加载场景
        // 因为用的是默认的 LoadSceneMode.Single，这会把当前大厅场景连同里面的 UI 彻底销毁，然后载入新场景
        SceneManager.LoadScene(sceneName);
    }
}