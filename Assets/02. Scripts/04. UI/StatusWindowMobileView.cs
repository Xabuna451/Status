using System;
using System.Collections.Generic;
using StatusWindow.Combat;
using StatusWindow.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace StatusWindow.UI
{
    /// <summary>GameObject based mobile presentation for the status window runtime state.</summary>
    internal sealed class StatusWindowMobileView : MonoBehaviour
    {
        private const float ReferenceWidth = 1080f;
        private const float ReferenceHeight = 1920f;
        private static readonly Color Background = new Color(0.025f, 0.045f, 0.09f, 0.98f);
        private static readonly Color Card = new Color(0.055f, 0.09f, 0.16f, 0.96f);
        private static readonly Color Cyan = new Color(0.12f, 0.78f, 1f, 1f);
        private static readonly Color Text = new Color(0.88f, 0.94f, 1f, 1f);

        private enum Section { Growth, Skills, Equipment, Dungeon, Milestones, Rebirth }

        private StatusWindowPrototype host;
        private Font font;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private Text levelText;
        [SerializeField] private Text goldText;
        [SerializeField] private Text statText;
        [SerializeField] private Text memoryText;
        [SerializeField] private Text combatText;
        [SerializeField] private Text floorText;
        [SerializeField] private Text enemyNameText;
        [SerializeField] private Text hunterHealthText;
        [SerializeField] private Text enemyHealthText;
        [SerializeField] private Image hunterHealthFill;
        [SerializeField] private Image enemyHealthFill;
        [SerializeField] private Image hunterImpactFlash;
        [SerializeField] private Image enemyImpactFlash;
        [SerializeField] private Image combatProjectile;
        [SerializeField] private Text hunterDamagePopup;
        [SerializeField] private Text enemyDamagePopup;
        [SerializeField] private Text runResultText;
        [SerializeField] private Text equipmentRewardText;
        [SerializeField] private Button autoRepeatQuickButton;
        [SerializeField] private Text autoRepeatQuickLabel;
        [SerializeField] private List<Button> tabButtons = new List<Button>();
        [SerializeField] private GameObject statusPanel;
        [SerializeField] private Button closeStatusPanelButton;
        [SerializeField] private ScrollRect contentScroll;
        [SerializeField] private Button arenaActionButton;
        [SerializeField] private Text arenaActionLabel;
        [SerializeField] private StatusWindowNoticeView noticeOverlay;
        [SerializeField] private RawImage enemyImage;
        [SerializeField] private RawImage heroImage;
        [SerializeField] private RectTransform heroTransform;
        [SerializeField] private RectTransform enemyTransform;
        [SerializeField] private CanvasGroup heroVisualGroup;
        [SerializeField] private CanvasGroup enemyVisualGroup;
        [SerializeField] private Transform content;
        [Header("Editable Content Templates")]
        [SerializeField] private StatusWindowActionCardView actionCardTemplate;
        [SerializeField] private StatusWindowHeadingCardView headingCardTemplate;
        [SerializeField] private StatusWindowButtonRowView buttonRowTemplate;
        [Header("Optional Combat SFX")]
        [SerializeField] private AudioClip basicAttackClip;
        [SerializeField] private AudioClip criticalAttackClip;
        [SerializeField] private AudioClip activeSkillClip;
        [SerializeField] private AudioClip clearClip;
        [SerializeField] private AudioClip failureClip;
        private Section section;
        private string displayedEvent;
        private float eventEndTime;
        private float nextDynamicRefresh;
        private float enemySpawnEndTime;
        private string displayedEnemyName;
        private bool wasBuildLocked;
        private string displayedRunResult;
        private Rect lastSafeArea;
        private bool hasAppliedSafeArea;
        private Vector2 heroRestPosition;
        private Vector2 enemyRestPosition;
        private Vector3 heroRestScale = Vector3.one;
        private Vector3 enemyRestScale = Vector3.one;
        private bool hasCombatVisualRestState;
        private AudioSource effectsSource;

        internal void Initialize(StatusWindowPrototype owner)
        {
            host = owner;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            effectsSource = GetComponent<AudioSource>();
            if (effectsSource == null) effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
            effectsSource.spatialBlend = 0f;
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            if (safeArea == null)
            {
                Debug.LogError("StatusWindowMobileCanvas prefab references are not assigned.");
                enabled = false;
                return;
            }
            CaptureCombatVisualRestState();
            BindStaticInteractions();
            RefreshAll();
            HideStatusPanel();
            wasBuildLocked = !CanEditBuild;
            if (!string.IsNullOrEmpty(host.SystemMessage)) ShowSystemNotice(host.SystemMessage);
        }

        private void Update()
        {
            ApplySafeArea();
            AnimateCombat();
            var buildLocked = !CanEditBuild;
            if (buildLocked != wasBuildLocked)
            {
                wasBuildLocked = buildLocked;
                RebuildContent();
            }
            if (Time.unscaledTime < nextDynamicRefresh) return;
            nextDynamicRefresh = Time.unscaledTime + 0.12f;
            RefreshDynamic();
        }

        private void ShowSystemNotice(string message)
        {
            noticeOverlay?.Show(message, DismissSystemNotice);
        }

        private void DismissSystemNotice()
        {
            noticeOverlay?.Hide();
        }

        private void ShowRunResult(DungeonRun run)
        {
            var equipment = string.IsNullOrEmpty(run.EquipmentRewardName) ? "장비 획득 없음" : $"신규 장비: {run.EquipmentRewardName}";
            var title = run.Result == DungeonResult.Cleared ? "공략 성공" : run.Result == DungeonResult.Cancelled ? "전투 중단" : "공략 실패";
            var goal = host.GetNextProgressionGoal();
            var message = $"{title}\n\n{run.ResultMessage}\n\n보상  GOLD +{run.GoldEarned:N0} · EXP +{run.ExperienceEarned:N0}\n{equipment}\n\n빌드 분석\n{run.Recommendation}\n\n다음 목표 · {goal.Title}\n{goal.Description}";
            var cleared = run.Result == DungeonResult.Cleared;
            noticeOverlay?.Show(message, cleared ? "다시 도전" : "빌드 확인", () => ResolveRunResult(run, cleared));
        }

        private void ResolveRunResult(DungeonRun completedRun, bool retry)
        {
            DismissSystemNotice();
            if (host == null || host.ActiveDungeonRun != completedRun) return;
            if (retry)
            {
                host.TryStartDungeon();
                RefreshAll();
                return;
            }

            SetSection(Section.Growth);
        }

        private void RefreshAll()
        {
            RefreshDynamic();
            RebuildContent();
        }

        private void RefreshDynamic()
        {
            if (host == null || host.GameState == null) return;
            var state = host.GameState;
            levelText.text = $"Lv. {state.Level}";
            goldText.text = $"GOLD {state.Gold:N0}";
            statText.text = $"STAT {state.UnspentStatPoints}";
            memoryText.text = $"MEMORY {state.AvailableLegacyShards}";
            enemyImage.texture = host.CurrentEnemyPortrait;
            var run = host.ActiveDungeonRun;
            var combatEvent = run.LastCombatEvent;
            var combatEventType = run.LastCombatEventType;
            combatText.text = GetCombatEventLabel(combatEvent);
            combatText.color = GetCombatEventColor(combatEventType);
            var isRunning = run.IsRunning;
            if (arenaActionLabel != null) arenaActionLabel.text = GetArenaActionLabel(run);
            if (autoRepeatQuickLabel != null) autoRepeatQuickLabel.text = state.AutoRepeatDungeon ? "자동 반복  ON" : "자동 반복  OFF";
            floorText.text = isRunning ? $"{run.Floor}층  {run.Kills}/{run.KillTarget}  {run.TimeRemaining:0}s" : "균열 대기";
            enemyNameText.text = isRunning ? run.CurrentEnemyName : "균열의 잔재";
            if (displayedEnemyName != enemyNameText.text)
            {
                displayedEnemyName = enemyNameText.text;
                enemySpawnEndTime = Time.unscaledTime + 0.26f;
            }
            if (runResultText != null)
            {
                var rewardLine = $"획득  GOLD +{run.GoldEarned:N0}  ·  EXP +{run.ExperienceEarned:N0}";
                runResultText.text = isRunning
                    ? $"목표: {run.KillTarget - run.Kills}체 처치  ·  {rewardLine}"
                    : BuildIdleCombatSummary(run, host.GetNextProgressionGoal(), rewardLine);
            }
            if (equipmentRewardText != null)
            {
                var receivedEquipment = run.EquipmentRewardName;
                equipmentRewardText.text = string.IsNullOrEmpty(receivedEquipment) ? string.Empty : $"NEW EQUIPMENT  {receivedEquipment}";
                equipmentRewardText.gameObject.SetActive(!string.IsNullOrEmpty(receivedEquipment));
            }
            if (displayedRunResult != run.ResultMessage)
            {
                displayedRunResult = run.ResultMessage;
                if (!isRunning && IsFinishedResult(run.Result)) ShowRunResult(run);
            }
            SetHealth(hunterHealthFill, hunterHealthText, isRunning ? run.PlayerHealth : 1, isRunning ? run.PlayerMaxHealth : 1, new Color(0.10f, 0.82f, 1f, 1f));
            var enemyCurrent = isRunning ? run.EnemyHealth + run.EnemyBarrier : 1;
            var enemyMaximum = isRunning ? run.EnemyMaxHealth + Mathf.CeilToInt(run.EnemyMaxHealth * 0.2f) : 1;
            SetHealth(enemyHealthFill, enemyHealthText, enemyCurrent, enemyMaximum, new Color(0.95f, 0.24f, 0.68f, 1f));
            if (displayedEvent != combatEvent)
            {
                displayedEvent = combatEvent;
                eventEndTime = Time.unscaledTime + 0.38f;
                PrepareDamagePopup(combatEvent);
                PlayCombatFeedback(combatEventType);
                TriggerHapticFeedback(combatEventType);
            }
        }

        private void SetSection(Section newSection)
        {
            if (statusPanel != null && statusPanel.activeSelf && section == newSection)
            {
                HideStatusPanel();
                return;
            }
            section = newSection;
            if (statusPanel != null) statusPanel.SetActive(true);
            RefreshTabSelection();
            RebuildContent();
            ResetContentScroll();
        }

        private void HideStatusPanel()
        {
            if (statusPanel != null) statusPanel.SetActive(false);
            RefreshTabSelection();
        }

        private void ResetContentScroll()
        {
            if (contentScroll == null) return;
            Canvas.ForceUpdateCanvases();
            contentScroll.verticalNormalizedPosition = 1f;
        }

        private void RefreshTabSelection()
        {
            for (var index = 0; index < tabButtons.Count; index++)
            {
                var active = (statusPanel == null || statusPanel.activeSelf) && index == (int)section;
                var button = tabButtons[index];
                button.GetComponent<Image>().color = active ? Cyan : new Color(0.08f, 0.28f, 0.42f, 1f);
                var label = button.GetComponentInChildren<Text>();
                if (label != null) label.color = active ? Background : Text;
            }
        }

        private void BindStaticInteractions()
        {
            for (var index = 0; index < tabButtons.Count; index++)
            {
                var captured = (Section)index;
                var button = tabButtons[index];
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SetSection(captured));
            }
            if (arenaActionButton != null)
            {
                arenaActionButton.onClick.RemoveAllListeners();
                arenaActionButton.onClick.AddListener(() =>
                {
                    if (host.ActiveDungeonRun.IsRunning)
                    {
                        host.TryCancelDungeon();
                        RefreshAll();
                        return;
                    }

                    if (host.TryStartDungeon())
                    {
                        HideStatusPanel();
                        RefreshAll();
                    }
                });
            }
            if (autoRepeatQuickButton != null)
            {
                autoRepeatQuickButton.onClick.RemoveAllListeners();
                autoRepeatQuickButton.onClick.AddListener(() =>
                {
                    host.GameState.SetAutoRepeatDungeon(!host.GameState.AutoRepeatDungeon);
                    host.SaveProgress();
                    RefreshAll();
                });
            }
            if (closeStatusPanelButton != null)
            {
                closeStatusPanelButton.onClick.RemoveAllListeners();
                closeStatusPanelButton.onClick.AddListener(HideStatusPanel);
            }
        }

        private void RebuildContent()
        {
            if (content == null || host == null || host.GameState == null) return;
            for (var index = content.childCount - 1; index >= 0; index--) Destroy(content.GetChild(index).gameObject);
            switch (section)
            {
                case Section.Growth: BuildGrowth(); break;
                case Section.Skills: BuildSkills(); break;
                case Section.Equipment: BuildEquipment(); break;
                case Section.Dungeon: BuildDungeon(); break;
                case Section.Milestones: BuildMilestones(); break;
                case Section.Rebirth: BuildRebirth(); break;
            }
            if (!CanEditBuild) SetContentButtonsInteractable(false);
        }

        private void BuildGrowth()
        {
            AddHeading("기본 스탯", "스탯 포인트를 배분해 자동전투 빌드를 구성하세요.");
            AddButtonRow($"골드 {host.GameState.Catalog.StatPointGoldCost}로 스탯 포인트 +1", () => { if (CanEditBuild && host.GameState.TryBuyStatPoint()) host.SaveProgress(); });
            AddStat(StatType.Strength, "근력", "물리 공격력과 최대 체력");
            AddStat(StatType.Agility, "민첩", "공격 속도와 이동 시간");
            AddStat(StatType.Magic, "마력", "자동 액티브 스킬 피해");
            AddStat(StatType.Sense, "감각", "치명타 확률");
            AddStat(StatType.Will, "의지", "방어력과 최대 체력");
            AddButtonRow($"빌드 재설정 · 스킬 투자금 {host.GameState.BuildResetGoldRefund}G 환급", () => { if (CanEditBuild && host.GameState.TryResetBuild()) host.SaveProgress(); }, new Color(0.34f, 0.34f, 0.60f, 1f));
            AddHeading("빌드 프리셋", "스탯·스킬·장비 조합을 저장해 상황에 따라 즉시 바꿉니다.");
            for (var index = 0; index < host.GameState.BuildPresetCount; index++)
            {
                var captured = index;
                var saved = host.GameState.HasBuildPreset(index);
                AddActionCard($"프리셋 {index + 1}", saved ? "저장된 빌드가 있습니다." : "비어 있음", "현재 저장", () => { if (CanEditBuild && host.GameState.SaveBuildPreset(captured)) host.SaveProgress(); });
                if (saved) AddButtonRow($"프리셋 {index + 1} 불러오기", () => { if (CanEditBuild && host.GameState.TryApplyBuildPreset(captured)) host.SaveProgress(); }, new Color(0.18f, 0.50f, 0.72f, 1f));
            }
        }

        private void BuildSkills()
        {
            AddHeading("스킬 노드", "해금한 스킬을 장착하면 자동전투에 반영됩니다.");
            foreach (var skill in host.GameState.Catalog.SkillNodes)
            {
                var unlocked = host.GameState.HasSkill(skill);
                var equipped = host.GameState.IsSkillEquipped(skill);
                AddActionCard(skill.DisplayName, skill.Description, unlocked ? (equipped ? "장착 해제" : "장착") : $"해금 {skill.GoldCost}G", () =>
                {
                    if (!CanEditBuild) return;
                    if (unlocked) host.GameState.TryToggleSkill(skill); else host.GameState.TryUnlockSkill(skill);
                    host.SaveProgress();
                });
            }
        }

        private void BuildEquipment()
        {
            AddHeading("장비", "장착 장비는 자동전투 빌드에 즉시 반영됩니다.");
            AddHeading("장비 세트", "같은 계열의 장비를 모두 장착하면 세트 효과가 전투에 적용됩니다.");
            foreach (var equipmentSet in host.GameState.Catalog.EquipmentSets)
            {
                var equippedCount = host.GameState.GetEquipmentSetEquippedCount(equipmentSet);
                var requiredCount = equipmentSet.RequiredEquipment == null ? 0 : equipmentSet.RequiredEquipment.Count;
                var active = host.GameState.IsEquipmentSetActive(equipmentSet);
                var canEquip = !active && requiredCount > 0 && equippedCount >= requiredCount;
                var stateLabel = active ? "발동 중" : $"{equippedCount}/{requiredCount}";
                AddActionCard(
                    $"{equipmentSet.DisplayName} · {stateLabel}",
                    $"{equipmentSet.Description}\n필요 장비: {GetEquipmentSetRequirementText(equipmentSet)}\n효과: {GetEquipmentSetBonusText(equipmentSet)}",
                    active ? "적용됨" : canEquip ? "세트 장착" : "장비 부족",
                    () =>
                    {
                        if (CanEditBuild && canEquip && host.GameState.TryEquipSet(equipmentSet)) host.SaveProgress();
                    });
            }
            foreach (var equipment in host.GameState.Catalog.Equipment)
            {
                var owned = host.GameState.HasEquipment(equipment);
                var equipped = host.GameState.GetEquipped(equipment.Slot) == equipment;
                var upgradeLevel = host.GameState.GetEquipmentUpgradeLevel(equipment);
                AddEquipmentCard(equipment, upgradeLevel, equipped, owned, () =>
                {
                    if (!CanEditBuild) return;
                    if (equipped) host.GameState.Unequip(equipment.Slot); else host.GameState.TryEquip(equipment);
                    host.SaveProgress();
                });
                if (owned) AddButtonRow($"{equipment.DisplayName} 강화 · {equipment.GetUpgradeGoldCost(upgradeLevel)}G", () => { if (CanEditBuild && host.GameState.TryUpgradeEquipment(equipment)) host.SaveProgress(); }, new Color(0.42f, 0.30f, 0.76f, 1f));
            }
        }

        private static string GetEquipmentSetRequirementText(EquipmentSetDefinition equipmentSet)
        {
            if (equipmentSet == null || equipmentSet.RequiredEquipment == null || equipmentSet.RequiredEquipment.Count == 0) return "없음";
            var result = string.Empty;
            foreach (var equipment in equipmentSet.RequiredEquipment)
            {
                if (equipment == null) continue;
                result += string.IsNullOrEmpty(result) ? equipment.DisplayName : $" · {equipment.DisplayName}";
            }
            return string.IsNullOrEmpty(result) ? "없음" : result;
        }

        private static string GetEquipmentSetBonusText(EquipmentSetDefinition equipmentSet)
        {
            if (equipmentSet == null) return "없음";
            var result = string.Empty;
            if (equipmentSet.DamageBonus > 0) result += $"공격 +{equipmentSet.DamageBonus}";
            if (equipmentSet.ActiveDamageBonus > 0) result += AppendBonus(result, $"스킬 +{equipmentSet.ActiveDamageBonus}");
            if (equipmentSet.MaxHealthBonus > 0) result += AppendBonus(result, $"체력 +{equipmentSet.MaxHealthBonus}");
            if (equipmentSet.DefenseBonus > 0) result += AppendBonus(result, $"방어 +{equipmentSet.DefenseBonus}");
            if (equipmentSet.MoveDelayReduction > 0f) result += AppendBonus(result, $"이동 -{equipmentSet.MoveDelayReduction:0.00}s");
            if (equipmentSet.CriticalChanceBonus > 0f) result += AppendBonus(result, $"치명 +{equipmentSet.CriticalChanceBonus:P0}");
            return string.IsNullOrEmpty(result) ? "전투 효과 없음" : result;
        }

        private static string AppendBonus(string existing, string next) => string.IsNullOrEmpty(existing) ? next : $" · {next}";

        private void AddEquipmentCard(EquipmentDefinition equipment, int upgradeLevel, bool equipped, bool owned, Action callback)
        {
            var card = CreateCard(equipment.DisplayName);
            var iconFrame = CreateRect("IconFrame", card, GetEquipmentRarityColor(equipment.GoldCost));
            SetAnchored(iconFrame, new Vector2(0.03f, 0.16f), new Vector2(0.16f, 0.86f));
            var icon = CreateRawImage("Icon", iconFrame, host.GameState.Catalog.EquipmentIconSheet);
            Stretch(icon.rectTransform, 4f);
            icon.uvRect = GetEquipmentIconUv(equipment.Id);
            var title = CreateText("Title", card, $"{equipment.DisplayName} +{upgradeLevel}{(equipped ? "  [장착]" : string.Empty)}", 27, equipped ? Cyan : Text, TextAnchor.UpperLeft);
            SetTopStretch(title.rectTransform, 14f, 34f, 150f, 238f);
            var description = CreateText("Description", card, equipment.Description, 21, Text, TextAnchor.LowerLeft);
            SetBottomStretch(description.rectTransform, 14f, 54f, 150f, 238f);
            var action = equipped ? "해제" : (owned ? "장착" : $"구매 {equipment.GoldCost}G");
            var button = CreateButton("Action", card, action, callback, equipped ? new Color(0.22f, 0.48f, 0.64f, 1f) : Cyan);
            SetBottomRight(button.GetComponent<RectTransform>(), 18f, 20f, 220f, 58f);
            SetPreferredHeight(card, 136f);
        }

        private void BuildDungeon()
        {
            AddHeading("균열 던전", "현재 빌드를 잠그면 캐릭터가 모든 전투를 자동으로 수행합니다.");
            for (var index = 0; index < host.GameState.Catalog.Dungeons.Count; index++)
            {
                var captured = index;
                var dungeon = host.GameState.Catalog.Dungeons[index];
                if (dungeon == null) continue;
                var selected = host.GameState.SelectedDungeonIndex == index;
                var available = host.GameState.Level >= dungeon.RequiredLevel;
                var record = host.GameState.GetDungeonBestClearSeconds(dungeon);
                var action = selected ? "선택됨" : (available ? "선택" : $"Lv.{dungeon.RequiredLevel}");
                AddActionCard(dungeon.DisplayName, $"{dungeon.Description}\n{GetDungeonThreatBrief(dungeon)}\n{dungeon.FloorCount}층 · 보상 {dungeon.ClearGoldReward}G · 최고 {FormatRecord(record)}", action, () => { if (!host.ActiveDungeonRun.IsRunning && available && host.GameState.TrySelectDungeon(captured)) host.SaveProgress(); });
            }
            AddHeading("위험 프로토콜", "난이도와 보상을 교환합니다. 선택은 입장 전에만 가능합니다.");
            for (var index = 0; index < host.GameState.Catalog.DungeonProtocols.Count; index++)
            {
                var captured = index;
                var protocol = host.GameState.Catalog.DungeonProtocols[index];
                if (protocol == null) continue;
                AddActionCard(protocol.DisplayName, protocol.Description, host.GameState.SelectedProtocolIndex == index ? "선택됨" : "선택", () => { if (!host.ActiveDungeonRun.IsRunning && host.GameState.TrySelectProtocol(captured)) host.SaveProgress(); });
            }
            AddHeading("전술 지침", "자동전투의 우선 성향을 지정합니다. 조작 없이 빌드에만 반영됩니다.");
            for (var index = 0; index < host.GameState.Catalog.CombatDirectives.Count; index++)
            {
                var captured = index;
                var directive = host.GameState.Catalog.CombatDirectives[index];
                if (directive == null) continue;
                AddActionCard(directive.DisplayName, directive.Description, host.GameState.SelectedCombatDirectiveIndex == index ? "선택됨" : "선택", () => { if (!host.ActiveDungeonRun.IsRunning && host.GameState.TrySelectCombatDirective(captured)) host.SaveProgress(); });
            }
            var report = host.AnalyzeSelectedDungeon();
            AddHeading($"균열 분석 · {GetReadinessLabel(report.Readiness)}", $"예상 {report.ProjectedClearSeconds:0.0}초 / 제한 {report.TotalTimeLimit:0.0}초\n{report.Recommendation}");
            AddButtonRow(host.GameState.AutoRepeatDungeon ? "자동 반복: 켜짐" : "자동 반복: 꺼짐", () => { if (!host.ActiveDungeonRun.IsRunning) { host.GameState.SetAutoRepeatDungeon(!host.GameState.AutoRepeatDungeon); host.SaveProgress(); } }, host.GameState.AutoRepeatDungeon ? new Color(0.24f, 0.72f, 0.55f, 1f) : new Color(0.17f, 0.27f, 0.38f, 1f));
            AddButtonRow(host.ActiveDungeonRun.IsRunning ? "자동전투 중단 · 빌드 잠금 해제" : "던전 입장 · 빌드 잠금", () =>
            {
                if (host.ActiveDungeonRun.IsRunning) host.TryCancelDungeon();
                else host.TryStartDungeon();
            }, Cyan);
            AddHeading("최근 전투", $"{host.ActiveDungeonRun.ResultMessage}\n{host.ActiveDungeonRun.Recommendation}");
            AddHeading("이번 공략 보상", GetRunRewardSummary(host.ActiveDungeonRun));
        }

        private void BuildMilestones()
        {
            BuildDailyContracts();
            AddHeading("업적", "달성한 목표의 보상을 수령하세요.");
            foreach (var milestone in host.GameState.Catalog.Milestones)
            {
                var ready = host.GameState.IsMilestoneComplete(milestone) && !host.GameState.HasClaimedMilestone(milestone);
                AddActionCard(milestone.DisplayName, $"{milestone.Description}\n보상 {milestone.GoldReward}G / 스탯 {milestone.StatPointReward}", ready ? "보상 수령" : "진행 중", () => { if (ready) host.GameState.TryClaimMilestone(milestone); host.SaveProgress(); });
            }
        }

        private void BuildDailyContracts()
        {
            var state = host.GameState;
            state.RefreshDailyContracts(DateTime.UtcNow);
            AddHeading("일일 균열 의뢰", "매일 00:00 UTC에 새 의뢰가 갱신됩니다.");
            AddDailyContract(state, DailyContractType.RiftClear, "균열 정리", "균열 던전을 공략해 보상을 확보하세요.");
            AddDailyContract(state, DailyContractType.CombatGold, "전리품 회수", "자동전투로 획득한 골드를 모으세요.");
        }

        private void AddDailyContract(StatusWindowGameState state, DailyContractType contractType, string title, string description)
        {
            var progress = state.GetDailyContractProgress(contractType);
            var target = state.GetDailyContractTarget(contractType);
            var claimed = state.HasClaimedDailyContract(contractType);
            var ready = !claimed && progress >= target;
            var rewards = $"보상 {state.GetDailyContractGoldReward(contractType)}G / 스탯 {state.GetDailyContractStatPointReward(contractType)}";
            var action = claimed ? "수령 완료" : ready ? "보상 수령" : $"{progress}/{target}";
            AddActionCard(title, $"{description}\n진행 {Mathf.Min(progress, target)}/{target} · {rewards}", action, () =>
            {
                if (ready && state.TryClaimDailyContract(contractType)) host.SaveProgress();
            });
        }

        private void BuildRebirth()
        {
            var state = host.GameState;
            var progression = state.Catalog.Progression;
            AddHeading("다시 쓴 상태창", $"회귀 {state.RebirthCount}회 · 기억의 파편 {state.LegacyShards} · 사용 가능 {state.AvailableLegacyShards}");
            AddHeading("영구 기본 효과", $"공격력 +{state.LegacyShards * progression.DamageBonusPerShard:P0} · 골드 획득량 +{state.LegacyShards * progression.GoldBonusPerShard:P0}");
            AddHeading("회귀 조건", $"Lv. {progression.RebirthRequiredLevel} / 던전 {progression.RebirthRequiredClears}회 클리어 필요\n현재 Lv. {state.Level} / {state.ClearedDungeonCount}회");
            AddButtonRow(state.CanRebirth ? $"회귀하기 · 기억의 파편 +{progression.RebirthShardReward}" : "회귀 조건 미달", () => { if (CanEditBuild && state.TryRebirth()) host.SaveProgress(); }, new Color(0.45f, 0.2f, 0.72f, 1f));
            AddHeading("계승 특성", "기억의 파편을 사용해 다음 회귀 이후에도 유지되는 성장 방향을 선택합니다.");
            foreach (var upgrade in state.Catalog.LegacyUpgrades)
            {
                var rank = state.GetLegacyUpgradeRank(upgrade);
                var action = rank >= upgrade.MaximumRank ? "최대" : $"{upgrade.ShardCostPerRank} 파편";
                AddActionCard($"{upgrade.DisplayName}  Rank {rank}/{upgrade.MaximumRank}", upgrade.Description, action, () => { if (CanEditBuild && state.TryPurchaseLegacyUpgrade(upgrade)) host.SaveProgress(); });
            }
            AddHeading("환경 설정", "사운드와 진동은 언제든지 끌 수 있습니다. 실제 SFX는 에셋 연결 뒤 이 설정을 따릅니다.");
            AddButtonRow(state.SoundEnabled ? "사운드: 켜짐" : "사운드: 꺼짐", () => { state.SetSoundEnabled(!state.SoundEnabled); host.SaveProgress(); }, state.SoundEnabled ? new Color(0.20f, 0.65f, 0.78f, 1f) : new Color(0.25f, 0.27f, 0.36f, 1f));
            AddButtonRow(state.VibrationEnabled ? "진동: 켜짐" : "진동: 꺼짐", () => { state.SetVibrationEnabled(!state.VibrationEnabled); host.SaveProgress(); }, state.VibrationEnabled ? new Color(0.20f, 0.65f, 0.78f, 1f) : new Color(0.25f, 0.27f, 0.36f, 1f));
        }

        private void AddStat(StatType stat, string title, string description)
        {
            AddActionCard($"{title}  {host.GameState.GetStat(stat)}", description, "+", () => { if (CanEditBuild && host.GameState.TrySpendStatPoint(stat)) host.SaveProgress(); });
        }

        private void AddHeading(string title, string description)
        {
            if (headingCardTemplate == null) return;
            Instantiate(headingCardTemplate, content).Bind(title, description);
        }

        private void AddActionCard(string title, string description, string action, Action callback)
        {
            if (actionCardTemplate == null) return;
            var view = Instantiate(actionCardTemplate, content);
            view.Bind(title, description, action, () => { callback?.Invoke(); RefreshAll(); }, Cyan);
        }

        private void AddButtonRow(string label, Action callback, Color? color = null)
        {
            if (buttonRowTemplate == null) return;
            var view = Instantiate(buttonRowTemplate, content);
            view.Bind(label, () => { callback?.Invoke(); RefreshAll(); }, color ?? Cyan);
        }

        private RectTransform CreateCard(string name)
        {
            var card = CreateRect(name, content, Card);
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.42f, 0.72f, 0.55f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return card;
        }

        private bool CanEditBuild => host != null && host.ActiveDungeonRun != null && !host.ActiveDungeonRun.IsRunning;

        private void SetContentButtonsInteractable(bool interactable)
        {
            foreach (var button in content.GetComponentsInChildren<Button>())
            {
                button.interactable = interactable;
                var image = button.GetComponent<Image>();
                if (image != null) image.color = interactable ? image.color : new Color(0.12f, 0.16f, 0.24f, 0.95f);
            }
        }

        private void AnimateCombat()
        {
            if (heroTransform == null || enemyTransform == null) return;
            if (!hasCombatVisualRestState) CaptureCombatVisualRestState();
            var eventPulse = Mathf.Clamp01((eventEndTime - Time.unscaledTime) / 0.38f);
            var arc = Mathf.Sin((1f - eventPulse) * Mathf.PI) * eventPulse;
            var combatEvent = displayedEvent ?? string.Empty;
            var combatEventType = host != null && host.ActiveDungeonRun != null
                ? host.ActiveDungeonRun.LastCombatEventType
                : CombatEventType.Idle;
            var heroAttack = IsHeroAttack(combatEventType);
            var enemyAttack = combatEventType == CombatEventType.EnemyAttack;
            var activeSkill = combatEventType == CombatEventType.ActiveSkill;
            var critical = combatEventType == CombatEventType.CriticalAttack || combatEventType == CombatEventType.Execute;
            var floorAdvanced = combatEventType == CombatEventType.FloorAdvanced;
            var cleared = combatEventType == CombatEventType.Cleared;
            var failed = combatEventType == CombatEventType.Failed || combatEventType == CombatEventType.Cancelled;
            var heroIdle = Mathf.Sin(Time.unscaledTime * 2.3f) * 8f;
            var enemyIdle = Mathf.Sin(Time.unscaledTime * 1.8f + 0.6f) * 7f;
            var heroOffset = new Vector2(heroAttack ? arc * (activeSkill ? 92f : 70f) : failed ? -arc * 58f : floorAdvanced ? -arc * 20f : 0f, heroIdle + (cleared ? arc * 18f : 0f));
            var enemyOffset = new Vector2(enemyAttack ? -arc * 60f : heroAttack ? arc * 14f : cleared ? arc * 86f : floorAdvanced ? arc * 32f : 0f, enemyIdle);
            heroTransform.anchoredPosition = heroRestPosition + heroOffset;
            enemyTransform.anchoredPosition = enemyRestPosition + enemyOffset;
            heroTransform.localScale = heroRestScale * (1f + (activeSkill ? arc * 0.16f : 0f) + (enemyAttack ? arc * 0.08f : 0f) + (cleared ? arc * 0.14f : 0f));
            enemyTransform.localScale = enemyRestScale * (1f + (critical ? arc * 0.16f : heroAttack ? arc * 0.10f : 0f) - (cleared ? arc * 0.24f : 0f));
            var spawnPulse = Mathf.Clamp01((enemySpawnEndTime - Time.unscaledTime) / 0.26f);
            SetVisualOpacity(heroVisualGroup, 0.92f + Mathf.Sin(Time.unscaledTime * 2.3f) * 0.04f + (heroAttack ? arc * 0.08f : 0f) - (failed ? arc * 0.35f : 0f));
            SetVisualOpacity(enemyVisualGroup, Mathf.Lerp(1f, 0.58f, spawnPulse) + (heroAttack ? arc * 0.10f : 0f) - (cleared ? arc * 0.46f : 0f));
            SetImpactFlash(hunterImpactFlash, enemyAttack || failed ? arc : 0f, failed ? new Color(1f, 0.22f, 0.38f, 1f) : Cyan);
            SetImpactFlash(enemyImpactFlash, heroAttack || cleared || floorAdvanced ? arc : 0f, cleared ? new Color(0.18f, 1f, 0.66f, 1f) : floorAdvanced ? Cyan : critical ? new Color(1f, 0.83f, 0.2f, 1f) : activeSkill ? new Color(0.40f, 0.96f, 1f, 1f) : new Color(0.88f, 0.22f, 0.8f, 1f));
            AnimateProjectile(heroAttack, enemyAttack, eventPulse, combatEventType);
            AnimateDamagePopup(hunterDamagePopup, enemyAttack || failed, eventPulse, new Color(1f, 0.45f, 0.58f, 1f));
            AnimateDamagePopup(enemyDamagePopup, heroAttack || cleared || floorAdvanced, eventPulse, GetCombatEventColor(combatEventType));
        }

        /// <summary>
        /// The scene owns each combatant's authored lane and scale. Animation contributes only
        /// a temporary offset, so a combat tick cannot collapse both visual lanes to the center.
        /// </summary>
        private void CaptureCombatVisualRestState()
        {
            if (heroTransform == null || enemyTransform == null) return;
            heroRestPosition = heroTransform.anchoredPosition;
            enemyRestPosition = enemyTransform.anchoredPosition;
            heroRestScale = heroTransform.localScale;
            enemyRestScale = enemyTransform.localScale;
            hasCombatVisualRestState = true;
        }

        private void PrepareDamagePopup(string combatEvent)
        {
            if (hunterDamagePopup != null) hunterDamagePopup.text = string.Empty;
            if (enemyDamagePopup != null) enemyDamagePopup.text = string.Empty;
            if (string.IsNullOrEmpty(combatEvent)) return;
            var combatEventType = host != null && host.ActiveDungeonRun != null
                ? host.ActiveDungeonRun.LastCombatEventType
                : CombatEventType.Idle;
            if (combatEventType == CombatEventType.EnemyAttack)
            {
                if (hunterDamagePopup != null) hunterDamagePopup.text = $"-{ExtractDamage(combatEvent)}";
                return;
            }
            if (combatEventType == CombatEventType.FloorAdvanced)
            {
                if (enemyDamagePopup != null) enemyDamagePopup.text = "NEXT\nFLOOR";
                return;
            }
            if (combatEventType == CombatEventType.Cleared)
            {
                if (enemyDamagePopup != null) enemyDamagePopup.text = "RIFT\nCLEAR";
                return;
            }
            if (combatEventType == CombatEventType.Failed || combatEventType == CombatEventType.Cancelled)
            {
                if (hunterDamagePopup != null) hunterDamagePopup.text = combatEventType == CombatEventType.Cancelled ? "STOP" : "DOWN";
                return;
            }
            if (enemyDamagePopup == null) return;
            if (combatEventType == CombatEventType.Execute) enemyDamagePopup.text = "EXECUTE";
            else if (combatEventType == CombatEventType.CriticalAttack) enemyDamagePopup.text = $"CRIT\n-{ExtractDamage(combatEvent)}";
            else if (combatEventType == CombatEventType.ActiveSkill) enemyDamagePopup.text = $"SKILL\n-{ExtractDamage(combatEvent)}";
            else if (combatEventType == CombatEventType.BasicAttack) enemyDamagePopup.text = $"-{ExtractDamage(combatEvent)}";
        }

        private static string ExtractDamage(string combatEvent)
        {
            var marker = combatEvent.IndexOf(" 피해", StringComparison.Ordinal);
            if (marker <= 0) return "HIT";
            var end = marker - 1;
            while (end >= 0 && !char.IsDigit(combatEvent[end])) end--;
            if (end < 0) return "HIT";
            var start = end;
            while (start > 0 && char.IsDigit(combatEvent[start - 1])) start--;
            return combatEvent.Substring(start, end - start + 1);
        }

        private static void AnimateDamagePopup(Text popup, bool shouldShow, float eventPulse, Color color)
        {
            if (popup == null) return;
            var visible = shouldShow && eventPulse > 0f && !string.IsNullOrEmpty(popup.text);
            popup.color = new Color(color.r, color.g, color.b, visible ? eventPulse : 0f);
            popup.rectTransform.anchoredPosition = new Vector2(0f, visible ? (1f - eventPulse) * 74f : 0f);
            popup.rectTransform.localScale = Vector3.one * (visible ? 1f + eventPulse * 0.24f : 1f);
        }

        private void ApplySafeArea()
        {
            if (safeArea == null) return;
            var area = Screen.safeArea;
            if (hasAppliedSafeArea && area == lastSafeArea) return;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            safeArea.anchorMin = new Vector2(area.x / Screen.width, area.y / Screen.height);
            safeArea.anchorMax = new Vector2((area.x + area.width) / Screen.width, (area.y + area.height) / Screen.height);
            lastSafeArea = area;
            hasAppliedSafeArea = true;
        }

        private Text CreateText(string name, Transform parent, string value, int size, Color color, TextAnchor alignment)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private RawImage CreateRawImage(string name, Transform parent, Texture texture)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter)).GetComponent<RawImage>();
            image.transform.SetParent(parent, false);
            image.texture = texture;
            image.color = Color.white;
            image.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            return image;
        }

        private Image CreateHealthBar(Transform parent, string name, Vector2 minimum, Vector2 maximum, Color fillColor, out Text valueText)
        {
            var bar = CreateRect(name, parent, new Color(0.01f, 0.025f, 0.07f, 0.9f));
            SetAnchored(bar, minimum, maximum);
            var fill = CreateRect("Fill", bar, fillColor).GetComponent<Image>();
            Stretch(fill.rectTransform, 4f);
            var label = CreateText("Value", bar, string.Empty, 17, Text, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 2f);
            valueText = label;
            return fill;
        }

        private Button CreateButton(string name, Transform parent, string label, Action action, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            image.transform.SetParent(parent, false);
            image.GetComponent<Image>().color = color;
            var shadow = image.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.01f, 0.05f, 0.72f);
            shadow.effectDistance = new Vector2(2f, -3f);
            var button = image.GetComponent<Button>();
            button.onClick.AddListener(() => { action?.Invoke(); RefreshAll(); });
            var text = CreateText("Label", image.transform, label, 24, Background, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 8f);
            return button;
        }

        private static RectTransform CreateRect(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.one * inset; rect.offsetMax = Vector2.one * -inset;
        }
        private static void SetStretch(RectTransform rect, float inset = 0f)
        {
            Stretch(rect, inset);
        }
        private static void SetStretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
        private static void SetAnchored(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
        private static void SetTopStretch(RectTransform rect, float top, float height, float side, float right = -1f)
        {
            rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f); rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top); rect.sizeDelta = new Vector2(-(side + (right < 0f ? side : right)), height);
        }
        private static void SetBottomStretch(RectTransform rect, float bottom, float height, float side, float right = -1f)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1f, 0f); rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottom); rect.sizeDelta = new Vector2(-(side + (right < 0f ? side : right)), height);
        }
        private static void SetBottomRight(RectTransform rect, float right, float bottom, float width, float height)
        {
            rect.anchorMin = new Vector2(1f, 0f); rect.anchorMax = new Vector2(1f, 0f); rect.pivot = new Vector2(1f, 0f); rect.anchoredPosition = new Vector2(-right, bottom); rect.sizeDelta = new Vector2(width, height);
        }
        private static void SetPreferredHeight(RectTransform rect, float height)
        {
            var element = rect.gameObject.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
        }
        private static void SetHealth(Image fill, Text valueText, int current, int maximum, Color color)
        {
            if (fill == null || valueText == null) return;
            maximum = Mathf.Max(1, maximum);
            current = Mathf.Clamp(current, 0, maximum);
            var fraction = current / (float)maximum;
            fill.gameObject.SetActive(fraction > 0f);
            if (fraction <= 0f)
            {
                valueText.text = $"HP 0/{maximum:N0}";
                return;
            }
            fill.color = color;
            var rect = fill.rectTransform;
            rect.anchorMax = new Vector2(fraction, 1f);
            rect.offsetMax = new Vector2(-4f, -4f);
            valueText.text = $"HP {current:N0}/{maximum:N0}";
        }
        private static void SetImpactFlash(Image flash, float intensity, Color color)
        {
            if (flash == null) return;
            flash.color = new Color(color.r, color.g, color.b, intensity * 0.34f);
            flash.rectTransform.localScale = Vector3.one * (1f + intensity * 0.22f);
        }
        private static void SetVisualOpacity(CanvasGroup group, float alpha)
        {
            if (group == null) return;
            group.alpha = Mathf.Clamp01(alpha);
        }
        private void AnimateProjectile(bool heroAttack, bool enemyAttack, float eventPulse, CombatEventType combatEventType)
        {
            if (combatProjectile == null) return;
            if (eventPulse <= 0f || (!heroAttack && !enemyAttack))
            {
                combatProjectile.color = Color.clear;
                combatProjectile.rectTransform.sizeDelta = Vector2.zero;
                return;
            }
            var progress = 1f - eventPulse;
            var source = heroAttack ? new Vector2(0.40f, 0.52f) : new Vector2(0.60f, 0.52f);
            var target = heroAttack ? new Vector2(0.63f, 0.52f) : new Vector2(0.37f, 0.52f);
            var position = Vector2.Lerp(source, target, progress);
            var color = GetCombatEventColor(combatEventType);
            combatProjectile.color = new Color(color.r, color.g, color.b, eventPulse);
            combatProjectile.rectTransform.anchorMin = position;
            combatProjectile.rectTransform.anchorMax = position;
            combatProjectile.rectTransform.anchoredPosition = Vector2.zero;
            var size = combatEventType == CombatEventType.ActiveSkill ? 64f : combatEventType == CombatEventType.CriticalAttack || combatEventType == CombatEventType.Execute ? 52f : 38f;
            combatProjectile.rectTransform.sizeDelta = Vector2.one * (size + eventPulse * 20f);
            combatProjectile.rectTransform.localRotation = Quaternion.Euler(0f, 0f, heroAttack ? 45f : -45f);
        }
        private static string GetCombatEventLabel(string value)
        {
            if (string.IsNullOrEmpty(value)) return "자동전투 대기 중";
            var close = value.IndexOf(']');
            return close >= 0 && close + 1 < value.Length ? value.Substring(close + 1).Trim() : value;
        }
        private static bool IsHeroAttack(CombatEventType eventType)
        {
            return eventType == CombatEventType.BasicAttack || eventType == CombatEventType.CriticalAttack ||
                   eventType == CombatEventType.ActiveSkill || eventType == CombatEventType.Execute;
        }
        private static Color GetCombatEventColor(CombatEventType eventType)
        {
            if (eventType == CombatEventType.CriticalAttack || eventType == CombatEventType.Execute) return new Color(1f, 0.84f, 0.22f, 1f);
            if (eventType == CombatEventType.EnemyAttack) return new Color(1f, 0.45f, 0.58f, 1f);
            if (eventType == CombatEventType.Cleared) return new Color(0.18f, 1f, 0.66f, 1f);
            if (eventType == CombatEventType.Failed) return new Color(1f, 0.26f, 0.38f, 1f);
            if (eventType == CombatEventType.Cancelled) return new Color(0.70f, 0.74f, 0.86f, 1f);
            if (eventType == CombatEventType.BarrierRaised || eventType == CombatEventType.EnemyEnraged) return new Color(0.78f, 0.38f, 1f, 1f);
            return Cyan;
        }

        private static string GetArenaActionLabel(DungeonRun run)
        {
            if (run == null) return "균열 입장";
            if (run.IsRunning) return "전투 중단";
            return run.Result == DungeonResult.Cleared ? "다시 도전" : "균열 입장";
        }

        private static bool IsFinishedResult(DungeonResult result)
        {
            return result == DungeonResult.Cleared || result == DungeonResult.TimeExpired ||
                   result == DungeonResult.Defeated || result == DungeonResult.Cancelled;
        }

        private void TriggerHapticFeedback(CombatEventType eventType)
        {
            if (host == null || !host.GameState.VibrationEnabled) return;
            if (eventType != CombatEventType.CriticalAttack && eventType != CombatEventType.ActiveSkill &&
                eventType != CombatEventType.Cleared && eventType != CombatEventType.Failed) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        private void PlayCombatFeedback(CombatEventType eventType)
        {
            if (host == null || !host.GameState.SoundEnabled || effectsSource == null) return;
            AudioClip clip = null;
            switch (eventType)
            {
                case CombatEventType.BasicAttack: clip = basicAttackClip; break;
                case CombatEventType.CriticalAttack:
                case CombatEventType.Execute: clip = criticalAttackClip; break;
                case CombatEventType.ActiveSkill: clip = activeSkillClip; break;
                case CombatEventType.Cleared: clip = clearClip; break;
                case CombatEventType.Failed: clip = failureClip; break;
            }
            if (clip != null) effectsSource.PlayOneShot(clip);
        }
        private static string GetReadinessLabel(DungeonReadiness readiness)
        {
            switch (readiness)
            {
                case DungeonReadiness.Dominant: return "압도적";
                case DungeonReadiness.Ready: return "공략 가능";
                case DungeonReadiness.Risky: return "생존 위험";
                default: return "시간 초과 위험";
            }
        }
        private static string FormatRecord(float seconds) => seconds <= 0f ? "기록 없음" : $"{seconds:0.0}초";
        private static string GetDungeonThreatBrief(DungeonDefinition dungeon)
        {
            switch (dungeon.Id)
            {
                case "training_rift": return "위협: 속공 감시자 다수 · 장벽 포식자";
                case "deep_rift": return "위협: 장벽 · 폭주 적 중심 / 방어 빌드 권장";
                case "calamity_rift": return "위협: 속공 · 폭주 · 수호자가 혼재 / 균형 빌드 권장";
                case "void_rift": return "위협: 장벽 수호자 다수 / 처형·고화력 빌드 권장";
                default: return "위협: 불안정한 균열 개체";
            }
        }
        private static string BuildIdleCombatSummary(DungeonRun run, ProgressionGoal goal, string rewardLine)
        {
            return $"{run.ResultMessage}\n{rewardLine}\n다음 목표 · {goal.Title}\n{goal.Description}\n하단 ‘{GetArenaActionLabel(run)}’으로 즉시 다시 도전할 수 있습니다.";
        }

        private static string GetRunRewardSummary(DungeonRun run)
        {
            var equipment = string.IsNullOrEmpty(run.EquipmentRewardName) ? "장비 획득 없음" : $"신규 장비 {run.EquipmentRewardName}";
            return $"GOLD +{run.GoldEarned:N0} · EXP +{run.ExperienceEarned:N0}\n{equipment}";
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
        private static Color GetEquipmentRarityColor(int goldCost)
        {
            if (goldCost >= 150) return new Color(0.96f, 0.35f, 0.93f, 0.95f);
            if (goldCost >= 110) return new Color(0.67f, 0.36f, 1f, 0.95f);
            if (goldCost >= 80) return new Color(0.20f, 0.70f, 1f, 0.95f);
            return new Color(0.22f, 0.90f, 0.72f, 0.9f);
        }
    }
}
