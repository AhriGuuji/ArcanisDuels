using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class CharacterSelectionManager : MonoBehaviour
{
    [SerializeField] private string battleSceneName = "BattleScene";
    
    private GameObject _selectedPlayer1Prefab;
    private GameObject _selectedPlayer2Prefab;
    
    // Called when Player 1 picks a character
    public void Player1SelectCharacter(GameObject Prefab)
    {
        _selectedPlayer1Prefab = Prefab;
        // Update UI, etc.
    }
    
    // Called when Player 2 picks a character
    public void Player2SelectCharacter(GameObject Prefab)
    {
        _selectedPlayer2Prefab = Prefab;
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
    
    // When both have selected, start battle
    public void StartBattle()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        // Spawn Player 1's chosen character
        GameObject player1 = Instantiate(_selectedPlayer1Prefab);
        NetworkObject netObj1 = player1.GetComponent<NetworkObject>();
        ulong player1ClientId = GetPlayer1ClientId(); // Your logic to get Player 1's client ID
        netObj1.SpawnWithOwnership(player1ClientId);
        player1.GetComponent<CharacterStats>().SetOwnerClientId(player1ClientId);
        
        // Spawn Player 2's chosen character (can be different prefab!)
        GameObject player2 = Instantiate(_selectedPlayer2Prefab);
        NetworkObject netObj2 = player2.GetComponent<NetworkObject>();
        ulong player2ClientId = GetPlayer2ClientId();
        netObj2.SpawnWithOwnership(player2ClientId);
        player2.GetComponent<CharacterStats>().SetOwnerClientId(player2ClientId);
        
        // Load battle scene
        DontDestroyOnLoad(player1);
        DontDestroyOnLoad(player2);
        SceneManager.LoadScene(battleSceneName);
    }
}