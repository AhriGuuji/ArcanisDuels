using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LocalLogin : MonoBehaviour
{
    [SerializeField] private TMP_InputField input;

    private void Start()
    {
        if (!PlayerPrefs.HasKey("PlayerName")) return;

        string savedName = PlayerPrefs.GetString("PlayerName");

        if (!string.IsNullOrEmpty(savedName))
            input.text = PlayerPrefs.GetString("PlayerName");
    }

    public void SaveLogin(string nextScene = "MainMenu")
    {
        if(input.text == "") return;

        PlayerPrefs.SetString("PlayerName", input.text);
        SceneManager.LoadScene(nextScene);
    }
}
