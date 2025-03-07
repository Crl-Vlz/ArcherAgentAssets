using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Gamemanager : MonoBehaviour
{
    [Header("Player Settings")]
    public ArcherController archer;
    public Transform playerSpawnPoint;
    
    [Header("Enemy Settings")]
    public EnemyController enemyPrefab;
    public Transform enemySpawnPoint;
    public Transform[] enemyWaypoints;
    
    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI forceText;
    public TextMeshProUGUI angleText;
    public Slider forceSlider;
    public Slider angleSlider;
    
    [Header("Game Settings")]
    public bool autoRespawnEnemy = true;
    public float respawnDelay = 3f;
    public int maxScore = 100;
    
    // ML Agent integration variables
    [HideInInspector] public int currentEpisode = 0;
    [HideInInspector] public int maxEpisodes = 1000;
    [HideInInspector] public float totalReward = 0f;
    
    private int score = 0;
    public EnemyController currentEnemy;
    
    void Start()
    {
        InitializeGame();
    }
    
    void Update()
    {
        // Update UI if available
        UpdateUI();
        
        // Check for reset key (R)
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetGame();
        }
    }
    
    void InitializeGame()
    {
        // Spawn player if needed
        if (archer && playerSpawnPoint)
        {
            archer.transform.position = playerSpawnPoint.position;
        }
        
        // Spawn initial enemy
        SpawnEnemy();
        
        // Reset score
        score = 0;
        UpdateUI();
    }
    
    void SpawnEnemy()
    {
        if (!enemyPrefab || !enemySpawnPoint) return;
        
        // Create enemy
        currentEnemy = Instantiate(enemyPrefab, enemySpawnPoint.position, Quaternion.identity);
        
        // Assign waypoints
        if (enemyWaypoints != null && enemyWaypoints.Length > 0)
        {
            currentEnemy.waypoints.Clear();
            foreach (Transform waypoint in enemyWaypoints)
            {
                currentEnemy.waypoints.Add(waypoint);
            }
        }
    }
    
    // Called when enemy is destroyed
    public void OnEnemyDestroyed()
    {
        
        score += 10;
        UpdateUI();
        Debug.Log("alo?");
        // For ML-Agent tracking
        totalReward += 10f;
        
        if (autoRespawnEnemy)
        {
            Debug.Log("alo?");
            Invoke("SpawnEnemy", respawnDelay);
        }
        
        // Check win condition
        if (score >= maxScore)
        {
            // Game completed
            Debug.Log("Game Completed!");
            
            // Could trigger win screen here
        }
    }
    
    void UpdateUI()
    {
        if (scoreText)
        {
            scoreText.text = "Score: " + score;
        }
        
        if (archer)
        {
            if (forceText)
            {
                forceText.text = "Force: " + archer.currentForce.ToString("F1");
            }
            
            if (angleText)
            {
                angleText.text = "Angle: " + archer.currentAngle.ToString("F1") + "°";
            }
            
            if (forceSlider)
            {
                forceSlider.minValue = archer.minForce;
                forceSlider.maxValue = archer.maxForce;
                forceSlider.value = archer.currentForce;
            }
            
            if (angleSlider)
            {
                angleSlider.minValue = archer.minAngle;
                angleSlider.maxValue = archer.maxAngle;
                angleSlider.value = archer.currentAngle;
            }
        }
    }
    
    public void ResetGame()
    {
        // Destroy current enemy if exists
        if (currentEnemy)
        {
            Destroy(currentEnemy.gameObject);
        }
        
        // Reset player
        if (archer)
        {
            archer.ResetEnvironment();
        }
        
        // Reset game state
        score = 0;
        totalReward = 0f;
        currentEpisode++;
        // Spawn new enemy
        SpawnEnemy();
        
        // Update UI
        UpdateUI();
    }
    
    // Helper method for ML-Agent integration (to be used later)
    public void CollectObservations(float[] observations)
    {
        int i = 0;
        
        // Player position
        if (archer)
        {
            observations[i++] = archer.transform.position.x;
            observations[i++] = archer.currentAngle / 90f; // Normalize angle
            observations[i++] = archer.currentForce / archer.maxForce; // Normalize force
        }
        
        // Enemy position
        if (currentEnemy)
        {
            observations[i++] = currentEnemy.transform.position.x;
            observations[i++] = currentEnemy.transform.position.y;
        }
    }
}