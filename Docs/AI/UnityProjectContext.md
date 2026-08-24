# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `C:\Users\준이\Desktop\1인\상태창!!\상태창!!`
- Last analyzed: 2026-08-24
- Last analyzed commit: Unverified
- Project purpose: **상태창!!** — 상태창 빌드 설계와 조작 없는 자동전투를 중심으로 한 싱글플레이 증분 게임 프로토타입.

## Confirmed Environment

- Unity version: 6000.3.21f1 (Unity 6.3 LTS)
- Render pipeline: Universal Render Pipeline (Confirmed)
- Input system: Input System package installed; 실제 사용 여부는 아직 없음
- Target platforms: Android/iOS 우선 (Confirmed by `Docs/DevelopmentRoadmap.md`; Android project settings still require device-build verification)

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Rendering | Universal Render Pipeline 17.3.0 | Confirmed | `Packages/manifest.json` |
| UI | uGUI 2.0.0 | Confirmed | `Packages/manifest.json` |
| Input | Input System 1.20.0 | Confirmed | `Packages/manifest.json` |
| Tests | Unity Test Framework 1.6.0 | Confirmed | `Packages/manifest.json` |
| Unity MCP | Official Unity AI Assistant 2.17.0-pre.1, Editor relay connected | Confirmed | `Packages/manifest.json`, Editor log |

## Directory Structure

| Path | Purpose | Confidence |
| --- | --- | --- |
| `Assets/01. Scenes` | 게임 씬 | Confirmed |
| `Assets/02. Scripts` | `01. Core`, `02. Combat`, `03. Progression`, `04. UI` 런타임 코드 | Confirmed |
| `Assets/03. ScriptableObjects` | `01. Combat`, `02. Items`, `03. Progression`, `04. Skills` 콘텐츠 원본 | Confirmed |
| `Assets/04. Prefabs` | `01. Gameplay`, `02. UI` 프리팹 | Confirmed |
| `Assets/05. Art` | `01. Animations`, `02. Materials`, `03. Sprites` | Confirmed |
| `Assets/06. Audio` | `01. Music`, `02. SFX` | Confirmed |
| `Assets/07. UI` | `01. Icons`, `02. UXML`, `03. USS` | Confirmed |
| `Assets/08. Editor` | 에디터 전용 코드 | Confirmed |
| `Assets/09. Tests` | `01. EditMode`, `02. PlayMode` 테스트 | Confirmed |

## Assembly Boundaries

- 현재 별도 `.asmdef`는 확인되지 않았다. 기본 `Assembly-CSharp` 구조로 시작한 새 프로젝트다.
- MVP가 커질 때까지 별도 어셈블리를 만들지 않는다.

## Scenes And Startup Flow

- Build scenes: `Assets/Scenes/SampleScene.unity` (enabled)
- Startup scene: 기본 `SampleScene` (Confirmed)
- Scene loading flow: 아직 구현되지 않음

## Architecture

| Pattern | Finding | Confidence |
| --- | --- | --- |
| 콘텐츠 데이터 | ScriptableObject 원본 + 별도 런타임 상태 | Confirmed |
| 게임 상태 | 단일 런타임 상태 소유자로 구현 예정 | Planned |
| UI | 씬의 uGUI `StatusWindowMobileView`가 주 화면이며, `StatusWindowPrototype`의 IMGUI는 참조 누락 시의 개발용 폴백 | Confirmed |
| 전투 | 입장 시 빌드 잠금, 조작 없는 자동전투 | Confirmed by game design |
| 프로토타입 진입 | `RuntimeInitializeOnLoadMethod`가 `StatusWindowPrototype`을 생성하므로, 씬 수동 연결 없이 SampleScene에서 실행 가능 | Confirmed |

## Coding Conventions

- 기존 사용자 코드가 없는 새 프로젝트이므로, `namespace StatusWindow`와 `[SerializeField] private` 필드 사용을 기본 규칙으로 삼는다.
- 밸런스 값은 코드에 직접 넣지 않고 ScriptableObject로 분리한다.
- 런타임의 골드·레벨·스탯·장착 장비는 ScriptableObject 원본을 수정하지 않는다.

## Testing And Validation

- EditMode tests: `CombatBuildAdvisorTests` 4개 작성됨. 최신 Unity 임포트 뒤 실행 필요.
- PlayMode tests: 패키지 지원, 아직 테스트 없음
- Unity MCP Console/Play Mode: 연결 후 확인 필요

