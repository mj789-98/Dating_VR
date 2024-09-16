using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class PlayFabManager : MonoBehaviour
{
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
public GameObject mainMenuPanel;  // Add this line

public Toggle[] interestToggles;
public Toggle[] hobbyToggles;

private List<string> selectedInterests = new List<string>();
private List<string> selectedHobbies = new List<string>();

private void Start()
{
    PlayFabSettings.TitleId = "365D8"; // Replace with your PlayFab Title ID
    StartCoroutine(ShowSplashScreen());
}

private IEnumerator ShowSplashScreen()
{
    splashScreenPanel.SetActive(true);
    signInPanel.SetActive(false);
    signUpPanel.SetActive(false);
    forgotPasswordPanel.SetActive(false);
    interestsPanel.SetActive(false);
    hobbiesPanel.SetActive(false);
    mainMenuPanel.SetActive(false);  // Add this line

    yield return new WaitForSeconds(2);

    splashScreenPanel.SetActive(false);
    signInPanel.SetActive(true);
    AutoLogin();
   SceneManager.LoadScene("SampleScene");
}

public void OnSignUpButtonClick()
{
    SignUp(signUpEmailInput.text, signUpPasswordInput.text, signUpUsernameInput.text, signUpGenderInput.text, signUpDobInput.text);
}

public void OnLoginButtonClick()
{
    Login(signInEmailInput.text, signInPasswordInput.text);
}

public void OnForgotPasswordButtonClick()
{
    OpenForgotPasswordPanel();
}

private void SignUp(string email, string password, string username, string gender, string dob)
{
    var registerRequest = new RegisterPlayFabUserRequest
    {
        Email = email,
        Password = password,
        Username = username,
        RequireBothUsernameAndEmail = true
    };

    PlayFabClientAPI.RegisterPlayFabUser(registerRequest,
        result =>
        {
            Debug.Log("User registered successfully!");
            SaveUserData(username, gender, dob);
            signUpPanel.SetActive(false);
            signInPanel.SetActive(true);
        },
        error =>
        {
            Debug.LogError("Error in registration: " + error.GenerateErrorReport());
        }
    );
}

private void SaveUserData(string username, string gender, string dob)
{
    var updateUserDataRequest = new UpdateUserDataRequest
    {
        Data = new Dictionary<string, string>
        {
            {"Username", username},
            {"Gender", gender},
            {"DateOfBirth", dob}
        }
    };

    PlayFabClientAPI.UpdateUserData(updateUserDataRequest,
        result => Debug.Log("User data saved successfully!"),
        error => Debug.LogError("Error saving user data: " + error.GenerateErrorReport()));
}

private void UpdateUserData(string username, string gender, string dob)
{
    var updateUserDataRequest = new UpdateUserDataRequest
    {
        Data = new Dictionary<string, string>()
    };

    if (!string.IsNullOrEmpty(username)) updateUserDataRequest.Data["Username"] = username;
    if (!string.IsNullOrEmpty(gender)) updateUserDataRequest.Data["Gender"] = gender;
    if (!string.IsNullOrEmpty(dob)) updateUserDataRequest.Data["DateOfBirth"] = dob;

    if (updateUserDataRequest.Data.Count > 0)
    {
        PlayFabClientAPI.UpdateUserData(updateUserDataRequest,
            result => Debug.Log("User data updated successfully!"),
            error => Debug.LogError("Error updating user data: " + error.GenerateErrorReport()));
    }
    else
    {
        Debug.Log("No data to update.");
    }
}

private void Login(string email, string password)
{
    var loginRequest = new LoginWithEmailAddressRequest
    {
        Email = email,
        Password = password
    };

    PlayFabClientAPI.LoginWithEmailAddress(loginRequest,
        result =>
        {
            Debug.Log("User logged in successfully!");
            SaveLoginCredentials(email, password);
            CheckUserInterestsAndHobbies();  // Modified this line
        },
        error =>
        {
            Debug.LogError("Error in login: " + error.GenerateErrorReport());
        }
    );
}

private void SaveLoginCredentials(string email, string password)
{
    PlayerPrefs.SetString("UserEmail", email);
    PlayerPrefs.SetString("UserPassword", password);
    PlayerPrefs.Save();
}

private void AutoLogin()
{
    if (PlayerPrefs.HasKey("UserEmail") && PlayerPrefs.HasKey("UserPassword"))
    {
        Login(PlayerPrefs.GetString("UserEmail"), PlayerPrefs.GetString("UserPassword"));
    }
    
}

private void ForgotPassword(string email)
{
    if (string.IsNullOrEmpty(email))
    {
        Debug.LogError("Email address is required.");
        return;
    }

    if (!IsValidEmail(email))
    {
        Debug.LogError("Invalid email address format.");
        return;
    }

    var request = new SendAccountRecoveryEmailRequest
    {
        Email = email,
        TitleId = PlayFabSettings.TitleId
    };

    PlayFabClientAPI.SendAccountRecoveryEmail(request,
        result =>
        {
            Debug.Log("Password reset email sent successfully!");
            forgotPasswordPanel.SetActive(false);
            signInPanel.SetActive(true);
        },
        error => Debug.LogError("Error sending password reset email: " + error.GenerateErrorReport()));
}

private bool IsValidEmail(string email)
{
    try
    {
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address == email;
    }
    catch
    {
        return false;
    }
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

// New methods for interests and hobbies

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
    SaveUserInterestsAndHobbies(string.Join(",", selectedInterests), string.Join(",", selectedHobbies));
}

private void SaveUserInterestsAndHobbies(string interests, string hobbies)
{
    var updateUserDataRequest = new UpdateUserDataRequest
    {
        Data = new Dictionary<string, string>
        {
            {"Interests", interests},
            {"Hobbies", hobbies}
        }
    };

    PlayFabClientAPI.UpdateUserData(updateUserDataRequest,
        result => 
        {
            Debug.Log("Interests and hobbies saved successfully!");
            ShowMainMenuPanel();  // Modified this line
        },
        error => Debug.LogError("Error saving interests and hobbies: " + error.GenerateErrorReport()));
}

// New methods

private void CheckUserInterestsAndHobbies()
{
    PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
        result => 
        {
            if (result.Data != null && 
                result.Data.ContainsKey("Interests") && 
                result.Data.ContainsKey("Hobbies") &&
                !string.IsNullOrEmpty(result.Data["Interests"].Value) &&
                !string.IsNullOrEmpty(result.Data["Hobbies"].Value))
            {
                // User already has interests and hobbies saved
                Debug.Log("User already has interests and hobbies saved.");
                ShowMainMenuPanel();
            }
            else
            {
                // User doesn't have interests and hobbies saved
                ShowInterestsPanel();
            }
        },
        error => 
        {
            Debug.LogError("Error getting user data: " + error.GenerateErrorReport());
            // If there's an error, show the interests panel anyway
            ShowInterestsPanel();
        }
    );
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
public void Logout()
{
    // Clear saved email and password
    PlayerPrefs.DeleteKey("UserEmail");
    PlayerPrefs.DeleteKey("UserPassword");
    PlayerPrefs.Save();

    // Logout from PlayFab
    PlayFabClientAPI.ForgetAllCredentials();

    // Open the login panel
    signInPanel.SetActive(true);
    mainMenuPanel.SetActive(false);
}}