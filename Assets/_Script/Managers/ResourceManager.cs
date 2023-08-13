using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUtilities;

public class ResourceManager : SingletonMonoBehaviour<ResourceManager>
{
    [Header("Bundle")] public List<Crate> Crates;

    public Crate GetRandomCrate()
    {
        var index = Random.Range(0, Crates.Count);
        return Crates[index];
    }
}
