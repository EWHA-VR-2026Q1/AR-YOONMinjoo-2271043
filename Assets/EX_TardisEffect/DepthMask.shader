Shader "Custom/DepthMask" {
    SubShader {
        // 배경(Skybox)과 충돌을 피하기 위해 Geometry-1 설정
        Tags { "Queue"="Geometry-1" "RenderType"="Transparent" }
        
        // 중요: 모든 빛 계산과 그림자 영향을 받지 않음
        Lighting Off
        ZTest LEqual
        ZWrite On
        
        // 중요: RGB와 Alpha 모두 그리지 않음 (0은 색상 출력을 완전히 끔)
        ColorMask 0

        Pass {
            // 아무 내용 없음
        }
    }
}