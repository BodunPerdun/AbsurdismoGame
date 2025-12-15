using UnityEngine;
using UnityEngine.AI; // Обязательно для использования NavMeshAgent

[RequireComponent(typeof(NavMeshAgent))] // Гарантирует наличие компонента
public class EnemyAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform playerTarget;

    [Header("AI Settings")]
    public float chaseRange = 15f; 
    public float attackRange = 2f; 
    public float lookSpeed = 5f; 

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (GameObject.FindWithTag("Player") != null)
        {
            playerTarget = GameObject.FindWithTag("Player").transform;
        }
        else
        {
            Debug.LogError("Объект с тегом 'Player' не найден!");
        }
    }

    void Update()
    {
        if (playerTarget == null) return;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (distanceToPlayer <= chaseRange)
        {
            ChasePlayer();
        }

        if (distanceToPlayer <= attackRange)
        {
            AttackPlayer();
        }
    }

    void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(playerTarget.position);
    }

    void AttackPlayer()
    {
        agent.isStopped = true;
        Vector3 direction = (playerTarget.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * lookSpeed);
        
    }
}