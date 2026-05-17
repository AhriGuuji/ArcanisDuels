using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Deck
{
    private List<Card> _deck;
    private List<Card> _availableDeck;
    private List<Card> _cemitery;

    public Deck(List<Card> deck)
    {
        _deck = deck;
        _availableDeck = new ();
        _cemitery = new ();
    }

    public Card GetRandomCard()
    {
        if (_availableDeck.Count == 0 || _availableDeck == null ) Shuffle();
        Card newCard = _availableDeck[Random.Range(0, _availableDeck.Count)];
        _availableDeck.Remove(newCard);
        return newCard;
    }

    public void Shuffle()
    {
        _availableDeck = _deck.OrderBy(i => Random. value).ToList();
        _cemitery.Clear();
    }

    public void AddCard(Card card)
    {
        if(_deck.Count == 30) return;
        _deck.Add(card);
    }

    public void RemoveCard(Card card)
    {
        if(_deck.Count == 0) return;
        _deck.Remove(card);
    }
}