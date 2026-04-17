using UnityEngine;

public static class PlayerSession
{
    public static string PlayerName;

    public static bool HasName => !string.IsNullOrWhiteSpace(PlayerName);
}