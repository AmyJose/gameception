using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Gameplay;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }
    private DatabaseReference _db;

    [SerializeField] private string databaseUrl = "https://groove-galaxy-6d5c7-default-rtdb.europe-west1.firebasedatabase.app/";

    [Serializable]
    public class ScoreEntry
    {
        public string name;
        public int score;
        public long timestamp;

        public int promptsHit;
        public int promptsMissed;
        public int longestStreak;
        public int sequencesCompleted;
        public float accuracy;
        public float runDuration;

        public ScoreEntry() { }

        public ScoreEntry(string name, RunResults results)
        {
            this.name = name;
            this.score = results.finalScore;
            this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            this.promptsHit = results.promptsHit;
            this.promptsMissed = results.promptsMissed;
            this.longestStreak = results.longestStreak;
            this.sequencesCompleted = results.sequencesCompleted;
            this.accuracy = results.accuracy;
            this.runDuration = results.runDuration;
        }
    }
    private void Awake()
    {
        if (Instance!= null && Instance != this)
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
        {
            yield return null;
        }

        var app = FirebaseApp.DefaultInstance;
        var dbInstance = FirebaseDatabase.GetInstance(app, databaseUrl);
        _db = dbInstance.RootReference;

        Debug.Log("[LeaderboardManager] Database connected.");
    }
    public bool IsReady => _db != null;

    public void SubmitScore(string playerName, RunResults results, Action<bool> onComplete = null)
    {
        if (_db == null)
        {
            Debug.LogWarning("[LeaderboardManager] Database reference is null.");
            onComplete?.Invoke(false);
            return;
        }

        string safeName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();

        string key = _db.Child("leaderboard").Child("scores").Push().Key;
        ScoreEntry entry = new ScoreEntry(safeName, results);

        string json = JsonUtility.ToJson(entry);

        _db.Child("leaderboard").Child("scores").Child(key).SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("[LeaderboardManager] Failed to submit score: " + task.Exception);
                    onComplete?.Invoke(false);
                    return;
                }

                Debug.Log($"[LeaderboardManager] Score submitted: {safeName} - {entry.score}");
                onComplete?.Invoke(true);
            });
    }
    public void LoadTopScores(Action<List<ScoreEntry>> onLoaded, int limit = 10)
    {
        if (_db == null)
        {
            Debug.LogWarning("[LeaderboardManager] Database reference is null.");
            onLoaded?.Invoke(new List<ScoreEntry>());
            return;
        }

        _db.Child("leaderboard").Child("scores")
            .OrderByChild("score")
            .LimitToLast(limit)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                List<ScoreEntry> results = new List<ScoreEntry>();

                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("[LeaderboardManager] Failed to load scores: " + task.Exception);
                    onLoaded?.Invoke(results);
                    return;
                }

                DataSnapshot snapshot = task.Result;

                foreach (var child in snapshot.Children)
                {
                    string json = child.GetRawJsonValue();
                    if (!string.IsNullOrEmpty(json))
                    {
                        ScoreEntry entry = JsonUtility.FromJson<ScoreEntry>(json);
                        if (entry != null)
                            results.Add(entry);
                    }
                }

                results.Sort((a, b) => b.score.CompareTo(a.score));
                onLoaded?.Invoke(results);
            });
    }
}
