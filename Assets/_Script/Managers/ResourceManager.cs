using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUtilities;

public class ResourceManager : PersistentSingletonMonoBehaviour<ResourceManager>
{
    [Header("Storage")] public StackStorage stackStorage;

    [Header("Bundle")] public List<Crate> crates;

    public Crate GetRandomCrate()
    {
        var index = Random.Range(0, crates.Count);
        return crates[index];
    }
}
