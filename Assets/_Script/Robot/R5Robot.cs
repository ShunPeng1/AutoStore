using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class R5Robot : MonoBehaviour
{
    [SerializeField] private float speed = 1f;
    
    private Rigidbody _rigidbody;
    
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"), vertical = Input.GetAxisRaw("Vertical"); 
        _rigidbody.velocity = new Vector3(horizontal, 0,vertical )*speed;
    }
}
