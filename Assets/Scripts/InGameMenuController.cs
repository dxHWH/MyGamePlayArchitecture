using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
// 【新增】引入新版输入系统命名空间
using UnityEngine.InputSystem;

public class InGameMenuController : MonoBehaviour
{
    [Header("UI 容器与按钮")]
    public GameObject PausePanel;
    public Button BtnResume;
    public Button BtnReturnToMain;

    private bool _isPaused = false;

    private void Start()
    {
        if (PausePanel != null)
            PausePanel.SetActive(false);

        if (BtnResume != null)
            BtnResume.onClick.AddListener(ResumeGame);

        if (BtnReturnToMain != null)
            BtnReturnToMain.onClick.AddListener(ReturnToMainMenu);
    }

    private void Update()
    {
        // 【核心修改】：使用新版 Input System 监听 ESC 键
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    private void PauseGame()
    {
        _isPaused = true;
        if (PausePanel != null) PausePanel.SetActive(true);
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ResumeGame()
    {
        _isPaused = false;
        if (PausePanel != null) PausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }
}