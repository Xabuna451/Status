# Android 개발 환경

> 최종 갱신일: 2026-08-23

## 현재 구성

- Unity 6000.3.21f1 Android 모듈, SDK, NDK, OpenJDK 설치 확인
- 배포 식별자(개발용): `com.park963496347.statuswindow`
- 화면: 세로 고정(Portrait), 1080 × 1920 기준
- 아키텍처: ARM64
- 스크립팅 백엔드: IL2CPP
- 최소 Android 버전: API 25
- 대상 SDK: Unity 설치 SDK 자동 선택

## 첫 기기 테스트 절차

1. Android 기기에서 개발자 옵션과 USB 디버깅을 켠다.
2. USB로 연결한 뒤 Unity의 Android Build Profile에서 기기를 선택한다.
3. Development Build로 APK를 빌드·설치한다.
4. 세로 화면, 안전 영역, 첫 실행 저장, 백그라운드 복귀, 오프라인 보상을 확인한다.

## 출시 전 체크

- Google Play Console 등록 후 개발용 패키지명과 서명 키를 확정한다.
- AAB 형식으로 빌드한다.
- 광고·결제 SDK는 테스트용 구현 검증 뒤에 추가한다.
- 출시용 아이콘, 스플래시, 개인정보처리방침, 연령 등급을 준비한다.

## 주의

- Google Play에 한 번 배포한 패키지명은 변경하지 않는다.
- 서명 키와 키 비밀번호는 프로젝트·Git·채팅에 저장하지 않는다.