## Current Prototype Slice

- 구현됨: 레벨·경험치·골드·스탯 포인트 구매 및 배분, 스킬 노드 해금, 장비 구매·장착·해제, 빌드 잠금, 3층 시간제한 자동전투, 실패 원인 표시.
- 구현됨: 전투 통계 객체가 가한 피해·받은 피해·기본 공격·치명타·액티브 스킬·처형을 소유하고 UI에 제공한다.
- 구현됨: `GameSaveService`가 PlayerPrefs JSON 저장·불러오기·삭제를 담당하며, 런타임 상태는 `GameSaveData`로 변환된다.
- 구현됨: 저장은 이전 정상 JSON을 보조 슬롯에 유지하고, 주 저장이 손상되면 백업을 복구한다. 완전히 손상된 저장은 삭제하지 않고 신규 진행으로 시작하도록 상태를 UI에 알린다.
- 구현됨: 전투 홈의 고정 주 행동은 대기 시 균열 입장, 성공 후 재도전, 전투 중 중단으로 전환한다. 입장 시 진행도를 저장하며, 중단은 보상을 지급하지 않고 자동 반복도 해제한다.
- 구현됨: 전투 이벤트 타입을 기준으로 층 이동·공략 성공·실패·중단에 좌우 슬롯 반동, 플래시, 결과 팝업과 색상 피드백을 제공한다. 전투 로직은 UI 문자열에 의존하지 않는다.
- 구현됨: ProgressionGoalAdvisor가 권한 있는 게임 상태에서 스탯 투자, 다음 균열 해금, 숙련도, 회귀, 현재 공략 중 한 가지 목표를 선택해 모바일 전투 홈과 결과 오버레이에 제공한다.
- 구현됨: 레벨과 던전 클리어 조건을 만족하면 회귀할 수 있고, 기억의 파편이 공격력·골드 획득 영구 보너스를 제공한다.
- 구현됨: 훈련·심층·재앙 균열은 각각 레벨 1·4·8에서 해금되며, 선택한 던전의 난이도와 보상을 전투에 적용한다.
- 구현됨: 몬스터 유형은 ScriptableObject 데이터로 분리한다. 속공형·체력형·공격형은 층마다 섞여 등장하고, 층 마지막은 보스로 고정한다.
- 구현됨: 전투 종료 시 `CombatBuildAdvisor`가 시간 초과·생존 실패·클리어를 분석해 다음 성장 방향을 반환하며, EditMode 테스트로 핵심 분기를 검증한다.
- 구현됨: 장비는 무기·방어구·장화·반지 슬롯을 지원하며, 새 방어구 슬롯은 저장 DTO의 선택적 `armorId`로 이전 저장과 호환된다.
- 구현됨: 스킬 노드는 기본 5개와 선행 조건을 가진 상위 4개로 구성되어, 화력·이동·생존·액티브 피해의 투자 순서를 만든다.
- 구현됨: 자동 반복 공략은 클리어 시에만 동일 던전을 재시작하고 실패 시 멈춘다. 이 설정은 저장되며 오프라인 보상은 발생하지 않는다.
- 구현됨: `CombatProfile`은 치명타와 5초 주기의 자동 액티브를 포함한 예상 DPS를 제공한다. 장비는 총 8종으로 슬롯별 교체 선택지를 제공한다.
- 구현됨: 회귀 파편은 기존의 기본 공격·골드 영구 보너스를 유지하면서, 계승 특성 3종의 랭크 구매에도 사용된다. 구매 랭크·사용 파편은 저장 데이터에 별도로 보관한다.
- 구현됨: 던전 프로토콜은 입장 전 선택하며 적 체력·피해·제한시간·보상 배율을 동시에 조정한다. 선택은 저장되고 자동 반복 공략에도 유지된다.
- 구현됨: 빌드 재설정은 전투 밖에서만 가능하다. 모든 배분 스탯 포인트를 반환하고 스킬 골드의 70%를 환급하며, 장비와 계승 특성은 보존한다.
- 구현됨: 장비 강화는 보유 장비별 최대 5단계이며, 골드로 강화할수록 해당 장비의 모든 전투 보정치가 증가한다. 장비와 강화 수치는 회귀 시 함께 초기화되고 저장 DTO로 복원된다.
- 구현됨: 에디터 데이터 생성기는 기존 장비 자산의 강화 기본값과 진행도 자산의 치명타 상한을 안전하게 보완한다.
- 구현됨: 업적은 레벨·던전 공략·회귀 조건과 보상 데이터를 ScriptableObject로 보관하며, 수령 상태는 저장 데이터로 유지한다.
- 검증 대기: 저장 복구와 균열 콘텐츠 누락 참조 방어 코드는 .NET 프로젝트 컴파일만 완료했다. Unity EditMode/Play Mode 회귀 테스트가 필요하다.
- 구현됨: `SkillNodeDefinition`, `EquipmentDefinition`, `DungeonDefinition`, `PrototypeCatalog` ScriptableObject 타입과 자산.
- 구현됨: 시작 재화, 스탯 포인트 비용, 스킬 노드, 장비, 던전 난이도는 `Assets/03. ScriptableObjects/00. Prototype`의 자산에서 읽는다.
- 구현됨: `SampleScene`의 `StatusWindowPrototypeBootstrap`이 카탈로그 자산을 직렬화 참조한다. 빌드에서도 에셋 참조가 유지된다.
- 부분 검증: 런타임과 EditMode 테스트 어셈블리는 .NET 정적 컴파일을 통과했다. 현재 Editor.log에는 변경 전 `BuildDungeon`의 null 참조가 남아 있어, 새 코드 임포트 후 Play Mode 재확인이 필요하다.
- 미검증: Play Mode에서의 실제 UI 상호작용과 전투 루프.

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| Unity MCP relay | available | Unity Editor log의 relay client connection |
| Codex Unity tool schema | pending refresh | 현재 작업에서 도구 목록 재로딩 필요 |
| Unity Console read | pending refresh | Codex Unity tool schema 재연결 필요 |
| Scene and prefab mutation | pending refresh | Codex Unity tool schema 재연결 필요 |

