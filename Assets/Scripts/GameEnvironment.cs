using System.Collections.Generic;
using UnityEngine;

public sealed class GameEnvironment
{
    private static GameEnvironment instance;
    
    public static GameEnvironment Instance
    {
        get
        {
            if (instance != null) return instance;
            
            instance = new GameEnvironment();
                
            instance.Checkpoints.AddRange(GameObject.FindGameObjectsWithTag("Checkpoint"));

            return instance;
        }
    }

    public List<GameObject> Checkpoints { get; private set; } = new();
}