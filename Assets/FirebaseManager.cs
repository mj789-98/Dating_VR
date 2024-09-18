using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using TMPro;
using UnityEngine.UI;

public class FirebaseManager : MonoBehaviour
{
    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;
    public FirebaseUser user;
    public DatabaseReference dbReference;

    [SerializeField] private string databaseUrl = "https://metamatch-1280a-default-rtdb.firebaseio.com/";

    [Header("UI")]
    public TMP_InputField signInEmailInput;
    public TMP_InputField signInPasswordInput;
    public TMP_InputField signUpEmailInput;
    public TMP_InputField signUpPasswordInput;
    public TMP_InputField signUpUsernameInput;
    public TMP_InputField signUpGenderInput;
    public TMP_InputField signUpDobInput;
    public TMP_InputField forgotPasswordEmailInput;

    public GameObject splashScreenPanel;
    public GameObject signInPanel;
    public GameObject signUpPanel;
    public GameObject forgotPasswordPanel;
    public GameObject interestsPanel;
    public GameObject hobbiesPanel;
    public GameObject mainMenuPanel;

    public Toggle[] interestToggles;
    public Toggle[] hobbyToggles;

    private List<string> selectedInterests = new List<string>();
    private List<string> selectedHobbies = new List<string>();

    private bool isFirebaseInitialized = false;

    // Dictionary to store user data
    private Dictionary<string, object> userData;

    private void Awake()
    {
        StartCoroutine(InitializeFirebase());
    }

    public bool IsFirebaseInitialized()
    {
        return isFirebaseInitialized;
    }

    // Method to get user data
    public Dictionary<string, object> GetUserData()
    {
        return userData;
    }

    private IEnumerator InitializeFirebase()
    {
        Debug.Log("Initializing Firebase...");

        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => dependencyTask.IsCompleted);

