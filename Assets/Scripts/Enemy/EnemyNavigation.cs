using UnityEngine;
using UnityEngine.AI;
using Mirror; // 1. Додаємо Mirror

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : NetworkBehaviour // 2. Успадковуємося від NetworkBehaviour
{
    private NavMeshAgent agent;
    private Transform targetTransform;

    [Header("AI Settings")]
    public float chaseRange = 15f;
    public float attackRange = 2f;
    public float lookSpeed = 5f;

    // Інтервал пошуку гравця (щоб не навантажувати процесор кожного кадру)
    private float searchTimer;
    private float searchInterval = 0.5f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Якщо це клієнт (не сервер), вимикаємо інтелект і фізику навігації
        // Щоб ворог рухався ТІЛЬКИ так, як каже NetworkTransform
        if (!isServer)
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = false;
            }

         }
    }

    // 3. [ServerCallback] означає, що цей Update виконується ТІЛЬКИ на сервері
    [ServerCallback]
    void Update()
    {
        // Періодично шукаємо найближчого гравця
        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0)
        {
            FindClosestPlayer();
            searchTimer = searchInterval;
        }

        // Якщо цілі немає — стоїмо
        if (targetTransform == null) return;

        float distance = Vector3.Distance(transform.position, targetTransform.position);

        if (distance <= chaseRange)
        {
            ChasePlayer();
        }
        else
        {
            // Якщо гравець втік далеко - зупиняємось
            agent.isStopped = true;
        }

        if (distance <= attackRange)
        {
            AttackPlayer();
        }
    }

    [Server] // Цей метод викликається тільки на сервері
    void FindClosestPlayer()
    {
        // Шукаємо ВСІХ гравців на карті
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        float closestDistance = Mathf.Infinity;
        Transform potentialTarget = null;

        foreach (GameObject player in players)
        {
            float d = Vector3.Distance(transform.position, player.transform.position);

            // Якщо цей гравець ближче за попереднього знайденого
            if (d < closestDistance)
            {
                closestDistance = d;
                potentialTarget = player.transform;
            }
        }

        // Призначаємо ціль
        targetTransform = potentialTarget;
    }

    [Server]
    void ChasePlayer()
    {
        if (targetTransform == null) return;

        agent.isStopped = false;
        agent.SetDestination(targetTransform.position);
    }

    [Server]
    void AttackPlayer()
    {
        if (targetTransform == null) return;

        agent.isStopped = true;

        // Поворот до гравця
        Vector3 direction = (targetTransform.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * lookSpeed);
        }

        // Тут можна додати логіку нанесення шкоди
        // наприклад: targetTransform.GetComponent<PlayerHealth>().TakeDamage(10);
    }
}