using UnityEngine;

public class MoveAndLook_TeleportBehavior : MonoBehaviour
{
    private IMoveAndLook inputSource;
    private MoveAndLook_SimpleBehavior moveSimple;
    private CharacterController character;

    [Header("Teleport Settings")]
    public GameObject TeleportMarker;
    public string TeleportTag = "Teleportable";
    public float MaxTeleportDistance = 15f;
    private bool isTeleportCanceled = false;
    private bool isValidTag = false;

    [Header("Arc Visuals")]
    public LineRenderer ArcRenderer;
    public float ArcLineWidth = 0.05f;
    public int ArcResolution = 30;
    public float ArcVelocity = 10f;
    public float ArcStepTime = 0.1f;
    public float Gravity = -9.81f;

    void Awake()
    {
        inputSource = GetComponent<IMoveAndLook>();
        moveSimple = GetComponent<MoveAndLook_SimpleBehavior>();
        character = GetComponent<CharacterController>();

        ArcRenderer.startWidth = ArcLineWidth;
        ArcRenderer.endWidth = ArcLineWidth;
        ArcRenderer.useWorldSpace = true;
    }

    void Update()
    {
        if (inputSource == null || moveSimple == null) return;
        HandleTeleport();
    }

    void HandleTeleport()
    {
        if (inputSource.TeleportAimingInput)
        {
            if (inputSource.TeleportCancelInput)
            {
                isTeleportCanceled = true;
                StopTeleportVisuals();
                moveSimple.currentMoveState = MoveAndLook_SimpleBehavior.PlayerState.Ground;
            }

            if (!isTeleportCanceled)
            {
                moveSimple.currentMoveState = MoveAndLook_SimpleBehavior.PlayerState.Teleport;
                moveSimple.moveVelocity = Vector3.zero; // 이동 멈춤
                UpdateTeleportPointer();
            }
        }
        else if (inputSource.TeleportExecuteInput)
        {
            if (!isTeleportCanceled && isValidTag && TeleportMarker != null && TeleportMarker.activeSelf)
            {
                ExecuteTeleport(TeleportMarker.transform.position);
            }
            ResetTeleportState();
        }
        else
        {
            if (ArcRenderer.enabled) StopTeleportVisuals();
        }
    }

    void UpdateTeleportPointer()
    {
        ArcRenderer.enabled = true;
        ArcRenderer.positionCount = ArcResolution;
        Vector3[] points = new Vector3[ArcResolution];
        
        // ArcRenderer의 위치와 방향을 기준으로 발사
        Vector3 startPos = ArcRenderer.transform.position;
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
                if (Physics.Raycast(prevPoint, dir.normalized, out RaycastHit hit, dir.magnitude))
                {
                    if (hit.collider.CompareTag(TeleportTag))
                    {
                        isValidTag = true;
                        TeleportMarker.SetActive(true);
                        TeleportMarker.transform.position = hit.point + Vector3.up * 0.05f;
                        for (int j = i; j < ArcResolution; j++) points[j] = hit.point;
                        break;
                    }
                }
            }
        }
        if (!isValidTag) TeleportMarker.SetActive(false);
        ArcRenderer.SetPositions(points);
    }

    void ExecuteTeleport(Vector3 targetPosition)
    {
        character.enabled = false;
        transform.position = targetPosition + Vector3.up * (character.height / 2f);
        character.enabled = true;
        moveSimple.moveVelocity = Vector3.zero;
        Debug.Log("Teleport Success");
    }

    void StopTeleportVisuals()
    {
        ArcRenderer.enabled = false;
        TeleportMarker.SetActive(false);
        isValidTag = false;
    }

    void ResetTeleportState()
    {
        isTeleportCanceled = false;
        StopTeleportVisuals();
        moveSimple.currentMoveState = MoveAndLook_SimpleBehavior.PlayerState.Ground;
    }
}