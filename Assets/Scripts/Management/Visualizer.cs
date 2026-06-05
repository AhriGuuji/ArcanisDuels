using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Visualizer : NetworkBehaviour
{
    [SerializeField] private BattleManager BM;
    [SerializeField] private Slider healthP1, healthP2;
    [SerializeField] private TextMeshProUGUI turn;
    private CharacterStats p;
    private CardSelector selector;
    [SerializeField] private Transform[] cardPos;
    private NetworkSetup _networkSetup;
    private bool initialized = false;

    public void Init(CharacterStats player, NetworkSetup setup, CardSelector cardSelector)
    {
        if(initialized) return;
        initialized = true;

        _networkSetup = setup;

        p = player;
        selector = cardSelector;

        BM.OnEndTurn += UpdateLifes;

        healthP1.maxValue = p.CurrentHealth;
        healthP1.value = p.CurrentHealth;

        p.OnHealthChange += UpdateLifes;

        turn.text = BM.ActualTurn.ToString();

        BM.OnEndTurn += UpdateTurn;
    }

    private void UpdateLifes()
    {
        healthP1.value = p.CurrentHealth;
    }

    [ClientRpc]
    public void UpdateLifesClientRpc(float currentHealth, ClientRpcParams rpcParams = default)
    {
        healthP2.value = currentHealth;
    }

    private void UpdateTurn()
    {
        turn.text = BM.ActualTurn.ToString();
        foreach (Transform slot in cardPos)
        {
            slot.GetComponent<Button>().onClick.RemoveAllListeners();
            slot.GetComponent<Image>().sprite = null;
        }

    }

    public void DisposeCards(CardMessanger[] hand, ulong ID)
    {
        Debug.Log($"DisposeCards called. My ID: {_networkSetup.ClientID}, Target ID: {ID}");
        
        if (_networkSetup.ClientID == ID)
        {
            for (int i = 0; i < hand.Length; i++)
            {
                Debug.Log($"Loading card: Prefabs/{hand[i].CardPrefabId}");
                GameObject cardPrefab = Resources.Load<GameObject>($"Prefabs/{hand[i].CardPrefabId}");
                Debug.Log(cardPrefab == null ? "CARD IS NULL" : "Card loaded OK");
            }
        }
    }

    [ClientRpc]
    public void DisposeCardsClientRpc(CardMessanger[] hand, ulong ID, ClientRpcParams rpcParams = default)
    {
        DisposeCards(hand, ID);
    }
}