using System;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace AFPC {

    /// <summary>
    /// This class allows the user to move.
    /// </summary>
    [Serializable]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class Movement {

        public bool isDebugLog;
        public GameObject plr;
        public LayerMask layerMask;

        [Header("Inputs")]
        public Vector3 movementInputValues;
        public bool runningInputValue;
        public bool jumpingInputValue;

        [Header("Acceleration")]
        public float referenceAcceleration = 2.66f;
        public float currentAcceleration = 2.5f;
        private float movementSmoothing = 0.3f;
        private Vector3 vector3_Reference;
        private Vector3 vector3_Target;
        private Vector3 delta;
        private bool isMovementAvailable = true;
        private bool releaseAcceleration = true;

        [Header("Running")]
        public float runningAcceleration = 4.5f;
        private bool isRunningAvaiable = true;

        public bool isRunning = false;

        [Header("Endurance")]
        public float referenceEndurance = 20.0f;
        public float endurance = 20.0f;

        [Header("Jumping")]
        public float jumpForce = 7.5f;
        private bool Available = true;
        private bool isAirControl = true;
        private Vector3 groungCheckPosition;
        private bool isLandingActionPerformed;
        private UnityAction landingAction;
        public bool isJumping = false;
        public bool onWall = false;

        [Header("Crouching")]
        public float crouchHeight = 0.8f;
        public bool isCrouching = false;
        public float originalAcceleration;
        public float crouchSpeed;
        public bool crouchInputValue;
        private float originalHeight;
        private Vector3 originalCenter;
	
        [Header("Physics")]
        public bool isGeneratePhysicMaterial = true;
        public float mass = 70.0f;
        public float drag = 3.0f;
	    [Tooltip ("For Initialize()")] public float height = 1.6f;

        [Header("Looking for ground")]
        public LayerMask groundMask = 1;
        public bool isGrounded;

        [Header("References")]
        public Rigidbody rb;
        public CapsuleCollider cc;

        private float epsilon = 0.01f;

        [Header("Idle")]

        private float idleTimeOut = 2.0f;
        private float idleTime = 0f;

        [Header("ray")]

        private float radius = 0.5f;
        private float numRays = 35f;
        private float verticalOffset = 0f;
        private float angle;
        private Vector3 dir;
        private Vector3 origin;
        private Vector3 horizontalDir;
        private float heightRay = 5f;
        public string hitTag = "";
        private int wallJumpTimes = 0;

        [Header("ledge-hanging")]
        public bool isLedgeHanging = false;
        public bool ledgeHangingInputValue;
        public bool isLedgeHangingAvailable = false;
        public float ledgeHangingSpeed = 1.0f;
        public float ledgeCheckDistance = 0.5f;
        GameObject[] ledges;


        private Hero hero;

        /// <summary>
        /// Initialize the movement. Generate physic material if needed. Prepare the rigidbody.
        /// </summary>
        public virtual void Initialize() {
        preInitializeRB();
        originalHeight = cc.height;
        originalCenter = cc.center;
        originalAcceleration = referenceAcceleration;
        crouchSpeed = referenceAcceleration *0.5f;
    }

        private void preInitializeRB()
        {
            hero = plr.GetComponent<Hero>();
            rb.freezeRotation = true;
            rb.mass = mass;
            rb.drag = drag;
            cc.height = height;
            if (isGeneratePhysicMaterial) {
                PhysicMaterial physicMaterial = new PhysicMaterial {
                    name = "Generated Material",
                    bounciness = 0.01f,
                    dynamicFriction = 0.5f,
                    staticFriction = 0.0f,
                    frictionCombine = PhysicMaterialCombine.Minimum,
                    bounceCombine = PhysicMaterialCombine.Minimum
                };
                cc.material = physicMaterial;
            }
        }

        /// <summary>
        /// Casts precomputed rays from the player’s position (offset by verticalOffset) and returns the tag
        /// of the first collider hit. Debug rays are drawn for visualization.
        /// </summary>

        public void Ray()
        {
            if(isGrounded)
            {
                hitTag = "";
                onWall = false;
                return;
            }
            origin = new Vector3(plr.transform.position.x,plr.transform.position.y+verticalOffset,plr.transform.position.z);
            for(int i = 0; i < numRays; ++i)
            {
                angle = i * MathF.PI*2f /numRays;
                horizontalDir = new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));

                for(float y = -heightRay/2f; y <= heightRay / 2; y += heightRay/numRays)
                {
                    dir = new Vector3(horizontalDir.x,y+verticalOffset,horizontalDir.z).normalized;
                    RaycastHit hit;
                    if(Physics.Raycast(origin,dir, out hit, radius, layerMask))
                    {
                        hitTag = hit.collider.tag;
                        if(hitTag == "wallJumpSurface" && !isGrounded) // *do*-if hit collider is null make it so hitTag is empty-*do* //
                        {
                            onWall = true;
                        }
                    }
                    Debug.DrawRay(origin,dir*radius,Color.red);
                }
            }

        }

        private void ledgeHolding()
        {
            if (!isLedgeHangingAvailable) return;
            if (isLedgeHanging)
            {

            }
        }

        public virtual void Crouching()
        {
            if (!isMovementAvailable) return;
            if (isJumping || isRunning)
            {
                cc.height = originalHeight;
                cc.center = originalCenter;
                referenceAcceleration = originalAcceleration;
                return;
            }
            if (crouchInputValue)
            {
                if (!isCrouching)
                {
                    // Start crouching
                    isCrouching = true;
                    cc.height = crouchHeight;
                    cc.center = originalCenter * (crouchHeight / originalHeight);
                    referenceAcceleration = crouchSpeed;
                }
            }
            else
            {
                if (isCrouching)
                {
                    if (!Physics.Raycast(rb.position, Vector3.up, originalHeight))
                    {
                        // Stop crouching
                        isCrouching = false;
                        cc.height = originalHeight;
                        cc.center = originalCenter;
                        referenceAcceleration = originalAcceleration;
                    }
                }
            }

            if (isCrouching)
            {
                cc.height = Mathf.Lerp(cc.height, crouchHeight, Time.deltaTime * 5f);
                currentAcceleration = Mathf.Lerp(currentAcceleration, crouchSpeed, Time.deltaTime * 5f);
            }
            else
            {
                cc.height = Mathf.Lerp(cc.height, originalHeight, Time.deltaTime * 5f);
                currentAcceleration = Mathf.Lerp(currentAcceleration, referenceAcceleration, Time.deltaTime * 5f);
            }
        }
        /// <summary>
        /// Jumping state. Better use it in Update.
        /// </summary>/// <summary>
        /// Jumping state. When the jump button is pressed, if grounded a normal jump is performed.
        /// If airborne, we cast rays to see if a wall jump surface is touched and, if so, execute a wall jump.
        /// </summary>
        public virtual void Jumping()
        {
            if (!Available) return;
            if(endurance < 0.2f) return;

            if (isGrounded && hitTag == "" && jumpingInputValue)
            {
                wallJumpTimes = 0;
                endurance -= .075f;
                rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
                isJumping = true;
                //Debug.Log("Normal jump");
            }

            else if(!isGrounded)
            {
                if(jumpingInputValue && hitTag == "wallJumpSurface" && wallJumpTimes <= 1)
                {
                    wallJumpTimes++;
                    rb.velocity = new Vector3(rb.velocity.x, jumpForce * 1.05f, rb.velocity.z * 1.25f);
                    isJumping = true;
                    endurance -= .15f;
                    //Debug.Log("Wall jumping");
                }
            }                                                                                                                                                                                                                                                                                                                                                                                       
        }

        /// <summary>
        /// Running state. Better use it in Update.
        /// </summary>
        /// 
	    public virtual void Running () {
		    if (!isRunningAvaiable) return;
		    if (!isGrounded) return;
		    if (runningInputValue && endurance > 0.05f && Idle() == false) {
                isRunning = true;
                releaseAcceleration = false;
			    currentAcceleration = Mathf.MoveTowards (currentAcceleration, runningAcceleration, Time.deltaTime * 5);
                endurance -= Time.deltaTime * 1.48f;
                Debug.Log("Running");
		    }
		    else {
                isRunning = false;
                releaseAcceleration = true;
		    }
	    }
        public virtual bool Idle()
        {
            // Determine if the player is idle (no keys pressed)
            bool idle = !(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.Space));

            if (idle)
            {
                idleTime += Time.deltaTime;
                if (idleTime >= idleTimeOut)
                {
                    // restores endurance faster to its reference value when idle
                    endurance = Mathf.MoveTowards(endurance, referenceEndurance, Time.deltaTime * 1.8f);
                }
            }
            else
            {
                idleTime = 0f;
            }
            
            return idle;
        }

                /// <summary>
        /// Physical movement. Better use it in FixedUpdate.
        /// </summary>
        public virtual void Accelerate () {
            LookingForGround ();
            MoveTorwardsAcceleration ();
            if (!isMovementAvailable) return;
            if (!rb) return;
            if (Math.Abs(movementInputValues.x) < epsilon & Math.Abs(movementInputValues.y) < epsilon) return;
            if (!isAirControl) {
                if (!isGrounded) return;
            }
            if (rb.velocity.magnitude > 1.0f) {
                rb.interpolation = RigidbodyInterpolation.Extrapolate;
            }
            else {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
            delta = new Vector3 (movementInputValues.x, 0, movementInputValues.y);
            delta = Vector3.ClampMagnitude (delta, 1);
            delta = rb.transform.TransformDirection (delta) * currentAcceleration;
            vector3_Target = new Vector3 (delta.x, rb.velocity.y, delta.z);
            rb.velocity = Vector3.SmoothDamp (rb.velocity, vector3_Target, ref vector3_Reference, Time.smoothDeltaTime * movementSmoothing);
        }


        private void LookingForGround () {
            groungCheckPosition = new Vector3 (cc.transform.position.x, cc.transform.position.y - height / 2, cc.transform.position.z);
            if (Physics.CheckSphere (groungCheckPosition, 0.1f, groundMask, QueryTriggerInteraction.Ignore)) {
                isGrounded = true;
                if (!isLandingActionPerformed) {
                    isLandingActionPerformed = true;
                    landingAction?.Invoke ();
                    isJumping = false;
                }
                rb.drag = drag;
            }
            else {
                isGrounded = false;
                isLandingActionPerformed = false;
                rb.drag = 0.5f;
            }
        }

        private void MoveTorwardsAcceleration () {
            if (!releaseAcceleration) return;
            if (Math.Abs(currentAcceleration - referenceAcceleration) > epsilon) {
                currentAcceleration = Mathf.MoveTowards (currentAcceleration, referenceAcceleration, Time.deltaTime * 10);
            }
        }

        /// <summary>
        /// Allow the user to move.
        /// </summary>
        public virtual void AllowMovement () {
            isMovementAvailable = true;
            if (isDebugLog) Debug.Log (rb.gameObject.name + ": Allow Movement");
        }


        /// <summary>
        /// Ban the user to move. Optional, immediately stop the rigidbody.
        /// </summary>
        /// <param name="isStopImmediately"></param>
        public virtual void BanMovement (bool isStopImmediately = false) {
            isMovementAvailable = true;
            if (isDebugLog) Debug.Log (rb.gameObject.name + ": Ban Movement");
            if (isStopImmediately) {
                rb.velocity = Vector3.zero;
                if (isDebugLog) Debug.Log (rb.gameObject.name + ": Stop Movement");
            }
        }

        /// <summary>
        /// Allow the user to move faster.
        /// </summary>
        public virtual void AllowRunning () {
            isRunningAvaiable = true;
            if (isDebugLog) Debug.Log (rb.gameObject.name + ": Allow Running");
        }

        /// <summary>
        /// Ban the uset to move faster.
        /// </summary>
        public virtual void BanRunning () {
            isRunningAvaiable = false;
            if (isDebugLog) Debug.Log (rb.gameObject.name + ": Ban Running");
        }

        /// <summary>
        /// Allow the user to jump up.
        /// </summary>
        public virtual void AllowJumping () {
            Available = true;
            if (isDebugLog) Debug.Log (rb.gameObject.name + ": Allow Jumping");
        }

        /// <summary>
        /// Ban the user from jumping up.
        /// </summary>
        public virtual void BanJumping () {
            Available = false;
            if (isDebugLog) Debug.Log (rb.gameObject.name + ": Ban Jumping");
        }

        /// <summary>
        /// Perform an action when the character was landed.
        /// </summary>
        /// <param name="action"></param>
        public void AssignLandingAction (UnityAction action) {
            landingAction = action;
        }

        public void ClearLandingAction () {
            landingAction = null;
        }

        /// <summary>
        /// Allow the user to change movement direction in the air.
        /// </summary>
        public virtual void AllowAirControl () {
            isAirControl = true;
            if (isDebugLog) Debug.Log (rb.gameObject.name + ": Allow Air Control");
        }

        /// <summary>
        /// Ban the user to change movement direction in the air.
        /// </summary>
        public virtual void BanAirControl () {
            isAirControl = false;
            if (isDebugLog) Debug.Log (rb.gameObject.name + ": Ban Air Control");
        }

        /// <summary>
        /// Current endurance value.
        /// </summary>
        /// <returns></returns>
        public float GetEnduranceValue () {
            return endurance;
        }

        /// <summary>
        /// Is this controller on the ground?
        /// </summary>
        /// <returns></returns>
        public bool IsGrounded () {
            return isGrounded;
        }
    }
}
