using UnityEngine;

public class Grab_Behavior : MonoBehaviour
{
    // private IGeneral_IS generalInterface; // 상호작용 인터페이스
    private IGrab grabInput;
    private Ray_Behavior rayBehavior;

    [Header("Grab Settings")]
    public Transform GrabAnchor; // 물체가 붙을 손 위치 (카메라 앞 등)
    private GameObject grabbedObject = null;
    private Transform originalParent = null;

    [Header("Touch Settings")]
    [SerializeField] private GameObject touchingObject = null; // 현재 손에 닿아 있는 물체

    void Awake()
    {
        // generalInterface = GetComponent<IGeneral_IS>();
        grabInput = GetComponent<IGrab>();
        rayBehavior = GetComponent<Ray_Behavior>();
    }

    void Update()
    {
        //if(!TryGetComponent<IGeneral_IS>(out IGeneral_IS currentInterface)) return;

        // if(grabInput.Pull) Debug.Log("Pull Input Detected!"); // 버튼을 누르는 순간 로그가 찍혀야 함
        HandleGrab();
    }
    void HandleGrab()
    {
        if (grabbedObject != null)
        {
            if (grabInput.Release) TryRelease();
            return; 
        }

        // 원거리 상호작용 (Ray 기반)
        if (rayBehavior.IsHitting)
        {
            GameObject target = rayBehavior.CurrentHit.Value.collider.gameObject;
            if(!target.TryGetComponent<IGeneral>(out IGeneral targetInterface)) return;

            if (grabInput.Pull) TryPull(target);
            else if (grabInput.DistancePoke) TryDistancePoke(target);
            else if (grabInput.DistanceGrab) TryDistanceGrab(target);            
        }

        // 근거리 상호작용 (Trigger 기반)
        if (touchingObject != null)
        {
            if(!touchingObject.TryGetComponent<IGeneral>(out IGeneral targetInterface)) return;
            
            if (grabInput.Poke) TryPoke(touchingObject);
            else if (grabInput.Pinch) TryPinch(touchingObject);
            else if (grabInput.Grab) TryGrab(touchingObject);
        }

        // 공통 Release
        if (grabInput.Release && grabbedObject != null) TryRelease();
    }

    void TryDistancePoke(GameObject target)
    {
        Debug.Log("Distance Poke");
        target.GetComponent<IGeneral>()?.OnClick(gameObject);
    }

    void TryDistanceGrab(GameObject target)
    {
        ExecuteGrab(target, false); // snap false
    }

    void TryPull(GameObject target)
    {
        Debug.Log("Pull");
        ExecuteGrab(target, true); // snap true
    }

    void TryPoke(GameObject target)
    {
        Debug.Log("Poke");
        target.GetComponent<IGeneral>()?.OnClick(gameObject);
    }

    void TryPinch(GameObject target)
    {
        Debug.Log("Pinch");
        ExecuteGrab(target, true);
    }

    void TryGrab(GameObject target)
    {
        Debug.Log("Grab");
        ExecuteGrab(target, true);
    }

    private void ExecuteGrab(GameObject target, bool snapToAnchor)
    {
        Debug.Log("ExecuteGrab...");
        if (!target.TryGetComponent<Actor_Grabable>(out _)) return;
        Debug.Log("ExecuteGrab");
        grabbedObject = target;
        originalParent = grabbedObject.transform.parent;

        if (grabbedObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.isKinematic = true; // 퀘스트 이식 시 rb.useGravity = false도 고려
        }

        grabbedObject.transform.SetParent(GrabAnchor);

        if (snapToAnchor)
        {   Debug.Log("Snapping to Anchor");
            grabbedObject.transform.SetPositionAndRotation(GrabAnchor.position, GrabAnchor.rotation);
        }
    }

    void TryRelease()
    {
        //Debug.Log("Release");
        if (grabbedObject == null) return;

        if (grabbedObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.isKinematic = false;
        }

        grabbedObject.transform.SetParent(originalParent);
        grabbedObject = null;
        Debug.Log("Released");
    }

    // 물리 충돌 감지 (손에 닿았는지 여부)
    private void OnTriggerEnter(Collider other)
    {
        // Interactable 레이어나 특정 컴포넌트가 있는지 확인
        if (other.TryGetComponent<Actor_Grabable>(out _))
        {
            touchingObject = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (touchingObject == other.gameObject)
        {
            touchingObject = null;
        }
    }
}