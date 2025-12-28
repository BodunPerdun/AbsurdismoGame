using UnityEngine;
using UnityEngine.AI;
using Mirror; // 1. Додаємо Mirror

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : NetworkBehaviour // 2. Успадковуємося від NetworkBehaviour
{
    private NavMeshAgent agent;
    private Transform targetTransform;

    private PlayerController targetController; // Посилання на контролер гравця

    public NetworkAnimator networkAnimator;

    [Header("AI Settings")]
    public float chaseRange = 15f;
    public float attackRange = 2f;
    public float lookSpeed = 5f;

    // Змінні для частоти атак в EnemyAI
    [Header("Combat Settings")]
    public float attackInterval = 1.5f; // Ворог б'є кожні 1.5 сек
    private float lastAttackTime;
    public float damageAmount = 10f; // Сила удару

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
        // 1. Якщо у нас є ціль, перевіряємо, чи вона не померла "щойно"
        if (targetTransform != null && targetController != null)
        {
            if (targetController.GetPlayerIsDead())
            {
                // Ціль померла під час погоні - забуваємо її
                targetTransform = null;
                targetController = null;
                agent.isStopped = true;
            }
        }

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
        PlayerController bestController = null;

        foreach (GameObject player in players)
        {
            PlayerController pc = player.GetComponent<PlayerController>();

            if (pc == null || pc.GetPlayerIsDead()) continue; // Пропускаємо мертвих гравців 

            float d = Vector3.Distance(transform.position, player.transform.position);

            // Якщо цей гравець ближче за попереднього знайденого
            if (d < closestDistance)
            {
                closestDistance = d;
                potentialTarget = player.transform;
                bestController = pc;
            }
        }

        // Призначаємо ціль
        targetTransform = potentialTarget;
        targetController = bestController;
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

        if (Time.time >= lastAttackTime + attackInterval)
        {
            // Наносимо урон конкретному гравцю через збережене посилання
            targetController.TakeDamage(damageAmount);

            Debug.Log($"Enemy attacked player for {damageAmount} damage");

            // Оновлюємо таймер
            lastAttackTime = Time.time;

            // Тут можна запустити анімацію удару через NetworkAnimator
            networkAnimator.SetTrigger("Attack");
        }
    }
}