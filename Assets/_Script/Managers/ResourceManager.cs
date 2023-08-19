using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityUtilities;

public class ResourceManager : SingletonMonoBehaviour<ResourceManager>
{
    [FormerlySerializedAs("Crates")] [Header("Bundle")] public List<Bin> Bins;

    public Bin GetRandomBin()
    {
        var index = Random.Range(0, Bins.Count);
        return Bins[index];
    }
}
