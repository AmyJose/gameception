using Audio;
using Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSceneController : MonoBehaviour
{
    [SerializeField] private string tutorialSceneName = "Tutorial_User_Testing";
    [SerializeField] private string mainMenuSceneName = "Start";
    [SerializeField] private string playSceneName = "Level1DanceSequence";
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private GameObject namePopup;

    public void OnStartClicked()
    {
        namePopup.SetActive(true);
    }

    private void Start()
    {
        MusicManager.Instance.SetMenuMode();
        namePopup.SetActive(false);
    }

    public void OnConfirmName()
    {
        string name = "Player";

        if (!string.IsNullOrWhiteSpace(nameInputField.text))
        {
            name = nameInputField.text.Trim();
        }

        PlayerSession.PlayerName = name;
        RunResultsStore.LastResults = null;

        PlayerPrefs.SetString("player_name", name);
        PlayerPrefs.Save();

        SceneManager.LoadScene(tutorialSceneName);
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