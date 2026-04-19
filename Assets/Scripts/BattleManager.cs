using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    private CharacterStats _firstAtLastTurn;
    private CharacterStats _player1, _player2;
    private List<ExtraEffect> _extras;
    private int _actualTurn;

    private void Update()
    {
        
    }

    private void Turn(Card[] sequence1, Card[] sequence2)
    {
        _actualTurn++;

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
                if (_firstAtLastTurn = null)
                {
                    Card[][] cardsSequences = {sequence1,sequence2};
                    Card[] firstSequence = cardsSequences[Random.Range(0,cardsSequences.Length)];

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
    }

    private void DoSequence(Card[] cards)
    {
        CharacterStats opponent = cards[0].Owner == _player1 ? _player2 : _player1;

        foreach (Card card in cards)
        {
            switch(card.Type)
            {
                case CardType.Damage:
                    DoDamage(card, opponent);
                    break;
                case CardType.Heal:
                    DoHealing(card, card.Owner);
                    break;
                case CardType.Block:
                    DoBlock(card, card.Owner);
                    break;
                case CardType.PowerUp:
                    PowerUp(card as StatusCard, card.Owner); 
                    break;
                case CardType.PowerDown:
                    PowerDown(card as StatusCard, opponent);
                    break;
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
