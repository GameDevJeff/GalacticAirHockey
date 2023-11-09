using System;
using UnityEngine;

public class scoreZone : MonoBehaviour
{
    public GameStats gameStats;
    public Collider puck;
    public bool isPlayer1;

    public Action<bool> scoredGoal; 

    private void OnTriggerEnter(Collider other)
    {
        if (other == puck)
        {
            scoredGoal?.Invoke(isPlayer1);
        }

        
    }
}
