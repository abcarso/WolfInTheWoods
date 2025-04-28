// CHANGE LOG
// 
// CHANGES || version VERSION
//

// ───────────────────────────────────────────────────────────────────────────────
// FirstPersonController.cs  –  Runtime + Custom Inspector (No Jump, No Crouch)
// ───────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR         // editor-only references MUST be before any class decls
using UnityEditor;
#endif

// ───────────── Runtime Controller ─────────────
public class FirstPersonController : MonoBehaviour
{
    private Rigidbody rb;

    #region Camera
    public Camera playerCamera;
    public float fov = 60f;
    public bool invertCamera = false;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 50f;

    private float yaw, pitch;
    public bool lockCursor = true;

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

    #region Movement / Sprint
    public bool playerCanMove = true;
    public float walkSpeed = 5f;
    public float maxVelocityChange = 10f;

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
    public Image hungerBarBG;
    public Image hungerBar;
    public float hungerBarWidthPercent = .3f;
    public float hungerBarHeightPercent = .015f;

    private float hungerRemaining;
    private bool hungerPenalty = false;
    private CanvasGroup hungerBarCG;
    #endregion

    #region Head Bob
    public bool enableHeadBob = true;
    public Transform joint;
    public float bobSpeed = 10f;
    public Vector3 bobAmount = new Vector3(.15f, .05f, 0f);

    private Vector3 jointOriginalPos;
    private float timer = 0;
    private bool isWalking = false;
    #endregion

    public GameObject losePanel;

    // ─────────── Unity Messages ───────────
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        crosshairObject = GetComponentInChildren<Image>();

        playerCamera.fieldOfView = fov;
        jointOriginalPos = joint.localPosition;

        if (!unlimitedSprint)
        {
            sprintRemaining = sprintDuration;
            sprintCooldownReset = sprintCooldown;
        }

        hungerRemaining = startingHunger;
    }

    private void Start()
    {
        if (lockCursor) Cursor.lockState = CursorLockMode.Locked;

        if (crosshair)
        {
            crosshairObject.sprite = crosshairImage;
            crosshairObject.color = crosshairColor;
        }
        else
        {
            crosshairObject.gameObject.SetActive(false);
        }

        sprintBarCG  = GetComponentInChildren<CanvasGroup>();
        hungerBarCG  = GetComponentInChildren<CanvasGroup>();
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
    }

    private void FixedUpdate() => HandleMovement();

    // ─────────── Feature Blocks ───────────
    void HandleCamera()
    {
        if (!cameraCanMove) return;

        yaw = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch += (invertCamera ? 1 : -1) * mouseSensitivity * Input.GetAxis("Mouse Y");
        pitch  = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

        transform.localEulerAngles          = new Vector3(0, yaw, 0);
        playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
    }

    void HandleZoom()
    {
        if (!enableZoom) return;

        if (!holdToZoom && Input.GetKeyDown(zoomKey) && !isSprinting)
            isZoomed = !isZoomed;

        if (holdToZoom && !isSprinting)
            isZoomed = Input.GetKey(zoomKey);

        float targetFOV = isZoomed ? zoomFOV : (isSprinting ? sprintFOV : fov);
        playerCamera.fieldOfView =
            Mathf.Lerp(playerCamera.fieldOfView, targetFOV, zoomStepTime * Time.deltaTime);
    }

    void HandleSprint()
    {
        if (!enableSprint) return;

        if (Input.GetKey(sprintKey) && sprintRemaining > 0f && !isSprintCooldown)
        {
            isSprinting     = true;
            sprintRemaining -= Time.deltaTime;
            if (!unlimitedSprint && sprintRemaining <= 0)
            {
                isSprinting     = false;
                isSprintCooldown = true;
                hungerPenalty    = true;
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
            if (sprintCooldown <= 0) isSprintCooldown = false;
        }
        else sprintCooldown = sprintCooldownReset;

        if (useSprintBar && !unlimitedSprint)
        {
            float p = sprintRemaining / sprintDuration;
            sprintBar.transform.localScale = new Vector3(p, 1, 1);
            if (hideBarWhenFull) sprintBarCG.alpha = p < .99f ? 1 : 0;
        }
    }

    void HandleHunger()
    {
        if (!enableHunger) return;

        hungerRemaining -= hungerLossRate * Time.deltaTime;
        if (hungerPenalty) { hungerRemaining -= 20; hungerPenalty = false; }

        if (hungerRemaining <= 0)
        {
            losePanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            DisablePlayer();
        }

        float p = hungerRemaining / startingHunger;
        hungerBar.transform.localScale = new Vector3(p, 1, 1);
    }

    void HandleMovement()
    {
        if (!playerCanMove) return;

        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        isWalking     = input.sqrMagnitude > .01f;

        float speed   = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = transform.TransformDirection(input) * speed;

        Vector3 vel   = rb.linearVelocity;
        Vector3 change = targetVelocity - vel;
        change.x = Mathf.Clamp(change.x, -maxVelocityChange, maxVelocityChange);
        change.z = Mathf.Clamp(change.z, -maxVelocityChange, maxVelocityChange);
        change.y = 0;

        rb.AddForce(change, ForceMode.VelocityChange);
    }

    void HeadBob()
    {
        if (isWalking)
        {
            timer += Time.deltaTime * (isSprinting ? (bobSpeed + sprintSpeed) : bobSpeed);
            joint.localPosition =
                jointOriginalPos + new Vector3(Mathf.Sin(timer) * bobAmount.x,
                                               Mathf.Sin(timer) * bobAmount.y,
                                               0);
        }
        else
        {
            timer = 0;
            joint.localPosition = Vector3.Lerp(joint.localPosition,
                                               jointOriginalPos,
                                               Time.deltaTime * bobSpeed);
        }
    }

    // ─────────── Helpers ───────────
    public void AddHunger()   => hungerRemaining = Mathf.Clamp(hungerRemaining + 50, 0, startingHunger);

    public void DisablePlayer()
    {
        enableSprint = false;
        enableHunger = false;
        playerCanMove = false;
        hungerBarBG.gameObject.SetActive(false);
        hungerBar.gameObject.SetActive(false);
        sprintBarBG.gameObject.SetActive(false);
        sprintBar.gameObject.SetActive(false);
    }
}

