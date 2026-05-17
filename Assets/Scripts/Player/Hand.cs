using UnityEngine;

public class Hand : MonoBehaviour
{
    [SerializeField] private BattleManager BM;
    private Deck _myDeck;
    private Card[] _hand = new Card[3];
    public Card GetCard(int idx) => _hand[idx];

    private void Start()
    {
        _myDeck = new Deck(SelectionData.deck);
        DrawCards();
        BM.OnEndTurn += DrawCards;
    }

    public void DrawCards()
    {
        for(int i = 0; i < _hand.Length; i++)
        {
            _hand[i] = _myDeck.GetRandomCard();
        }
    }
}