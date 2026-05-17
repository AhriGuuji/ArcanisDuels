using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class CharacterSelection : MonoBehaviour
{
    [SerializeField] private string battleSceneName = "BattleScene";
    
    // When both have selected, start battle
    public void StartBattle(ulong client1Id, GameObject char1Prefab, ulong client2Id, GameObject char2Prefab)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        // Spawn Player 1's chosen character
        GameObject player1 = Instantiate(char1Prefab);
        NetworkObject netObj1 = player1.GetComponent<NetworkObject>();
        ulong player1ClientId = client1Id; // Your logic to get Player 1's client ID
        netObj1.SpawnWithOwnership(player1ClientId);
        player1.GetComponent<CharacterStats>().SetOwnerClientId(player1ClientId);
        
        // Spawn Player 2's chosen character (can be different prefab!)
        GameObject player2 = Instantiate(char2Prefab);
        NetworkObject netObj2 = player2.GetComponent<NetworkObject>();
        ulong player2ClientId = client2Id;
        netObj2.SpawnWithOwnership(player2ClientId);
        player2.GetComponent<CharacterStats>().SetOwnerClientId(player2ClientId);
        
        // Load battle scene
        DontDestroyOnLoad(player1);
        DontDestroyOnLoad(player2);
        SceneManager.LoadScene(battleSceneName);
    }
}