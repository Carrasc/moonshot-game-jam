using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController2D : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    private TrailRenderer trail;

    private Vector3 m_Velocity = Vector3.zero;
    [SerializeField] private float movementSmoothing = 0.07f;	// How much to smooth out the movement
    private float inputX;
    private float inputY;
    private float turnTimer;
    private float turnTimerSet = 0.1f;  // The amount of time the character will freeze after pressing away from a wall
    private float dashTimer;
    private float dashTimerSet = 0.05f;

    [SerializeField] private float moveSpeed = 600f;
    [SerializeField] private float jumpSpeed = 1000f;
    [SerializeField] private float wallSlidingSpeed = 0.5f;
    [SerializeField] private float airDragMultiplier = 0.95f;
    [SerializeField] private float variableJumpHeightMultiplier = 0.5f;
    [SerializeField] private float wallJumpForce;
    [SerializeField] private float dashSpeed = 80f;
    private float fallMultiplier = 2.5f;
    private float lowJumpMultiplier = 6f;

    // This variable is public static so the player combat script can acces the variable
    public static int facingDirection = 1;

    private int amountOfJumps = 2;
    private int amountOfJumpsLeft;

    private bool isLookingLight = true;
    private bool jump;
    private bool lowJump;
    private bool isGrounded;            // Whether or not the player is grounded.
    private bool isTouchingWall;        // Whether or not the player is touching a wall
    private bool isWallSliding;
    private bool canMove;   // variable to freeze the movement for a short time after pressing away from a wall slide( to give the user time to jump normally)
    private bool canFlip;
    private bool isDashing;
    private bool dashInfo;

    private Vector2 wallJumpDirection = new Vector2(1f, 1.5f);

    [SerializeField] private Transform groundCheck;	// A position marking where to check if the player is grounded.
    [SerializeField] private Transform wallCheck;   // A position marking where to check if the player is touching a wall

    const float k_GroundedRadius = .1f; // Radius of the overlap circle to determine if grounded
    public float wallCheckDistance;     // Distance of the raycast to check for a wall
    public float dashDistance = 7f;

    [SerializeField] private LayerMask whatIsGround;


    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        wallJumpDirection.Normalize();
        dashTimer = dashTimerSet;
        trail = GetComponent<TrailRenderer>();
        amountOfJumpsLeft = amountOfJumps;
    }

    void Update()
    {
        CheckInput();
        UpdateAnimations();
        CheckIfWallSliding();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        CheckSurroundings();
        MovePlayer();
        jump = false;

    }

    private void CheckInput()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");

        if (Input.GetButtonDown("Jump"))
        {
            jump = true;
        }

        // If the user is not holding the jump button, perform a low jump (in the MovePlayer function thats in fixed Update)
        if (!Input.GetButton("Jump"))
        {
            lowJump = true;
        }

        // This checks if the character is against a wall and pressing either away from that wall or not
        if (Input.GetButtonDown("Horizontal") && isTouchingWall)
        {
            // Now, if it want to move away from the wall, we will freeze him for a very short time, so he has time to press jump again
            if (!isGrounded && inputX != facingDirection)
            {
                canMove = false;
                canFlip = false;
                turnTimer = turnTimerSet;
            }
        }

        if (Input.GetButtonDown("Fire3") && !isDashing && isGrounded)
        {
            // The player CAN dash if he is the raycast is NOT casting upon the ground layer (whatIsGround)
            RaycastHit2D dashInfo = Physics2D.Raycast(wallCheck.position, transform.right, dashDistance, whatIsGround);

            // If the raycast didnt hit anything, dash the max dash distance 
            if (!dashInfo)
            {
                isDashing = true;
                CameraShake.Instance.ShakeCamera(5f, .2f);
                StartCoroutine(RenderTrail());
                transform.position = new Vector2(transform.position.x + dashDistance * facingDirection, transform.position.y);
                StartCoroutine(DashCooldown());

            }

            // This means the raycast hit a wall, then just dash the distance to that wall 
            else
            {
                isDashing = true;
                CameraShake.Instance.ShakeCamera(5f, .2f);
                StartCoroutine(RenderTrail());
                transform.position = new Vector2(transform.position.x + dashInfo.distance * facingDirection, transform.position.y);
                StartCoroutine(DashCooldown());

            }

        }

        // If he currently cant move, meaning he just pressed away from the wall, decrease the timer 
        if (!canMove)
        {
            turnTimer -= Time.deltaTime;

            // When the timer reaches 0, give the character control again
            if (turnTimer <= 0)
            {
                canMove = true;
                canFlip = true;
            }
        }

    }

    IEnumerator DashCooldown()
    {
        //Print the time of when the function is first called.
        //Debug.Log("Started Coroutine at timestamp : " + Time.time);

        //yield on a new YieldInstruction that waits for 5 seconds.
        yield return new WaitForSeconds(5);
        isDashing = false;

        //After we have waited 5 seconds print the time again.
        //Debug.Log("Finished Coroutine at timestamp : " + Time.time);
    }

    IEnumerator RenderTrail()
    {
        trail.emitting = true;

        //yield on a new YieldInstruction that waits for 5 seconds.
        yield return new WaitForSeconds(trail.time);
        trail.emitting = false;
    }

    private void CheckIfWallSliding()
    {
        if (isTouchingWall && rb.velocity.y < 0.1 && !isGrounded && inputX == facingDirection)
        {
            isWallSliding = true;
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void CheckSurroundings()
    {
        isTouchingWall = Physics2D.Raycast(wallCheck.position, transform.right, wallCheckDistance, whatIsGround);
        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, k_GroundedRadius, whatIsGround);

    }

    private void UpdateAnimations()
    {
        anim.SetFloat("Speed", Mathf.Abs(inputX));
        //anim.SetBool("jump", jump);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("yVelocity", rb.velocity.y);
        anim.SetBool("isWallSliding", isWallSliding);
    }

    void MovePlayer()
    {
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0 && lowJump)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
            lowJump = false;
        }

        // If the player is jumping, the movement in the air is reduced (so we dont have the same control as while walking) 
        if (!isGrounded && !isWallSliding && inputX == 0)
        {
            rb.velocity = new Vector2(rb.velocity.x * airDragMultiplier, rb.velocity.y);
        }
        // The player is grounded, just move normally
        else if (canMove)
        {
            // Move the character by finding the target velocity
            Vector3 targetVelocity = new Vector2(inputX * Time.fixedDeltaTime * moveSpeed, rb.velocity.y);
            // And then smoothing it out and applying it to the character
            rb.velocity = Vector3.SmoothDamp(rb.velocity, targetVelocity, ref m_Velocity, movementSmoothing);
        }


        // if the player is wall sliding (touching wall and going in a negative Y speed)
        if (isWallSliding)
        {
            if (rb.velocity.y < -wallSlidingSpeed)
            {
                rb.velocity = new Vector2(rb.velocity.x, -wallSlidingSpeed);
            }
        }


        // After checking the surroundings and knowing if the are on the ground or not ...
        // If the player should jump... (meaning jump is true, but he is currently on the ground)
        if (isGrounded && jump) // Normal ground Jump
        {
            // Add a vertical force to the player.
            rb.AddForce(new Vector2(0f, jumpSpeed));
        }
        else if ((isWallSliding || isTouchingWall) && jump) // Wall Jump
        {
            // Reset the Y velocity if he wants to wall jump, to keep the jump consistant, even is he was falling down fast (dont jump with momentum)
            rb.velocity = new Vector2(rb.velocity.x, 0);
            // Add the jumping force as a normalized vector 
            Vector2 forceToAdd = new Vector2(wallJumpForce * wallJumpDirection.x * -facingDirection, wallJumpForce * wallJumpDirection.y);
            rb.AddForce(forceToAdd, ForceMode2D.Impulse);

            // Option 1. If the character jumped, reset the timer and give him full control again
            turnTimer = 0;
            canMove = true;
            canFlip = true;
            // Option 2. Freeze the character after jump as well, so even if the presses against the wall, he jump in a diagonal for a short time
            //canMove = false;
            //canFlip = false;
            //turnTimer = turnTimerSet;
        }



        // Check the inputX to know if we should rotate the player
        if (inputX > 0 && !isLookingLight)
        {
            Flip();
        }
        else if (inputX < 0 && isLookingLight)
        {
            Flip();
        }
    }

    void DisableFlip()
    {
        canFlip = false;
    }

    void EnableFlip()
    {
        canFlip = true;
    }

    void Flip()
    {
        // Dont rotate the character is its wall sliding
        if (!isWallSliding && canFlip)
        {
            isLookingLight = !isLookingLight;
            facingDirection = facingDirection * -1;
            transform.Rotate(0f, 180, 0f);
        }
    }

    private void OnDrawGizmos()
    {
        if (groundCheck == null)
            return;

        Gizmos.DrawWireSphere(groundCheck.position, k_GroundedRadius);
        Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + wallCheckDistance, wallCheck.position.y, wallCheck.position.z));
        Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + dashDistance, wallCheck.position.y, wallCheck.position.z));
    }
}
