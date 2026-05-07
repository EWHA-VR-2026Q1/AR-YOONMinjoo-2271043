using UnityEngine;
using UnityEngine.InputSystem;

public class Ray_Controller_PC : MonoBehaviour, IRay
{
    [Header("Ray Properties")]
    public InputActionProperty RaycastAction;

    // 인터페이스 프로퍼티 구현
    public bool RaycastInput => RaycastAction.action?.ReadValue<float>() > 0.5f;
    
    private void OnEnable()
    {
        //Debug.Log("All Actions Enabled");
        EnableAllActions(true);
        SetCursorState(false);
    }

    private void OnDisable()
    {
        //Debug.Log("All Actions Disabled");
        EnableAllActions(false);
        SetCursorState(true);
    }

    private void EnableAllActions(bool enable)
    {
        // 모든 액션을 포함한 배열 생성
        InputActionProperty[] allActions = 
        {
            RaycastAction
            //RayOnAction, RayOffAction,
            //RayPosAction, RayStayAction
        };

        foreach (var property in allActions)
        {
            if (property.action != null)
            {
                if (enable) property.action.Enable();
                else property.action.Disable();
            }
        }
    }

    public void SetCursorState(bool cutsorState)
    {
        Cursor.lockState = cutsorState ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = cutsorState;
    }
}