## Important Constraints

- 오프라인 보상은 제공한다.
- 전투 중 캐릭터 조작과 빌드 변경은 허용하지 않는다.
- 상태창의 스탯·스킬 노드·장비 편성이 핵심 플레이어 입력이다.
- Android/iOS 우선 세로형 모바일 UI를 지원한다. 실제 Android 기기 검증은 아직 미완료다.

## Unknowns And Confidence

- 정식 UI 전환 방식(uGUI 또는 UI Toolkit)은 미결정이다. 현재 MVP는 IMGUI다.
- 던전 씬의 최종 비주얼 표현은 아직 결정하지 않았다.
- 최신 Unity 임포트와 Play Mode 회귀 테스트는 아직 완료하지 않았다.

## Source Files Inspected

- `ProjectSettings/ProjectVersion.txt`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `Assets/` 디렉터리 구조
- `Assets/02. Scripts/01. Core/StatusWindowGameState.cs`
- `Assets/02. Scripts/01. Core/PrototypeBootstrap.cs`
- `Assets/02. Scripts/01. Core/GameSaveService.cs`
- `Assets/02. Scripts/02. Combat/DungeonRun.cs`
- `Assets/02. Scripts/02. Combat/CombatRunStatistics.cs`
- `Assets/02. Scripts/03. Progression/SkillNodeDefinition.cs`
- `Assets/02. Scripts/03. Progression/EquipmentDefinition.cs`
- `Assets/02. Scripts/03. Progression/DungeonDefinition.cs`
- `Assets/02. Scripts/03. Progression/EnemyDefinition.cs`
- `Assets/02. Scripts/02. Combat/CombatBuildAdvisor.cs`
- `Assets/09. Tests/01. EditMode/CombatBuildAdvisorTests.cs`
- `Assets/02. Scripts/03. Progression/PrototypeCatalog.cs`
- `Assets/02. Scripts/03. Progression/ProgressionDefinition.cs`
- `Assets/02. Scripts/03. Progression/GameSaveData.cs`
- `Assets/02. Scripts/04. UI/StatusWindowPrototype.cs`
- `Assets/08. Editor/PrototypeDataAssetCreator.cs`
- `Assets/03. ScriptableObjects/00. Prototype/StatusWindowPrototypeCatalog.asset`
- Unity Editor relay 로그

<!-- unity-onboarding:generated:end -->
