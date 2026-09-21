using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CreditsUI : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private string mainMenuScene = "MainMenu";

    private void Start()
    {
        Time.timeScale = 1f;
        backButton.onClick.AddListener(Back);
    }

    private void Update()
    {
        if (GameInput.CancelPressed)
        {
            Back();
        }
    }

    private void Back()
    {
        SceneManager.LoadScene(mainMenuScene);
    }
}
