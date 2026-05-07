using System.Collections.Generic;
using UnityEngine;

public class Item_Behavior : MonoBehaviour
{
    private IGeneral generalInterface; // 상호작용 인터페이스
    private IItem itemInput;      // PC/VR 입력 인터페이스
    private Ray_Behavior rayBehavior; // 레이캐스트 정보

    [Header("Inventory")]
    public List<Actor_Item> Items = new List<Actor_Item>();
    public int SelectedItemIndex = 0;
    public Transform HoldPosition; // 아이템을 들고 있을 위치 (손 위치)
    private Actor_Item currentItem;

    [Header("Pickup Settings")]
    private Actor_Item PickedItem; // 현재 레이에 닿은 아이템

    void Awake()
    {
        itemInput = GetComponent<IItem>();
        rayBehavior = GetComponent<Ray_Behavior>();
        // generalInterface = GetComponent<IGeneral>();
    }

    void Update()
    {
        HandleWorldInteraction();
        HandleInventoryAction();
    }

    // 필드에 놓인 아이템 상호작용 (Raycast)
    private void HandleWorldInteraction()
    {
        if(currentItem != null) return; // 이미 아이템을 들고 있으면 레이 상호작용 무시
        
        //Debug.Log("HandleWorldInteraction");
        if (rayBehavior.IsHitting)
        {            
            // 레이에 닿은 객체가 Actor_Item인지 확인
            Actor_Item hitItem = rayBehavior.CurrentHit?.transform.GetComponent<Actor_Item>();

            if (hitItem != null)
            {
                if(hitItem.TryGetComponent<IGeneral>(out IGeneral generalInterface))
                {
                    if(itemInput.PickUp && generalInterface != null)
                    {
                        // 아이템 정보 표시 (예: "아이템: {ItemName}")
                        Debug.Log($"Picked {hitItem.ItemName}");
                        PickUpItem(hitItem);
                    }
                }                 
            }
            
            // if(hitItem != null && itemInput.PickUp)
            // {
            //     Debug.Log($"Picked {hitItem.ItemName}");
            //     PickUpItem(hitItem);
            // }
        }
    }

    // 현재 인벤토리에 있는 아이템 제어
    private void HandleInventoryAction()
    {
        if (Items.Count <= 0) return;

        // 아이템 버리기
        if (itemInput.Drop || itemInput.RemoveFromInventory)
        {
            DropItem(SelectedItemIndex);
        }

        // 아이템 사용 (예: "사용" 로그 출력)
        if (itemInput.Use)
        {
            Debug.Log($"{Items[SelectedItemIndex].ItemName}을 사용했습니다!");
        }
    }

    // --- 내부 동작 함수들 ---

    private void PickUpItem(Actor_Item item)
    {
        Debug.Log("PickUpItem");
        if (Items.Contains(item)) return;

        Items.Add(item);
        item.SetPhysics(false); // 물리 비활성화
        item.transform.SetParent(HoldPosition); // 손 위치로 부모 설정
        item.transform.localPosition = Vector3.zero;
        item.gameObject.SetActive(false); // 일단 인벤토리에 넣었으니 숨김

        Debug.Log($"{item.ItemName} 획득");
    }

    private void DropItem(int index)
    {
        if (index < 0 || index >= Items.Count) return;

        Actor_Item itemToDrop = Items[index];
        Items.RemoveAt(index);

        itemToDrop.transform.SetParent(null); // 부모 해제
        itemToDrop.gameObject.SetActive(true); // 다시 보이게 함
        itemToDrop.SetPhysics(true); // 물리 다시 활성화

        // 플레이어 앞 방향으로 던지기
        itemToDrop.transform.position = transform.position + transform.forward * 1.5f;

        // 인덱스 보정
        if (SelectedItemIndex >= Items.Count) SelectedItemIndex = Mathf.Max(0, Items.Count - 1);

        Debug.Log($"{itemToDrop.ItemName} 버림");
    }
}