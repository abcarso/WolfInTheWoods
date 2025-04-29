using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FirstPersonController : MonoBehaviour
{
    # region Player Model
    private Rigidbody rb;
    private Vector3 startingPosition;
    private Quaternion startingRotation;
    #endregion

    #region Camera
    public Camera playerCamera;
    public float fov = 60f;
    public bool invertCamera = false;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 50f;
    private float yaw, pitch;
    public bool crosshair = true;
    public Sprite crosshairImage;
    public Color crosshairColor = Color.white;
    private Image crosshairObject;

    public bool enableZoom = true;
    public bool holdToZoom = false;
    public KeyCode zoomKey = KeyCode.Mouse1;
    public float zoomFOV = 30f;
    public float zoomStepTime = 5f;
    private bool isZoomed = false;
    #endregion

    #region Movement
    public bool playerCanMove = true;
    public float walkSpeed = 5f;
    public float maxVelocityChange = 10f;
    #endregion

    #region Sprint
    public bool enableSprint = true;
    public bool unlimitedSprint = false;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public float sprintSpeed = 7f;
    public float sprintDuration = 5f;
    public float sprintCooldown = .5f;
    public float sprintFOV = 80f;
    public float sprintFOVStepTime = 10f;

    public bool useSprintBar = true;
    public bool hideBarWhenFull = true;
    public GameObject sprintBarGroup;
    public Image sprintBarBG;
    public Image sprintBar;
    public float sprintBarWidthPercent = .3f;
    public float sprintBarHeightPercent = .015f;

    private CanvasGroup sprintBarCG;
    private bool isSprinting = false;
    private bool isSprintCooldown = false;
    private float sprintRemaining;
    private float sprintCooldownReset;
    #endregion

    #region Hunger
    public bool enableHunger = true;
    public float hungerLossRate = 3f;
    public float startingHunger = 100;
    public float hungerAdded = 50;
    public GameObject hungerBarGroup;
    public Image hungerBarBG;
    public Image hungerBar;
    public float hungerBarWidthPercent = .3f;
    public float hungerBarHeightPercent = .015f;

    private CanvasGroup hungerBarCG;
    private float hungerRemaining;
    private bool hungerPenalty = false;
    #endregion

    #region Head Bob
    public bool enableHeadBob = true;
    public Transform joint;
    public float bobSpeed = 10f;
    public Vector3 bobAmount = new Vector3(.15f, .05f, 0f);

    private Vector3 jointOriginalPos;
    private float bobTimer = 0;
    private bool isWalking = false;
    #endregion

    #region Audio
    [Header("Footsteps & Breathing Audio")]
    public AudioSource audioSource;
    public AudioClip[] footstepClips;
    public float footstepIntervalWalk = 0.6f;
    public float footstepIntervalSprint = 0.4f;
    private float footstepTimer = 0;

    public AudioClip sprintBreathingClip;
    private bool breathPlaying = false;
    #endregion

    // Unity Methods
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        crosshairObject = GetComponentInChildren<Image>();
        Cursor.visible = false;
        playerCamera.fieldOfView = fov;
        jointOriginalPos = joint.localPosition;

        startingPosition = transform.position;
        startingRotation = transform.rotation;

        if (!unlimitedSprint)
        {
            sprintRemaining = sprintDuration;
            sprintCooldownReset = sprintCooldown;
        }

        hungerRemaining = startingHunger;
    }

    private void Start()
    {
        if (crosshair)
        {
            crosshairObject.sprite = crosshairImage;
            crosshairObject.color = crosshairColor;
        }
        else
            crosshairObject.gameObject.SetActive(false);

        sprintBarCG  = sprintBarGroup.GetComponent<CanvasGroup>();
        hungerBarCG  = hungerBarGroup.GetComponent<CanvasGroup>();
        hungerBarBG.gameObject.SetActive(true);
        hungerBar.gameObject.SetActive(true);
    }

    private void Update()
    {
        HandleCamera();
        HandleZoom();
        HandleSprint();
        HandleHunger();
        if (enableHeadBob) HeadBob();
        HandleFootsteps();
        HandleBreathingLoop();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    // Player Functions
    private void HandleCamera()
    {
        if (!cameraCanMove) return;

        yaw = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch += (invertCamera ? 1 : -1) * mouseSensitivity * Input.GetAxis("Mouse Y");
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

        transform.localEulerAngles = new Vector3(0, yaw, 0);
        playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
    }

    private void HandleZoom()
    {
        if (!enableZoom) return;

        if (Input.GetKeyDown(zoomKey) && !holdToZoom && !isSprinting)
            isZoomed = !isZoomed;

        if (holdToZoom && !isSprinting)
            isZoomed = Input.GetKey(zoomKey);

        float targetFOV = isZoomed ? zoomFOV : (isSprinting ? sprintFOV : fov);
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, zoomStepTime * Time.deltaTime);
    }

    private void HandleSprint()
    {
        if (!enableSprint) return;

        if (Input.GetKey(sprintKey) && sprintRemaining > 0f && !isSprintCooldown)
        {
            isSprinting = true;
            sprintRemaining -= Time.deltaTime;
            if (!unlimitedSprint && sprintRemaining <= 0)
            {
                isSprinting = false;
                isSprintCooldown = true;
                hungerPenalty = true;
            }
        }
        else
        {
            isSprinting = false;
            sprintRemaining = Mathf.Clamp(sprintRemaining + Time.deltaTime, 0, sprintDuration);
        }

        if (isSprintCooldown)
        {
            sprintCooldown -= Time.deltaTime;
            if (sprintCooldown <= 0)
                isSprintCooldown = false;
        }
        else
        {
            sprintCooldown = sprintCooldownReset;
        }

        // UI update 
        if (useSprintBar && !unlimitedSprint &&
            sprintBar   && sprintBarCG)
        {
            float percent = sprintRemaining / sprintDuration;

            sprintBar.fillAmount = percent;

            // Hide when almost full
            sprintBarCG.alpha = (hideBarWhenFull && percent > 0.99f) ? 0f : 1f;
        }
    }

    private void HandleHunger()
    {
        if (!enableHunger) return;

        // Hunger decreases with time
        hungerRemaining -= hungerLossRate * Time.deltaTime;
        if (hungerPenalty)
        {
            hungerRemaining -= 20;
            hungerPenalty = false;
        }

        // Player has no hunger
        if (hungerRemaining <= 0)
        {
            // Call lose screen
            GameManager.Instance.LoseGame();
            DisablePlayer();
        }

        // Show hunger left in ui
        float percent = hungerRemaining / startingHunger;
        hungerBar.fillAmount = percent;
    }

    private void HandleMovement()
    {
        if (!playerCanMove) return;

        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        isWalking = input.sqrMagnitude > 0.01f;

        float speed = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = transform.TransformDirection(input) * speed;

        Vector3 velocity = rb.linearVelocity;
        Vector3 velocityChange = (targetVelocity - velocity);
        velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
        velocityChange.y = 0;

        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    private void HeadBob()
    {
        // Camera bobs when player is walking
        if (isWalking)
        {
            bobTimer += Time.deltaTime * (isSprinting ? (bobSpeed + sprintSpeed) : bobSpeed);
            joint.localPosition = jointOriginalPos + new Vector3(Mathf.Sin(bobTimer) * bobAmount.x, Mathf.Sin(bobTimer) * bobAmount.y, 0);
        }
        else
        {
            bobTimer = 0;
            joint.localPosition = Vector3.Lerp(joint.localPosition, jointOriginalPos, Time.deltaTime * bobSpeed);
        }
    }

    private void HandleFootsteps()
    {
        // Play footstep audio from an array of audio clips
        if (!audioSource || footstepClips.Length == 0 || !isWalking) return;

        footstepTimer += Time.deltaTime;
        float interval = isSprinting ? footstepIntervalSprint : footstepIntervalWalk;
        if (footstepTimer >= interval)
        {
            footstepTimer = 0;
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            audioSource.PlayOneShot(clip);
        }
    }

    private void HandleBreathingLoop()
    {
        // Play breathing audio when player is sprinting
        if (!audioSource || !sprintBreathingClip) return;

        if (isSprinting && !breathPlaying)
        {
            audioSource.clip = sprintBreathingClip;
            audioSource.loop = true;
            audioSource.Play();
            breathPlaying = true;
        }
        else if (!isSprinting && breathPlaying)
        {
            audioSource.Stop();
            breathPlaying = false;
        }
    }

    public void AddHunger()
    {
        // Add hunger, but don't go above the max
        hungerRemaining = Mathf.Clamp(hungerRemaining + hungerAdded, 0, startingHunger);
    }

    public void DisablePlayer()
    {
        // Keep the player from moving
        enableSprint = false;
        enableHunger = false;
        playerCanMove = false;
        cameraCanMove = false;

        // Make the ui inactive
        sprintBarGroup.SetActive(false);
        hungerBarGroup.SetActive(false);

    }

    public void EnablePlayer()
    {
        enableSprint = true;
        enableHunger = true;
        playerCanMove = true;
        cameraCanMove = true;

        if (sprintBarGroup != null)
            sprintBarGroup.SetActive(true);

        if (hungerBarGroup != null)
            hungerBarGroup.SetActive(true);
    }

    public void ResetPlayer()
    {
        // Let player move again
        EnablePlayer();

        // Reset sprinting
        isSprinting = false;
        isSprintCooldown = false;
        sprintRemaining = sprintDuration;
        sprintCooldown = sprintCooldownReset;

        // Reset hunger
        hungerRemaining = startingHunger;
        hungerPenalty = false;

        // Reset movement
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        // Reset position
        transform.position = startingPosition;
        transform.rotation = startingRotation;
    }
}
