using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth; 
    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0) return; // Игнорировать урон, если враг уже мертв

        currentHealth -= damageAmount;
        Debug.Log(gameObject.name + " получил урон, HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " мертв.");
        currentHealth = maxHealth;
        gameObject.SetActive(false); 
    }

    
    
}