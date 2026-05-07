using UnityEngine;
using UnityEngine.InputSystem;

public class Grab_Controller_PC : MonoBehaviour, IGrab
{
    [Header("Grab Input Settings")]
    public InputActionProperty DistanceGrabAction;    // 예: Mouse Left Click + p Key
    public InputActionProperty PullAction;    // 예: Mouse Left Click + p Key
    public InputActionProperty DistancePokeAction;    // 예: Mouse Left Click + p Key
    public InputActionProperty PokeAction;    // 예: Mouse Left Click + p Key
    public InputActionProperty PinchAction;   // 예: Mouse Left Click (Hold)
    public InputActionProperty GrabAction;    // 예: Mouse Left Click (Hold)
    public InputActionProperty ReleaseAction; // 예: Key R (또는 Grab 해제 시 자동)

    // IGrab 구현
    public bool DistanceGrab => DistanceGrabAction.action?.WasPressedThisFrame() ?? false;
    public bool Pull => PullAction.action?.IsPressed() ?? false;
    public bool DistancePoke => DistancePokeAction.action?.WasPressedThisFrame() ?? false;
    public bool Poke => PokeAction.action?.WasPressedThisFrame() ?? false;

    // Pinch와 Grab은 PC에서 보통 마우스 왼쪽 클릭 하나로 대응
    public bool Pinch => PinchAction.action?.WasPressedThisFrame() ?? false;
    public bool Grab => GrabAction.action?.WasPressedThisFrame() ?? false;

    // Grab 버튼을 뗐을 때를 Release로 판단하거나 별도 키를 사용
    public bool Release => ReleaseAction.action?.WasReleasedThisFrame() ?? false;

    private void OnEnable()
    {
        EnableAllActions(true);
    }

    private void OnDisable()
    {
        EnableAllActions(false);
    }

    private void EnableAllActions(bool enable)
    {
        // 모든 액션을 포함한 배열 생성
        InputActionProperty[] allActions =
        {
            DistanceGrabAction, PullAction, DistancePokeAction,
            PokeAction, PinchAction, GrabAction,
            ReleaseAction
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