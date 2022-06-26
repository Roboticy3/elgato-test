using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Synchronize the rotation of a player's model with their camera
public class HeadTracking : MonoBehaviour
{
    public Transform playerCam;

    void Update()
    {
        transform.localRotation = Quaternion.Euler(playerCam.eulerAngles.x,0,0);
    }
}
