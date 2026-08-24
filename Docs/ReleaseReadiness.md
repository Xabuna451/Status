# 상태창!! 출시 준비 현황

## 현재 빌드 대상

- 플랫폼: Android / iOS 세로형 모바일
- 기준 해상도: 1080 x 1920 (9:16)
- 시작 씬: `Assets/Scenes/SampleScene.unity`
- Android 식별자: `com.joonistudio.statuswindow`
- 버전: 0.1.0 (internal alpha)
- Android: IL2CPP, ARM64, 세로 고정, 안전 영역 대응

## 출시 전 통과 기준

1. Android AAB를 실제 기기에 설치해 시작·저장·복귀·오프라인 보상을 확인한다.
2. Google Play Console에서 최종 패키지명, 앱 서명 키, 개인정보처리방침 URL을 등록한다.
3. Android/iOS 앱 아이콘·스플래시 이미지를 최종 아트로 교체한다.
4. 최소 3종의 실제 화면비(19.5:9, 20:9, 태블릿)에서 UI를 점검한다.
5. 크래시 없는 30분 자동전투·반복·중단·저장/재시작 시나리오를 수행한다.

## 현재 상태

- Unity 씬 및 런타임 동작: 검증 완료
- Android PlayerSettings: 출시 기본값 구성 완료
- 실기기 AAB: 아직 미생성
- 앱 서명 키 / 스토어 계정 / 최종 아이콘: 외부 계정 또는 최종 아트 필요

빌드 파일은 소스 폴더 밖의 `Builds/Android/`에 생성한다.
