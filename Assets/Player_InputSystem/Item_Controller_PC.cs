using UnityEngine;
using UnityEngine.InputSystem;

public class Item_Controller_PC : MonoBehaviour, IItem
{
    [Header("Grab Input Settings")]
    public InputActionProperty ItemNameAction;
    public InputActionProperty PickUpAction;    // 예: Mouse Left Click + p Key
    public InputActionProperty DropAction;    // 예: Mouse Left Click + p Key
    public InputActionProperty AddToInventoryAction;    // 예: Mouse Left Click + p Key
    public InputActionProperty RemoveFromInventoryAction;    // 예: Mouse Left Click + p Key
    public InputActionProperty EquipAction;   // 예: Mouse Left Click (Hold)
    public InputActionProperty UnEquipAction;    // 예: Mouse Left Click (Hold)
    public InputActionProperty UseAction; // 예: Key R (또는 Grab 해제 시 자동)
    public InputActionProperty StopUseAction;

    // IGrab_IS 구현
    // [중요] ItemName은 대개 string이므로 입력 판정이 아닌 고유 명칭을 반환하거나 제거해야 합니다.
    // 만약 인터페이스에 string ItemName이 있다면 아래처럼 처리합니다.
    public string ItemName => gameObject.name;
    public bool PickUp => PickUpAction.action?.WasPressedThisFrame() ?? false;
    public bool Drop => PickUpAction.action?.WasReleasedThisFrame() ?? false;
    public bool AddToInventory => AddToInventoryAction.action?.WasPressedThisFrame() ?? false;
    public bool RemoveFromInventory => RemoveFromInventoryAction.action?.WasPressedThisFrame() ?? false;
    public bool Equip => EquipAction.action?.WasPressedThisFrame() ?? false;
    public bool UnEquip => UnEquipAction.action?.WasPressedThisFrame() ?? false;
    public bool Use => UseAction.action?.WasPressedThisFrame() ?? false;
    public bool StopUse => UseAction.action?.WasReleasedThisFrame() ?? false;


    private void OnEnable() => EnableAllActions(true);    

    private void OnDisable() => EnableAllActions(false);    

    private void EnableAllActions(bool enable)
    {
        // 모든 액션을 포함한 배열 생성
        InputActionProperty[] allActions =
        {
            PickUpAction,
            DropAction, 
            AddToInventoryAction, RemoveFromInventoryAction,
            EquipAction, UnEquipAction, 
            UseAction, StopUseAction
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