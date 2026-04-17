using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSceneController : MonoBehaviour
{
    [SerializeField] private string tutorialSceneName = "Tutorial_User_Testing";
    [SerializeField] private string mainMenuSceneName = "Start";
    [SerializeField] private string playSceneName = "Level1DanceSequence";
    [SerializeField] private TMP_InputField nameInputField;

    public void StartGame()
    {
        string name = "Player";
        if (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
        {
            name = nameInputField.text.Trim();
        }

        PlayerSession.PlayerName = name;

        GoToPlayScene();
    }

    public void GoToTutorial()
    {
        SceneManager.LoadScene(tutorialSceneName);
    }

    public void GoToPlayScene()
    {
        SceneManager.LoadScene(playSceneName);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}