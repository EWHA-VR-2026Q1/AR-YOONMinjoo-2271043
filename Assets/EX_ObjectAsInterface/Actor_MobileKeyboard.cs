using UnityEngine;

public class Actor_MobileKeyboard : MonoBehaviour
{
    private TouchScreenKeyboard keyboard;
    private string inputText = "";

    private void Start()
    {
        //OpenMobileKeyboard();
    }
    // 버튼 이벤트나 특정 조건에서 호출할 함수
    public void OpenMobileKeyboard()
    {
        // TouchScreenKeyboard.Open(초기값, 키보드타입, 자동수정여부, 멀티라인여부, 비밀번호모드)
        keyboard = TouchScreenKeyboard.Open(inputText, TouchScreenKeyboardType.Default);
    }

    void Update()
    {
        // 키보드가 활성화되어 있고 열려있는 상태인지 확인
        if (keyboard != null && keyboard.active)
        {
            // 사용자가 입력한 텍스트를 실시간으로 가져옴
            inputText = keyboard.text;

            // 사용자가 키보드의 'Done(완료)'을 누르거나 입력을 마쳤을 때
            if (keyboard.status == TouchScreenKeyboard.Status.Done)
            {
                Debug.Log($"입력 완료된 텍스트: {inputText}");
                keyboard = null; // 참조 해제
            }
            // 사용자가 입력을 취소했을 때
            else if (keyboard.status == TouchScreenKeyboard.Status.Canceled)
            {
                Debug.Log("키보드 입력이 취소되었습니다.");
                keyboard = null;
            }
        }
    }
}