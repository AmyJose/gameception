using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseAuthController : MonoBehaviour
{
    public static FirebaseAuthController Instance { get; private set; }

    public FirebaseAuth Auth { get; private set; }
    public FirebaseUser User { get; private set; }

    public bool IsSignedIn => User != null;

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
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        StartCoroutine(SignInWhenFirebaseReady());
    }

    private System.Collections.IEnumerator SignInWhenFirebaseReady()
    {
        while (!FirebaseInitializer.IsFirebaseReady)
            yield return null;

        Auth = FirebaseAuth.DefaultInstance;

        if (Auth.CurrentUser != null)
        {
            User = Auth.CurrentUser;
            Debug.Log($"[Auth] Already signed in anonymously. UID: {User.UserId}");
            yield break;
        }

        var task = Auth.SignInAnonymouslyAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsCanceled || task.IsFaulted)
        {
            Debug.LogError("[Auth] Anonymous sign-in failed: " + task.Exception);
            yield break;
        }

        User = task.Result.User;
        Debug.Log($"[Auth] Anonymous sign-in succeeded. UID: {User.UserId}");
    }
}