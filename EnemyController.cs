using UnityEngine;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour
{
    [Header("Movement Settings")]
    public List<Transform> waypoints = new List<Transform>();
    public float moveSpeed = 3f;
    public float waypointReachedDistance = 0.1f;
    public bool randomizeWaypoints = false;
    public bool pingPong = true;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    private int currentWaypointIndex = 0;
    private int direction = 1;
    public SpriteRenderer spriteRenderer;
    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        animator.Play("walk");
        if (randomizeWaypoints && waypoints.Count > 0)
        {
            currentWaypointIndex = Random.Range(0, waypoints.Count);
        }
    }

    void Update()
    {
        if (waypoints.Count == 0) return;

        // Move towards current waypoint
        Transform currentWaypoint = waypoints[currentWaypointIndex];
        Vector2 targetPosition = currentWaypoint.position;
        Vector2 currentPosition = transform.position;

        // Calculate direction
        Vector2 movementDirection = (targetPosition - currentPosition).normalized;

        // Move enemy
        transform.position = Vector2.MoveTowards(
            currentPosition,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        // Flip sprite based on movement direction
        if (movementDirection.x != 0)
        {
            transform.localScale = new Vector3(-Mathf.Sign(movementDirection.x) * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

        // Check if waypoint reached
        float distanceToWaypoint = Vector2.Distance(currentPosition, targetPosition);
        if (distanceToWaypoint < waypointReachedDistance)
        {
            SelectNextWaypoint();
        }
    }

    void SelectNextWaypoint()
    {
        if (pingPong)
        {
            // Move back and forth between waypoints
            currentWaypointIndex += direction;

            // Change direction if at the end or beginning
            if (currentWaypointIndex >= waypoints.Count || currentWaypointIndex < 0)
            {
                direction *= -1;
                currentWaypointIndex += direction * 2;
            }
        }
        else
        {
            // Cycle through waypoints
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        // Flash red or play hit animation
        StartCoroutine(FlashDamage());
        animator.SetTrigger("hurt");
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator FlashDamage()
    {
        if (spriteRenderer)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
        }
    }

    void Die()
    {
        // Play death animation if available
        if (animator)
        {
            animator.Play("die");
            // Wait for animation to finish before destroying
            Destroy(gameObject, 1f);
        }
        else
        {
            // Destroy immediately if no animation
            Destroy(gameObject);
        }
    }

    // Helper function to create waypoints in editor
    public void CreateWaypoints(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject waypoint = new GameObject("Waypoint " + (waypoints.Count + 1));
            waypoint.transform.position = transform.position + new Vector3(i * 2, 0, 0);
            waypoint.transform.parent = transform.parent;
            waypoints.Add(waypoint.transform);
        }
    }
}