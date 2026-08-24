using System;
using StatusWindow.Combat;
using StatusWindow.Progression;
using UnityEngine;

namespace StatusWindow.UI
{
    public sealed class StatusWindowPrototype : MonoBehaviour
    {
        private enum Tab
        {
            Status,
            Skills,
            Equipment,
            Dungeon,
            Milestones,
            Rebirth,
        }

        private StatusWindowGameState gameState;
        private DungeonRun dungeonRun;
        private Tab selectedTab;
        private Vector2 scrollPosition;
        private GameSaveService saveService;
        private string saveMessage;
        private string presetMessage;
        private bool wasDungeonRunning;
        private readonly DungeonReadinessAnalyzer readinessAnalyzer = new DungeonReadinessAnalyzer();
        private readonly OfflineRewardCalculator offlineRewardCalculator = new OfflineRewardCalculator();
        private readonly ProgressionGoalAdvisor progressionGoalAdvisor = new ProgressionGoalAdvisor();
        private int combatSpeedIndex;
        private bool compactLayout;
        private string displayedCombatEvent;
        private float combatEventPulseEnd;
        private StatusWindowMobileView mobileView;
        private static readonly float[] CombatSpeeds = { 1f, 2f, 4f };
        private static GUIStyle titleStyle;
        private static GUIStyle sectionStyle;
        private static GUIStyle headerStyle;
        private static GUIStyle cyanEventStyle;
        private static GUIStyle criticalEventStyle;
        private static GUIStyle activeEventStyle;
        private static GUIStyle executeEventStyle;
        private static StatusWindowUiTheme uiTheme;

        private void Awake()
        {
            EnsurePortraitOrientation();
        }

        public void Initialize(PrototypeCatalog catalog)
        {
            if (gameState != null || catalog == null)
            {
                return;
            }

            EnsurePortraitOrientation();

            gameState = new StatusWindowGameState(catalog);
            dungeonRun = CreateDungeonRun();
            saveService = new GameSaveService();
            if (saveService.TryLoad(out var saveData) && gameState.TryLoad(saveData))
            {
                var offlineReward = gameState.ClaimOfflineReward(DateTime.UtcNow, offlineRewardCalculator);
                var loadPrefix = saveService.LastLoadStatus == GameSaveLoadStatus.RecoveredBackup
                    ? "최근 저장을 복구했습니다. "
                    : string.Empty;
                saveMessage = offlineReward.HasReward
                    ? $"{loadPrefix}부재 시간 {FormatOfflineDuration(offlineReward.ElapsedSeconds)} · 오프라인 보상 GOLD +{offlineReward.Gold}, EXP +{offlineReward.Experience}"
                    : $"{loadPrefix}저장 데이터를 불러왔습니다.";
                saveService.Save(gameState.CreateSaveData());
            }
            else
            {
                saveMessage = saveService.LastLoadStatus == GameSaveLoadStatus.Invalid
                    ? "저장 데이터를 읽을 수 없어 새 게임을 시작했습니다. 기존 데이터는 보존되어 있습니다."
                    : "새 게임을 시작했습니다.";
            }
            displayedCombatEvent = dungeonRun.LastCombatEvent;
            presetMessage = string.Empty;
            mobileView = FindFirstObjectByType<StatusWindowMobileView>();
            if (mobileView == null)
            {
                Debug.LogError("StatusWindowMobileCanvas prefab is missing from the active scene.");
                return;
            }
            mobileView.Initialize(this);
        }

        internal StatusWindowGameState GameState => gameState;
        internal DungeonRun ActiveDungeonRun => dungeonRun;
        internal Texture2D CurrentEnemyPortrait => gameState == null ? null : GetEnemyPortrait(gameState.Catalog);
        internal string SystemMessage => saveMessage;
        internal ProgressionGoal GetNextProgressionGoal() => progressionGoalAdvisor.Create(gameState);
        internal DungeonReadinessReport AnalyzeSelectedDungeon()
        {
            return gameState == null
                ? readinessAnalyzer.Analyze(null, null, default)
                : readinessAnalyzer.Analyze(gameState.SelectedDungeon, gameState.SelectedProtocol, gameState.CreateCombatProfile());
        }

        internal bool TryStartDungeon()
        {
            if (gameState == null || dungeonRun == null || dungeonRun.IsRunning) return false;
            dungeonRun = CreateDungeonRun();
            dungeonRun.Start(gameState);
            wasDungeonRunning = true;
            saveMessage = "균열에 진입했습니다. 자동전투 결과를 확인하세요.";
            SaveGame();
            return true;
        }

        internal bool TryCancelDungeon()
        {
            if (dungeonRun == null || !dungeonRun.Cancel()) return false;

            gameState.SetAutoRepeatDungeon(false);
            wasDungeonRunning = false;
            saveMessage = "자동전투를 중단했습니다. 자동 반복도 꺼졌습니다.";
            SaveGame();
            return true;
        }

        internal void SaveProgress()
        {
            SaveGame();
        }

        private void Update()
        {
            if (dungeonRun == null) return;
            dungeonRun.Tick(Time.deltaTime * CombatSpeeds[combatSpeedIndex]);
            if (displayedCombatEvent != dungeonRun.LastCombatEvent)
            {
                displayedCombatEvent = dungeonRun.LastCombatEvent;
                combatEventPulseEnd = Time.unscaledTime + 0.38f;
            }
            if (wasDungeonRunning && !dungeonRun.IsRunning)
            {
                SaveGame();
                TryStartAutoRepeat();
            }
            wasDungeonRunning = dungeonRun.IsRunning;
        }

        private void TryStartAutoRepeat()
        {
            if (!gameState.AutoRepeatDungeon || dungeonRun.Result != DungeonResult.Cleared) return;

            dungeonRun = CreateDungeonRun();
            dungeonRun.Start(gameState);
            saveMessage = "자동 반복 공략을 계속합니다.";
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveGame();
        }

