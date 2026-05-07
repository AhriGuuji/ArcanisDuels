using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.UI;

public class Visualizer : MonoBehaviour
{
    [SerializeField] private GameObject[] Cards;
    [SerializeField] private BattleManager BM;
    [SerializeField] private Hand hand;

    private void Start()
    {
        ShowCards();
        BM.OnEndTurn += ShowCards;
    }

    private void ShowCards()
    {
        for(int i = 0; i < Cards.Length; i++)
        {
            Debug.Log("Cards shown");
            try
            {
                string path = "Assets/Prefabs/" + $"{hand.GetCard(i)}";
                Cards[i] = Resources.Load(path) as GameObject;
            }
            catch
            {
                i--;
            }
            
        }
    }
}