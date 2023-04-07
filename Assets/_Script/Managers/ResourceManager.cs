using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUtilities;

public class ResourceManager : PersistentSingletonMonoBehaviour<ResourceManager>
{
    [Header("Storage")] public StackStorage stackStorage;

    [Header("Bundle")] public Crate crate;
}
