using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Visualizer : MonoBehaviour
{
    [SerializeField] private BattleManager BM;
    [SerializeField] private Slider healthP1, healthP2;
    [SerializeField] private TextMeshProUGUI turn;
    private CharacterStats p1, p2;
    [SerializeField] private Button[] cardPos;
    [SerializeField] private Image[] cardSprites;
    [SerializeField] private Button sendSequence;
    [SerializeField] private Sprite placeHolder;
    [SerializeField] private GameObject winScreen, loseScreen;

    public void Init(CharacterStats player1, CharacterStats player2)
    {
        p1 = player1;
        p2 = player2;

        BM.OnEndTurn += UpdateLifes;
        BM.OnEndTurn += UpdateTurn;
        BM.OnTurnChanged += UpdateTurn;

        healthP1.maxValue = p1.CurrentHealth;
        healthP1.value = p1.CurrentHealth;
        healthP2.maxValue = p2.CurrentHealth;
        healthP2.value = p2.CurrentHealth;

        p1.OnHealthChange += UpdateLifes;
        p2.OnHealthChange += UpdateLifes;

        turn.text = BM.ActualTurn.ToString();
    }

    private void UpdateLifes()
    {
        healthP1.value = p1.CurrentHealth;
        healthP2.value = p2.CurrentHealth;
    }

    private void UpdateTurn()
    {
        turn.text = BM.ActualTurn.ToString();
        foreach (Button button in cardPos)
            button.onClick.RemoveAllListeners();
        foreach (Image image in cardSprites)
            image.sprite = placeHolder;
    }

    public void DisposeCards(CardMessanger[] hand, ulong ID, CardSelector selector)
    {
        if (NetworkManager.Singleton.LocalClientId != ID) return;

        sendSequence.onClick.RemoveAllListeners();
        sendSequence.onClick.AddListener(() => selector.SendSequence());

        for (int i = 0; i < hand.Length; i++)
        {
            Card card = Resources.Load<Card>("Prefabs/" + hand[i].CardPrefabId);
            int pos = hand[i].PositionInHand;
            cardPos[i].onClick.AddListener(() => selector.SelectCard(card, pos));
            cardPos[i].interactable = true;
            cardSprites[i].sprite = card.GetComponent<Image>().sprite;
        }
    }

    public void ShowResultScreen(bool isWin)
    {
        if (isWin)
            winScreen.SetActive(true);
        else
            loseScreen.SetActive(true);
    }
}