// ───────────── Custom Inspector (Editor-only) ─────────────
#if UNITY_EDITOR
[CustomEditor(typeof(FirstPersonController)), InitializeOnLoadAttribute]
public class FirstPersonControllerEditor : Editor
{
    FirstPersonController fpc;
    SerializedObject so;

    void OnEnable()
    {
        fpc = (FirstPersonController)target;
        so  = new SerializedObject(fpc);
    }

    public override void OnInspectorGUI()
    {
        so.Update();

        EditorGUILayout.Space();
        CenteredBold("Modular First Person Controller");
        EditorGUILayout.Space();

        DrawCamera();
        DrawEndScreen();
        DrawMovement();
        DrawHeadBob();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(fpc);
            Undo.RecordObject(fpc, "FPC Change");
            so.ApplyModifiedProperties();
        }
    }

    // ───────── Inspector Sections ─────────
    void DrawCamera()
    {
        Separator("Camera");
        fpc.playerCamera   = (Camera)EditorGUILayout.ObjectField("Camera", fpc.playerCamera, typeof(Camera), true);
        fpc.fov            = EditorGUILayout.Slider("Field of View", fpc.fov, fpc.zoomFOV, 179);
        fpc.cameraCanMove  = EditorGUILayout.Toggle("Enable Camera Rotation", fpc.cameraCanMove);

        if (fpc.cameraCanMove)
        {
            fpc.invertCamera   = EditorGUILayout.Toggle("Invert Y", fpc.invertCamera);
            fpc.mouseSensitivity = EditorGUILayout.Slider("Look Sensitivity", fpc.mouseSensitivity, .1f, 10);
            fpc.maxLookAngle   = EditorGUILayout.Slider("Max Look Angle", fpc.maxLookAngle, 40, 90);
        }

        fpc.lockCursor = EditorGUILayout.Toggle("Lock & Hide Cursor", fpc.lockCursor);
        fpc.crosshair  = EditorGUILayout.Toggle("Show Crosshair", fpc.crosshair);

        if (fpc.crosshair)
        {
            fpc.crosshairImage = (Sprite)EditorGUILayout.ObjectField("Crosshair Image", fpc.crosshairImage, typeof(Sprite), false);
            fpc.crosshairColor = EditorGUILayout.ColorField("Crosshair Color", fpc.crosshairColor);
        }

        Separator("Zoom");
        fpc.enableZoom  = EditorGUILayout.Toggle("Enable Zoom", fpc.enableZoom);
        if (fpc.enableZoom)
        {
            fpc.holdToZoom = EditorGUILayout.Toggle("Hold To Zoom", fpc.holdToZoom);
            fpc.zoomKey    = (KeyCode)EditorGUILayout.EnumPopup("Zoom Key", fpc.zoomKey);
            fpc.zoomFOV    = EditorGUILayout.Slider("Zoom FOV", fpc.zoomFOV, .1f, fpc.fov);
            fpc.zoomStepTime = EditorGUILayout.Slider("Zoom Step Time", fpc.zoomStepTime, .1f, 10);
        }
    }

    void DrawEndScreen()
    {
        Separator("End Screen");
        fpc.losePanel = (GameObject)EditorGUILayout.ObjectField("Lose Panel", fpc.losePanel, typeof(GameObject), true);
    }

    void DrawMovement()
    {
        Separator("Movement");
        fpc.playerCanMove = EditorGUILayout.Toggle("Enable Movement", fpc.playerCanMove);
        if (fpc.playerCanMove)
            fpc.walkSpeed = EditorGUILayout.Slider("Walk Speed", fpc.walkSpeed, .1f, fpc.sprintSpeed);

        Separator("Sprint");
        fpc.enableSprint = EditorGUILayout.Toggle("Enable Sprint", fpc.enableSprint);
        if (fpc.enableSprint)
        {
            fpc.unlimitedSprint = EditorGUILayout.Toggle("Unlimited Sprint", fpc.unlimitedSprint);
            fpc.sprintKey   = (KeyCode)EditorGUILayout.EnumPopup("Sprint Key", fpc.sprintKey);
            fpc.sprintSpeed = EditorGUILayout.Slider("Sprint Speed", fpc.sprintSpeed, fpc.walkSpeed, 20);
            fpc.sprintDuration = EditorGUILayout.Slider("Sprint Duration", fpc.sprintDuration, 1, 20);
            fpc.sprintCooldown = EditorGUILayout.Slider("Sprint Cooldown", fpc.sprintCooldown, .1f, fpc.sprintDuration);
            fpc.sprintFOV   = EditorGUILayout.Slider("Sprint FOV", fpc.sprintFOV, fpc.fov, 179);
            fpc.sprintFOVStepTime = EditorGUILayout.Slider("Sprint FOV Step Time", fpc.sprintFOVStepTime, .1f, 20);

            fpc.useSprintBar       = EditorGUILayout.Toggle("Use Sprint Bar", fpc.useSprintBar);
            if (fpc.useSprintBar)
            {
                EditorGUI.indentLevel++;
                fpc.hideBarWhenFull   = EditorGUILayout.Toggle("Hide When Full", fpc.hideBarWhenFull);
                fpc.sprintBarBG       = (Image)EditorGUILayout.ObjectField("Bar BG", fpc.sprintBarBG, typeof(Image), true);
                fpc.sprintBar         = (Image)EditorGUILayout.ObjectField("Bar Fill", fpc.sprintBar, typeof(Image), true);
                fpc.sprintBarWidthPercent  = EditorGUILayout.Slider("Bar Width %", fpc.sprintBarWidthPercent, .1f, .5f);
                fpc.sprintBarHeightPercent = EditorGUILayout.Slider("Bar Height %", fpc.sprintBarHeightPercent, .001f, .025f);
                EditorGUI.indentLevel--;
            }
        }

        Separator("Hunger");
        fpc.enableHunger = EditorGUILayout.Toggle("Enable Hunger", fpc.enableHunger);
        if (fpc.enableHunger)
        {
            EditorGUI.indentLevel++;
            fpc.hungerLossRate  = EditorGUILayout.FloatField("Loss Rate", fpc.hungerLossRate);
            fpc.startingHunger  = EditorGUILayout.FloatField("Starting Hunger", fpc.startingHunger);
            fpc.hungerBarBG     = (Image)EditorGUILayout.ObjectField("Bar BG", fpc.hungerBarBG, typeof(Image), true);
            fpc.hungerBar       = (Image)EditorGUILayout.ObjectField("Bar Fill", fpc.hungerBar, typeof(Image), true);
            fpc.hungerBarWidthPercent  = EditorGUILayout.Slider("Bar Width %", fpc.hungerBarWidthPercent, .1f, .5f);
            fpc.hungerBarHeightPercent = EditorGUILayout.Slider("Bar Height %", fpc.hungerBarHeightPercent, .001f, .025f);
            EditorGUI.indentLevel--;
        }
    }

    void DrawHeadBob()
    {
        Separator("Head Bob");
        fpc.enableHeadBob = EditorGUILayout.Toggle("Enable Head Bob", fpc.enableHeadBob);
        if (fpc.enableHeadBob)
        {
            EditorGUI.indentLevel++;
            fpc.joint     = (Transform)EditorGUILayout.ObjectField("Camera Joint", fpc.joint, typeof(Transform), true);
            fpc.bobSpeed  = EditorGUILayout.Slider("Bob Speed", fpc.bobSpeed, 1, 20);
            fpc.bobAmount = EditorGUILayout.Vector3Field("Bob Amount", fpc.bobAmount);
            EditorGUI.indentLevel--;
        }
    }

    // ───────── Helpers ─────────
    static void Separator(string title)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    static void CenteredBold(string txt)
    {
        var s = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 14 };
        EditorGUILayout.LabelField(txt, s);
    }
}
#endif
