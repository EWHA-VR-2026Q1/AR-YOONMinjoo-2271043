using UnityEngine;

public interface IMoveAndLook
{
    // 보기 관련
    Vector2 LookInput { get; }

    // 이동 관련
    Vector2 MoveInput { get; }    
    bool SprintInput { get; }
    bool JumpInput { get; }
    // 텔레포트 탐색 중 (T + LMB 누르고 있는 상태)
    bool TeleportAimingInput { get; } 
    // 텔레포트 실행 (T + LMB 떼는 순간)
    bool TeleportExecuteInput { get; }
    bool TeleportCancelInput { get; }
    
    // 회전 관련 (Quest 스냅턴 등)
    float SnapTurnInput { get; }
}