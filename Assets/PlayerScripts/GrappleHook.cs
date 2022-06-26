using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GrappleHook : MonoBehaviourPunCallbacks, IPunObservable
{

    //Layermask to ignore grapples
    public LayerMask IgnoreMe;

    [Header("Player References")]
    //Player camera object, can be the actual camera or the object the camera is attached to
    public Transform playerCamera;
    //player rigidbody
    public Rigidbody rb;
    //player movement script
    public PlayerMovement pm;


    //Variable to store keycode to use Grapple
    public String pc;

    [Header("Grappling Data")]
    //variable used to store the hit target
    public Rigidbody target;

    //Object to return hooks to and draw line from
    public Transform startPoint;

    //The speed that the grapples pull the player
    private float pullSpeed = 35.0f;

    //The furthest distance away that this GrappleHook can grapple
    private float range = 100.0f;

    [Header("Grappling States")]
    //store whether this GrappleHook is active, pulling or is the primary hook
    public bool isgrappling = false;
    public bool ispulling = false;
    public bool isprimary = true;

    [Header("Grapple Rendering")]
    //Linerenderer to render Grapple lines
    public LineRenderer lineRenderer;

    //Previous Grapple velocity for reasons
    private Vector3 prevgrapplevelocity = new Vector3(0f, 0f, 0f);

    [Header("Grappling Target Data")]
    //point to Grapple to, can probably be removed? 
    public Vector3 grapplePoint;

    //Hook of the Grapple, allows linerendering to work in multiplayer and allows player to follow moving objects
    public Transform Obunga;
    private Vector3 prevObungaPosition;

    // Start is called before the first frame update
    void Start()
    {
        //determines if this Grapple is the primary or secondary Grapple to assign the proper keybind.
        if (!isprimary){
            pc = "GrappleLeft";
        }
        else{
            pc = "GrappleRight";
        }

        //set the grapple line to render with 2 points for the start and end
        lineRenderer.positionCount = 2;
    }

    /// <summary>
    /// Update the state of GrappleHook each frame
    /// </summary>
    void Update(){
        //ignore inputs if the client does not own this GrappleHook's player
        if (!pm.view.IsMine) { return; }

        //if the client has just pressed down the grapple button, make a RayCast to see if the grapple hits, and update GrappleHook's state accordingly
        if (Input.GetButtonDown(pc))
        {
            RaycastHit hit;
            Transform cam = playerCamera.transform;
            bool result = Physics.Raycast(cam.position, cam.forward, out hit, range, ~IgnoreMe);

            if (result){
                //set the target RigidBody and the position to grapple to, and put GrappleHook into the grappling state
                target = hit.transform.GetComponent<Rigidbody>();
                grapplePoint = hit.point;
                isgrappling = true;

                //set the parent to adopt the point to and move Grapple point by relative vector to parent in isgrappling && !ispulling
                Obunga.position = grapplePoint;
                Obunga.SetParent(hit.collider.transform);
                if (target != null && target.mass < rb.mass)
                {
                ispulling = true;
                }
            }
        }
        //if the client has just stopped pressing the grapple button, reset GrappleHook's states
        else if (Input.GetButtonUp(pc) && isgrappling)
        {
            isgrappling = false;
            ispulling = false;
            prevgrapplevelocity = new Vector3(0f, 0f, 0f);
            Obunga.SetParent(startPoint);
            Obunga.localPosition = new Vector3(0f, 0f, 0f);
            Obunga.localScale = new Vector3(.25f, .25f, .25f);
        }

        //update the grapple line to match the current positions
        lineRenderer.SetPosition(0, startPoint.transform.position);
        lineRenderer.SetPosition(1, Obunga.position);
    }

    /// <summary>
    /// Execute Grapple() on each physics time step
    /// </summary>
    void FixedUpdate()
    {
        if (!pm.view.IsMine) { return; }

        if (isgrappling){
            Grapple();
        }
    }

    /// <summary>
    /// Assign the velocities and positions necessary to update a grapple
    /// </summary>
    void Grapple()
    {
        //pullSpeed as a Vector3 so it can be used with Vector3.Min() and Vector3.Max()
        Vector3 pullVelocity = new Vector3(pullSpeed, pullSpeed, pullSpeed);

        //if the grapple is not pulling the target, apply a new velocity to the player's RigidBody
        if (!ispulling)
        {
            Vector3 grapplevelocity = (grapplePoint - rb.position).normalized;
            Vector3 newvelocity = (rb.velocity + (grapplevelocity.normalized * 1.5f));

            newvelocity = Vector3.Min(newvelocity, pullVelocity);
            newvelocity = Vector3.Max(newvelocity, -pullVelocity);

            rb.velocity = newvelocity;
            grapplePoint = Obunga.position;
        }
        //otherwise, apply the velocity to the target
        else
        {
            Vector3 grapplevelocity = (transform.position - target.position);
            Vector3 newvelocity = ((target.velocity - prevgrapplevelocity) + grapplevelocity.normalized / 2) * 5f;

            newvelocity = Vector3.Min(newvelocity, pullVelocity);
            newvelocity = Vector3.Max(newvelocity, -pullVelocity);

            target.velocity = newvelocity;
        }
    }

    /// <summary>
    /// Synchronize the positions and state of GrappleHook with the server
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting){
            stream.SendNext(Obunga.position);
            stream.SendNext(isgrappling);
            prevObungaPosition = Obunga.position;
        }
        else {
            Vector3 newObungaPosition = (Vector3)stream.ReceiveNext();
            if ((bool)stream.ReceiveNext()){
                Obunga.parent = null;
                Obunga.position = newObungaPosition;
            }
            else{
                Obunga.parent = startPoint;
                Obunga.localPosition = new Vector3(0f,0f,0f);
            }
        }
    }
}