        private void OnGUI()
        {
            if (mobileView != null) return;
            if (gameState == null)
            {
                return;
            }

            var originalButtonStyle = GUI.skin.button;
            var originalBoxStyle = GUI.skin.box;
            var originalMatrix = GUI.matrix;
            GUI.skin.button = UiTheme().Button;
            GUI.skin.box = UiTheme().Card;
            compactLayout = Screen.height > Screen.width;
            var safeArea = Screen.safeArea;
            var mobileScale = compactLayout ? Mathf.Max(1f, safeArea.width / 390f) : 1f;
            if (compactLayout)
            {
                GUI.matrix = Matrix4x4.TRS(new Vector3(safeArea.x, safeArea.y, 0f), Quaternion.identity, new Vector3(mobileScale, mobileScale, 1f));
            }

            var availableWidth = compactLayout ? safeArea.width / mobileScale : Screen.width;
            var availableHeight = compactLayout ? safeArea.height / mobileScale : Screen.height;
            var horizontalMargin = compactLayout ? 10f : 20f;
            var verticalMargin = compactLayout ? 10f : 20f;
            var outerWidth = compactLayout ? availableWidth - horizontalMargin * 2f : Mathf.Min(1060f, availableWidth - horizontalMargin * 2f);
            var outerHeight = compactLayout ? availableHeight - verticalMargin * 2f : Mathf.Min(720f, availableHeight - verticalMargin * 2f);
            var left = compactLayout ? horizontalMargin : (availableWidth - outerWidth) * 0.5f;
            var top = compactLayout ? verticalMargin : (availableHeight - outerHeight) * 0.5f;

            GUI.Box(new Rect(left, top, outerWidth, outerHeight), GUIContent.none, UiTheme().Panel);
            GUILayout.BeginArea(new Rect(left + 16f, top + 16f, outerWidth - 32f, outerHeight - 32f));
            DrawHeader();
            DrawResourceStrip();
            if (compactLayout) DrawMobileCombatSnapshot();
            DrawTabs();
            GUILayout.Space(8f);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            switch (selectedTab)
            {
                case Tab.Status:
                    DrawStatusTab();
                    break;
                case Tab.Skills:
                    DrawSkillsTab();
                    break;
                case Tab.Equipment:
                    DrawEquipmentTab();
                    break;
                case Tab.Dungeon:
                    DrawDungeonTab();
                    break;
                case Tab.Milestones:
                    DrawMilestonesTab();
                    break;
                case Tab.Rebirth:
                    DrawRebirthTab();
                    break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.skin.button = originalButtonStyle;
            GUI.skin.box = originalBoxStyle;
            GUI.matrix = originalMatrix;
        }

        private static void EnsurePortraitOrientation()
        {
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
            if (Screen.height > Screen.width || Screen.orientation == ScreenOrientation.PortraitUpsideDown)
            {
                Screen.orientation = ScreenOrientation.Portrait;
            }
#endif
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("상태창!!", TitleStyle());
            GUILayout.FlexibleSpace();
            GUILayout.Label(compactLayout
                ? $"Lv.{gameState.Level}  G {gameState.Gold}"
                : $"Lv. {gameState.Level}   EXP {gameState.Experience}/{gameState.ExperienceToNextLevel}   GOLD {gameState.Gold}", HeaderStyle());
            GUILayout.EndHorizontal();
            GUILayout.Label(dungeonRun.IsRunning ? "던전 진행 중 — 현재 빌드는 잠겨 있습니다." : "상태창에서 빌드를 구성한 뒤 던전에 입장하세요.", HeaderStyle());
        }

        private void DrawResourceStrip()
        {
            GUILayout.BeginHorizontal(UiTheme().Card, GUILayout.MinHeight(compactLayout ? 36f : 30f));
            GUILayout.Label($"GOLD  {gameState.Gold:N0}", HeaderStyle(), GUILayout.ExpandWidth(true));
            GUILayout.Label($"STAT  {gameState.UnspentStatPoints}", HeaderStyle(), GUILayout.ExpandWidth(true));
            GUILayout.Label($"MEMORY  {gameState.AvailableLegacyShards}", HeaderStyle(), GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            if (compactLayout)
            {
                GUILayout.BeginHorizontal();
                DrawTabButton(Tab.Status, "성장");
                DrawTabButton(Tab.Skills, "스킬");
                DrawTabButton(Tab.Equipment, "장비");
                DrawTabButton(Tab.Dungeon, "균열");
                DrawTabButton(Tab.Milestones, "업적");
                DrawTabButton(Tab.Rebirth, "환생");
                GUILayout.EndHorizontal();
                return;
            }

            GUILayout.BeginHorizontal();
            DrawTabButton(Tab.Status, "스탯");
            DrawTabButton(Tab.Skills, "스킬 노드");
            DrawTabButton(Tab.Equipment, "장비");
            DrawTabButton(Tab.Dungeon, "던전");
            DrawTabButton(Tab.Milestones, "업적");
            DrawTabButton(Tab.Rebirth, "회귀");
            GUILayout.EndHorizontal();
        }

        private void DrawTabButton(Tab tab, string label)
        {
            var style = selectedTab == tab ? UiTheme().SelectedButton : UiTheme().Button;
            if (GUILayout.Button(label, style, GUILayout.Height(compactLayout ? 46f : 34f)))
            {
                selectedTab = tab;
            }
        }

        private void DrawMobileCombatSnapshot()
        {
            DrawDungeonVisuals();
            GUILayout.BeginHorizontal(UiTheme().Card, GUILayout.Height(42f));
            if (dungeonRun.IsRunning)
            {
                GUILayout.Label($"층 {dungeonRun.Floor} · 처치 {dungeonRun.Kills}/{dungeonRun.KillTarget} · {Mathf.Max(0f, dungeonRun.TimeRemaining):0}초", HeaderStyle());
                GUILayout.FlexibleSpace();
                GUILayout.Label($"AUTO x{CombatSpeeds[combatSpeedIndex]:0}", HeaderStyle());
            }
            else
            {
                GUILayout.Label("균열을 선택하고 빌드를 잠그면 자동전투가 시작됩니다.", HeaderStyle());
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("균열 준비", UiTheme().SelectedButton, GUILayout.Width(116f), GUILayout.Height(30f))) selectedTab = Tab.Dungeon;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawStatusTab()
        {
            DrawHunterIdentityCard();
            GUILayout.Label("기본 스탯", SectionStyle());
            GUILayout.Label($"남은 스탯 포인트: {gameState.UnspentStatPoints}");
            GUI.enabled = !dungeonRun.IsRunning;
            if (GUILayout.Button($"골드 {gameState.Catalog.StatPointGoldCost}로 스탯 포인트 +1", GUILayout.Height(30f)))
            {
                if (gameState.TryBuyStatPoint()) SaveGame();
            }

            DrawStat(StatType.Strength, "근력", "물리 공격력과 최대 체력");
            DrawStat(StatType.Agility, "민첩", "공격 속도와 다음 적까지의 이동 시간");
            DrawStat(StatType.Magic, "마력", "5초마다 사용하는 자동 액티브 스킬 피해");
            DrawStat(StatType.Sense, "감각", "치명타 확률");
            DrawStat(StatType.Will, "의지", "방어력과 최대 체력");
            GUILayout.Space(6f);
            if (GUILayout.Button($"빌드 재설정 — 스탯 포인트 반환 / 스킬 골드 {gameState.BuildResetGoldRefund}G 환급", GUILayout.Height(30f)))
            {
                if (gameState.TryResetBuild()) SaveGame();
            }
            DrawBuildPresets();
            GUI.enabled = true;

            var profile = gameState.CreateCombatProfile();
            GUILayout.Space(12f);
            GUILayout.Label("현재 전투 예상치", SectionStyle());
            GUILayout.Label($"기본 공격 {profile.Damage} / 공격 간격 {profile.AttackInterval:0.00}초 / 최대 체력 {profile.MaxHealth} / 방어 {profile.Defense}");
            GUILayout.Label($"이동 지연 {profile.MoveDelay:0.00}초 / 액티브 피해 {profile.ActiveDamage} / 치명타 {profile.CriticalChance:P0}");
            GUILayout.Label($"예상 전투 화력 {profile.ExpectedDamagePerSecond:0.0} DPS (치명타·자동 액티브 포함)", HeaderStyle());
            GUILayout.Label($"균열 숙련도 {gameState.TotalDungeonMasteryRank} / 영구 공격력 +{gameState.TotalDungeonMasteryDamageBonus:P0}");
            GUILayout.Label($"선택 전술 지침: {gameState.SelectedCombatDirective?.DisplayName ?? "없음"}");
            GUILayout.Space(12f);
            GUILayout.Label("다음 목표", SectionStyle());
            DrawGoals();
        }

        private void DrawHunterIdentityCard()
        {
            var profile = gameState.CreateCombatProfile();
            GUILayout.BeginHorizontal(UiTheme().Card, GUILayout.MinHeight(compactLayout ? 92f : 112f));
            if (gameState.Catalog.HunterPortrait != null)
            {
                GUILayout.Box(gameState.Catalog.HunterPortrait, GUILayout.Width(compactLayout ? 68f : 88f), GUILayout.Height(compactLayout ? 82f : 102f));
            }

            GUILayout.BeginVertical();
            GUILayout.Label("상태창 사용자", SectionStyle());
            GUILayout.Label($"전투력 {profile.ExpectedDamagePerSecond:0.0} DPS  ·  생존력 {profile.MaxHealth} HP", HeaderStyle());
            GUILayout.Label(gameState.SelectedDungeon == null
                ? "다음 균열을 선택하세요."
                : $"현재 목표: {gameState.SelectedDungeon.DisplayName} · 권장 Lv. {gameState.SelectedDungeon.RequiredLevel}");
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
        }

        private void DrawGoals()
        {
            var selectedDungeon = gameState.SelectedDungeon;
            GUILayout.Label($"• {selectedDungeon.DisplayName} 공략: 권장 레벨 {selectedDungeon.RequiredLevel}, 현재 Lv. {gameState.Level}");
            if (gameState.UnspentStatPoints > 0)
            {
                GUILayout.Label($"• 남은 스탯 포인트 {gameState.UnspentStatPoints}개를 배분해 빌드를 완성하세요.");
            }
            else
            {
                GUILayout.Label($"• 골드 {gameState.Catalog.StatPointGoldCost}를 모아 다음 스탯 포인트를 구매하세요.");
            }

            var progression = gameState.Catalog.Progression;
            if (!gameState.CanRebirth)
            {
                GUILayout.Label($"• 첫 회귀: Lv. {progression.RebirthRequiredLevel}, 던전 {progression.RebirthRequiredClears}회 클리어가 필요합니다.");
            }
            else
            {
                GUILayout.Label("• 회귀가 가능합니다. 기억의 파편으로 다음 생의 성장 효율을 올리세요.");
            }

            foreach (var milestone in gameState.Catalog.Milestones)
            {
                if (!gameState.IsMilestoneComplete(milestone) || gameState.HasClaimedMilestone(milestone)) continue;
                GUILayout.Label($"• 업적 보상 수령 가능: {milestone.DisplayName} — {milestone.GoldReward}G / 스탯 포인트 {milestone.StatPointReward}");
                break;
            }
        }

        private void DrawStat(StatType statType, string displayName, string description)
        {
            GUILayout.BeginHorizontal(UiTheme().Card);
            GUILayout.Label($"{displayName}  {gameState.GetStat(statType)}", GUILayout.Width(160f));
            GUILayout.Label(description, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("+1", UiTheme().Button, GUILayout.Width(60f), GUILayout.Height(26f)))
            {
                if (gameState.TrySpendStatPoint(statType)) SaveGame();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawBuildPresets()
        {
            var canEdit = GUI.enabled;
            GUILayout.Space(10f);
            GUILayout.Label("빌드 프리셋", SectionStyle());
            GUILayout.Label("스탯·장착 스킬·장착 장비를 저장합니다. 현재 보유한 장비와 해금 스킬이 모두 있어야 불러올 수 있습니다.");
            for (var slot = 0; slot < gameState.BuildPresetCount; slot++)
            {
                GUILayout.BeginHorizontal("box");
                GUILayout.Label($"프리셋 {slot + 1}: {(gameState.HasBuildPreset(slot) ? "저장됨" : "비어 있음")}", GUILayout.Width(180f));
                if (GUILayout.Button("현재 빌드 저장", GUILayout.Width(120f)))
                {
                    if (gameState.SaveBuildPreset(slot))
                    {
                        SaveGame();
                        presetMessage = $"프리셋 {slot + 1}에 현재 빌드를 저장했습니다.";
                    }
                }

                GUI.enabled = canEdit && gameState.HasBuildPreset(slot);
                if (GUILayout.Button("불러오기", GUILayout.Width(90f)))
                {
                    if (gameState.TryApplyBuildPreset(slot))
                    {
                        SaveGame();
                        presetMessage = $"프리셋 {slot + 1}을 적용했습니다.";
                    }
                    else
                    {
                        presetMessage = "현재 스탯 포인트가 부족하거나, 저장된 장비·스킬을 보유하지 않아 적용할 수 없습니다.";
                    }
                }
                GUI.enabled = canEdit;
                GUILayout.EndHorizontal();
            }
            if (!string.IsNullOrEmpty(presetMessage)) GUILayout.Label(presetMessage);
        }

        private void DrawSkillsTab()
        {
            GUILayout.Label("스킬 노드", SectionStyle());
            GUILayout.Label($"해금에는 골드가 필요합니다. 장착 {gameState.EquippedSkillCount}/{gameState.MaximumEquippedSkillCount}. 전투 중에는 변경할 수 없습니다.");
            GUI.enabled = !dungeonRun.IsRunning;
            foreach (var skill in gameState.Catalog.SkillNodes)
            {
                DrawSkill(skill);
            }
            GUI.enabled = true;
        }

        private void DrawSkill(SkillNodeDefinition skill)
        {
            var unlocked = gameState.HasSkill(skill);
            var equipped = gameState.IsSkillEquipped(skill);
            var prerequisiteMet = skill.Prerequisite == null || gameState.HasSkill(skill.Prerequisite);
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(unlocked ? (equipped ? "장착" : "해금") : "잠김", GUILayout.Width(60f));
            GUILayout.BeginVertical();
            GUILayout.Label(skill.DisplayName, HeaderStyle());
            GUILayout.Label(skill.Description);
            if (!prerequisiteMet) GUILayout.Label("선행 노드가 필요합니다.");
            GUILayout.EndVertical();
            GUI.enabled = GUI.enabled && !unlocked && prerequisiteMet;
            if (!unlocked && GUILayout.Button($"해금 {skill.GoldCost} G", GUILayout.Width(110f), GUILayout.Height(34f)))
            {
                if (gameState.TryUnlockSkill(skill)) SaveGame();
            }

            GUI.enabled = !dungeonRun.IsRunning && unlocked && (equipped || gameState.EquippedSkillCount < gameState.MaximumEquippedSkillCount);
            if (unlocked && GUILayout.Button(equipped ? "해제" : "장착", GUILayout.Width(80f), GUILayout.Height(34f)))
            {
                if (gameState.TryToggleSkill(skill)) SaveGame();
            }

            GUI.enabled = !dungeonRun.IsRunning;
            GUILayout.EndHorizontal();
        }

        private void DrawEquipmentTab()
        {
            GUILayout.Label("장비", SectionStyle());
            GUILayout.Label("던전 보상으로 장비를 획득하거나 골드로 구매하세요. 보유한 장비는 이후 무료로 다시 장착할 수 있습니다.");
            DrawEquippedLoadout();
            GUI.enabled = !dungeonRun.IsRunning;
            foreach (var equipment in gameState.Catalog.Equipment)
            {
                DrawEquipment(equipment);
            }
            GUILayout.Space(10f);
            GUILayout.Label("장비 세트", SectionStyle());
            foreach (var equipmentSet in gameState.Catalog.EquipmentSets)
            {
                var active = gameState.IsEquipmentSetActive(equipmentSet);
                GUILayout.BeginVertical("box");
                GUILayout.Label($"{equipmentSet.DisplayName} {(active ? "[발동 중]" : "[미발동]")}", HeaderStyle());
                GUILayout.Label(equipmentSet.Description);
                GUILayout.EndVertical();
            }
            GUI.enabled = true;
        }

        private void DrawEquipment(EquipmentDefinition equipment)
        {
            var equipped = gameState.GetEquipped(equipment.Slot) == equipment;
            var owned = gameState.HasEquipment(equipment);
            GUILayout.BeginHorizontal("box");
            DrawEquipmentIcon(equipment, compactLayout ? 54f : 66f);
            GUILayout.BeginVertical();
            GUILayout.Label($"{equipment.DisplayName} +{gameState.GetEquipmentUpgradeLevel(equipment)} {(equipped ? "[장착 중]" : string.Empty)}", HeaderStyle());
            GUILayout.Label(equipment.Description);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            if (equipped)
            {
                if (GUILayout.Button("해제", GUILayout.Width(80f), GUILayout.Height(34f)))
                {
                    gameState.Unequip(equipment.Slot);
                    SaveGame();
                }
            }
            else if (GUILayout.Button(owned ? "장착" : $"구매 {equipment.GoldCost} G", GUILayout.Width(100f), GUILayout.Height(34f)))
            {
                if (gameState.TryEquip(equipment)) SaveGame();
            }

            var upgradeLevel = gameState.GetEquipmentUpgradeLevel(equipment);
            if (owned && upgradeLevel < equipment.MaximumUpgradeLevel)
            {
                if (GUILayout.Button($"강화 {equipment.GetUpgradeGoldCost(upgradeLevel)} G", GUILayout.Width(110f), GUILayout.Height(34f)))
                {
                    if (gameState.TryUpgradeEquipment(equipment)) SaveGame();
                }
            }

            GUILayout.EndHorizontal();
        }

        private void DrawEquippedLoadout()
        {
            GUILayout.BeginHorizontal(UiTheme().Card);
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                var equipment = gameState.GetEquipped(slot);
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                GUILayout.Label(GetEquipmentSlotName(slot), HeaderStyle());
                if (equipment == null)
                {
                    GUILayout.Box("비어 있음", GUILayout.Width(compactLayout ? 54f : 66f), GUILayout.Height(compactLayout ? 54f : 66f));
                }
                else
                {
                    DrawEquipmentIcon(equipment, compactLayout ? 54f : 66f);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
        }

        private void DrawEquipmentIcon(EquipmentDefinition equipment, float size)
        {
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            var rarityColor = GetEquipmentRarityColor(equipment.GoldCost);
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, rarityColor, 0f, 0f);
            var innerRect = new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f);
            var iconSheet = gameState.Catalog.EquipmentIconSheet;
            if (iconSheet == null)
            {
                GUI.Label(innerRect, GetEquipmentSlotName(equipment.Slot), HeaderStyle());
                return;
            }

            GUI.DrawTextureWithTexCoords(innerRect, iconSheet, GetEquipmentIconUv(equipment.Id), true);
        }

        private static Rect GetEquipmentIconUv(string equipmentId)
        {
            switch (equipmentId)
            {
                case "training_sword": return new Rect(0f, 0.66f, 0.25f, 0.34f);
                case "mana_staff": return new Rect(0.25f, 0.66f, 0.25f, 0.34f);
                case "plasma_blade": return new Rect(0f, 0f, 0.25f, 0.34f);
                case "reinforced_coat": return new Rect(0.5f, 0.66f, 0.25f, 0.34f);
                case "barrier_jacket": return new Rect(0.75f, 0.66f, 0.25f, 0.34f);
                case "phantom_coat": return new Rect(0.25f, 0f, 0.25f, 0.34f);
                case "swift_boots": return new Rect(0f, 0.33f, 0.25f, 0.33f);
                case "assault_boots": return new Rect(0.25f, 0.33f, 0.25f, 0.33f);
                case "severance_boots": return new Rect(0.5f, 0f, 0.25f, 0.34f);
                case "focus_ring": return new Rect(0.5f, 0.33f, 0.25f, 0.33f);
                case "vitality_ring": return new Rect(0.75f, 0.33f, 0.25f, 0.33f);
                case "void_ring": return new Rect(0.75f, 0f, 0.25f, 0.34f);
                default: return new Rect(0f, 0f, 0.25f, 0.34f);
            }
        }

        private static string GetEquipmentSlotName(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon: return "무기";
                case EquipmentSlot.Armor: return "방어구";
                case EquipmentSlot.Boots: return "장화";
                default: return "반지";
            }
        }

        private static Color GetEquipmentRarityColor(int goldCost)
        {
            if (goldCost >= 150) return new Color(0.96f, 0.35f, 0.93f, 0.95f);
            if (goldCost >= 110) return new Color(0.67f, 0.36f, 1f, 0.95f);
            if (goldCost >= 80) return new Color(0.20f, 0.70f, 1f, 0.95f);
            return new Color(0.22f, 0.90f, 0.72f, 0.9f);
        }

        private void DrawDungeonTab()
        {
            GUILayout.Label("균열 던전", SectionStyle());
            GUILayout.Label("각 층의 처치 목표를 제한시간 안에 달성하세요. 캐릭터는 표적 탐색·이동·공격·스킬 사용을 모두 자동으로 수행합니다.");
            DrawCombatSpeedControls();
            if (!compactLayout) DrawDungeonVisuals();
            GUILayout.Space(8f);
            GUILayout.Label("던전 선택", HeaderStyle());
            GUI.enabled = !dungeonRun.IsRunning;
            for (var index = 0; index < gameState.Catalog.Dungeons.Count; index++)
            {
                DrawDungeonChoice(index, gameState.Catalog.Dungeons[index]);
            }

            GUI.enabled = true;
            GUILayout.Space(8f);

            GUILayout.Label("위험 프로토콜", HeaderStyle());
            GUI.enabled = !dungeonRun.IsRunning;
            for (var index = 0; index < gameState.Catalog.DungeonProtocols.Count; index++)
            {
                DrawProtocolChoice(index, gameState.Catalog.DungeonProtocols[index]);
            }

            GUI.enabled = true;
            GUILayout.Space(8f);

            GUILayout.Label("전술 지침", HeaderStyle());
            GUILayout.Label("입장 전 자동전투의 우선 성향을 지정합니다. 전투 중에는 변경할 수 없습니다.");
            GUI.enabled = !dungeonRun.IsRunning;
            for (var index = 0; index < gameState.Catalog.CombatDirectives.Count; index++)
            {
                DrawCombatDirectiveChoice(index, gameState.Catalog.CombatDirectives[index]);
            }
            GUI.enabled = true;
            GUILayout.Space(8f);

            DrawDungeonReadiness();

            GUI.enabled = !dungeonRun.IsRunning;
            var autoRepeat = GUILayout.Toggle(gameState.AutoRepeatDungeon, "자동 반복 공략: 클리어하면 같은 던전에 즉시 재입장하고, 실패하면 멈춥니다.");
            if (autoRepeat != gameState.AutoRepeatDungeon)
            {
                gameState.SetAutoRepeatDungeon(autoRepeat);
                SaveGame();
            }

            GUI.enabled = true;

            if (!dungeonRun.IsRunning)
            {
                if (GUILayout.Button("던전 입장 — 현재 빌드 잠금", GUILayout.Height(42f)))
                {
                    dungeonRun = CreateDungeonRun();
                    dungeonRun.Start(gameState);
                    wasDungeonRunning = true;
                }
            }

            GUILayout.BeginVertical("box");
            GUILayout.Label($"결과: {dungeonRun.ResultMessage}", HeaderStyle());
            if (!string.IsNullOrEmpty(dungeonRun.EquipmentRewardName)) GUILayout.Label($"획득 장비: {dungeonRun.EquipmentRewardName}");
            GUILayout.Label($"적용 프로토콜: {dungeonRun.ProtocolName}");
            GUILayout.Label(dungeonRun.Recommendation);
            GUILayout.Label($"층 {dungeonRun.Floor} / 남은 시간 {Mathf.Max(0f, dungeonRun.TimeRemaining):0.0}초 / 처치 {dungeonRun.Kills}/{dungeonRun.KillTarget}");
            GUILayout.Label($"이번 공략 경과 시간 {dungeonRun.ElapsedTime:0.0}초 / 현재 선택 던전 최고 기록 {FormatRecordTime(gameState.GetDungeonBestClearSeconds(gameState.SelectedDungeon))}");
            DrawMeter("균열 제한시간", dungeonRun.TimeRemaining, dungeonRun.CurrentFloorTimeLimit, new Color(0.38f, 0.82f, 1f));
            if (dungeonRun.IsRunning)
            {
                GUILayout.Label($"캐릭터 체력 {Mathf.Max(0, dungeonRun.PlayerHealth)}/{dungeonRun.PlayerMaxHealth}");
                DrawMeter("헌터 생존력", dungeonRun.PlayerHealth, dungeonRun.PlayerMaxHealth, new Color(0.22f, 0.92f, 0.62f));
                GUILayout.Label($"현재 몬스터: {dungeonRun.CurrentEnemyName} — {dungeonRun.CurrentEnemyDescription}");
                GUILayout.Label($"전투 특성: {dungeonRun.CurrentEnemyTrait}");
                GUILayout.Label($"현재 몬스터 체력 {Mathf.Max(0, dungeonRun.EnemyHealth)}/{dungeonRun.EnemyMaxHealth}");
                if (dungeonRun.EnemyBarrier > 0) GUILayout.Label($"균열 장벽 {dungeonRun.EnemyBarrier}");
                DrawMeter("표적 안정도", dungeonRun.EnemyHealth, dungeonRun.EnemyMaxHealth, new Color(0.86f, 0.32f, 0.52f));
            }
            GUILayout.Label($"전투 로그: {dungeonRun.LastCombatEvent}", HeaderStyle());

            var statistics = dungeonRun.Statistics;
            GUILayout.Space(8f);
            GUILayout.Label("전투 통계", SectionStyle());
            GUILayout.Label($"가한 피해 {statistics.DamageDealt} / 받은 피해 {statistics.DamageTaken}");
            GUILayout.Label($"기본 공격 {statistics.BasicAttackCount}회 / 치명타 {statistics.CriticalHitCount}회 / 액티브 스킬 {statistics.ActiveSkillCastCount}회 / 처형 {statistics.ExecuteCount}회");

            GUILayout.EndVertical();
        }

        private static void DrawMeter(string label, float currentValue, float maximumValue, Color fillColor)
        {
            var rect = GUILayoutUtility.GetRect(1f, 17f, GUILayout.ExpandWidth(true));
            var fraction = maximumValue <= 0f ? 0f : Mathf.Clamp01(currentValue / maximumValue);
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.04f, 0.06f, 0.10f, 1f), 0f, 0f);
            var fillRect = new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, (rect.width - 4f) * fraction), rect.height - 4f);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, fillColor, 0f, 0f);
            GUI.Label(rect, $"  {label}  {Mathf.Max(0f, currentValue):0}/{maximumValue:0}", HeaderStyle());
        }

        private void DrawDungeonVisuals()
        {
            var catalog = gameState.Catalog;
            var arenaHeight = compactLayout ? 250f : 310f;
            var arenaRect = GUILayoutUtility.GetRect(1f, arenaHeight, GUILayout.ExpandWidth(true));
            if (catalog.DungeonBackdrop != null) GUI.DrawTexture(arenaRect, catalog.DungeonBackdrop, ScaleMode.ScaleAndCrop);
            else GUI.DrawTexture(arenaRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.04f, 0.06f, 0.1f), 0f, 0f);
            var enemyPortrait = GetEnemyPortrait(catalog);
            if (enemyPortrait == null) enemyPortrait = catalog.RiftWatcherPortrait;
            var heroRect = new Rect(arenaRect.x + 20f, arenaRect.y + 38f, arenaRect.width * 0.34f, arenaRect.height - 58f);
            var enemyRect = new Rect(arenaRect.xMax - arenaRect.width * 0.34f - 20f, arenaRect.y + 38f, arenaRect.width * 0.34f, arenaRect.height - 58f);
            var eventPulse = Mathf.Clamp01((combatEventPulseEnd - Time.unscaledTime) / 0.38f);
            var actionProgress = 1f - eventPulse;
            var actionArc = Mathf.Sin(actionProgress * Mathf.PI) * eventPulse;
            var isHeroAttack = IsHeroAttackEvent(displayedCombatEvent);
            var isEnemyAttack = IsEnemyAttackEvent(displayedCombatEvent);
            var heroIdle = Mathf.Sin(Time.unscaledTime * 2.3f) * 3f;
            var enemyIdle = Mathf.Sin(Time.unscaledTime * 1.9f + 1.4f) * 3f;
            heroRect.y += heroIdle;
            enemyRect.y += enemyIdle;
            if (isHeroAttack) heroRect.x += actionArc * arenaRect.width * 0.10f;
            if (isEnemyAttack) enemyRect.x -= actionArc * arenaRect.width * 0.08f;
            if (isHeroAttack) enemyRect = ScaleRect(enemyRect, 1f + actionArc * 0.10f);
            if (isEnemyAttack) heroRect = ScaleRect(heroRect, 1f + actionArc * 0.07f);
            if (catalog.HunterPortrait != null) GUI.DrawTexture(heroRect, catalog.HunterPortrait, ScaleMode.ScaleToFit, true, 0f, Color.white, 0f, 0f);
            if (enemyPortrait != null) GUI.DrawTexture(enemyRect, enemyPortrait, ScaleMode.ScaleToFit, true, 0f, Color.white, 0f, 0f);

            var pulse = 0.45f + Mathf.PingPong(Time.time * 1.8f, 0.4f) + eventPulse * 0.35f;
            var bridgeRect = new Rect(arenaRect.center.x - 48f, arenaRect.center.y - 2f, 96f, 4f);
            var eventColor = GetCombatEventColor(displayedCombatEvent);
            GUI.DrawTexture(bridgeRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(eventColor.r, eventColor.g, eventColor.b, pulse), 0f, 0f);
            DrawCombatProjectile(heroRect, enemyRect, actionProgress, eventPulse, isHeroAttack, isEnemyAttack, eventColor);
            if (eventPulse > 0f)
            {
                var impactRect = new Rect(arenaRect.center.x - 3f - eventPulse * 46f, arenaRect.center.y - 3f - eventPulse * 46f, 6f + eventPulse * 92f, 6f + eventPulse * 92f);
                GUI.DrawTexture(impactRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(eventColor.r, eventColor.g, eventColor.b, eventPulse * 0.32f), 0f, 0f);
                GUI.Label(new Rect(arenaRect.center.x - 120f, arenaRect.center.y - 28f, 240f, 24f), GetCombatEventLabel(displayedCombatEvent), CombatEventStyle(eventColor));
            }
            GUI.Label(new Rect(arenaRect.x + 14f, arenaRect.y + 10f, 180f, 24f), "HUNTER // AUTO COMBAT", HeaderStyle());
            GUI.Label(new Rect(arenaRect.xMax - 185f, arenaRect.y + 10f, 170f, 24f), dungeonRun.IsRunning ? dungeonRun.CurrentEnemyName : "RIFT ENTITY", HeaderStyle());
            DrawArenaMeter(new Rect(arenaRect.x + 14f, arenaRect.yMax - 25f, arenaRect.width * 0.38f, 12f), dungeonRun.PlayerHealth, dungeonRun.PlayerMaxHealth, new Color(0.24f, 0.92f, 0.65f));
            DrawArenaMeter(new Rect(arenaRect.xMax - arenaRect.width * 0.38f - 14f, arenaRect.yMax - 25f, arenaRect.width * 0.38f, 12f), dungeonRun.EnemyHealth + dungeonRun.EnemyBarrier, dungeonRun.EnemyMaxHealth + Mathf.CeilToInt(dungeonRun.EnemyMaxHealth * 0.2f), new Color(0.94f, 0.32f, 0.52f));
        }

        private static void DrawCombatProjectile(Rect heroRect, Rect enemyRect, float actionProgress, float eventPulse, bool isHeroAttack, bool isEnemyAttack, Color color)
        {
            if (eventPulse <= 0f || (!isHeroAttack && !isEnemyAttack)) return;
            var source = isHeroAttack ? heroRect.center : enemyRect.center;
            var target = isHeroAttack ? enemyRect.center : heroRect.center;
            var projectilePosition = Vector2.Lerp(source, target, actionProgress);
            var projectileSize = 10f + eventPulse * 18f;
            var projectileRect = new Rect(projectilePosition.x - projectileSize * 0.5f, projectilePosition.y - projectileSize * 0.5f, projectileSize, projectileSize);
            GUI.DrawTexture(projectileRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(color.r, color.g, color.b, eventPulse), 0f, 0f);
        }

        private static bool IsHeroAttackEvent(string combatEvent)
        {
            return combatEvent != null && (combatEvent.Contains("기본 공격") || combatEvent.Contains("치명타") || combatEvent.Contains("자동 액티브") || combatEvent.Contains("처형"));
        }

        private static bool IsEnemyAttackEvent(string combatEvent)
        {
            return combatEvent != null && combatEvent.Contains("의 공격!");
        }

        private static Rect ScaleRect(Rect rect, float scale)
        {
            var width = rect.width * scale;
            var height = rect.height * scale;
            return new Rect(rect.center.x - width * 0.5f, rect.center.y - height * 0.5f, width, height);
        }

        private static string GetCombatEventLabel(string combatEvent)
        {
            if (string.IsNullOrEmpty(combatEvent)) return string.Empty;
            var closingBracket = combatEvent.IndexOf(']');
            return closingBracket >= 0 && closingBracket + 1 < combatEvent.Length
                ? combatEvent.Substring(closingBracket + 1).Trim()
                : combatEvent;
        }

        private static Color GetCombatEventColor(string combatEvent)
        {
            if (combatEvent != null && combatEvent.Contains("치명타")) return new Color(1f, 0.84f, 0.24f);
            if (combatEvent != null && combatEvent.Contains("액티브")) return new Color(0.55f, 0.42f, 1f);
            if (combatEvent != null && combatEvent.Contains("처형")) return new Color(1f, 0.28f, 0.65f);
            return new Color(0.26f, 0.9f, 1f);
        }

        private static GUIStyle CombatEventStyle(Color color)
        {
            if (color.r > 0.9f && color.g > 0.7f) return criticalEventStyle ??= CreateCombatEventStyle(color);
            if (color.r > 0.9f && color.b > 0.5f) return executeEventStyle ??= CreateCombatEventStyle(color);
            if (color.b > 0.9f && color.r > 0.4f) return activeEventStyle ??= CreateCombatEventStyle(color);
            return cyanEventStyle ??= CreateCombatEventStyle(color);
        }

        private static GUIStyle CreateCombatEventStyle(Color color)
        {
            return new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = color },
            };
        }

        private Texture2D GetEnemyPortrait(PrototypeCatalog catalog)
        {
            if (!dungeonRun.IsRunning) return catalog.RiftWatcherPortrait;
            if (dungeonRun.CurrentEnemyName == "균열 수호자") return catalog.NullWardenPortrait;
            if (dungeonRun.CurrentEnemyName == "마력 포식자") return catalog.ManaDevourerPortrait;
            if (dungeonRun.CurrentEnemyName == "균열 광전사") return catalog.RiftBerserkerPortrait;
            return catalog.RiftWatcherPortrait;
        }

        private static void DrawArenaMeter(Rect rect, float current, float maximum, Color color)
        {
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.02f, 0.03f, 0.06f, 0.9f), 0f, 0f);
            var fill = new Rect(rect.x + 1f, rect.y + 1f, Mathf.Max(0f, (rect.width - 2f) * (maximum <= 0f ? 0f : Mathf.Clamp01(current / maximum))), rect.height - 2f);
            GUI.DrawTexture(fill, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
        }

        private void DrawDungeonChoice(int index, DungeonDefinition definition)
        {
            var selected = gameState.SelectedDungeonIndex == index;
            var available = gameState.Level >= definition.RequiredLevel;
            GUILayout.BeginHorizontal("box");
            GUILayout.BeginVertical();
            GUILayout.Label($"{definition.DisplayName} {(selected ? "[선택됨]" : string.Empty)}", HeaderStyle());
            GUILayout.Label(definition.Description);
            GUILayout.Label($"권장 레벨 {definition.RequiredLevel} / {definition.FloorCount}층 / 클리어 {definition.ClearGoldReward}G, {definition.ClearExperienceReward} EXP");
            GUILayout.Label($"균열 숙련도 {gameState.GetDungeonMasteryRank(definition)}/{definition.MaximumMasteryRank} / 랭크당 영구 공격력 +{definition.DamageBonusPerMasteryRank:P0}");
            GUILayout.Label($"누적 공략 {gameState.GetDungeonTotalClearCount(definition)}회 / 최고 기록 {FormatRecordTime(gameState.GetDungeonBestClearSeconds(definition))}");
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUI.enabled = GUI.enabled && available && !selected;
            if (GUILayout.Button(available ? "선택" : $"Lv. {definition.RequiredLevel} 필요", GUILayout.Width(105f), GUILayout.Height(34f)))
            {
                if (gameState.TrySelectDungeon(index)) SaveGame();
            }

            GUI.enabled = !dungeonRun.IsRunning;
            GUILayout.EndHorizontal();
        }

        private void DrawCombatSpeedControls()
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("전투 관전 배속", HeaderStyle(), GUILayout.Width(120f));
            for (var index = 0; index < CombatSpeeds.Length; index++)
            {
                var speed = CombatSpeeds[index];
                if (GUILayout.Toggle(combatSpeedIndex == index, $"x{speed:0}", "Button", GUILayout.Width(60f), GUILayout.Height(28f))) combatSpeedIndex = index;
            }
            GUILayout.Label("배속은 관전 편의 기능이며, 직접 조작은 추가하지 않습니다.");
            GUILayout.EndHorizontal();
        }

        private static string FormatRecordTime(float seconds)
        {
            return seconds <= 0f ? "기록 없음" : $"{seconds:0.0}초";
        }

        private void DrawProtocolChoice(int index, DungeonProtocolDefinition protocol)
        {
            var selected = gameState.SelectedProtocolIndex == index;
            GUILayout.BeginHorizontal("box");
            GUILayout.BeginVertical();
            GUILayout.Label($"{protocol.DisplayName} {(selected ? "[선택됨]" : string.Empty)}", HeaderStyle());
            GUILayout.Label(protocol.Description);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUI.enabled = GUI.enabled && !selected;
            if (GUILayout.Button("선택", GUILayout.Width(90f), GUILayout.Height(34f)))
            {
                if (gameState.TrySelectProtocol(index)) SaveGame();
            }

            GUI.enabled = !dungeonRun.IsRunning;
            GUILayout.EndHorizontal();
        }

        private void DrawCombatDirectiveChoice(int index, CombatDirectiveDefinition directive)
        {
            var selected = gameState.SelectedCombatDirectiveIndex == index;
            GUILayout.BeginHorizontal("box");
            GUILayout.BeginVertical();
            GUILayout.Label($"{directive.DisplayName} {(selected ? "[선택됨]" : string.Empty)}", HeaderStyle());
            GUILayout.Label(directive.Description);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUI.enabled = GUI.enabled && !selected;
            if (GUILayout.Button("선택", GUILayout.Width(90f), GUILayout.Height(34f)))
            {
                if (gameState.TrySelectCombatDirective(index)) SaveGame();
            }
            GUI.enabled = !dungeonRun.IsRunning;
            GUILayout.EndHorizontal();
        }

        private void DrawDungeonReadiness()
        {
            var report = readinessAnalyzer.Analyze(gameState.SelectedDungeon, gameState.SelectedProtocol, gameState.CreateCombatProfile());
            GUILayout.BeginVertical("box");
            GUILayout.Label($"균열 분석: {GetReadinessLabel(report.Readiness)}", HeaderStyle());
            GUILayout.Label($"예상 공략 {report.ProjectedClearSeconds:0.0}초 / 총 제한 {report.TotalTimeLimit:0.0}초 / 예상 피격 {report.ProjectedIncomingDamage:0}");
            GUILayout.Label(report.Recommendation);
            GUILayout.EndVertical();
        }

        private static string GetReadinessLabel(DungeonReadiness readiness)
        {
            switch (readiness)
            {
                case DungeonReadiness.Dominant: return "압도적";
                case DungeonReadiness.Ready: return "공략 가능";
                case DungeonReadiness.Risky: return "생존 주의";
                default: return "화력 부족";
            }
        }

        private void DrawRebirthTab()
        {
            var progression = gameState.Catalog.Progression;
            GUILayout.Label("다시 쓴 상태창", SectionStyle());
            GUILayout.Label("회귀하면 레벨, 골드, 스탯, 스킬, 장비가 초기화됩니다. 기억의 파편은 유지됩니다.");
            GUILayout.Space(8f);
            GUILayout.Label($"회귀 횟수 {gameState.RebirthCount} / 기억의 파편 {gameState.LegacyShards} / 사용 가능 {gameState.AvailableLegacyShards}", HeaderStyle());
            GUILayout.Label($"기본 영구 효과: 공격력 +{gameState.LegacyShards * progression.DamageBonusPerShard:P0}, 골드 획득량 +{gameState.LegacyShards * progression.GoldBonusPerShard:P0}");
            GUILayout.Label($"조건: Lv. {progression.RebirthRequiredLevel} 이상, 던전 {progression.RebirthRequiredClears}회 클리어 (현재 Lv. {gameState.Level}, {gameState.ClearedDungeonCount}회)");
            GUI.enabled = !dungeonRun.IsRunning && gameState.CanRebirth;
            if (GUILayout.Button($"회귀하기 — 기억의 파편 +{progression.RebirthShardReward}", GUILayout.Height(42f)))
            {
                if (gameState.TryRebirth())
                {
                    saveMessage = "회귀 완료. 다음 생의 성장이 빨라집니다.";
                    SaveGame();
                    selectedTab = Tab.Status;
                }
            }

            GUI.enabled = true;
            GUILayout.Space(12f);
            GUILayout.Label("계승 특성", SectionStyle());
            GUILayout.Label("기억의 파편을 사용해 영구 빌드 방향을 선택합니다. 기본 파편 보너스는 소비 여부와 관계없이 유지됩니다.");
            GUI.enabled = !dungeonRun.IsRunning;
            foreach (var upgrade in gameState.Catalog.LegacyUpgrades)
            {
                DrawLegacyUpgrade(upgrade);
            }

            GUI.enabled = true;
            GUILayout.Space(12f);
            GUILayout.Label("저장 데이터", SectionStyle());
            GUILayout.Label(saveMessage);
            if (GUILayout.Button("현재 진행 저장", GUILayout.Height(30f))) SaveGame();
            if (GUILayout.Button("저장 삭제 후 새 게임 시작", GUILayout.Height(30f)))
            {
                saveService.DeleteSave();
                gameState = new StatusWindowGameState(gameState.Catalog);
                dungeonRun = CreateDungeonRun();
                saveMessage = "저장 데이터를 삭제하고 새 게임을 시작했습니다.";
                selectedTab = Tab.Status;
            }
        }

        private void DrawMilestonesTab()
        {
            GUILayout.Label("업적", SectionStyle());
            GUILayout.Label("달성한 목표의 보상을 수령해 다음 빌드를 빠르게 완성하세요.");
            GUI.enabled = !dungeonRun.IsRunning;
            foreach (var milestone in gameState.Catalog.Milestones)
            {
                DrawMilestone(milestone);
            }

            GUI.enabled = true;
        }

        private void DrawMilestone(MilestoneDefinition milestone)
        {
            var claimed = gameState.HasClaimedMilestone(milestone);
            var complete = gameState.IsMilestoneComplete(milestone);
            GUILayout.BeginHorizontal("box");
            GUILayout.BeginVertical();
            GUILayout.Label($"{milestone.DisplayName} {(claimed ? "[수령 완료]" : string.Empty)}", HeaderStyle());
            GUILayout.Label(milestone.Description);
            GUILayout.Label($"진행도: {Mathf.Min(gameState.GetMilestoneProgress(milestone), milestone.TargetValue)}/{milestone.TargetValue}");
            GUILayout.Label($"보상: {milestone.GoldReward}G / 스탯 포인트 {milestone.StatPointReward}");
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUI.enabled = GUI.enabled && complete && !claimed;
            if (GUILayout.Button(complete ? "보상 수령" : "미달성", GUILayout.Width(100f), GUILayout.Height(34f)))
            {
                if (gameState.TryClaimMilestone(milestone)) SaveGame();
            }

            GUI.enabled = !dungeonRun.IsRunning;
            GUILayout.EndHorizontal();
        }

        private void SaveGame()
        {
            if (gameState == null) return;
            saveService.Save(gameState.CreateSaveData());
            saveMessage = "진행 상황을 저장했습니다.";
        }

        private static string FormatOfflineDuration(int totalSeconds)
        {
            var duration = TimeSpan.FromSeconds(Mathf.Max(0, totalSeconds));
            return duration.TotalHours >= 1d
                ? $"{(int)duration.TotalHours}시간 {duration.Minutes}분"
                : $"{duration.Minutes}분";
        }

        private DungeonRun CreateDungeonRun()
        {
            return new DungeonRun(gameState.SelectedDungeon, gameState.SelectedProtocol);
        }

        private void DrawLegacyUpgrade(LegacyUpgradeDefinition upgrade)
        {
            var rank = gameState.GetLegacyUpgradeRank(upgrade);
            var canPurchase = rank < upgrade.MaximumRank && gameState.AvailableLegacyShards >= upgrade.ShardCostPerRank;
            GUILayout.BeginHorizontal("box");
            GUILayout.BeginVertical();
            GUILayout.Label($"{upgrade.DisplayName}  Rank {rank}/{upgrade.MaximumRank}", HeaderStyle());
            GUILayout.Label(upgrade.Description);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUI.enabled = GUI.enabled && canPurchase;
            if (GUILayout.Button($"{upgrade.ShardCostPerRank} 파편", GUILayout.Width(90f), GUILayout.Height(34f)))
            {
                if (gameState.TryPurchaseLegacyUpgrade(upgrade)) SaveGame();
            }

            GUI.enabled = !dungeonRun.IsRunning;
            GUILayout.EndHorizontal();
        }

        private static GUIStyle TitleStyle()
        {
            return titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.42f, 0.88f, 1f) },
            };
        }

        private static GUIStyle SectionStyle()
        {
            return sectionStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.70f, 0.82f, 1f) },
            };
        }

        private static GUIStyle HeaderStyle()
        {
            return headerStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.84f, 0.92f, 1f) },
            };
        }

        private static StatusWindowUiTheme UiTheme()
        {
            return uiTheme ??= new StatusWindowUiTheme();
        }
    }
}