        if (dependencyTask.Result != DependencyStatus.Available)
        {
            Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyTask.Result}");
            yield break;
        }

        // Initialize Firebase app with the database URL
        FirebaseApp.Create(new AppOptions { 
            DatabaseUrl = new Uri(databaseUrl)
        });

        FirebaseApp app = FirebaseApp.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        if (dbReference == null)
        {
            Debug.LogError("Failed to initialize Realtime Database reference");
            yield break;
        }

        isFirebaseInitialized = true;
        Debug.Log("Firebase initialized successfully");
        yield return StartCoroutine(ShowSplashScreen());
    }

    private IEnumerator ShowSplashScreen()
    {
        splashScreenPanel.SetActive(true);
        signInPanel.SetActive(false);
        signUpPanel.SetActive(false);
        forgotPasswordPanel.SetActive(false);
        interestsPanel.SetActive(false);
        hobbiesPanel.SetActive(false);
        mainMenuPanel.SetActive(false);

        yield return new WaitForSeconds(2);

        splashScreenPanel.SetActive(false);
        signInPanel.SetActive(true);
        StartCoroutine(AutoLogin());
    }

    public void OnSignUpButtonClick()
    {
        StartCoroutine(SignUp(signUpEmailInput.text, signUpPasswordInput.text, signUpUsernameInput.text, signUpGenderInput.text, signUpDobInput.text));
    }

    private IEnumerator SignUp(string email, string password, string username, string gender, string dob)
    {
        yield return new WaitUntil(() => isFirebaseInitialized);

        if (auth == null)
        {
            Debug.LogError("FirebaseAuth is not initialized.");
            yield break;
        }

        var task = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"Error in registration: {task.Exception}");
        }
        else
        {
            Debug.Log("User registered successfully!");
            yield return StartCoroutine(SaveUserData(username, gender, dob));
            signUpPanel.SetActive(false);
            signInPanel.SetActive(true);
        }
    }

    private IEnumerator SaveUserData(string username, string gender, string dob)
    {
        user = auth.CurrentUser;
        if (user != null)
        {
            UserProfile profile = new UserProfile { DisplayName = username };
            var profileTask = user.UpdateUserProfileAsync(profile);
            yield return new WaitUntil(() => profileTask.IsCompleted);

            if (profileTask.Exception != null)
            {
                Debug.LogError($"Error updating user profile: {profileTask.Exception}");
                yield break;
            }

            Dictionary<string, object> userData = new Dictionary<string, object>
            {
                { "Username", username },
                { "Gender", gender },
                { "DateOfBirth", dob }
            };

            var dbTask = dbReference.Child("users").Child(user.UserId).UpdateChildrenAsync(userData);
            yield return new WaitUntil(() => dbTask.IsCompleted);

            if (dbTask.Exception != null)
            {
                Debug.LogError($"Error saving user data: {dbTask.Exception}");
            }
            else
            {
                Debug.Log("User data saved successfully!");
            }
        }
    }

    public void OnLoginButtonClick()
    {
        StartCoroutine(Login(signInEmailInput.text, signInPasswordInput.text));
    }

    private IEnumerator Login(string email, string password)
    {
        yield return new WaitUntil(() => isFirebaseInitialized);

        Debug.Log($"Attempting to login with email: {email}");
        if (auth == null)
        {
            Debug.LogError("FirebaseAuth is null. Firebase may not be properly initialized.");
            yield break;
        }

        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogError($"Error in login: {loginTask.Exception}");
        }
        else
        {
            Debug.Log("User logged in successfully!");
            AuthResult authResult = loginTask.Result;
            user = authResult.User;
            SaveLoginCredentials(email, password);
            yield return StartCoroutine(FetchUserData());
        }
    }

    private IEnumerator FetchUserData()
    {
        if (user == null)
        {
            Debug.LogError("User is null in FetchUserData");
            yield break;
        }

        var task = dbReference.Child("users").Child(user.UserId).GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"Error getting user data: {task.Exception.Message}");
            yield break;
        }

        if (!task.Result.Exists)
        {
            Debug.Log("User data does not exist.");
            yield break;
        }

        userData = task.Result.Value as Dictionary<string, object>;
        if (userData != null)
        {
            Debug.Log($"User data retrieved: {string.Join(", ", userData.Keys)}");
            
            // Add email to userData
            userData["Email"] = user.Email;

            if (userData.ContainsKey("Interests") && userData.ContainsKey("Hobbies") &&
                !string.IsNullOrEmpty(userData["Interests"] as string) &&
                !string.IsNullOrEmpty(userData["Hobbies"] as string))
            {
                Debug.Log("User already has interests and hobbies saved.");
                ShowMainMenuPanel();
            }
            else
            {
                Debug.Log("User needs to set interests and hobbies.");
                ShowInterestsPanel();
            }
        }
        else
        {
            Debug.LogWarning("User data could not be cast to Dictionary<string, object>");
            ShowInterestsPanel();
        }
    }

    public void OnForgotPasswordButtonClick()
    {
        StartCoroutine(ForgotPassword(forgotPasswordEmailInput.text));
    }

    private IEnumerator ForgotPassword(string email)
    {
        yield return new WaitUntil(() => isFirebaseInitialized);

        if (string.IsNullOrEmpty(email))
        {
            Debug.LogError("Email address is required.");
            yield break;
        }

        var task = auth.SendPasswordResetEmailAsync(email);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"Error sending password reset email: {task.Exception}");
        }
        else
        {
            Debug.Log("Password reset email sent successfully!");
            forgotPasswordPanel.SetActive(false);
            signInPanel.SetActive(true);
        }
    }

    private void SaveLoginCredentials(string email, string password)
    {
        PlayerPrefs.SetString("UserEmail", email);
        PlayerPrefs.SetString("UserPassword", password);
        PlayerPrefs.Save();
    }

    private IEnumerator AutoLogin()
    {
        yield return new WaitUntil(() => isFirebaseInitialized);

        if (PlayerPrefs.HasKey("UserEmail") && PlayerPrefs.HasKey("UserPassword"))
        {
            string email = PlayerPrefs.GetString("UserEmail");
            string password = PlayerPrefs.GetString("UserPassword");
            yield return StartCoroutine(Login(email, password));
        }
    }

    private void ShowInterestsPanel()
    {
        signInPanel.SetActive(false);
        interestsPanel.SetActive(true);
    }

    public void OnInterestToggled(Toggle toggle)
    {
        if (toggle.isOn)
        {
            selectedInterests.Add(toggle.name);
        }
        else
        {
            selectedInterests.Remove(toggle.name);
        }
    }

    public void OnInterestsNextButtonClick()
    {
        interestsPanel.SetActive(false);
        hobbiesPanel.SetActive(true);
    }

    public void OnHobbyToggled(Toggle toggle)
    {
        if (toggle.isOn)
        {
            selectedHobbies.Add(toggle.name);
        }
        else
        {
            selectedHobbies.Remove(toggle.name);
        }
    }

    public void OnSaveInterestsAndHobbiesButtonClick()
    {
        StartCoroutine(SaveUserInterestsAndHobbies(string.Join(",", selectedInterests), string.Join(",", selectedHobbies)));
    }

    private IEnumerator SaveUserInterestsAndHobbies(string interests, string hobbies)
    {
        yield return new WaitUntil(() => isFirebaseInitialized);

        user = auth.CurrentUser;
        if (user != null)
        {
            Dictionary<string, object> userData = new Dictionary<string, object>
            {
                { "Interests", interests },
                { "Hobbies", hobbies }
            };

            var task = dbReference.Child("users").Child(user.UserId).UpdateChildrenAsync(userData);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Exception != null)
            {
                Debug.LogError($"Error saving interests and hobbies: {task.Exception}");
            }
            else
            {
                Debug.Log("Interests and hobbies saved successfully!");
                ShowMainMenuPanel();
            }
        }
    }

    private void ShowMainMenuPanel()
    {
        signInPanel.SetActive(false);
        signUpPanel.SetActive(false);
        forgotPasswordPanel.SetActive(false);
        interestsPanel.SetActive(false);
        hobbiesPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    private void ShowSignInPanel()
    {
        signInPanel.SetActive(true);
        signUpPanel.SetActive(false);
        forgotPasswordPanel.SetActive(false);
        interestsPanel.SetActive(false);
        hobbiesPanel.SetActive(false);
        mainMenuPanel.SetActive(false);
    }

    public void OpenForgotPasswordPanel()
    {
        signInPanel.SetActive(false);
        forgotPasswordPanel.SetActive(true);
    }

    public void OpenSignUpPanel()
    {
        signInPanel.SetActive(false);
        signUpPanel.SetActive(true);
    }

    public void OpenSignInPanel()
    {
        signInPanel.SetActive(true);
        signUpPanel.SetActive(false);
    }

    public void Logout()
    {
        if (auth != null)
        {
            auth.SignOut();
            PlayerPrefs.DeleteKey("UserEmail");
            PlayerPrefs.DeleteKey("UserPassword");
            PlayerPrefs.Save();
            userData = null;
            signInPanel.SetActive(true);
            mainMenuPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Cannot logout: FirebaseAuth is null");
        }
    }
}