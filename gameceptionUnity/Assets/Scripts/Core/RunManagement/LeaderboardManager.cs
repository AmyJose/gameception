using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
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

        public ScoreEntry() { }

        public ScoreEntry(string name, int score)
        {
            this.name = name;
            this.score = score;
            this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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

    public void SubmitScore(string playerName, int playerScore, Action<bool> onComplete = null)
    {
        if (_db == null)
        {
            Debug.LogWarning("[LeaderboardManager] Database reference is null.");
            onComplete?.Invoke(false);
            return;
        }

        string safeName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();

        string key = _db.Child("leaderboard").Child("scores").Push().Key;

        ScoreEntry entry = new ScoreEntry(safeName, playerScore);
        string json = JsonUtility.ToJson(entry);

        var writeTask = _db.Child("leaderboard").Child("scores").Child(key).SetRawJsonValueAsync(json);
        Debug.Log("[LeaderboardManager] SetRawJsonValueAsync called.");

        writeTask.ContinueWithOnMainThread(task =>
        {
            Debug.Log("[LeaderboardManager] Submit callback reached.");

            if (task.IsCanceled)
            {
                Debug.LogError("[LeaderboardManager] Submit was canceled.");
                onComplete?.Invoke(false);
                return;
            }

            if (task.IsFaulted)
            {
                Debug.LogError("[LeaderboardManager] Submit faulted: " + task.Exception);
                onComplete?.Invoke(false);
                return;
            }

            Debug.Log($"[LeaderboardManager] Score submitted successfully: {safeName} - {playerScore}");
            onComplete?.Invoke(true);
        });
    }
}
