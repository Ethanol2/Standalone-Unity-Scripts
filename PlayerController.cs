// Script written by EthanASC
// https://www.fiverr.com/ethanasc

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // Public Variables
    [Header("Unity Input Buttons")]
    [Tooltip("Find input manager at Edit->Project Settings->Input Manager")]
    public string xMovement = "Horizontal";
    [Tooltip("Find input manager at Edit->Project Settings->Input Manager")]
    public string zMovement = "Vertical";
    [Tooltip("Find input manager at Edit->Project Settings->Input Manager")]
    public string jumpButton = "Jump";
    [Tooltip("Find input manager at Edit->Project Settings->Input Manager")]
    public string attackButton = "Fire1";
    [Tooltip("Find input manager at Edit->Project Settings->Input Manager | Implement on line 136")]
    public string interactButton = "Fire2";
    [Header("Scene References")]
    [Tooltip("Camera that will be used in the scene. This affects the direction the player moves. If left unfilled Main Camera will be used")]
    public Transform activeCamera;
    [Tooltip("Reference for the sword, or damaging object, held by the player")]
    public GameObject sword; 
    [Header("Character Config")]
    [Tooltip("Speed at which the player will move")]
    public float horizontalSpeed = 6f;
    [Tooltip("Speed at which the player will fall")]
    public float gravity = 0.08f;
    [Tooltip("Power with which the player jumps")]
    public float jumpPower = 0.03f;
    [Tooltip("Time the attack is out for in seconds")]
    public float attackLength = 0.1f;
    [Tooltip("Time for attack to become reusable in seconds")]
    public float attackCooldown = 0.2f;
    [Tooltip("Deadzone for input. This is important for gamepads")]
    public float deadzone = 0.1f;
    [Tooltip("Time it takes for the player to rotate to the direction of movement")]
    public float turnSmoothTime = 0.1f;
    float turnVelocity; // Variable for the function that smooths the player rotation
    CharacterController controller; // Reference to the Character Controller component
    [Header("Debug")]
    public bool playerIsGrounded = true;
    public float verticalSpeed = 0f;
    public float attackOutTime = 0f;
    public float cooldownTime = 0f;

    // Start is called before the first frame update
    void Start()
    {
        // Get reference to the Character Controller
        controller = this.GetComponent<CharacterController>();

        sword.SetActive(false);

        // Set active character if left blank
        if (activeCamera == null)
        {
            activeCamera = GameObject.Find("Main Camera").transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Receive input from the Unity input manager
        float hor = Input.GetAxisRaw(xMovement);
        float vert = Input.GetAxisRaw(zMovement);
        Vector3 inputVector = new Vector3(hor, 0f, vert).normalized;
        
        // Declare movement vector
        Vector3 moveDirection = new Vector3();

        // Check if the player is on the ground
        if (controller.isGrounded)
        {
            // Check if the player pressed jump, and apply jump power
            if (Input.GetButtonDown(jumpButton))
            {
                verticalSpeed = 0f;
                verticalSpeed = jumpPower;
                Debug.Log(verticalSpeed);
            }
        }

        // Apply gravity
        verticalSpeed -= gravity * Time.deltaTime;

        // Check if player is out of the controls deadzone and apply motion
        if (inputVector.magnitude >= deadzone)
        {
            float angle = Mathf.Atan2(inputVector.x, inputVector.z) * Mathf.Rad2Deg + activeCamera.eulerAngles.y;
            angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, angle, ref turnVelocity, turnSmoothTime);
            this.transform.rotation = Quaternion.Euler(0f, angle, 0f);

            moveDirection = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            moveDirection = moveDirection.normalized * horizontalSpeed * Time.deltaTime;
        }

        // Add vertical speed to movement vector
        moveDirection.y = verticalSpeed;

        // Use character controller to apply motion
        controller.Move(moveDirection);

        // Display if the player is on the ground in the inspector
        playerIsGrounded = controller.isGrounded;

        // Decrement the attack timers
        cooldownTime -= Time.deltaTime;
        attackOutTime -= Time.deltaTime;

        // Check if the player pressed the attack button, and the cooldown is over. Make the sword appear
        if (Input.GetButtonDown(attackButton))
        {
            if (cooldownTime <= 0f)
            {
                sword.SetActive(true);
                attackOutTime = attackLength;
                cooldownTime = attackCooldown + attackOutTime;
            }

        }
        // Disable sword if the attack time is finished
        else if (attackOutTime <= 0f)
        {
            sword.SetActive(false);
        }

        // Check if the interact button is pressed
        if (Input.GetButton(interactButton))
        {
            // Insert interact code here
            Debug.Log("There's no interaction code!");
        }
    }
}
