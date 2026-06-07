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
    private GameObject _firstPlayer, _secondPlayer;
    private ulong _player1ID, _player2ID;
    private List<Card> _player1Sequence, _player2Sequence;
    private Hand _hand1, _hand2;
    private List<ExtraEffect> _extras;
    private NetworkVariable<int> _actualTurn  = new NetworkVariable<int>(0);
    public int ActualTurn => _actualTurn.Value;
    public ulong ClientID => networkSetup.ClientID;
    public event Action OnEndTurn;
    public event Action OnTurnChanged;
    private IEnumerator EndTurn()
    {
        if (_player1.IsDead || _player2.IsDead) 
        {
            if (_player1.IsDead)
            {
                ulong deadNetObjId = _player1.GetComponent<NetworkObject>().NetworkObjectId;
                CallDeathAndEndGameClientRpc(deadNetObjId, true);
                _player1.GetComponent<NetworkObject>().Despawn();
            }
            else if (_player2.IsDead)
            {
                ulong deadNetObjId = _player2.GetComponent<NetworkObject>().NetworkObjectId;
                CallDeathAndEndGameClientRpc(deadNetObjId, false);
                _player2.GetComponent<NetworkObject>().Despawn();
            }

            yield break;
        }
        
        OnEndTurn?.Invoke();

        yield return new WaitForSeconds(0.5f);

        _selector1.EndTurnReset();
        _selector2.EndTurnReset();

        _player1.ClearBlock();
        _player2.ClearBlock();

        DisposeCardsClientRpc(_hand1.DrawCards(), _player1ID, _hand2.DrawCards(), _player2ID);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        networkSetup = FindAnyObjectByType<NetworkSetup>();
        _extras = new();
        if (IsServer) StartCoroutine(StartMatch());
        _actualTurn.OnValueChanged += (_, __) => OnTurnChanged?.Invoke();
    }

    private IEnumerator StartMatch()
    {
        yield return new WaitUntil(() => networkSetup.CanStart);

        _firstPlayer = Instantiate(Resources.Load<GameObject>("Prefabs/" + networkSetup.Players[0].CharacterName), pos1.position, pos1.rotation);
        _secondPlayer = Instantiate(Resources.Load<GameObject>("Prefabs/" + networkSetup.Players[1].CharacterName), pos2.position, pos2.rotation);
        
        NetworkObject netObj1 = _firstPlayer.GetComponent<NetworkObject>();
        NetworkObject netObj2 = _secondPlayer.GetComponent<NetworkObject>();
        netObj1.SpawnWithOwnership(networkSetup.Players[0].ClientID, true);
        netObj2.SpawnWithOwnership(networkSetup.Players[1].ClientID, true);

        yield return new WaitUntil(() =>
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(netObj1.NetworkObjectId) &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(netObj2.NetworkObjectId));

        InitSelectorsClientRpc(netObj1.NetworkObjectId, netObj2.NetworkObjectId);
        
        _player1ID = networkSetup.Players[0].ClientID;
        _player2ID = networkSetup.Players[1].ClientID;
        
        yield return null;
        
        _player1 = _firstPlayer.GetComponent<CharacterStats>();
        _player2 = _secondPlayer.GetComponent<CharacterStats>();

        _selector1 = _firstPlayer.GetComponent<CardSelector>();
        _selector2 = _secondPlayer.GetComponent<CardSelector>();

        _selector1.OnSequenceSelect += OnLocalSequenceSelected;
        _selector2.OnSequenceSelect += OnLocalSequenceSelected;

        _hand1 = new();
        _hand2 = new();

        _hand1.ReceiveDeck(new Deck(networkSetup.Players[0].DeckIds));
        _hand2.ReceiveDeck(new Deck(networkSetup.Players[1].DeckIds));

        InitVisualsClientRpc(netObj1.NetworkObjectId, netObj2.NetworkObjectId);

        yield return null;

        DisposeCardsClientRpc(_hand1.DrawCards(), _player1ID, _hand2.DrawCards(), _player2ID);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void InitVisualsClientRpc(ulong obj1Id, ulong obj2Id)
    {
        CharacterStats p1 = NetworkManager.Singleton.SpawnManager.SpawnedObjects[obj1Id].GetComponent<CharacterStats>();
        CharacterStats p2 = NetworkManager.Singleton.SpawnManager.SpawnedObjects[obj2Id].GetComponent<CharacterStats>();
        visuals.Init(p1, p2);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void InitSelectorsClientRpc(ulong obj1Id, ulong obj2Id)
    {
        _selector1 = NetworkManager.Singleton.SpawnManager.SpawnedObjects[obj1Id].GetComponent<CardSelector>();
        _selector2 = NetworkManager.Singleton.SpawnManager.SpawnedObjects[obj2Id].GetComponent<CardSelector>();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void DisposeCardsClientRpc(CardMessanger[] hand1, ulong id1, CardMessanger[] hand2, ulong id2)
    {
        visuals.DisposeCards(hand1, id1, _selector1);
        visuals.DisposeCards(hand2, id2, _selector2);
    }

    private void OnLocalSequenceSelected(CardMessanger[] cards, ulong senderId)
    {
        ReceiveSequencesServerRpc(cards, senderId);
    }

    [Rpc(SendTo.Server)]
    private void ReceiveSequencesServerRpc(CardMessanger[] selections, ulong ID)
    {   
        List<Card> actualCards = new List<Card>();

        foreach (CardMessanger selection in selections)
        {
            if (ID == _player1.OwnerClientId)
            {
                Card card = _hand1.GetCard(selection.PositionInHand);
                card.BattleManager = this;
                card.SetOwner(_player1);
                actualCards.Add(card);

                _player1Sequence = actualCards;
            }
            else if (ID == _player2.OwnerClientId)
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

        ApplyExtras();
        if (IsServer) StartCoroutine(EndTurn());
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
        foreach (ExtraEffect effect in _extras.ToList())
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
        target.PlayAnimation(attackCard.Type);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void CallDeathAndEndGameClientRpc(ulong deadNetObjId, bool player1Died)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(deadNetObjId, out var netObj))
            netObj.gameObject.SetActive(false);

        if (player1Died)
        {
            visuals.ShowResultScreen(NetworkManager.Singleton.LocalClientId == _player2ID);
        }
        else
        {
            visuals.ShowResultScreen(NetworkManager.Singleton.LocalClientId == _player1ID);
        }
    }

    private void DoHealing(Card healingCard, CharacterStats target)
    {
        healingCard.BattleManager = this;
        target.ReceiveHealing(healingCard.Effect(target));
        target.PlayAnimation(healingCard.Type);
    }

    private void DoBlock(Card blockCard, CharacterStats target)
    {
        blockCard.BattleManager = this;
        target.ReceiveBlocking(blockCard.Effect(target));
        target.PlayAnimation(blockCard.Type);
    }

    private void PowerUp(StatusCard powerUpCard, CharacterStats target)
    {
        powerUpCard.BattleManager = this;
        target.GetPowered(powerUpCard);
        target.PlayAnimation(powerUpCard.Type);
    }

    private void PowerDown(StatusCard powerDownCard, CharacterStats target)
    {
        powerDownCard.BattleManager = this;
        target.GetUnpowered(powerDownCard);
        target.PlayAnimation(powerDownCard.Type);
    }


    public void ExitBattle(string scene)
    {
        networkSetup.RetrieveScene(scene);
    }
}
