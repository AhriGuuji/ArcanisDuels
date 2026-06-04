using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Visualizer : MonoBehaviour
{
    [SerializeField] private BattleManager BM;
    [SerializeField] private Slider healthP1, healthP2;
    [SerializeField] private TextMeshProUGUI turn;
    private CharacterStats p1, p2;
    [SerializeField] private Transform[] cardPos;

    public void Init(CharacterStats player1, CharacterStats player2)
    {
        p1 = player1;
        p2 = player2;

        BM.OnEndTurn += UpdateLifes;

        healthP1.maxValue = p1.CurrentHealth;
        healthP1.value = p1.CurrentHealth;

        healthP2.maxValue = p2.CurrentHealth;
        healthP2.value = p2.CurrentHealth;

        p1.OnHealthChange += UpdateLifes;
        p2.OnHealthChange += UpdateLifes;

        turn.text = BM.ActualTurn.ToString();

        BM.OnEndTurn += UpdateTurn;
    }

    private void UpdateLifes()
    {
        healthP1.value = p1.CurrentHealth;
        healthP2.value = p2.CurrentHealth;
    }

    private void UpdateTurn()
    {
        turn.text = BM.ActualTurn.ToString();
    }

    public void DisposeCards(Card[] hand, ulong ID)
    {
        if (BM.GetClientID == ID)
        for (int i = 0; i < hand.Length; i++)
        {
            string cardName = hand[i].Name;
            string path = "Prefabs/" + cardName;
            GameObject cardPrefab = Resources.Load<GameObject>(path);
            Instantiate(cardPrefab,cardPos[i]);
        }
    }
}