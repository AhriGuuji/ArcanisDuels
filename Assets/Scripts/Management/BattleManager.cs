using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.Netcode;

public class BattleManager : NetworkBehaviour
{
    private NetworkSetup networkSetup;
    [SerializeField] private Visualizer visuals;
    [SerializeField] private Transform pos1, pos2;
    [SerializeField] private Transform[] cardPos;
    private CardSelector _selector1, _selector2;
    private CharacterStats _firstAtLastTurn;
    private CharacterStats _player1, _player2;
    private Hand _hand1, _hand2;
    private List<ExtraEffect> _extras;
    private NetworkVariable<int> _actualTurn  = new NetworkVariable<int>(
    0,  // Initial value
    NetworkVariableReadPermission.Everyone,  // Everyone can read
    NetworkVariableWritePermission.Server    // Only server can write
);
    public int ActualTurn => _actualTurn.Value;
    private List<List<Card>> _sequences;
    public event Action OnEndTurn;
    public ulong GetClientID => networkSetup.ClientID;
    private void EndTurn()
    {
        _sequences.Clear();
        _selector1.EndTurnReset();
        _selector2.EndTurnReset();
        visuals.DisposeCards(_hand1.DrawCards(), networkSetup.Players[0].ClientID);
        visuals.DisposeCards(_hand2.DrawCards(), networkSetup.Players[0].ClientID);
        OnEndTurn?.Invoke();
    }

    private void Awake()
    {
        if (!IsServer)
        {
            enabled = false;  // Disable the component, but keep it
            return;
        }

        networkSetup = FindAnyObjectByType<NetworkSetup>();
        _extras = new();
        _sequences = new();

        StartCoroutine(StartMatch());
    }

    private IEnumerator StartMatch()
    {
        yield return new WaitUntil(() => networkSetup.CanStart);

        GameObject firstPlayer = Instantiate(Resources.Load("Prefabs/" + networkSetup.Players[0].CharacterName) as GameObject, pos1);
        GameObject secondPlayer = Instantiate(Resources.Load("Prefabs/" + networkSetup.Players[1].CharacterName) as GameObject, pos2);

        _player1 = firstPlayer.GetComponent<CharacterStats>();
        _player2 = secondPlayer.GetComponent<CharacterStats>();

        visuals.Init(_player1, _player2);

        _selector1 = firstPlayer.GetComponent<CardSelector>();
        _selector2 = secondPlayer.GetComponent<CardSelector>();

        _selector1.OnSequenceSelect += ReceiveSequences;
        _selector2.OnSequenceSelect += ReceiveSequences;

        _hand1 = new();
        _hand2 = new();

        _hand1.ReceiveDeck(new Deck(networkSetup.Players[0].DeckIds));
        _hand2.ReceiveDeck(new Deck(networkSetup.Players[1].DeckIds));

        visuals.DisposeCards(_hand1.DrawCards(), networkSetup.Players[0].ClientID);
        visuals.DisposeCards(_hand2.DrawCards(), networkSetup.Players[1].ClientID);
    }

    private void ReceiveSequences(List<Card> sequence)
    {
        Debug.Log("Cards received from: " + sequence[0].Owner.name);
        if (_sequences.Count > 0) if(sequence[0].Owner == _sequences[0][0].Owner) return;
        _sequences.Add(sequence);
        if (_sequences.Count == 2)
            Turn(_sequences[0], _sequences[1]);
    }

    private void Turn(List<Card> sequence1, List<Card> sequence2)
    {
        _actualTurn.Value++;

        bool seq1priority = sequence1.Any(card => card.Priority);
        bool seq2priority = sequence2.Any(card => card.Priority);

        if (seq1priority & !seq2priority)
        {
            DoSequence(sequence1);
            DoSequence(sequence2);
        }
        else if (!seq1priority & seq2priority)
        {
            DoSequence(sequence2);
            DoSequence(sequence1);
        }
        else
        {
            if ( sequence1[0].Owner.GetSpeed > sequence2[0].Owner.GetSpeed )
            {
                DoSequence(sequence1);
                DoSequence(sequence2);
            }
            else if ( sequence1[0].Owner.GetSpeed < sequence2[0].Owner.GetSpeed )
            {
                DoSequence(sequence2);
                DoSequence(sequence1);
            }
            else
            {
                if (_firstAtLastTurn == null)
                {
                    List<List<Card>> cardsSequences = new (){sequence1,sequence2};
                    List<Card> firstSequence = cardsSequences[Random.Range(0,cardsSequences.Count)];

                    DoSequence(firstSequence);
                    if (_firstAtLastTurn == sequence1[0].Owner)
                        DoSequence(sequence2);
                    else
                        DoSequence(sequence1);
                    
                    _firstAtLastTurn = firstSequence[0].Owner;
                }
                else
                {
                    if (_firstAtLastTurn == sequence1[0].Owner)
                    {
                        DoSequence(sequence2);
                        _firstAtLastTurn = sequence2[0].Owner;
                    }
                    else
                    {
                        DoSequence(sequence1);
                        _firstAtLastTurn = sequence1[0].Owner;
                    }
                }
            }
        }

        //Apply Extra Effects
        ApplyExtras();
        EndTurn();
    }

    private void DoSequence(List<Card> cards)
    {
        CharacterStats opponent = cards[0].Owner == _player1 ? _player2 : _player1;

        foreach (Card card in cards)
        {
            switch(card.Type)
            {
                case CardType.Damage:
                    {
                        DoDamage(card, opponent);
                        break;
                    }
                case CardType.Heal:
                    {
                        DoHealing(card, card.Owner);
                        break;
                    }
                case CardType.Block:
                    {
                        DoBlock(card, card.Owner);
                        break;
                    }
                case CardType.PowerUp:
                    {
                       PowerUp(card as StatusCard, card.Owner); 
                        break; 
                    }
                case CardType.PowerDown:
                    {
                        PowerDown(card as StatusCard, opponent);
                        break;
                    }
            }
        }
    }

    private void ApplyExtras()
    {
        foreach (ExtraEffect effect in _extras)
        {
            effect.Effect();
        }
    }

    public void AddExtra(ExtraEffect extra)
    {
        if(!_extras.Contains(extra)) 
        {
            _extras.Add(extra);
            extra.BattleManager = this;
        }
    }

    public void RemoveExtra(ExtraEffect extra)
    {
        _extras.Remove(extra);
    }

    private void DoDamage(Card attackCard, CharacterStats target)
    {
        attackCard.BattleManager = this;
        target.TakeDamage(attackCard.Effect(target));
    }

    private void DoHealing(Card healingCard, CharacterStats target)
    {
        healingCard.BattleManager = this;
        target.ReceiveHealing(healingCard.Effect(target));
    }

    private void DoBlock(Card blockCard, CharacterStats target)
    {
        blockCard.BattleManager = this;
        target.ReceiveBlocking(blockCard.Effect(target));
    }

    private void PowerUp(StatusCard powerUpCard, CharacterStats target)
    {
        powerUpCard.BattleManager = this;
        target.GetPowered(powerUpCard);
    }

    private void PowerDown(StatusCard powerDownCard, CharacterStats target)
    {
        powerDownCard.BattleManager = this;
        target.GetUnpowered(powerDownCard);
    }
}
