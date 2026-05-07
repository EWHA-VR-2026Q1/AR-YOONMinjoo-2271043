using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Ray_Behavior : MonoBehaviour
{
    private IRay rayInput;

    [Header("Player Camera Settings")]
    public Transform PlayerCamera; // 카메라를 기준으로 움직이려면 카메라 트랜스폼이 필요합니다.

    [Header("Ray Anchor Settings")]
    public Transform RayAnchor;
    public Vector3 RayAnchorOffset = new Vector3(-0.3f, -0.6f, 0.3f);

    [Header("Raycast Settings")]
    public float RayMaxDistance = 20f;
    public float RayWidth = 0.01f;
    public RaycastHit? CurrentHit { get; private set; } // 외부에서는 읽기만 가능하고 내부에서만 수정 가능한 프로퍼티
    public bool IsHitting { get; private set; }

    [Header("Interface State")]
    IGeneral lastSensedInterface;
    // public string TargetTag = "Interactable";
    // string prevTag = null;
    // string currentTag = null;

    [Header("LineRenderer Settings")]
    public LineRenderer CurrentLineRenderer;

    [Header("HitPointer Settings")]
    public GameObject HitPointer;
    public GameObject Marker;

    // [Header("IInterface Settings")]
    // private IGeneral_IS lastSensedInterface = null; // 이전에 감지된 인터페이스 저장

    void Awake()
    {
        // Debug.unityLogger.logEnabled = false;

        rayInput = GetComponent<IRay>(); 
        CurrentLineRenderer = RayAnchor.GetComponent<LineRenderer>();
        CurrentLineRenderer.startWidth = RayWidth;
        CurrentLineRenderer.endWidth = RayWidth;
        CurrentLineRenderer.positionCount = 2; // 시작점과 끝점
        CurrentLineRenderer.enabled = false;
        HitPointer.SetActive(false);
        if(RayAnchorOffset != Vector3.zero ) Marker.SetActive(false );
    }

    void Update()
    {
        RaycastAction();        
    }

    private Ray GetRay()
    {
        return new Ray(RayAnchor.position, RayAnchor.forward);
    }
    
    void RaycastAction()
    {
        bool RaycastAction = rayInput.RaycastInput;
        //Debug.Log($"RaycastAction:{RaycastAction}");
        if (RaycastAction)
        {
            // Debug.Log("Ray On");
            RayAnchor.SetPositionAndRotation(PlayerCamera.TransformPoint(RayAnchorOffset), PlayerCamera.rotation);
            Ray ray = GetRay();           
            CurrentLineRenderer.enabled = true;
            DrawRay();
        }
        else
        {
            // Debug.Log("Ray Off");
            CurrentLineRenderer.enabled = false;
            HitPointer.SetActive(false);
            HandleInterfaceState(null);
        }
    }
    void DrawRay()
    {
        RaycastHit hit;
        Vector3 lineStartPos = RayAnchor.position;
        Vector3 lineDirection = RayAnchor.forward;
        Vector3 lineEndPos;

        // 레이어 제한 없이 모든 물체 감지 (Ignore Raycast 레이어 제외)
        if (Physics.Raycast(lineStartPos, lineDirection, out hit, RayMaxDistance))
        {
            CurrentHit = hit;
            IsHitting = true;

            lineEndPos = hit.point;

            IGeneral generalInterface = null;
            if (hit.transform.TryGetComponent<IGeneral>(out generalInterface))
            {
                HitPointer.SetActive(true);
                HitPointer.transform.SetPositionAndRotation(
                    hit.point + (hit.normal * 0.01f),
                    Quaternion.FromToRotation(Vector3.up, hit.normal)
                );
            }
            else
            {
                HitPointer.SetActive(false);
            }

            /*
            if(hit.collider.CompareTag("Interactable"))
            {
                HitPointer.SetActive(true);
                HitPointer.transform.SetPositionAndRotation(
                    hit.point + (hit.normal * 0.01f),
                    Quaternion.FromToRotation(Vector3.up, hit.normal)
                );
            }
            else
            {
                HitPointer.SetActive(false);
            } 
            */          

            // 인터페이스 처리 (상호작용 가능한지 여부는 HandleInterfaceState 내부에서 판단)
            HandleInterfaceState(hit);
        }
        else
        {
            // 아무것도 맞지 않았을 때
            CurrentHit = null;
            IsHitting = false;

            lineEndPos = lineStartPos + (lineDirection * RayMaxDistance);
            HitPointer.SetActive(false);
            HandleInterfaceState(null);
        }

        // 선은 항상 그리기 (모든 물체에 닿는 지점까지)
        CurrentLineRenderer.SetPosition(0, lineStartPos);
        CurrentLineRenderer.SetPosition(1, lineEndPos);
    }

    /*
    // Tag를 사용하는 방식
    void HandleInterfaceState(RaycastHit? hit)
    {
        IGeneral_IS generalInterface = null;
        // IItem_IS itemInterface = null;

        // 현재 레이에 맞은 물체에서 인터페이스 추출
        if (hit.HasValue)
        {
            currentTag = hit.Value.collider.tag;  
            hit.Value.transform.TryGetComponent<IGeneral_IS>(out generalInterface);        
            // hit.Value.transform.TryGetComponent<IItem_IS>(out itemInterface);
        }
        else
        {
            currentTag = null;
        }
        // Debug.Log($"CurrentTag: {currentTag}, PrevTag: {prevTag}");
        
        if(hit.HasValue && (generalInterface != null))
        {         
            //Debug.Log($"CurrentTag: {currentTag}, PrevTag: {prevTag}");
            // 상태 비교 및 실행
            // Case A: 이번 프레임에 새로운 인터페이스를 만남 (Enter)
            if (currentTag == TargetTag && prevTag != TargetTag)
            {
                string targetInfo = generalInterface.GetTargetInfo();
                //string targetInfo = currentInterface.GetTargetInfo();
                Debug.Log(targetInfo);
                generalInterface.OnEnter(gameObject);
                // Debug.Log("Enter Interface");
            }
            // Case B: 이전과 같은 인터페이스를 계속 보고 있음 (Stay/Action)
            else if (currentTag == TargetTag && prevTag == TargetTag)
            {
                generalInterface.OnAction(gameObject);
            }
            // Case C: 보고 있던 인터페이스에서 벗어남 (Exit)
            else if (currentTag != TargetTag && prevTag == TargetTag)
            {
                generalInterface.OnExit(gameObject);
                // Debug.Log("Exit Interface");
            }
            // Case D: A 인터페이스에서 B 인터페이스로 바로 옮겨감 (Exit 후 Enter)
            else if (currentTag != TargetTag && prevTag != TargetTag)
            {
                generalInterface.OnExit(gameObject);
                generalInterface.OnEnter(gameObject);
                // Debug.Log("Switched Interface");
            }
        }

        // 마지막 상태 저장
        prevTag = currentTag;
    }
    */

    // 인터페이스 직접 참조 방식
    void HandleInterfaceState(RaycastHit? hit)
    {
        IGeneral currentInterface = null;

        // 현재 레이에 맞은 물체에서 인터페이스 추출
        if (hit.HasValue)
        {
            hit.Value.transform.TryGetComponent<IGeneral>(out currentInterface);
        }

        // 상태 비교 및 실행
        // Case A: 이번 프레임에 새로운 인터페이스를 만남 (Enter)
        if (currentInterface != null && lastSensedInterface == null)
        {
            currentInterface.OnEnter(gameObject);
            // Debug.Log("Enter Interface");
        }
        // Case B: 이전과 같은 인터페이스를 계속 보고 있음 (Stay/Action)
        else if (currentInterface != null && lastSensedInterface == currentInterface)
        {
            currentInterface.OnStay(gameObject);
        }
        // Case C: 보고 있던 인터페이스에서 벗어남 (Exit)
        else if (currentInterface == null && lastSensedInterface != null)
        {
            lastSensedInterface.OnExit(gameObject);
            // Debug.Log("Exit Interface");
        }
        // Case D: A 인터페이스에서 B 인터페이스로 바로 옮겨감 (Exit 후 Enter)
        else if (currentInterface != null && lastSensedInterface != currentInterface)
        {
            lastSensedInterface.OnExit(gameObject);
            currentInterface.OnEnter(gameObject);
            // Debug.Log("Switched Interface");
        }

        // 마지막 상태 저장
        lastSensedInterface = currentInterface;
    }
}