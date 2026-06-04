using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ShowLobbyCode : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI codeText;  // Renamed for clarity
    
    public string LobbyCode { get; private set; }

    private void Start()
    {
        // Initial UI state
        if (!SelectionData.isServer)
        {
            if (codeText != null)
                codeText.text = "Searching for opponent...";
        }
        else
        {
            if (codeText != null)
                codeText.text = "Creating lobby...";
        }
    }

    public void SetCode(string code)
    {
        LobbyCode = code;

        if (codeText != null)
        {
            codeText.text = $"Join Code: {code}";
            Debug.Log($"Lobby code displayed: {code}");
        }
    }
}