namespace StatusWindow.UI
{
    /// <summary>
    /// Owns the prototype's reusable visual language. Runtime textures keep the IMGUI slice
    /// resolution-independent while final exported UI sprites can later replace these styles.
    /// </summary>
    internal sealed class StatusWindowUiTheme
    {
        private const int TextureSize = 32;

        public GUIStyle Panel { get; }
        public GUIStyle Card { get; }
        public GUIStyle Button { get; }
        public GUIStyle SelectedButton { get; }

        public StatusWindowUiTheme()
        {
            Panel = CreateBoxStyle(new Color(0.035f, 0.055f, 0.105f, 0.98f), new Color(0.18f, 0.76f, 0.98f, 0.8f), 2);
            Card = CreateBoxStyle(new Color(0.055f, 0.085f, 0.145f, 0.96f), new Color(0.18f, 0.42f, 0.65f, 0.85f), 1);
            Button = CreateButtonStyle(new Color(0.075f, 0.15f, 0.235f, 0.98f), new Color(0.13f, 0.29f, 0.42f, 1f), new Color(0.19f, 0.82f, 1f, 0.92f));
            SelectedButton = CreateButtonStyle(new Color(0.12f, 0.34f, 0.48f, 1f), new Color(0.16f, 0.45f, 0.62f, 1f), new Color(0.61f, 0.94f, 1f, 1f));
        }

