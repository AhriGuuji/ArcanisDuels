using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ShowLobbyCode : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI code;
    public readonly NetworkVariable<string> lobbyCode;

    private void Start()
    {
        if (SelectionData.isServer)
        {
            lobbyCode.Value = Random.Range(100000,1000000).ToString();
            code.text = lobbyCode.Value;
        }
        else
        {
            code.text = "Searching Opponent";
        }
    }
}
