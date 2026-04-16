using Firebase;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseInitializer : MonoBehaviour
{
    public static bool IsFirebaseReady { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var status = task.Result;
            if (status == DependencyStatus.Available)
            {
                IsFirebaseReady = true;
                Debug.Log("Firebase ready");

                Firebase.Analytics.FirebaseAnalytics.LogEvent("game_startup");
            }
            else
            {
                IsFirebaseReady = false;
                Debug.LogError($"Could not resolve Firebase dependencies: {status}");
            }
        });
    }
}