        private static GUIStyle CreateBoxStyle(Color fill, Color border, int borderWidth)
        {
            return new GUIStyle(GUI.skin.box)
            {
                normal = { background = CreateFrame(fill, border, borderWidth) },
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(10, 10, 8, 8),
            };
        }

        private static GUIStyle CreateButtonStyle(Color normal, Color hover, Color text)
        {
            return new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { background = CreateFrame(normal, new Color(0.28f, 0.8f, 1f, 0.75f), 1), textColor = text },
                hover = { background = CreateFrame(hover, new Color(0.55f, 0.94f, 1f, 1f), 1), textColor = Color.white },
                active = { background = CreateFrame(hover * 0.76f, new Color(0.7f, 0.97f, 1f, 1f), 1), textColor = Color.white },
                border = new RectOffset(8, 8, 8, 8),
            };
        }

        private static Texture2D CreateFrame(Color fill, Color border, int borderWidth)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = "StatusWindowRuntimeUiFrame",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color[TextureSize * TextureSize];
            for (var y = 0; y < TextureSize; y++)
            {
                for (var x = 0; x < TextureSize; x++)
                {
                    var edge = x < borderWidth || y < borderWidth || x >= TextureSize - borderWidth || y >= TextureSize - borderWidth;
                    pixels[y * TextureSize + x] = edge ? border : fill;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
