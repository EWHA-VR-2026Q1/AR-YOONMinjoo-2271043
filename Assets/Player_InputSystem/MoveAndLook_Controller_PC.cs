using UnityEngine;
using UnityEngine.InputSystem;

public class MoveAndLook_Controller_PC : MonoBehaviour, IMoveAndLook
{
    [Header("Look Properties")]
    public InputActionProperty LookAction;

    [Header("Move Properties")]
    public InputActionProperty MoveAction;    
    public InputActionProperty SprintAction;
    public InputActionProperty JumpAction;
    public InputActionProperty TeleportAimingAction;
    public InputActionProperty TeleportExecuteAction;
    public InputActionProperty TeleportCancelAction;
    public InputActionProperty SnapTurnAction;

    // --- 인터페이스 프로퍼티 구현 ---
    // 이동 및 시선
    public Vector2 MoveInput => MoveAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
    public Vector2 LookInput => LookAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
    public bool SprintInput => SprintAction.action?.IsPressed() ?? false;
    public bool JumpInput => JumpAction.action?.WasPressedThisFrame() ?? false;
    public bool TeleportAimingInput => TeleportAimingAction.action?.IsPressed() ?? false;
    public bool TeleportExecuteInput => TeleportExecuteAction.action?.WasReleasedThisFrame() ?? false;
    public bool TeleportCancelInput => TeleportCancelAction.action?.WasReleasedThisFrame() ?? false;
    public float SnapTurnInput => SnapTurnAction.action?.ReadValue<float>() ?? 0f;

    private void OnEnable()
    {
        //Debug.Log("All Actions Enabled");
        EnableAllActions(true);
    }

    private void OnDisable()
    {
        //Debug.Log("All Actions Disabled");
        EnableAllActions(false);
    }

    private void EnableAllActions(bool enable)
    {
        // 모든 액션을 포함한 배열 생성
        InputActionProperty[] allActions = 
        { 
            LookAction, 
            MoveAction, SprintAction, JumpAction,  
            TeleportAimingAction, TeleportExecuteAction, TeleportCancelAction,
            SnapTurnAction
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
}