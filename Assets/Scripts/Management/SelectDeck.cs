using UnityEngine;
using UnityEngine.UI;

public class SelectDeck : MonoBehaviour
{
    [SerializeField] private GameObject parentList;

    public void SelectCard(GameObject prefab)
    {
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
