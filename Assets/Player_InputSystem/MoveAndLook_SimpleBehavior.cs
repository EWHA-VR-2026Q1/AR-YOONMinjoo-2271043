using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MoveAndLook_SimpleBehavior : MonoBehaviour
{
    private CharacterController character;
    private IMoveAndLook inputSource; 

    [Header("Look Settings")]
    public Transform CameraPivot;
    public Transform HeadModelPivot;
    public Transform PlayerTransform;
    public float PlayerRotSpeed = 1f;
    private float xRotation = 0f;

    [Header("Move Settings")]
    public float WalkSpeed = 1.5f;
    public float RunSpeed = 3.5f;
    public float Friction = 0.9f;
    public float ClimbSpeed = 1.2f;
    public float JumpHeight = 0.5f;
    public float Gravity = -9.81f;
    public Vector3 moveVelocity; // Teleport에서 접근 가능하도록 public 또는 protected

    [Header("SnapTurn Settings")]
    bool canSnapTurn = true;
    float snapTurnThreshold = 0.5f;

    public enum PlayerState { Ground, Air, Climb, Teleport }
    public PlayerState currentMoveState = PlayerState.Ground;

    enum ClimbType { None, Ladder, Cliff }
    ClimbType currentClimbType = ClimbType.None;

    void Awake()
    {
        character = GetComponent<CharacterController>();
        inputSource = GetComponent<IMoveAndLook>();   
        PlayerTransform = transform;  
    }

    void Update()
    {
        if (inputSource == null || character == null || !character.enabled) return;

        // 마우스 룩 (PC)
        if (!UnityEngine.XR.XRSettings.isDeviceActive) HandleLookBehavior();

        // 텔레포트 중이 아닐 때만 이동 로직 수행
        if (currentMoveState != PlayerState.Teleport)
        {
            HandleSnapTurn();
            HandleSetState();
            character.Move(moveVelocity * Time.deltaTime);
        }
    }

    void HandleSetState()
    {
        switch (currentMoveState)
        {
            case PlayerState.Ground: MoveGround(); break;
            case PlayerState.Air: MoveAir(); break;
            case PlayerState.Climb: MoveVertical(); break;
        }
    }

    // ... (기존 MoveGround, MoveAir, MoveVertical 로직 동일)
    void MoveGround()
    {
        Vector2 moveInput = inputSource.MoveInput;
        Vector3 dirInput = PlayerTransform.forward * moveInput.y + PlayerTransform.right * moveInput.x;
        float speed = inputSource.SprintInput ? RunSpeed : WalkSpeed;
        
        if (dirInput.magnitude > 0.1f) {
            moveVelocity.x = dirInput.x * speed;
            moveVelocity.z = dirInput.z * speed;
        } else {
            moveVelocity.x *= Friction;
            moveVelocity.z *= Friction;
        }

        if (inputSource.JumpInput && character.isGrounded) {
            moveVelocity.y = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            currentMoveState = PlayerState.Air;
        } else {
            moveVelocity.y = -2f; 
        }

        if (!character.isGrounded && moveVelocity.y < 0) currentMoveState = PlayerState.Air;
    }

    void MoveAir()
    {
        Vector2 moveInput = inputSource.MoveInput;
        Vector3 dirInput = PlayerTransform.forward * moveInput.y + PlayerTransform.right * moveInput.x;
        moveVelocity.x = dirInput.x * WalkSpeed;
        moveVelocity.z = dirInput.z * WalkSpeed;
        moveVelocity.y += Gravity * Time.deltaTime;
        if (character.isGrounded) currentMoveState = PlayerState.Ground;
    }

    void MoveVertical()
    {
        Vector2 moveInput = inputSource.MoveInput;
        moveVelocity = Vector3.zero;
        if (currentClimbType == ClimbType.Ladder) moveVelocity.y = moveInput.y * ClimbSpeed;
        else if (currentClimbType == ClimbType.Cliff) {
            moveVelocity = PlayerTransform.right * moveInput.x * ClimbSpeed + Vector3.up * moveInput.y * ClimbSpeed;
            moveVelocity += -PlayerTransform.forward * 0.1f;
        }

        if (inputSource.JumpInput) {
            Vector3 jumpDir = -PlayerTransform.forward + Vector3.up;
            moveVelocity = jumpDir.normalized * Mathf.Sqrt(JumpHeight * -2f * Gravity);
            currentMoveState = PlayerState.Air;
        }
    }

    void HandleSnapTurn()
    {
        float turnInput = inputSource.SnapTurnInput;
        if (Mathf.Abs(turnInput) > snapTurnThreshold && canSnapTurn) {
            PlayerTransform.Rotate(Vector3.up, turnInput > 0 ? 45f : -45f);
            canSnapTurn = false; 
        } else if (Mathf.Abs(turnInput) < 0.2f) canSnapTurn = true;
    }

    void HandleLookBehavior()
    {
        Vector2 look = inputSource.LookInput * PlayerRotSpeed;
        xRotation -= look.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        CameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        if(HeadModelPivot) HeadModelPivot.localRotation = CameraPivot.localRotation;
        PlayerTransform.Rotate(Vector3.up * look.x);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ClimbableLadder")) { currentClimbType = ClimbType.Ladder; currentMoveState = PlayerState.Climb; }
        if (other.CompareTag("ClimbableCliff")) { currentClimbType = ClimbType.Cliff; currentMoveState = PlayerState.Climb; }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ClimbableLadder") || other.CompareTag("ClimbableCliff")) currentMoveState = PlayerState.Air;
    }
}