using System;
using System.Collections.Generic;
using UnityEngine;

public class CardSelector : MonoBehaviour
{
    private bool wasSend;
    private List<Card> _sequence;
    public event Action<List<Card>> OnSequenceSelect;
    public void SendSequence()
    {
        if(wasSend) return;
        if(_sequence.Count == 0) return;
        Debug.Log(_sequence[0]);

        OnSequenceSelect?.Invoke(_sequence);
        wasSend = true;
    }

    private void Start()
    {
        _sequence = new();
        wasSend = false;
    }

    public void SelectCard(Card card)
    {
        _sequence.Add(card);
    }

    public void ClearSelection()
    {
        _sequence.Clear();
    }

    public void EndTurnReset()
    {
        wasSend = false;
        ClearSelection();
    }
}