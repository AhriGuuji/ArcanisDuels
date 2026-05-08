using UnityEngine;
using UnityEngine.UI;

public class Visualizer : MonoBehaviour
{
    [SerializeField] private GameObject[] Cards;
    [SerializeField] private BattleManager BM;
    [SerializeField] private Hand hand;
    [SerializeField] private Slider healthP1, healthP2;
    [SerializeField] private CharacterStats p1, p2;

    private void Start()
    {
        ShowCards();
        BM.OnEndTurn += ShowCards;
        BM.OnEndTurn += UpdateLifes;

        healthP1.maxValue = p1.CurrentHealth;
        healthP1.value = p1.CurrentHealth;

        healthP2.maxValue = p2.CurrentHealth;
        healthP2.value = p2.CurrentHealth;

        p1.OnHealthChange += UpdateLifes;
        p2.OnHealthChange += UpdateLifes;
    }

    private void ShowCards()
{
    for(int i = 0; i < Cards.Length; i++)
    {
        Card card = hand.GetCard(i);
        if (card == null)
        {
            Debug.LogWarning($"Card at index {i} is null");
            continue;
        }
        
        string cardName = card.Name;
        if (string.IsNullOrEmpty(cardName))
        {
            Debug.LogWarning($"Card at index {i} has no name");
            continue;
        }
        
        string path = "Prefabs/" + cardName;
        GameObject cardPrefab = Resources.Load<GameObject>(path);
        
        if (cardPrefab != null)
            Cards[i] = cardPrefab;
        else
            Debug.LogWarning($"Failed to load card: {cardName}");
    }
}

    private void UpdateLifes()
    {
        healthP1.value = p1.CurrentHealth;
        healthP2.value = p2.CurrentHealth;
    }
}