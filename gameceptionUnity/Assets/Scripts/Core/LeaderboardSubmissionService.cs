using System.Collections;
using System.Collections.Generic;
using Firebase.Functions;
using Firebase.Extensions;
using UnityEngine;
using Gameplay;

public class LeaderboardSubmissionService : MonoBehaviour
{
    public static LeaderboardSubmissionService Instance { get; private set; }

    private FirebaseFunctions _functions;
    public bool IsReady => _functions != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        while (!FirebaseInitializer.IsFirebaseReady)
            yield return null;

        _functions = FirebaseFunctions.DefaultInstance;
        Debug.Log("[Functions] Ready.");
    }

    public void SubmitScore(string playerName, RunResults results, System.Action<bool> onComplete = null)
    {
        if (_functions == null)
        {
            Debug.LogWarning("[Functions] Not ready.");
            onComplete?.Invoke(false);
            return;
        }

        var data = new Dictionary<string, object>
        {
            { "name", playerName },
            { "score", results.finalScore },
            { "promptsHit", results.promptsHit },
            { "promptsMissed", results.promptsMissed },
            { "longestStreak", results.longestStreak },
            { "sequencesCompleted", results.sequencesCompleted },
            { "accuracy", results.accuracy },
            { "runDuration", results.runDuration }
        };

        _functions
            .GetHttpsCallable("submitScore")
            .CallAsync(data)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("[Functions] submitScore canceled.");
                    onComplete?.Invoke(false);
                    return;
                }

                if (task.IsFaulted)
                {
                    Debug.LogError("[Functions] submitScore failed: " + task.Exception);
                    onComplete?.Invoke(false);
                    return;
                }

                Debug.Log("[Functions] submitScore succeeded.");
                onComplete?.Invoke(true);
            });
    }
}