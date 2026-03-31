using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSceneController : MonoBehaviour
{
    [SerializeField] private string tutorialSceneName = "Tutorial_beta_scene";
    [SerializeField] private string mainMenuSceneName = "Start";
    [SerializeField] private string playSceneName = "Level1DanceSequence";

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