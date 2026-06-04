using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Authentication;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using System.Threading.Tasks;

public class LocalLogin : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Toggle rememberMeToggle;
    [SerializeField] private TMP_Text errorText;
    
    private bool isNewUser = false;
    
    public async void OnLoginClicked()
    {
        await UnityServices.InitializeAsync();

        string username = usernameInput.text.Trim();
        string password = passwordInput.text;
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Enter username and password!");
            return;
        }
        
        // Disable button, show loading
        errorText.text = "Logging in...";

        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("Signing out current user before creating new account...");
            AuthenticationService.Instance.SignOut();
            // Wait a frame for signout to complete
            await Task.Delay(100);
        }
        
        try
        {
            // First try to sign in
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            Debug.Log("Login successful!");
            
            // Save if remember me
            if (rememberMeToggle != null && rememberMeToggle.isOn)
            {
                PlayerPrefs.SetString("SavedUsername", username);
                PlayerPrefs.SetString("SavedPassword", password); // Not secure, but for demo
                PlayerPrefs.Save();
            }
            
            // Load next scene
            SceneManager.LoadScene("MainMenu");
        }
        catch (AuthenticationException ex)
        {
            if (ex.ErrorCode == 401)
            {
                // Account doesn't exist - ask to create
                ShowError("Account not found. Create new account?");
                isNewUser = true;
                errorText.text = "Account not found. Click 'Create Account' to sign up.";
            }
            else
            {
                ShowError($"Login error: {ex.Message}");
            }
        }
        catch (RequestFailedException ex)
        {
            ShowError($"Network error: {ex.Message}");
        }
    }
    
    public async void OnCreateAccountClicked()
    {
        await UnityServices.InitializeAsync();
        
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Enter username and password!");
            return;
        }
        
        if (password.Length < 6)
        {
            ShowError("Password must be at least 6 characters!");
            return;
        }
        
        errorText.text = "Creating account...";

        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("Signing out current user before creating new account...");
            AuthenticationService.Instance.SignOut();
            // Wait a frame for signout to complete
            await Task.Delay(100);
        }
        
        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            Debug.Log("Account created successfully!");
            
            // Auto sign in
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            
            // Save for next time
            PlayerPrefs.SetString("SavedUsername", username);
            PlayerPrefs.Save();
            
            SceneManager.LoadScene("MainMenu");
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == 409)
        {
            ShowError("Username already exists! Try another one.");
        }
        catch (AuthenticationException ex)
        {
            ShowError($"Sign up failed: {ex.Message}");
        }
        catch (RequestFailedException ex)
        {
            ShowError($"Network error: {ex.Message}");
        }
    }
    
    private void ShowError(string message)
    {
        errorText.text = message;
        Debug.LogError(message);
    }
}