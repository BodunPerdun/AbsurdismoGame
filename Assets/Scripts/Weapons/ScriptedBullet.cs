using UnityEngine;

public class ScriptedBullet : MonoBehaviour
{
    [HideInInspector]
    public Vector3 direction;
    [HideInInspector]
    public float speed;       
    [HideInInspector]
    public float damage;      

    public float bulletLifeTime = 5f; 

    void Start()
    {
        Destroy(gameObject, bulletLifeTime);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            return;
        }

        EnemyBase enemyHealth = other.GetComponent<EnemyBase>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
        }
        
        // Уничтожаем пулю
        Destroy(gameObject);
    }
}