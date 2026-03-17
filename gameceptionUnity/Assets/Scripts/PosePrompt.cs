using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
public class PosePromptUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Pose Settings")]
    private string[] poseNames = new string[] { "earth", "water", "fire", "air" };
    [SerializeField] private float countdownSeconds = 10f;
    [SerializeField] private float getReadyDelay = 2f;

    [Header("Dialogue Settings")]
    [SerializeField] private float dialogueDelay = 2.5f;

    private void Start()
    {
        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        // 🌌 Intro Dialogue
        yield return ShowDialogue("We are refugees from a distant galaxy...");
        yield return ShowDialogue("Our world was lost long ago.");
        yield return ShowDialogue("We have travelled far across the stars.");
        yield return ShowDialogue("But we cannot rebuild alone...");
        yield return ShowDialogue("We need your help to create a new planet.");
        yield return ShowDialogue("Channel the elements through your body.");
        yield return ShowDialogue("Hold each pose to bring our world to life.");

        // 🌍 Pose sequence
        foreach (var pose in poseNames)
        {
            instructionText.text = $"Get ready for {pose.ToUpper()}";
            countdownText.text = "";
            yield return new WaitForSeconds(getReadyDelay);

            instructionText.text = $"Hold {pose.ToUpper()} pose";

            for (int i = (int)countdownSeconds; i > 0; i--)
            {
                countdownText.text = i.ToString();
                yield return new WaitForSeconds(1f);
            }

            countdownText.text = "0";
            instructionText.text = $"{pose.ToUpper()} energy collected!";
            yield return new WaitForSeconds(1.5f);
        }

        instructionText.text = "The planet is forming... Thank you.";
        countdownText.text = "";
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(2);
    }

    private IEnumerator ShowDialogue(string message)
    {
        instructionText.text = message;
        countdownText.text = "";
        yield return new WaitForSeconds(dialogueDelay);
    }


}