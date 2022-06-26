using System;
using UnityEngine;
using Photon.Pun;

public class PlayerMovement : MonoBehaviour {

    //Cringe ass networking shit
    [Header("Don't Touch These")]
    public PhotonView view;

    public float velocity;

    //Assingables
    [Header("References")]
    public Transform playerCam;
    public Transform orientation;
    public Animator animator;
    public GameObject playercapsule;
    
    //Other
    private Rigidbody rb;
    private CapsuleCollider CapCol;

    //Rotation and look
    [Header("Camera Settings")]
    private float xRotation;
    private float sensitivity = 50f;
    private float sensMultiplier = 1f;
    
    //Movement
    [Header("Movement Settings")]
    public float moveSpeed = 4500;
    public float maxSpeed = 20;
    public float topSpeed = 40;
    public bool grounded;
    public bool prevgrounded;
    public LayerMask whatIsGround;
    
    public float counterMovement = 0.175f;
    private float threshold = 0.01f;
    //the steepest slope that can be walked on, also the steepest slope that will reset jumps
    [Range(0,90)]
    public float maxSlopeAngle = 35f;

    //Crouch & Slide
    [Header("Crouch Settings")]
    public float slideForce = 400;
    public float slideCounterMovement = 2f;

    //Jumping
    [Header("Jump Settings")]
    public float jumpCooldown = 0.25f;
    public float jumpForce = 1700f;
    private bool readyToJump = true;
    
    //WallRunning
    [Header("Wall Running Settings")]
    public bool WallRunning = false;
    public bool CanWallRun = true;
    public int WallRunDirection;
    public float WallRunModifier;

    //Input
    [Header("Input Readouts")]
    //horizontal and vertical input axes
    float x, y;
    //booleans for if action keys are pressed down
    public bool jumping, sprinting, crouching;
    
    //Sliding
    private Vector3 normalVector = Vector3.up;
    private Vector3 wallNormalVector;

    //Grappling Comms
    private GrappleHook hook1;
    private GrappleHook hook2;


    void Awake() {
        rb = GetComponent<Rigidbody>();
        CapCol = GetComponentInChildren<CapsuleCollider>();
    }
    
    /// <summary>
    /// Set aspects of the player that cannot be predetermined in the editor
    /// Set up the client's cursor
    /// </summary>
    void Start() {
        //set the cursor of the client to be locked to the center of the screen and invisible while tabbed in
        //? should this be inside the if (view.IsMine) block?
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        //the player prefab will have a PhotonView component attached to it, retrieve it here
        view = GetComponent<PhotonView>();
        //assure that the client owns this player before assigning it the main camera and setting its collision layer
        if (view.IsMine)
        {
            Camera.main.transform.SetParent(playerCam, false);
            Camera.main.transform.localPosition = Vector3.zero;
            playercapsule.layer = 2;
        }
    }

    /// <summary>
    /// run Movement() in FixedUpdate() for more consistent physics
    /// </summary>
    private void FixedUpdate() {
        if (view.IsMine)
        {
            Movement();
        }
    }

    /// <summary>
    /// remind velocity of this GameObject's velocity for smoother networking
    /// run visual and input aspects of player movement in Update() for smoother movement feel
    /// </summary>
    private void Update() {
        velocity = rb.velocity.magnitude;
        if (view.IsMine)
        {
            MyInput();
            Look();
            Animate();
        }
    }

    /// <summary>
    /// Find user input. Should put this in its own class but im lazy
    /// </summary>
    private void MyInput() {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        jumping = Input.GetButton("Jump"); 
        crouching = Input.GetButton("Crouch");
      
        //Crouching
        if (Input.GetButtonDown("Crouch"))
            StartCrouch();
        if (Input.GetButtonUp("Crouch"))
            StopCrouch();
    }

    /// <summary>
    /// Initiates a crouch on the player by halving their physics collider's height. 
    /// The crouch will lower the player to their base, and will raise them slightly if they are in the air, making the player appear to crouch on the ground or curl around their center of mass in the air.
    /// Grounded crouches will also push the player across the ground in a slide
    /// </summary>
    private void StartCrouch() {
        //halve the height of the player.
        CapCol.height = 1;
        if (grounded) {
            rb.AddForce(orientation.transform.forward * slideForce);
        }
        else if (!WallRunning) {
            transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
        }
    }

