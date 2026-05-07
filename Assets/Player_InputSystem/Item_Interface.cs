public interface IItem
{
    string ItemName { get; }

    bool PickUp {  get; }
    bool Drop { get; }
    bool AddToInventory {  get; }
    bool RemoveFromInventory { get; }
    bool Equip {  get; }
    bool UnEquip { get; }

    bool Use {  get; }
    bool StopUse {  get; }

    /*
    void Use();     // 아이템 사용 (공격, 사진 찍기, 불 켜기 등)
    void StopUse(); // 사용 중지 (예: 라이터 끄기)
    void Equip(int itemIndex);   // 장착했을 때 로직
    void UnEquip(); // 해제했을 때 로직

    void Grab();
    void AddItem();
    void Release();
    */
    /*
       float UseValue { get; }       // 아이템 사용 (LMB)
    bool UseStarted { get; }
    bool UseReleased { get; }
    bool ReloadTriggered { get; } // 재장전 (R)
    bool PickTriggered { get; }   // 획득 (F)
    bool DropTriggered { get; }   // 버리기 (G)
    float SelectValue { get; }    // 휠 전환 (Scroll)
    int QuickSlotIndex { get; }   // 숫자키 (1~9) 
    */
}