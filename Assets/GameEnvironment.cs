using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// sealed means that class can NOT be inherited!
public sealed class GameEnvironment
{
    private static GameEnvironment instance;
    private List<GameObject> checkpoints = new List<GameObject>();
    public List<GameObject> Checkpoints { get { return checkpoints; } }

    // use singleton pattern to make juggling all the states easier
    public static GameEnvironment Singleton
    {
        get
        {
            if (instance == null)
            {
                instance = new GameEnvironment();
                instance.Checkpoints.AddRange(
                    GameObject.FindGameObjectsWithTag("Checkpoint"));

                instance.checkpoints = instance.checkpoints.OrderBy(x => x.name).ToList();
            }
            return instance;
        }
    }
}
