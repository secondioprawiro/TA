using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushWithAnimation : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rigid = hit.collider.attachedRigidbody;

        if(rigid != null)
        {

        }
    }
}
