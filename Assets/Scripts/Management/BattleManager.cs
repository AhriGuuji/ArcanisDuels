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
    private CardSelector _selector1, _selector2;
    private CharacterStats _firstAtLastTurn;
    private CharacterStats _player1, _player2;
    private List<Card> _player1Sequence, _player2Sequence;
    private Hand _hand1, _hand2;
    private List<ExtraEffect> _extras;
    private NetworkVariable<int> _actualTurn  = new NetworkVariable<int>(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server 
);
    public int ActualTurn => _actualTurn.Value;
    public event Action OnEndTurn;
    private void EndTurn()
    {
        _selector1.EndTurnReset();
        _selector2.EndTurnReset();

        _player1.ClearBlock();
        _player2.ClearBlock();

        visuals.DisposeCardsClientRpc(_hand1.DrawCards(), _player1.OwnerClientId);
        visuals.DisposeCardsClientRpc(_hand2.DrawCards(), _player2.OwnerClientId);
        OnEndTurn?.Invoke();
    }

    public override void OnNetworkSpawn()
    {
        networkSetup = FindAnyObjectByType<NetworkSetup>();
        _extras = new();

        if (IsServer)
            StartCoroutine(StartMatch());
    }

    private IEnumerator StartMatch()
    {
        yield return new WaitUntil(() => networkSetup.CanStart);

        GameObject firstPlayer = Instantiate(Resources.Load("Prefabs/" + networkSetup.Players[0].CharacterName) as GameObject, pos1.position, pos1.rotation);
        GameObject secondPlayer = Instantiate(Resources.Load("Prefabs/" + networkSetup.Players[1].CharacterName) as GameObject, pos2.position, pos2.rotation);

        NetworkObject netObj1 = firstPlayer.GetComponent<NetworkObject>();
        NetworkObject netObj2 = secondPlayer.GetComponent<NetworkObject>();
        netObj1.Spawn();
        netObj2.Spawn();

        _player1 = firstPlayer.GetComponent<CharacterStats>();
        _player2 = secondPlayer.GetComponent<CharacterStats>();

        _selector1 = firstPlayer.GetComponent<CardSelector>();
        _selector2 = secondPlayer.GetComponent<CardSelector>();

        _selector1.OnSequenceSelect += OnLocalSequenceSelected;
        _selector2.OnSequenceSelect += OnLocalSequenceSelected;

        _hand1 = new();
        _hand2 = new();

        _hand1.ReceiveDeck(new Deck(networkSetup.Players[0].DeckIds));
        _hand2.ReceiveDeck(new Deck(networkSetup.Players[1].DeckIds));

        visuals.Init(_player1, networkSetup, _selector1);
        InitVisualsClientRpc();

        ClientRpcParams rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds.ToArray()
            }
        };

        visuals.DisposeCardsClientRpc(_hand1.DrawCards(), _player1.OwnerClientId, rpcParams);
        visuals.DisposeCardsClientRpc(_hand2.DrawCards(), _player2.OwnerClientId, rpcParams);
    }

    [ClientRpc]
    private void InitVisualsClientRpc()
    {
        foreach (var spawned in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            CharacterStats stats = spawned.Value.GetComponent<CharacterStats>();
            if (stats != null && stats.OwnerClientId == NetworkManager.Singleton.LocalClientId)
            {
                CardSelector mySelector = spawned.Value.GetComponent<CardSelector>();
                visuals.Init(stats, networkSetup, mySelector);
                break;
            }
        }
    }

    private void OnLocalSequenceSelected(CardMessanger[] cards)
    {
        ReceiveSequencesServerRpc(cards);
    }

    [ServerRpc]
    private void ReceiveSequencesServerRpc(CardMessanger[] selections, ServerRpcParams rpcParams = default)
    {   
        List<Card> actualCards = new List<Card>();
        ulong senderId = rpcParams.Receive.SenderClientId;
        
        foreach (CardMessanger selection in selections)
        {
            if (senderId == _player1.OwnerClientId)
            {
                Card card = _hand1.GetCard(selection.PositionInHand);
                card.BattleManager = this;
                card.SetOwner(_player1);
                actualCards.Add(card);

                _player1Sequence = actualCards;
            }
            else if (senderId == _player2.OwnerClientId)
            {
                Card card = _hand2.GetCard(selection.PositionInHand);
                card.BattleManager = this;
                card.SetOwner(_player2);
                actualCards.Add(card);
                
                _player2Sequence = actualCards;
            }
        }
    
        
        if (_player1Sequence != null && _player2Sequence != null)
        {
            StartCoroutine(Turn(_player1Sequence, _player2Sequence));
            _player1Sequence = null;
            _player2Sequence = null;
        }
    }

    private IEnumerator Turn(List<Card> sequence1, List<Card> sequence2)
    {
        _actualTurn.Value++;

        yield return null;

        bool seq1priority = sequence1.Any(card => card.Priority);
        bool seq2priority = sequence2.Any(card => card.Priority);

        if (seq1priority && !seq2priority)
        {
            DoSequence(sequence1);
            DoSequence(sequence2);
        }
        else if (!seq1priority && seq2priority)
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
                    _firstAtLastTurn = firstSequence[0].Owner;

                    if (_firstAtLastTurn == sequence1[0].Owner)
                        DoSequence(sequence2);
                    else
                        DoSequence(sequence1);
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

    private void BroadcastHealthToOpponent(CharacterStats damagedPlayer)
    {
        float opponentHealth = damagedPlayer == _player1 ? _player2.CurrentHealth : _player1.CurrentHealth;
        ulong targetClientId = damagedPlayer == _player1 ? _player2.OwnerClientId : _player1.OwnerClientId;
        
        ClientRpcParams rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { targetClientId }
            }
        };
        
        visuals.UpdateLifesClientRpc(opponentHealth, rpcParams);
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
        target.TakeDamage(attackCard.Effect(target) * attackCard.Owner.GetAttack);

        BroadcastHealthToOpponent(target);

        target.PlayAnimationClientRpc(attackCard.Type);
        target.PlayAnimation(attackCard.Type);
    }

    private void DoHealing(Card healingCard, CharacterStats target)
    {
        healingCard.BattleManager = this;
        target.ReceiveHealing(healingCard.Effect(target));

        BroadcastHealthToOpponent(target);

        target.PlayAnimationClientRpc(healingCard.Type);
        target.PlayAnimation(healingCard.Type);
    }

    private void DoBlock(Card blockCard, CharacterStats target)
    {
        blockCard.BattleManager = this;
        target.ReceiveBlocking(blockCard.Effect(target));
        target.PlayAnimationClientRpc(blockCard.Type);
        target.PlayAnimation(blockCard.Type);
    }

    private void PowerUp(StatusCard powerUpCard, CharacterStats target)
    {
        powerUpCard.BattleManager = this;
        target.GetPowered(powerUpCard);
        target.PlayAnimationClientRpc(powerUpCard.Type);
        target.PlayAnimation(powerUpCard.Type);
    }

    private void PowerDown(StatusCard powerDownCard, CharacterStats target)
    {
        powerDownCard.BattleManager = this;
        target.GetUnpowered(powerDownCard);
        target.PlayAnimationClientRpc(powerDownCard.Type);
        target.PlayAnimation(powerDownCard.Type);
    }
}
