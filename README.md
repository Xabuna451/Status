# 상태창!! (Status Window)

> **빌드를 구성하고, 조작 없는 자동전투로 균열을 공략하는 모바일 증분 RPG 프로토타입**

`상태창!!`은 캐릭터를 직접 조작하지 않는 대신, 스탯·스킬·장비·전술 지침의 조합으로 전투 결과를 바꾸는 Unity 기반 모바일 RPG입니다. 전투를 관전하고 보상을 다음 빌드에 투자하며, 더 높은 균열에 도전합니다.

> 이 저장소는 현재 활발히 개발 중인 프로토타입입니다. 기능과 화면 구성, 저장 형식은 변경될 수 있습니다.

## 핵심 플레이 루프

```text
빌드 구성 → 균열·전술 선택 → 자동전투 관전 → 보상 획득 → 성장 및 다음 균열 도전
```

- 전투 중에는 빌드가 잠기며, 직접 조작은 없습니다.
- 던전 클리어 시에만 자동 반복이 다시 입장합니다. 실패하면 반복은 멈춥니다.
- 게임을 종료한 시간도 보상으로 정산합니다.

## 구현된 기능

- 5종 기본 스탯, 레벨업 및 골드 기반 스탯 투자
- 스킬 노드 9종과 4개 장착 슬롯
- 장비 4슬롯, 강화, 세트 보너스, 장비 드롭
- 시간 제한 층형 던전, 보스, 적 특성, 위험 프로토콜
- 전술 지침과 균열 분석을 통한 빌드 선택 지원
- 자동전투 로그, 관전 배속, 공략 기록과 균열 숙련도
- 빌드 프리셋, 회귀·계승, 업적, 로컬 저장 및 오프라인 보상
- Unity uGUI 기반 세로형 모바일 UI와 2D 전투 비주얼

## 기술 스택

- Unity **6.3 LTS** (`6000.3.21f1`)
- C#
- Universal Render Pipeline (URP)
- Unity Input System / uGUI
- Unity Test Framework
- 로컬 저장: PlayerPrefs JSON

## 시작하기

### 요구 사항

- Unity Hub
- Unity Editor `6000.3.21f1` (Unity 6.3 LTS)

### 실행 방법

1. Unity Hub에서 이 프로젝트 폴더를 엽니다.
2. `Assets/Scenes/SampleScene.unity`를 엽니다.
3. 컴파일이 끝난 후, 상단 메뉴에서 `StatusWindow > Create Prototype Data`를 한 번 실행합니다.
   - ScriptableObject와 카탈로그 참조를 생성하거나 보완합니다.
4. Play 버튼을 눌러 실행합니다.

## 테스트

Unity Test Runner에서 **EditMode** 테스트를 실행합니다. 주요 검증 범위는 전투 빌드 분석, 던전 진행, 오프라인 보상, 저장 복구, 랭킹 점수 계산, 스킬 장착입니다.

```text
Window → General → Test Runner → EditMode → Run All
```

## 프로젝트 구조

```text
Assets/
├─ 01. Scenes/              # Unity 씬
├─ 02. Scripts/             # 런타임 로직과 UI
├─ 03. ScriptableObjects/   # 전투·성장·장비 콘텐츠 데이터
├─ 04. Prefabs/             # UI와 게임플레이 프리팹
├─ 05. Art/                 # 2D 아트와 애니메이션
├─ 06. Audio/               # 음악과 효과음
├─ 07. UI/                  # UI 리소스
├─ 08. Editor/              # 에디터 도구
└─ 09. Tests/               # EditMode / PlayMode 테스트
Docs/                       # 로드맵, 기획, 출시 준비 문서
```

## 로드맵과 문서

- [개발 로드맵](Docs/DevelopmentRoadmap.md)
- [완료 기준 체크리스트](Docs/GameCompletionChecklist.md)
- [출시 준비 현황](Docs/ReleaseReadiness.md)
- [온라인 랭킹 설계](Docs/OnlineRankingPlan.md)
- [상품 전략](Docs/Production/ProductStrategy.md)

## 기여와 이슈

개선 제안과 버그 제보는 Issue로 남겨 주세요. 기능을 수정하는 PR에는 가능하면 다음을 함께 포함해 주세요.

- 변경 목적과 플레이어 경험에 미치는 영향
- Unity Console 오류 여부와 테스트 결과
- UI 변경 시 9:16 화면 확인 이미지 또는 설명

## 라이선스

현재 라이선스는 아직 정하지 않았습니다. 코드·아트·기획 문서의 재사용 또는 배포 전에는 저장소 소유자에게 문의해 주세요.
