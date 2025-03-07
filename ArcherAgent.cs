using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class ArcherAgent : Agent
{
    private Rigidbody2D rBody2D;
    public ArcherController archerController;
    private float episodeTime;
    private Gamemanager gameManager;
    private bool arrowFired;

    void Start()
    {
        rBody2D = GetComponent<Rigidbody2D>();
        archerController = GetComponent<ArcherController>();
        gameManager = FindObjectOfType<Gamemanager>();
    }

    public Transform agentSpawn;
    public Transform targetSpawn;

    public override void OnEpisodeBegin()
    {
        agentSpawn.localPosition = new Vector3(Random.value * -7 - 1.3f, -4.55f, 0); //Spawn the archer inside the range
        targetSpawn.localPosition = new Vector3(Random.value * 7 + 1.3f, -4.55f, 0); //Spawn the target inside the range
        rBody2D.linearVelocityX = 1; // Sligthly move the agent to the right to fix arrow shooting
        episodeTime = 0f; // Reset episode time
        arrowFired = false; // Reset arrow fired flag
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(agentSpawn.localPosition); // Add the agent's position to the observation
        sensor.AddObservation(targetSpawn.localPosition); // Add the target's position to the observation

        // Agent Velocity
        sensor.AddObservation(rBody2D.linearVelocity.x);

        // Archer specific observations
        if (archerController != null)
        {
            sensor.AddObservation(archerController.currentAngle / archerController.maxAngle); // Normalize angle
            sensor.AddObservation(archerController.currentForce / archerController.maxForce); // Normalize force
        }

        // Game manager observations
        if (gameManager != null)
        {
            if (gameManager.archer != null)
            {
                sensor.AddObservation(gameManager.archer.transform.position); // Add archer's position
            }
            if (gameManager.currentEnemy != null)
            {
                sensor.AddObservation(gameManager.currentEnemy.transform.position); // Add enemy's position
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0]; // Get the action from the agent
        rBody2D.linearVelocity = new Vector2(moveX * 10, 0); // Move the agent
        
        // Increase or decrease force
        if (actions.DiscreteActions[0] == 1)
        {
            archerController.IncreaseForce();
        }
        else if (actions.DiscreteActions[0] == 2)
        {
            archerController.DecreaseForce();
        }

        // Increase or decrease angle
        if (actions.DiscreteActions[1] == 1)
        {
            archerController.AdjustAngle(1f);
        }
        else if (actions.DiscreteActions[1] == 2)
        {
            archerController.AdjustAngle(-1f);
        }

        // Fire arrow
        if (actions.DiscreteActions[2] == 1)
        {
            archerController.AIShoot();
            arrowFired = true; // Set arrow fired flag
            // Debug.Log("Fire Arrow");
        }

        // End episode after a minute
        episodeTime += Time.deltaTime;
        if (episodeTime >= 10f)
        {
            if (!arrowFired)
            {
                AddReward(-1.0f); // Penalize the agent for not firing an arrow
                Debug.Log("Penalize for not firing");
            }
            EndEpisode();
            Debug.Log("End Episode");
        }
        if (gameManager.currentEnemy == null)
        {
            EndEpisode();
            Debug.Log("Enemy dead! End Episode");
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");

        var discreteActions = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.UpArrow))
        {
            discreteActions[0] = 1; // Increase force
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            discreteActions[0] = 2; // Decrease force
        }

        if (Input.GetKey(KeyCode.W))
        {
            discreteActions[1] = 1; // Increase angle
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActions[1] = 2; // Decrease angle
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            discreteActions[2] = 1; // Fire arrow
        }
    }

    public void OnArrowHitTarget()
    {
        AddReward(2.0f); // Reward the agent for hitting the target
    }
}
