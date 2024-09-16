using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;

public class FetchUsernames : MonoBehaviour
{
    public Text usernameDisplay;

    void Start()
    {
        StartCoroutine(DelayedFetch());
    }

    IEnumerator DelayedFetch()
    {
        yield return new WaitForSeconds(6f);
        FetchAllUsernames();
    }

    void FetchAllUsernames()
    {
        var request = new GetUserDataRequest
        {
            Keys = null // Null fetches all keys
        };

        PlayFabClientAPI.GetUserData(request, OnUserDataSuccess, OnError);
    }

    void OnUserDataSuccess(GetUserDataResult result)
    {
        if (result.Data == null || !result.Data.ContainsKey("Username"))
        {
            Debug.LogWarning("No users found.");
            usernameDisplay.text = "No users found.";
            return;
        }

        List<string> userEntries = new List<string>();
        foreach (var entry in result.Data)
        {
            userEntries.Add($"Username: {entry.Value.Value}");
        }

        usernameDisplay.text = string.Join("\n", userEntries);
    }

    void OnError(PlayFabError error)
    {
        Debug.LogError("Error fetching usernames: " + error.GenerateErrorReport());
        usernameDisplay.text = "Error fetching usernames.";
    }
}