    /// <summary>
    /// Ends a crouch by resetting the height of the player's physics collider
    /// </summary>
    private void StopCrouch() {
        CapCol.height = 2;
    }

    /// <summary>
    /// Updates the grounded and jumping booleans
    /// </summary>
    private void GroundCheck() {
            prevgrounded = grounded;
            RaycastHit hitInfo;
            if (Physics.Raycast(rb.transform.position, Vector3.down, out hitInfo, (CapCol.height/2)+.5f))
            {
                grounded = true;
            }
            else
            {
                grounded = false;
            }
            if (!prevgrounded && grounded && jumping)
            {
                jumping = false;
            }
    }

    /// <summary>
    /// Generally handle the movement of the player by calling other methods of PlayerMovement based on conditions
    /// </summary>
    private void Movement() {

        GroundCheck();
        
        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        //Counteract sliding and sloppy movement
        CounterMovement(x, y, mag);
        
        //If holding jump && ready to jump, then jump
        if (grounded && readyToJump && jumping) Jump();

        //Set max speed
        float maxSpeed = this.maxSpeed;
        
        //If sliding down a ramp, add force down so player stays grounded and also builds speed
        if (crouching && grounded && readyToJump) {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000);
            return;
        }

        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;

        //Some multipliers
        float multiplier = 1f, multiplierV = 1f;
        
        // Movement in air
        if (!grounded && !WallRunning) {
            rb.AddForce(Vector3.down * Time.deltaTime * (9.8f * rb.mass* 110));
        }
        
        // Movement while sliding I think this is what causes the no friction while crouching?
        if (grounded && crouching) multiplierV = 0f;

