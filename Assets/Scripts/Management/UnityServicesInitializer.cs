using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using UnityEngine;

public class UnityServicesInitializer : MonoBehaviour
{
    public static bool IsInitialized { get; private set; }
    public static event System.Action OnInitialized;
    
    async void Awake()
    {
        DontDestroyOnLoad(gameObject);
        
        try
        {
            if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
            {
                await UnityServices.InitializeAsync();
                Debug.Log("Unity Services initialized!");
            }
            
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Signed in as: {AuthenticationService.Instance.PlayerId}");
            }
            
            IsInitialized = true;
            OnInitialized?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }
    }
}