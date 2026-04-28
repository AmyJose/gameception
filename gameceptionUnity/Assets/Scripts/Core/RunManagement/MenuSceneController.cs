using Audio;
using System.Globalization;
using Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuSceneController : MonoBehaviour
{
    private const string RunDurationPrefsKey = "run_duration_seconds";
    private const string SkipTutorialPrefsKey = "skip_tutorial";
    private const float DefaultRunDurationSeconds = 300f;

    [SerializeField] private string tutorialSceneName = "Tutorial_User_Testing";
    [SerializeField] private string mainMenuSceneName = "Start";
    [SerializeField] private string playSceneName = "Level1DanceSequence";
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private GameObject namePopup;
    [SerializeField] private GameObject OptionsMenu;
    [SerializeField] private Toggle skipTutorialToggle;
    [SerializeField] private TMP_InputField runDurationInputField;
    [SerializeField] private bool skipTutorial = false;

    public void OnStartClicked()
    {
        namePopup.SetActive(true);
    }

    public void OnOptionsClicked()
    {
        OptionsMenu.SetActive(true);
        SyncOptionsMenuFromCurrentSettings();
    }

    public void OnConfirmOptions()
    {
        if (skipTutorialToggle != null)
        {
            skipTutorial = skipTutorialToggle.isOn;
        }

        PlayerPrefs.SetInt(SkipTutorialPrefsKey, skipTutorial ? 1 : 0);
        PlayerPrefs.SetFloat(RunDurationPrefsKey, GetRunDurationFromOptions());
        PlayerPrefs.Save();

        OptionsMenu.SetActive(false);
    }

    private void Start()
    {
        MusicManager.Instance.SetMenuMode();
        skipTutorial = PlayerPrefs.GetInt(SkipTutorialPrefsKey, skipTutorial ? 1 : 0) == 1;
        namePopup.SetActive(false);
        OptionsMenu.SetActive(false);
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

        if (skipTutorial)
        {
            SceneManager.LoadScene(playSceneName);
        }
        else
        {
            SceneManager.LoadScene(tutorialSceneName);
        }
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

    private void SyncOptionsMenuFromCurrentSettings()
    {
        if (skipTutorialToggle != null)
        {
            skipTutorialToggle.isOn = skipTutorial;
        }

        if (runDurationInputField != null)
        {
            float runDurationSeconds = PlayerPrefs.GetFloat(RunDurationPrefsKey, DefaultRunDurationSeconds);
            runDurationInputField.text = runDurationSeconds.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

    private float GetRunDurationFromOptions()
    {
        if (runDurationInputField == null)
        {
            return PlayerPrefs.GetFloat(RunDurationPrefsKey, DefaultRunDurationSeconds);
        }

        if (float.TryParse(runDurationInputField.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedDuration) && parsedDuration > 0f)
        {
            return parsedDuration;
        }

        return PlayerPrefs.GetFloat(RunDurationPrefsKey, DefaultRunDurationSeconds);
    }
}