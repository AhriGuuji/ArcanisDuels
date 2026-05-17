using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class CharacterSelectionUI : NetworkBehaviour
{
    [SerializeField] private CharacterSelection _manager;
    [SerializeField] private Button[] chars;
    [SerializeField] private TextMeshProUGUI statusText;
    
    private Dictionary<ulong, int> _playerSelections = new Dictionary<ulong, int>();
    private bool _hasSelected = false;
    
    public override void OnNetworkSpawn()
    {
        // Only show buttons for the local player
        if (IsLocalPlayer)
        {
            EnableButtons(true);
            statusText.text = "Select your character!";
        }
        else
        {
            EnableButtons(false);
            statusText.text = "Waiting for opponent to select...";
        }
    }
    
    private void EnableButtons(bool enabled)
    {
        foreach (Button button in chars)
            button.interactable = enabled;
    }
    
    public void SelectCharacter(int charPrefabIdx)
    {
        if (_hasSelected) return;
        if (!IsLocalPlayer) return;
        
        _hasSelected = true;
        EnableButtons(false);
        statusText.text = "Waiting for opponent...";
        
        // Send selection to server
        SelectCharacterServerRpc(charPrefabIdx);
    }
    
    [ServerRpc]
    private void SelectCharacterServerRpc(int charPrefabIdx, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        
        _playerSelections[clientId] = charPrefabIdx;
        
        Debug.Log($"Player {clientId} selected character {charPrefabIdx}");
        
        // Let all clients know someone selected
        ShowSelectionClientRpc(clientId, charPrefabIdx);
        
        // When both have selected, start the game
        if (_playerSelections.Count == 2)
        {
            SendSelections();
        }
    }
    
    [ClientRpc]
    private void ShowSelectionClientRpc(ulong clientId, int charPrefabIdx)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            statusText.text = $"You selected {charPrefabIdx}. Waiting for opponent...";
        }
        else
        {
            statusText.text = $"Opponent selected {charPrefabIdx}. Waiting for them to ready up...";
        }
    }
    
    private void SendSelections()
    {
        // Get both selections
        ulong[] clientIds = new ulong[2];
        clientIds[0] = GetPlayer1ClientId();
        clientIds[1] = GetPlayer2ClientId();
        GameObject[] selections = new GameObject[2];
        selections[0] = GetPrefab(_playerSelections[clientIds[0]]);
        selections[1] = GetPrefab(_playerSelections[clientIds[1]]);
        
        // Load battle scene
        _manager.StartBattle(clientIds[0],selections[0],clientIds[1],selections[1]);
    }
    
    private GameObject GetPrefab(int charPrefabIdx)
    {
        return Resources.Load("Assets/Resources/Characters/" + charPrefabIdx.ToString()) as GameObject;
    }

    private ulong GetPlayer1ClientId()
    {
        // Player 1 is the host
        return NetworkManager.Singleton.LocalClientId;
    }

    private ulong GetPlayer2ClientId()
    {
        // Player 2 is the only other connected client
        foreach (KeyValuePair<ulong, NetworkClient> client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Key != NetworkManager.Singleton.LocalClientId)
                return client.Key;
        }
        return 0; // Not found
    }
}