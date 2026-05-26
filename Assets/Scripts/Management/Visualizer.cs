using UnityEngine;
using UnityEngine.UI;

public class Visualizer : MonoBehaviour
{
    [SerializeField] private BattleManager BM;
    [SerializeField] private Hand hand;
    [SerializeField] private Slider healthP1, healthP2;
    [SerializeField] private CharacterStats p1, p2;

    private void Start()
    {
        BM.OnEndTurn += UpdateLifes;

        healthP1.maxValue = p1.CurrentHealth;
        healthP1.value = p1.CurrentHealth;

        healthP2.maxValue = p2.CurrentHealth;
        healthP2.value = p2.CurrentHealth;

        p1.OnHealthChange += UpdateLifes;
        p2.OnHealthChange += UpdateLifes;
    }

    private void UpdateLifes()
    {
        healthP1.value = p1.CurrentHealth;
        healthP2.value = p2.CurrentHealth;
    }
}