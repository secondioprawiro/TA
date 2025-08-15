using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class SpawnPoint : NetworkBehaviour
{
    public static SpawnPoint instance;

    private void Awake()
    {
        instance = this;
    }
}
