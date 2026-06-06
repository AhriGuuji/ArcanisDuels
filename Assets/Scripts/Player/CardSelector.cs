using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CardSelector : NetworkBehaviour
{
    private List<(Card card, int handPosition)> _sequence;

    public event Action<CardMessanger[], ulong> OnSequenceSelect;
    private CharacterStats _stats;
    public void SendSequence()
    {
        if(!IsOwner) return;
        if(_sequence.Count == 0) return;

        CardMessanger[] messages = new CardMessanger[_sequence.Count];
        for(int i = 0; i < messages.Length; i++)
        {
            messages[i] = new CardMessanger
            {
                CardPrefabId = _sequence[i].card.CardID,
                PositionInHand = _sequence[i].handPosition
            };
        }

        SubmitSequenceServerRpc(messages);
    }

    [Rpc(SendTo.Server)]
    private void SubmitSequenceServerRpc(CardMessanger[] messages, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        OnSequenceSelect?.Invoke(messages, senderId); // ou chamar BattleManager diretamente
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _sequence = new();
        _stats = GetComponent<CharacterStats>();
    }

    public void SelectCard(Card card, int handPosition)
    {
        if(!IsOwner) return;
        if (_sequence.Count == 3) return;

        card.GetComponent<Button>().interactable = false;
        card.SetOwner(_stats);
        _sequence.Add((card, handPosition));
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