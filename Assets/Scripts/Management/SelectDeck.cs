using UnityEngine;
using UnityEngine.UI;

public class SelectDeck : MonoBehaviour
{
    [SerializeField] private GameObject parentList;
    [SerializeField] private GameObject[] cardsList;

    private void Start()
    {
        foreach(GameObject card in cardsList)
        {
            card.GetComponent<Button>().onClick.AddListener(() => SelectCard(card));
        }


        if (SelectionData.deck.Count > 0)
            foreach (int card in SelectionData.deck)
            {
                int cardName = card;
                string path = "Prefabs/" + cardName;
                GameObject cardPrefab = Resources.Load<GameObject>(path);

                GameObject obj = Instantiate(cardPrefab, parentList.transform);
                Button button = obj.GetComponent<Button>();
                button.enabled = true;
                button.onClick.AddListener(() => RemoveCard(obj));
            }
    }

    public void SelectCard(GameObject prefab)
    {
        if (SelectionData.deck.Count == 20) return;
        GameObject card = Instantiate(prefab, parentList.transform);
        Button button = card.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => RemoveCard(card));
        SelectionData.deck.Add(card.GetComponent<Card>().CardID);
    }
    public void RemoveCard(GameObject instance)
    {
        SelectionData.deck.Remove(instance.GetComponent<Card>().CardID);
        Destroy(instance);
    }
}
