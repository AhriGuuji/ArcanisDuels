using UnityEngine;
using UnityEngine.UI;

public class SelectDeck : MonoBehaviour
{
    [SerializeField] private GameObject parentList;

    private void Start()
    {
        if (SelectionData.deck.Count > 0)
            foreach (Card card in SelectionData.deck)
            {
                string cardName = card.Name;
                string path = "Prefabs/" + cardName;
                GameObject cardPrefab = Resources.Load<GameObject>(path);

                GameObject obj = Instantiate(cardPrefab, parentList.transform);
                obj.GetComponent<Button>().onClick.AddListener(() => RemoveCard(obj));
            }
    }

    public void SelectCard(GameObject prefab)
    {
        if (SelectionData.deck.Count == 20) return;
        GameObject card = Instantiate(prefab.gameObject, parentList.transform);
        card.GetComponent<Button>().onClick.AddListener(() => RemoveCard(card));
        SelectionData.deck.Add(card.GetComponent<Card>());
    }
    public void RemoveCard(GameObject instance)
    {
        SelectionData.deck.Remove(instance.GetComponent<Card>());
        Destroy(instance);
    }
}