        //Apply forces to move player
        rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * multiplier);

        WallRun();
    }

    /// <summary>
    /// Add the physical forces required to make the player jump
    /// </summary>
    private void Jump() {
        readyToJump = false;

        //Add jump forces
        rb.AddForce(Vector2.up * jumpForce * 1.5f);
        rb.AddForce(normalVector * jumpForce * 0.5f);
        
        //If jumping while falling, reset y velocity.
        Vector3 vel = rb.velocity;
        if (rb.velocity.y > 0) 
            rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);
        
        Invoke(nameof(ResetJump), jumpCooldown);
    }

    private void ResetJump() {
        readyToJump = true;
    }
    
    /// <summary>
    /// Handle WallRunning by checking conditions and calling StartWallRun() and EndWallRun().
    /// </summary>
    private void WallRun() {
        //Check inputs and grounded state before casing a ray in the direction matching the inputs
        //if all conditions are correct and the ray hits, start a wall run from the correct side
        //not sure how necessary CanWallRun is because as far as im sure it will always be true, but other scripts can access and change it so its a nice thing to have
        if (!grounded && CanWallRun && WallRunInput(-1) && Physics.Raycast(rb.position, playerCam.transform.right, 2f)) {
            StartWallRun(-1);
        }
        else if (!grounded && CanWallRun && WallRunInput(1) && Physics.Raycast(rb.position, -playerCam.transform.right, 2f)) {
            StartWallRun(1);
        }
        else { EndWallRun(); }

        //if the player is now wallrunning, add forces to their movement to move them along the wall
        if (WallRunning) {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(orientation.forward * rb.velocity.magnitude);
            rb.AddForce(orientation.forward * 150f);
        }

        if (WallRunning && Input.GetButton("Jump") && readyToJump)
        {
            WallJump(WallRunDirection);
        }
    }

    /// <summary>
    /// Return true if the inputs are valid for a wall run in the direction of the input direction.
    /// Can check for left-sided input if the argument left is set to true.
    /// </summary>
    /// <param name="direction">set to -1 to check for a right-sided wall run, 1 for a left-sided wall run, and 0 for... something</param>
    /// <returns></returns>
    private bool WallRunInput(int direction)
    {
        bool side = false;
        if (direction == -1) { side = Input.GetAxisRaw("Horizontal") > 0; }
        else if (direction == 1) { side = Input.GetAxisRaw("Horizontal") < 0; }
        else { side = Input.GetAxisRaw("Horizontal") == 0 ;}
        return Input.GetAxisRaw("Vertical") > 0 && side;
    }

    /// <summary>
    /// Begins a wall run in the input direction.
    /// </summary>
    /// <param name="direction">set to -1 for a right-sided wall run and to 1 for a left-sided wall run</param>
    /// <returns></returns>
    private void StartWallRun(int direction) {
        if (WallRunning == false){
            Invoke(nameof(ResetJump), jumpCooldown);
        }
        WallRunDirection = direction;
        WallRunning = true;
        //make sure player is not using gravity
        rb.useGravity = false;
    }

    /// <summary>
    /// Ends a wall run.
    /// </summary>
    private void EndWallRun() {
        WallRunDirection = 0;
        rb.useGravity = true;
        WallRunning = false;
    }

    /// <summary>
    /// Call EndWallRun and launch the player away from the wall they are running on using a modified jump.
    /// </summary>
    /// <param name="direction">set to -1 to jump out of a rihgt-sided wall run and to 1 for a left-sided wall run</param>
    /// <returns></returns>
    private void WallJump(int direction) {
        EndWallRun();
        Vector3 f = Vector3.up + orientation.right * direction;
        rb.AddForce(f * jumpForce*1.5f);
        Invoke(nameof(ResetJump), jumpCooldown);
    }

    private float desiredX;
    private void Look() {
        float mouseX = Input.GetAxis("LookX") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("LookY") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        //Find current look rotation
        Vector3 rot = playerCam.transform.localRotation.eulerAngles;
        desiredX = rot.y + mouseX;
        
        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Perform the rotations
        playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0);
        orientation.transform.localRotation = Quaternion.Euler(0, desiredX, 0);

    }

    private void CounterMovement(float x, float y, Vector2 mag) {
        if (!grounded || jumping) return;

        //Slow down sliding
        if (crouching && grounded) {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * slideCounterMovement);
            return;
        }

        //Counter movement
        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0)) {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0)) {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }
        
        //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
        if (Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > maxSpeed) {
            float fallspeed = rb.velocity.y;
            //Vector3 n = rb.velocity.normalized * maxSpeed;
            //rb.velocity = new Vector3(n.x, fallspeed, n.z);
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * 1.5f);
        }

    }

    /// <summary>
    /// Find the velocity relative to where the player is looking
    /// Useful for vectors calculations regarding movement and limiting movement
    /// </summary>
    /// <returns>
    /// The speed of the player scaled by the vector of the angle between the horizontal angles of the camera and RigidBody velocity
    /// </returns>
    public Vector2 FindVelRelativeToLook() {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        //shortest angle between the look and move angles
        float u = Mathf.DeltaAngle(lookAngle, moveAngle) * Mathf.Deg2Rad;

        float magnitue = rb.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u);
        float xMag = magnitue * Mathf.Sin(u);
        
        return new Vector2(xMag, yMag);
    }

    /// <summary>
    /// Control the state of this player's animator using the movement booleans
    /// </summary>
    private void Animate() {
        if (rb.velocity.magnitude > 1f && grounded) {
            animator.SetBool("Walking", true);
        }
        else{
            animator.SetBool("Walking", false);
        }
        animator.SetBool("Grounded", grounded);
        animator.SetBool("Crouching", crouching);
    }
    
    /// <summary>
    /// Use collision detection to refresh grounded state
    /// </summary>
    private void OnCollisionStay(Collision other) {
        //Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        //Iterate through every collision in a physics update and check if it passes the condition for ground

        for (int i = 0; i < other.contactCount; i++) {
            Vector3 normal = other.contacts[i].normal;
            //ground qualifies as a surface that is not too steep for the player to walk on
            float angle = Vector3.Angle(Vector3.up, normal);
            if (angle <= maxSlopeAngle) {
                grounded = true;
                break;
            }
        }
    }

}