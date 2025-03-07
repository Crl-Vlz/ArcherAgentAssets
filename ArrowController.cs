using UnityEngine;

public class ArrowController : MonoBehaviour
{
    [Header("Arrow Settings")]
    public float lifetime = 5f;
    public float gravity = 1f;
    public float damageAmount = 10f;

    [Header("Effects")]
    public GameObject hitEffect;

    private Vector2 velocity;
    private bool hasHit = false;
    [HideInInspector] public ArcherController archerController;

    private Rigidbody2D rb;
    private Collider2D col;

    // Variable estática para almacenar la mejor distancia (más cercana al centro) de tiros anteriores.
    // Se reinicia al comienzo de cada episodio.
    private static float bestShotDistance = float.MaxValue;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (hasHit) return;

        // Aplica gravedad manualmente
        velocity.y -= gravity * Time.deltaTime;
        // Mueve la flecha
        transform.position += (Vector3)velocity * Time.deltaTime;
    }

    public void Launch(Vector2 direction, float force)
    {
        velocity = direction.normalized * force;

        if (rb)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = velocity;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        hasHit = true;

        // Detén la flecha
        velocity = Vector2.zero;
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
            rb.freezeRotation = true;
        }

        float shotReward = 0f;
        Vector2 targetCenter;


        // Si golpea al target
        if (other.tag == "Enemy")
        {
            Debug.Log("Hit enemy");
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy)
            {
                enemy.TakeDamage(damageAmount);
            }
            // Usamos el centro del collider impactado como referencia
            targetCenter = other.bounds.center;
            shotReward = EvaluateShot(targetCenter);
        }
        else
        {
            // Para un tiro erróneo, se evalúa la distancia al centro del target esperado.
            if (archerController != null && archerController.targetTransform != null)
            {
                targetCenter = archerController.targetTransform.position;
                shotReward = EvaluateShot(targetCenter);
            }
            else
            {
                // Penalización por defecto si no se puede evaluar el tiro erróneo
                shotReward = -1f;
            }
        }

        // Notifica la recompensa al controlador y al agente
        if (archerController)
        {
            archerController.shotHit = (other.tag == "Enemy");
            archerController.SetReward(shotReward);
            ArcherAgent agent = archerController.GetComponent<ArcherAgent>();
            if (agent != null)
            {
                agent.OnArrowHitTarget();
                agent.AddReward(shotReward);
            }
        }

        // Instancia efecto de impacto si está asignado
        if (hitEffect)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        // Destruye la flecha tras un breve retraso
        Destroy(gameObject, 2f);
    }

    // Evalúa el tiro comparando la distancia entre el punto de impacto y el centro del target.
    // Compara con tiros anteriores y devuelve una recompensa positiva si mejora (más cerca) o negativa si empeora.
    // Regresa valor de recompensa basado en la mejora o empeoramiento
    private float EvaluateShot(Vector2 targetPosition)
    {
        Vector2 shotDirection = ((Vector2)transform.position - (Vector2)archerController.transform.position).normalized; // Dirección de disparo
        Vector2 targetDirection = (targetPosition - (Vector2)archerController.transform.position).normalized; // Dirección esperada hacia el target
        
        float distanceFromCenter = Vector2.Distance((Vector2)transform.position, targetPosition);
        float maxDistance = 5f; // Ajusta según el tamaño del objetivo

        // Verificar si el disparo fue en la dirección correcta
        float alignment = Vector2.Dot(shotDirection, targetDirection);

        // Penalizar tiros en dirección equivocada
        if (alignment < 0) 
        {
            return -1.0f; // Disparo en dirección contraria => máxima penalización
        }

        // Recompensa basada en precisión (solo si dispara en la dirección correcta)
        float reward = Mathf.Clamp(1.0f - (distanceFromCenter / maxDistance), -1.0f, 1.0f);
        
        return reward;
    }

    // Reinicia la mejor distancia registrada; utilízalo al comenzar un nuevo episodio.
    public static void ResetBestShotDistance()
    {
        bestShotDistance = float.MaxValue;
    }
}
