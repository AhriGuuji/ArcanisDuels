using UnityEngine;
using UnityEngine.UI;

public class Hand : MonoBehaviour
{
    private Deck _myDeck;
    private Card[] _hand = new Card[3];
    public Card GetCard(int idx) => _hand[idx];
    private CardSelector cardSelector;

    private void Start()
    {
        cardSelector = GetComponent<CardSelector>();
    }

    public Card[] DrawCards()
    {
        for(int i = 0; i < _hand.Length; i++)
        {
            _hand[i] = _myDeck.GetRandomCard();
        }

        return _hand;
    }

    public void ReceiveDeck(Deck deck)
    { 
        _myDeck = deck;
    }
}