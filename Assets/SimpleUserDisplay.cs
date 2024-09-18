using UnityEngine;
using Firebase.Database;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using System.Text;

public class SimpleUserDisplay : MonoBehaviour
{
    public TextMeshProUGUI userOneText;
    public TextMeshProUGUI userTwoText;

    private FirebaseManager firebaseManager;
    private bool isDataFetched = false;
    private float checkInterval = 2f;
    private float nextCheckTime = 0f;

    void Start()
    {
        Debug.Log("SimpleUserDisplay: Start method called");
        firebaseManager = FindObjectOfType<FirebaseManager>();

        if (firebaseManager == null)
        {
            Debug.LogError("SimpleUserDisplay: FirebaseManager not found in the scene!");
        }
    }

    void Update()
    {
        if (!isDataFetched && Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            StartCoroutine(TryFetchUsers());
        }
    }

    private IEnumerator TryFetchUsers()
    {
        if (firebaseManager == null || !firebaseManager.IsFirebaseInitialized())
        {
            Debug.Log("SimpleUserDisplay: Firebase not yet initialized, will try again later.");
            yield break;
        }

        Debug.Log("SimpleUserDisplay: Firebase initialized, fetching users...");
        yield return StartCoroutine(FetchAndDisplayTwoUsers());
    }

    private IEnumerator FetchAndDisplayTwoUsers()
    {
        Debug.Log("SimpleUserDisplay: Starting to fetch users");
        DatabaseReference dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        var usersTask = dbReference.Child("users").LimitToFirst(2).GetValueAsync();
        yield return new WaitUntil(() => usersTask.IsCompleted);

        if (usersTask.Exception != null)
        {
            Debug.LogError($"SimpleUserDisplay: Failed to fetch users: {usersTask.Exception}");
            yield break;
        }

        DataSnapshot snapshot = usersTask.Result;
        Debug.Log($"SimpleUserDisplay: Fetched {snapshot.ChildrenCount} users");

        int userCount = 0;
        foreach (var userSnapshot in snapshot.Children)
        {
            var userData = userSnapshot.Value as Dictionary<string, object>;
            if (userData != null)
            {
                string userInfo = FormatUserData(userData, userCount + 1);

                if (userCount == 0)
                {
                    userOneText.text = userInfo;
                    Debug.Log("SimpleUserDisplay: Set user one text");
                }
                else if (userCount == 1)
                {
                    userTwoText.text = userInfo;
                    Debug.Log("SimpleUserDisplay: Set user two text");
                }

                userCount++;
                if (userCount >= 2) break;
            }
        }

        if (userCount == 0)
        {
            Debug.LogWarning("SimpleUserDisplay: No users found in the database");
        }
        else
        {
            isDataFetched = true;
            Debug.Log("SimpleUserDisplay: Data successfully fetched and displayed");
        }
    }

    private string FormatUserData(Dictionary<string, object> userData, int userNumber)
    {
        StringBuilder userInfo = new StringBuilder($"User {userNumber}:\n");
        
        string[] fieldsToDisplay = { "Username", "Email", "Gender", "DateOfBirth", "Hobbies", "Interests" };
        
        foreach (string field in fieldsToDisplay)
        {
            if (userData.TryGetValue(field, out object value))
                userInfo.AppendLine($"{field}: {value}");
        }

        return userInfo.ToString();
    }

    public void RefreshDisplay()
    {
        isDataFetched = false;
        StartCoroutine(TryFetchUsers());
    }
}