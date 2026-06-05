public class Hand
{
    private Deck _myDeck;
    private CardMessanger[] _hand = new CardMessanger[3];
    private Card[] _currentHand = new Card[3];
    public Card GetCard(int idx) => _currentHand[idx];
    public CardMessanger[] DrawCards()
    {
        for(int i = 0; i < _hand.Length; i++)
        {
            Card rndCard = _myDeck.GetRandomCard();
            _hand[i] = new CardMessanger{ CardPrefabId = rndCard.CardID, PositionInHand = i};
            _currentHand[i] = rndCard;
        }

        return _hand;
    }

    public void ReceiveDeck(Deck deck)
    { 
        _myDeck = deck;
    }
}