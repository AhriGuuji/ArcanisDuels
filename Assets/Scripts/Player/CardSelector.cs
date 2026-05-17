using System;
using System.Collections.Generic;
using UnityEngine;

public class CardSelector : MonoBehaviour
{
    private List<Card> _sequence;
    public event Action<List<Card>> OnSequenceSelect;
    private CharacterStats _stats;
    public void SendSequence()
    {
        if(_sequence.Count == 0) return;

        OnSequenceSelect?.Invoke(_sequence);
    }

    private void Start()
    {
        _sequence = new();
        _stats = GetComponent<CharacterStats>();
    }

    public void SelectCard(Card card)
    {
        if (_sequence.Count == 3) return;
        //card.gameObject.SetActive(false);
        card.SetOwner(_stats);
        _sequence.Add(card);
    }

    public void ClearSelection()
    {
        //foreach(Card card in _sequence)
            //card.gameObject.SetActive(true);
        _sequence.Clear();
    }

    public void EndTurnReset()
    {
        ClearSelection();
    }
}