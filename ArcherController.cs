using UnityEngine;
using System.Collections;

public class ArcherController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float minX = -8f;
    public float maxX = 8f;

    [Header("Shooting Settings")]
    public Transform bowPosition;
    public GameObject arrowPrefab;
    public float minForce = 5f;
    public float maxForce = 20f;
    public float currentForce = 10f;
    public float forceIncreaseSpeed = 5f;
    public float minAngle = 0f;
    public float maxAngle = 90f;
    public float currentAngle = 45f;
    public float rotationSpeed = 30f;

    [Header("Visual Indicators")]
    public LineRenderer aimLine;
    public int aimLinePoints = 20;
    public float aimLineLength = 3f;

    private bool isChargingShot = false;
    private bool canShoot = true;
    private Rigidbody2D rb;
    private Animator animator;

    // ML-Agent related variables for future implementation
    [HideInInspector]
    public float lastShotScore = 0f;
    [HideInInspector]
    public bool shotHit = false;

    public Transform targetTransform;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Configure the aim line
        if (aimLine)
        {
            aimLine.positionCount = aimLinePoints;
            aimLine.enabled = false;
        }

        if (targetTransform == null)
        {
            GameObject target = GameObject.FindGameObjectWithTag("Enemy"); 
            if (target)
                targetTransform = target.transform;
        }
    }

    void Update()
    {
        // Horizontal movement
        float horizontalInput = Input.GetAxis("Horizontal");
        MoveHorizontally(horizontalInput);

        // Angle adjustment
        float angleInput = 0f;
        if (Input.GetKey(KeyCode.W)) angleInput = 1f;
        if (Input.GetKey(KeyCode.S)) angleInput = -1f;
        AdjustAngle(angleInput);

        // Force adjustment
        if (Input.GetKey(KeyCode.UpArrow))
        {
            IncreaseForce();
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            DecreaseForce();
        }

        // Shooting
        if (Input.GetKeyDown(KeyCode.Space) && canShoot)
        {
            ShootArrow();
        }

        // Update aim line if charging shot
        if (isChargingShot)
        {
            UpdateAimLine();
        }
    }

    public void MoveHorizontally(float direction)
    {
        Vector2 newPosition = rb.position + new Vector2(direction * moveSpeed * Time.deltaTime, 0);
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        rb.position = newPosition;

        // Flip character based on direction
        if (direction != 0)
        {
            //transform.localScale = new Vector3(-Mathf.Sign(direction) * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

            // Play animation if available
            if (animator) animator.SetBool("walking", true);
        }
        else
        {
            if (animator) animator.SetBool("walking", false);
        }
    }

    public void AdjustAngle(float direction)
    {
        currentAngle += direction * rotationSpeed * Time.deltaTime;
        currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);

        if (bowPosition)
        {
            // Adjust for character flip
            float sign = Mathf.Sign(transform.localScale.x);
            float actualAngle = sign > 0 ? currentAngle : -currentAngle;
            bowPosition.rotation = Quaternion.Euler(0, 0, actualAngle);
        }
    }

    public void IncreaseForce()
    {
        currentForce += forceIncreaseSpeed * Time.deltaTime;
        currentForce = Mathf.Clamp(currentForce, minForce, maxForce);
    }

    public void DecreaseForce()
    {
        currentForce -= forceIncreaseSpeed * Time.deltaTime;
        currentForce = Mathf.Clamp(currentForce, minForce, maxForce);
    }

    public void ShootArrow()
    {
        isChargingShot = false;
        canShoot = false;

        if (aimLine) aimLine.enabled = false;

        // Create arrow
        if (arrowPrefab && bowPosition)
        {
            GameObject arrow = Instantiate(arrowPrefab, bowPosition.position,bowPosition.rotation);
            ArrowController arrowController = arrow.GetComponent<ArrowController>();

            if (arrowController)
            {
                // Calculate direction based on character orientation and angle
                float sign = transform.localScale.x > 0 ? 1 : -1; // Asegurar que el signo es correcto
                Vector2 direction = new Vector2(-sign * Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                    Mathf.Sin(currentAngle * Mathf.Deg2Rad)
                ).normalized; // Normalizar para evitar velocidades inesperadas

                arrowController.Launch(direction, currentForce);

                arrowController.archerController = this;
                // Play shoot animation if available
                if (animator) animator.SetTrigger("Shoot");
            }
        }

        // Reset for next shot
        StartCoroutine(ResetShot());
    }

    private IEnumerator ResetShot()
    {
        yield return new WaitForSeconds(0.5f);
        canShoot = true;
        // currentForce = minForce;

        if (animator) animator.SetTrigger("Idle");
    }

    private void UpdateAimLine()
    {
        if (!aimLine) return;

        aimLine.enabled = true; // Asegurarse de que la línea está activa

        float sign = Mathf.Sign(transform.localScale.x);
        Vector2 startPos = bowPosition ? bowPosition.position : transform.position;
        Vector2 direction = new Vector2(
            -sign * Mathf.Cos(currentAngle * Mathf.Deg2Rad),
            Mathf.Sin(currentAngle * Mathf.Deg2Rad)
        );

        float scaledForce = currentForce / maxForce;
        float lineLength = aimLineLength * scaledForce;

        for (int i = 0; i < aimLinePoints; i++)
        {
            float t = (float)i / (aimLinePoints - 1);
            Vector3 point = startPos + direction * t * lineLength;
            aimLine.SetPosition(i, point);
        }
    }


    // Functions for ML-Agent integration (to be used later)
    public void SetReward(float reward)
    {
        lastShotScore = reward;
    }

    public void ResetEnvironment()
    {
        transform.position = new Vector3(0, transform.position.y, 0);
        currentAngle = 45f;
        currentForce = minForce;
        lastShotScore = 0f;
        shotHit = false;
    }
    public void ShootWithParameters(float angle, float force)
    {
        // Establece el ángulo                 QUIZA TENGA BUG DE FLECHA volteada
        currentAngle = Mathf.Clamp(angle, minAngle, maxAngle);
        float sign = Mathf.Sign(transform.localScale.x);
        float actualAngle = sign > 0 ? currentAngle : 180 - currentAngle;
        if (bowPosition) bowPosition.rotation = Quaternion.Euler(0, 0, actualAngle);

        // Establece la fuerza
        currentForce = Mathf.Clamp(force, minForce, maxForce);

        // Inicia el disparo
        isChargingShot = true;
        //currentForce = force;
        UpdateAimLine();
        ShootArrow();
    }

    // Añade este método para que el agente de QLearning pueda disparar fácilmente
    public void AIShoot()
    {
        if (canShoot)
        {
            ShootWithParameters(currentAngle, currentForce);
        }
    }
}