using System;
using UnityEngine;
using Unity.Netcode;

public class CharacterStats : NetworkBehaviour  
{
    [SerializeField] protected float maxHealth;
    [SerializeField] protected float attack;
    [SerializeField] protected float speed;
    public ulong ThisOwnerClientId { get; private set; }  // Which player owns this character
    public ulong ThisNetworkObjectId => NetworkObject.NetworkObjectId;  // Unique ID for this object
    public void SetOwnerClientId(ulong value) => ThisOwnerClientId = value;

    private float _health;
    private float _block;

    public float GetSpeed => speed;
    public float CurrentHealth => _health;

    public event Action OnHealthChange;
    private void HealthChanged()
    {
        OnHealthChange?.Invoke();
    }

    private void Start()
    {
        _health = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        float damageDone = damage;

        if (_block > 0)
        {
            _block -= damage;

            if (_block < 0)
            {
                damageDone = -_block;
            }
        }

        _health -= damageDone;
        HealthChanged();

        if (_health <= 0)
        {
            _health = 0;
            //Character Death Function
        }
    }

    public void ReceiveHealing(float healing)
    {
        _health += healing;
        HealthChanged();

        if (_health > maxHealth)
        {
            _health = maxHealth;
        }
    }

    public void ReceiveBlocking(float block)
    {
        _block += block;
    }

    public void GetPowered(StatusCard powerUpCard)
    {
        powerUpCard.Apply(this);
    }

    public void GetUnpowered(StatusCard powerDownCard)
    {
        powerDownCard.Apply(this);
    }

    // ========== NEW METHODS FOR OPTION A ==========
    
    [ClientRpc]
    public void PlayDamageAnimationClientRpc(float damage)
    {
        // Replace these with your actual visual effects
        PlayHurtAnimation();
        ShowDamageNumber(damage);
    }
    
    [ClientRpc]
    public void PlayHealAnimationClientRpc(float healing)
    {
        ShowHealNumber(healing);
    }
    
    [ClientRpc]
    public void PlayBlockAnimationClientRpc(float block)
    {
        ShowBlockEffect(block);
    }
    
    [ClientRpc]
    public void PlayPowerUpAnimationClientRpc(string powerUp)
    {
        ShowPowerUpVisual(powerUp);
    }
    
    [ClientRpc]
    public void PlayPowerDownAnimationClientRpc(string powerDown)
    {
        ShowPowerDownVisual(powerDown);
    }
    
    // ========== YOUR EXISTING VISUAL METHODS (add these if missing) ==========
    
    private void PlayHurtAnimation()
    {
        // Your existing hurt animation code
        // Example: animator?.SetTrigger("Hurt");
    }
    
    private void ShowDamageNumber(float damage)
    {
        // Your existing damage popup code
        // Example: DamagePopup.Create(transform.position, damage);
    }
    
    private void ShowHealNumber(float healing)
    {
        // Your existing heal popup code
    }
    
    private void ShowBlockEffect(float block)
    {
        // Your existing block visual code
    }
    
    private void ShowPowerUpVisual(string powerUp)
    {
        // Your existing power up visual code
    }
    
    private void ShowPowerDownVisual(string powerDown)
    {
        // Your existing power down visual code
    }
}