using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MoveAndLook_Behavior : MonoBehaviour
{
    private CharacterController character;
    private IMoveAndLook inputSource; 

    [Header("Look Settings")]
    [Tooltip("PlayerLook_PC 할당. 미할당시 현재 오브젝트에서 PlayerLook_PC 컴포넌트를 찾음.")]
    public Transform CameraPivot;
    public Transform HeadModelPivot;
    public Transform PlayerTransform;
    public float PlayerRotSpeed = 0.1f;
    private float xRotation = 0f;

    [Header("Move Settings")]
    public float WalkSpeed = 1.5f;
    public float RunSpeed = 3.5f;
    public float Friction = 0.9f;
    public float ClimbSpeed = 1.2f;
    public float JumpHeight = 0.5f;
    public float Gravity = -9.81f;
    private Vector3 moveVelocity;

    [Header("Teleport Settings")]
    public GameObject TeleportMarker; // 텔레포트 지점을 표시할 프리팹
    // public LayerMask TeleportLayer;  // 텔레포트 가능한 바닥 레이어
    public string TeleportTag = "Teleportable"; // 텔레포트 가능한 바닥 태그
    public float MaxTeleportDistance = 15f;
    private bool isTeleportCanceled = false; // 이번 조준 세션이 취소되었는지 확인
    // private bool isValidLayer = false; // 현재 레이가 유효한 바닥에 닿아있는가?
    private bool isValidTag = false; // 현재 레이가 유효한 태그에 닿아있는가?

    [Header("Arc Teleport Settings")]
    public LineRenderer ArcRenderer;  // 인스펙터에서 할당
    public float ArcLineWidth = 0.1f;
    public int ArcResolution = 30;    // 포물선을 구성하는 점의 개수
    public float ArcVelocity = 10f;   // 발사 속도 (멀리 보낼수록 큰 값)
    public float ArcStepTime = 0.1f;  // 점 사이의 시간 간격

    [Header("SnapTurn Settings")]
    bool canSnapTurn = true;
    float snapTurnThreshold = 0.5f;

    [Header("State Settings")]
    int dummyVar;
    enum playerState { Ground, Air, Climb, Teleport }
    playerState currentMoveState;

    enum climbType { None, Ladder, Cliff }
    climbType currentClimbType = climbType.None;

    void Awake()
    {
        character = GetComponent<CharacterController>();
        inputSource = GetComponent<IMoveAndLook>();   
        PlayerTransform = transform;  
        currentMoveState = playerState.Ground;
        currentClimbType = climbType.None;

        // 선 전체의 두께를 0.05f로 고정 (VR에서는 0.02~0.05가 적당합니다)
        ArcRenderer.startWidth = ArcLineWidth;
        ArcRenderer.endWidth = ArcLineWidth;
        
        // 월드 좌표를 사용해야 이동 시 선이 꼬이지 않습니다.
        ArcRenderer.useWorldSpace = true;
    }
    void Update()
    {
        if (inputSource == null || character == null || !character.enabled) return;

        if (!UnityEngine.XR.XRSettings.isDeviceActive)
        {
            HandleLookBehavior();
        }
        ArcRenderer.transform.rotation = CameraPivot.rotation;
        // ArcRenderer.transform.SetPositionAndRotation(PlayerCamera.TransformPoint(RayAnchorOffset), PlayerCamera.rotation);

        // 텔레포트 입력을 가장 먼저 처리 (독립적 로직)
        HandleTeleport();

        // 텔레포트 중이 아닐 때만 회전 및 이동 상태 처리
        if (currentMoveState != playerState.Teleport)
        {
            HandleSnapTurn();
            HandleSetState();
        }

        character.Move(moveVelocity * Time.deltaTime);
    }

    void HandleSetState()
    {
        switch (currentMoveState)
        {
            case playerState.Ground: MoveGround(); break;
            case playerState.Air: MoveAir(); break;
            case playerState.Climb: MoveVertical(); break;
            // case playerState.Teleport: 삭제 (Update에서 직접 관리)
        }
    }

    void MoveGround()
    {
        // 인터페이스를 통해 값을 가져옴
        Vector2 moveInput = inputSource.MoveInput;
        Vector3 dirInput = PlayerTransform.forward * moveInput.y + PlayerTransform.right * moveInput.x;
        
        float speed = inputSource.SprintInput ? RunSpeed : WalkSpeed;
        
        if (dirInput.magnitude > 0.1f)
        {
            moveVelocity.x = dirInput.x * speed;
            moveVelocity.z = dirInput.z * speed;
        }
        else
        {
            moveVelocity.x *= Friction;
            moveVelocity.z *= Friction;
        }

        if (inputSource.JumpInput && character.isGrounded)
        {
            Debug.Log("Jump!");
            moveVelocity.y = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            currentMoveState = playerState.Air;
        }
        else
        {
            moveVelocity.y = -2f; 
        }

        if (!character.isGrounded && moveVelocity.y < 0) 
        {
            currentMoveState = playerState.Air;
        }
    }
    
    void MoveAir()
    {
        Vector2 moveInput = inputSource.MoveInput;
        Vector3 dirInput = PlayerTransform.forward * moveInput.y + PlayerTransform.right * moveInput.x;
        moveVelocity.x = dirInput.x * WalkSpeed;
        moveVelocity.z = dirInput.z * WalkSpeed;
        moveVelocity.y += Gravity * Time.deltaTime;

        if (character.isGrounded) currentMoveState = playerState.Ground;
    }

    void MoveVertical()
    {
        Vector2 moveInput = inputSource.MoveInput;
        moveVelocity = Vector3.zero;
        if (currentClimbType == climbType.Ladder)
        {
            moveVelocity.y = moveInput.y * ClimbSpeed;
        }
        else if (currentClimbType == climbType.Cliff)
        {
            moveVelocity = PlayerTransform.right * moveInput.x * ClimbSpeed + Vector3.up * moveInput.y * ClimbSpeed;
            moveVelocity += -PlayerTransform.forward * 0.1f;
        }

        if (inputSource.JumpInput)
        {
            Vector3 jumpDir = -PlayerTransform.forward + Vector3.up;
            moveVelocity = jumpDir.normalized * Mathf.Sqrt(JumpHeight * -2f * Gravity);
            currentMoveState = playerState.Air;
        }
    }

    void HandleSnapTurn()
    {
        float turnInput = inputSource.SnapTurnInput;

        // 0.5 이상 밀었을 때만 회전하도록 문턱치(Threshold) 설정 (조이스틱 대응)
        if (Mathf.Abs(turnInput) > snapTurnThreshold && canSnapTurn)
        {
            float turnAngle = turnInput > 0 ? 45f : -45f;
            PlayerTransform.Rotate(Vector3.up, turnAngle);
            //Debug.Log($"Snap Turn: {turnAngle}");
            canSnapTurn = false; 
        }
        // 스틱을 다시 중앙 근처(0.2 미만)로 놓아야 다음 회전 가능
        else if (Mathf.Abs(turnInput) < 0.2f)
        {
            canSnapTurn = true;
        }
    }

    void HandleTeleport()
{
    // [1] 조준 중 (T + LMB를 누르고 있는 동안)
    if (inputSource.TeleportAimingInput)
    {
        //Debug.Log("Aiming..."); // 이 로그가 찍히는지 확인
        // 취소 입력 시 처리
        if (inputSource.TeleportCancelInput)
        {
            isTeleportCanceled = true;
            StopTeleportVisuals(); // 비주얼 즉시 끄기
            currentMoveState = playerState.Ground;
        }

        if (!isTeleportCanceled)
        {
            currentMoveState = playerState.Teleport;
            moveVelocity = Vector3.zero;

            // 포물선 가시화 및 유효성 검사
            UpdateTeleportPointer(); 
        }
    }
    // [2] 버튼을 떼는 순간 (LMB를 뗌)
    else if (inputSource.TeleportExecuteInput)
    {
        Debug.Log("Executing..."); // 이 로그가 찍히는지 확인
        if (!isTeleportCanceled && isValidTag && TeleportMarker != null && TeleportMarker.activeSelf)
        {
            ExecuteTeleport(TeleportMarker.transform.position);
        }

        ResetTeleportState();
    }
    // [3] 아무것도 안 누를 때 (평상시)
    else
    {
        // 조준 중이 아닐 때는 포물선이 보이지 않아야 함
        if (ArcRenderer != null && ArcRenderer.enabled)
        {
            StopTeleportVisuals();
        }
    }
}

void StopTeleportVisuals()
{
    if (ArcRenderer != null) ArcRenderer.enabled = false;
    if (TeleportMarker != null) TeleportMarker.SetActive(false);
    isValidTag = false;
}

/*
void UpdateTeleportPointer()
{
    if (ArcRenderer == null || ArcRenderer.transform == null) return;

    ArcRenderer.enabled = true;
    ArcRenderer.positionCount = ArcResolution;
    
    // [핵심] 카메라가 아닌 실제 발사 지점(FirePoint)의 위치와 방향을 사용
    Vector3 currentPos = ArcRenderer.transform.position; 
    Vector3 currentVelocity = ArcRenderer.transform.forward * ArcVelocity; 
    
    Vector3[] points = new Vector3[ArcResolution];
    points[0] = currentPos;

    isValidLayer = false;

    for (int i = 1; i < ArcResolution; i++)
    {
        // 1. 중력 적용 (Gravity: -9.81f)
        currentVelocity += Vector3.up * Gravity * ArcStepTime;
        
        // 2. 다음 위치 계산
        Vector3 nextPos = currentPos + currentVelocity * ArcStepTime;
        
        // 3. 충돌 검사
        if (Physics.Raycast(currentPos, (nextPos - currentPos).normalized, out RaycastHit hit, (nextPos - currentPos).magnitude, TeleportLayer))
        {
            isValidLayer = true;
            if (TeleportMarker != null)
            {
                TeleportMarker.SetActive(true);
                TeleportMarker.transform.position = hit.point + Vector3.up * 0.05f;
            }

            for (int j = i; j < ArcResolution; j++) points[j] = hit.point;
            break;
        }

        points[i] = nextPos;
        currentPos = nextPos;
    }

    ArcRenderer.SetPositions(points);
}
*/

/*
void UpdateTeleportPointer()
{
    if (ArcRenderer == null) return;

    ArcRenderer.enabled = true;
    ArcRenderer.positionCount = ArcResolution;
    
    Vector3 startPos = CameraPivot.position;
    Vector3 velocity = CameraPivot.forward * ArcVelocity;
    
    Vector3[] points = new Vector3[ArcResolution];
    points[0] = startPos;
    Vector3 currentPos = startPos;

    isValidLayer = false;

    for (int i = 1; i < ArcResolution; i++)
    {
        // 1. 속도에 중력 적용 (단순 가감승제)
        velocity += Vector3.up * Gravity * ArcStepTime;
        // 2. 현재 위치에서 속도만큼 이동한 다음 위치 계산
        Vector3 nextPos = currentPos + velocity * ArcStepTime;
        
        // 3. 충돌 검사
        if (Physics.Raycast(currentPos, (nextPos - currentPos).normalized, out RaycastHit hit, (nextPos - currentPos).magnitude, TeleportLayer))
        {
            isValidLayer = true;
            if (TeleportMarker != null)
            {
                TeleportMarker.SetActive(true);
                TeleportMarker.transform.position = hit.point + Vector3.up * 0.05f;
            }
            // 충돌 이후의 모든 점은 충돌 지점에 고정 (선이 튀는 현상 방지)
            for (int j = i; j < ArcResolution; j++) points[j] = hit.point;
            break;
        }

        points[i] = nextPos;
        currentPos = nextPos;
    }

    ArcRenderer.SetPositions(points);
}
*/
/*
void UpdateTeleportPointer()
{
    if (ArcRenderer == null) return;

    ArcRenderer.enabled = true;
    ArcRenderer.positionCount = ArcResolution;
    
    Vector3 currentPos = CameraPivot.position; // 시작 위치
    Vector3 currentVelocity = CameraPivot.forward * ArcVelocity; // 초기 속도
    
    Vector3[] points = new Vector3[ArcResolution];
    points[0] = currentPos;

    isValidLayer = false;

    for (int i = 1; i < ArcResolution; i++)
    {
        // 1. 중력을 속도에 누적 (Gravity는 -9.81f 음수)
        currentVelocity += Vector3.up * Gravity * ArcStepTime;
        
        // 2. 새로운 위치 계산 (이전 위치 + 현재 속도)
        Vector3 nextPos = currentPos + currentVelocity * ArcStepTime;
        
        // 3. 충돌 검사 (현재 구간)
        if (Physics.Raycast(currentPos, (nextPos - currentPos).normalized, out RaycastHit hit, (nextPos - currentPos).magnitude, TeleportLayer))
        {
            isValidLayer = true;
            if (TeleportMarker != null)
            {
                TeleportMarker.SetActive(true);
                TeleportMarker.transform.position = hit.point + Vector3.up * 0.05f;
            }

            // 남은 점들을 충돌 지점으로 채움
            for (int j = i; j < ArcResolution; j++) points[j] = hit.point;
            break;
        }

        // 4. 점 저장 및 위치 갱신
        points[i] = nextPos;
        currentPos = nextPos;
    }

    if (!isValidLayer && TeleportMarker != null) TeleportMarker.SetActive(false);
    ArcRenderer.SetPositions(points);
}
*/
/*
void UpdateTeleportPointer()
{
    if (ArcRenderer == null) return;

    ArcRenderer.enabled = true;
    ArcRenderer.positionCount = ArcResolution;
    Vector3[] points = new Vector3[ArcResolution];
    
    Vector3 startPos = CameraPivot.position;
    Vector3 startVelocity = CameraPivot.forward * ArcVelocity;

    isValidLayer = false;

    for (int i = 0; i < ArcResolution; i++)
    {
        float t = i * ArcStepTime;
        
        // [수정 핵심] Gravity가 음수(-9.81)이므로 공식 그대로 사용하면 아래로 향합니다.
        // 공식: p = p0 + v0*t + 0.5*g*t^2
        Vector3 currentPoint = startPos + (startVelocity * t) + (0.5f * Vector3.up * Gravity * t * t);
        points[i] = currentPoint;

        // [중요] 충돌 검사 로직이 반드시 포함되어야 선이 꼬이지 않고 바닥에서 멈춥니다.
        if (i > 0)
        {
            Vector3 prevPoint = points[i - 1];
            Vector3 dir = currentPoint - prevPoint;
            float dist = dir.magnitude;

            if (Physics.Raycast(prevPoint, dir.normalized, out RaycastHit hit, dist, TeleportLayer))
            {
                isValidLayer = true;
                if (TeleportMarker != null)
                {
                    TeleportMarker.SetActive(true);
                    TeleportMarker.transform.position = hit.point + Vector3.up * 0.05f;
                }

                // 충돌 지점 이후의 모든 점을 hit.point로 고정 (이게 없으면 선이 꼬여 보입니다)
                for (int j = i; j < ArcResolution; j++)
                {
                    points[j] = hit.point;
                }
                break;
            }
        }
    }

    // 레이어가 유효하지 않으면 마커 끄기
    if (!isValidLayer && TeleportMarker != null) TeleportMarker.SetActive(false);

    ArcRenderer.SetPositions(points);
}
*/
 /*
 void UpdateTeleportPointer()
{
    if (ArcRenderer == null) return;

    ArcRenderer.enabled = true;
    ArcRenderer.positionCount = ArcResolution;
    Vector3[] points = new Vector3[ArcResolution];
    
    Vector3 startPos = CameraPivot.position;
    Vector3 startDir = CameraPivot.forward;
    Vector3 startVelocity = startDir * ArcVelocity;

    isValidLayer = false;

    for (int i = 0; i < ArcResolution; i++)
    {
        // 시간을 i에 따라 순차적으로 증가시킴
        float t = i * ArcStepTime;
        
        // 포물선 공식: 시작점 + (초기속도 * 시간) + (1/2 * 중력 * 시간제곱)
        // Gravity가 -9.81이므로 Vector3.up과 곱해지면 자연스럽게 아래 방향 벡터가 됩니다.
        Vector3 gravityOffset = 0.5f * Vector3.up * Gravity * (t * t);
        Vector3 currentPoint = startPos + (startVelocity * t) + gravityOffset;
        
        points[i] = currentPoint;

        // 충돌 검사 로직 (생략 - 기존과 동일)
        if (i > 0) 
        {
            // ... (기존 Raycast 로직)
        }
    }
    ArcRenderer.SetPositions(points);
}
*/
 
 void UpdateTeleportPointer()
{
    if (ArcRenderer == null) {
        Debug.LogWarning("LineRenderer(ArcRenderer)가 할당되지 않았습니다!");
        return;
    }
    // if (ArcRenderer == null) return;

    ArcRenderer.enabled = true;
    ArcRenderer.positionCount = ArcResolution;
    Vector3[] points = new Vector3[ArcResolution];
    
    Vector3 startPos = ArcRenderer.transform.position;
    // 발사 속도가 너무 낮으면 포물선이 발 밑으로 꺼집니다. 
    // ArcVelocity가 인스펙터에서 0으로 되어있지 않은지 확인하세요.
    Vector3 startVelocity = ArcRenderer.transform.forward * ArcVelocity;

    isValidTag = false;

    for (int i = 0; i < ArcResolution; i++)
    {
        float t = i * ArcStepTime;
        Vector3 currentPoint = startPos + (startVelocity * t) + (0.5f * Vector3.up * Gravity * t * t);
        points[i] = currentPoint;

        if (i > 0)
        {
            Vector3 prevPoint = points[i - 1];
            Vector3 dir = currentPoint - prevPoint;
            float dist = dir.magnitude;

            // 디버그용: 에디터 씬 뷰에서 실제 계산되는 포물선 선분을 그려줍니다.
            Debug.DrawLine(prevPoint, currentPoint, Color.red);

            if (Physics.Raycast(prevPoint, dir.normalized, out RaycastHit hit, dist))
            {
                if(hit.collider.CompareTag(TeleportTag)){
                    isValidTag = true;
                    if (TeleportMarker != null)
                    {
                        TeleportMarker.SetActive(true);
                        TeleportMarker.transform.position = hit.point + Vector3.up * 0.05f;
                    }

                    // 충돌 지점 이후 점들 고정
                    for (int j = i; j < ArcResolution; j++) points[j] = hit.point;
                    
                    // 로그 추가: 무엇에 부딪혔는지 확인
                    // Debug.Log($"Hit: {hit.collider.name} at {hit.point}");
                    break;
                }
            }
        }
    }

    if (!isValidTag)
    {
        if (TeleportMarker != null) TeleportMarker.SetActive(false);
        // Debug.LogWarning("레이가 TeleportTag에 닿지 않았습니다.");
    }

    ArcRenderer.SetPositions(points);
}

    void ResetTeleportState()
    {
        isTeleportCanceled = false;
        isValidTag = false; // 상태 초기화
        if (TeleportMarker != null) TeleportMarker.SetActive(false);
        if (ArcRenderer != null) ArcRenderer.enabled = false;
        currentMoveState = playerState.Ground;        
    }

    void ExecuteTeleport(Vector3 targetPosition)
    {
        // CharacterController가 켜져 있으면 transform.position이 즉시 바뀌지 않을 수 있음
        character.enabled = false; 
        
        // 바닥에 발이 닿도록 높이 조절 (캐릭터 절반 높이만큼 위로)
        transform.position = targetPosition + Vector3.up * (character.height / 2f);
        
        character.enabled = true;
        
        // 텔레포트 직후 속도 초기화
        moveVelocity = Vector3.zero;
        Debug.Log("Teleport Complete");
    }
    // PC용 마우스 회전 로직
    void HandleLookBehavior()
    {
        Vector2 look = inputSource.LookInput * PlayerRotSpeed; // 감도 조절

        xRotation -= look.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // 캐릭터 좌우 회전
        CameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        HeadModelPivot.localRotation = CameraPivot.localRotation;
        // 캐릭터 좌우 회전
        PlayerTransform.Rotate(Vector3.up * look.x);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ClimbableLadder")) { currentClimbType = climbType.Ladder; currentMoveState = playerState.Climb; }
        if (other.CompareTag("ClimbableCliff")) { currentClimbType = climbType.Cliff; currentMoveState = playerState.Climb; }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ClimbableLadder") || other.CompareTag("ClimbableCliff")) currentMoveState = playerState.Air;
    }
}
