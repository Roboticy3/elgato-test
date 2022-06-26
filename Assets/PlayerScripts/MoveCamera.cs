using UnityEngine;
using Photon.Pun;

/// <summary>
/// Attach to the client's main camera to move and rotate it into the right position
/// </summary>
public class MoveCamera : MonoBehaviour {

    [Header("Player Reference")]
    //reference to player
    public PlayerMovement pm;

    [Header("Tilt Options")]
    //the amount the camera will tilt when wallrunning in degrees
    public float tilt = 10f;
    
    void Update() {
        transform.position = pm.playerCam.transform.position;
        Vector3 eul = transform.localRotation.eulerAngles;
        transform.localRotation = Quaternion.Euler(eul.x, eul.y, pm.WallRunDirection * -tilt);
    }
}