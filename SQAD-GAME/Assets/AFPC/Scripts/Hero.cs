using System.Security.Cryptography;
using System.ComponentModel;
using UnityEngine;
using AFPC;

/// <summary>
/// Example of setup AFPC with Lifecycle, Movement and Overview classes.
/// </summary>
public class Hero : MonoBehaviour {

    /* UI Reference */
    public HUD HUD;

    /* Lifecycle class. Damage, Heal, Death, Respawn... */
    public Lifecycle lifecycle;

    /* Movement class. Move, Jump, Run... */
    public Movement movement;

    /* Overview class. Look, Aim, Shake... */
    public Overview overview;

    public bool onStairs = false;


    /* Optional assign the HUD */
    private void Awake () {
        if (HUD) {
            HUD.hero = this;
        }
    }

    /* Some classes need to initizlize */
    private void Start () {

        /* a few apllication settings for more smooth. This is Optional. */
        QualitySettings.vSyncCount = 0;
        Cursor.lockState = CursorLockMode.Locked;

        /* Initialize lifecycle and add Damage FX */
        lifecycle.Initialize();
        lifecycle.AssignDamageAction (DamageFX);

        /* Initialize movement and add camera shake when landing */
        movement.Initialize();
        movement.AssignLandingAction (()=> overview.Shake(0.5f));
    }

    private void Update () {
        /* Read player input before check availability */
        ReadInput();

        /* Block controller when unavailable */
        if (!lifecycle.Availability()) return;

        /* Mouse look state */
        overview.Looking();

        /* Change camera FOV state */
        overview.Aiming();

        /* Shake camera state. Required "physical camera" mode on */
        overview.Shaking();

        /* Control the speed */
        movement.Running();
        
        /* If player idle faster endurance regen*/
        movement.Idle();

        /* Control the health recovery */
        lifecycle.Runtime();

        /* Control the jumping, ground search... */
        movement.Jumping();

        /* Ray for checking which game-object player is attached... */
        movement.Ray();

        jumpingEnduranceUpdaterInAir();
        UpdateEndurance();

        if (movement.isGrounded && movement.isJumping)
        {
            movement.isJumping = false;
        }
    }

    public void jumpingEnduranceUpdaterInAir()
    {
        if (!movement.isJumping || onStairs) return;

        if(movement.isRunning && movement.endurance > movement.endurance-1f)
        {
            movement.endurance-=Time.deltaTime*1.5f;
        }
        else 
        {   
            if(movement.endurance > movement.endurance-.75f)
            {
                movement.endurance-=Time.deltaTime*1.25f;
            }
            else if(movement.onWall == false)
            {
                movement.endurance-=Time.deltaTime;
            }
        }
    }

    private void FixedUpdate () {

        /* Block controller when unavailable */
        if (!lifecycle.Availability()) return;

        /* Physical movement */
        movement.Accelerate();

        /* Physical rotation with camera */
        overview.RotateRigigbodyToLookDirection (movement.rb);
    }

    private void UpdateEndurance()
    {
        // Check if the player is on stairs by raycasting downward.
        onStairs = false;
        RaycastHit hit;
        // Adjust the ray length (e.g., 1.0f) as needed based on your character's height.
        if (Physics.Raycast(movement.plr.transform.position, Vector3.down, out hit, 1.25f))
        {
            if (hit.collider.CompareTag("stair"))
            {
                onStairs = true;
            }
        }
        
        // Treat stairs as ground.
        bool treatAsGrounded = movement.isGrounded || onStairs;
        
        // If running or not grounded (and not on stairs), regenerate endurance slower.
        float regenRate = (movement.isRunning || !treatAsGrounded) ? Time.deltaTime / 1.5f : Time.deltaTime;
        movement.endurance = Mathf.MoveTowards(movement.endurance, movement.referenceEndurance, regenRate);
    }


    private void LateUpdate () {

        /* Block controller when unavailable */
        if (!lifecycle.Availability()) return;

        /* Camera following */
        overview.Follow (transform.position);
    }

        private void ReadInput() {
        // Cache mouse and movement axis values
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Update overview inputs
        overview.lookingInputValues = new Vector2(mouseX, mouseY);
        overview.aimingInputValue = Input.GetMouseButton(1);

        // Update movement inputs
        movement.movementInputValues = new Vector2(horizontal, vertical);
        movement.jumpingInputValue = Input.GetButtonDown("Jump");
        movement.runningInputValue = Input.GetKey(KeyCode.LeftShift);
    }
    private void DamageFX () {
        if (HUD) HUD.DamageFX();
        overview.Shake(0.75f);
    }
}