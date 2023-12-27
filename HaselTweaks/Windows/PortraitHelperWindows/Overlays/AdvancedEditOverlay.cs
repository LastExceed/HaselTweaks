using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Utils;
using HaselTweaks.Structs;
using ImGuiNET;
using ActionTimelineSlots = FFXIVClientStructs.FFXIV.Client.Game.ActionTimelineSlots;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

public unsafe class AdvancedEditOverlay : Overlay
{
    protected override OverlayType Type => OverlayType.LeftPane;

    private const float THIRTY_FPS = 30f;

    private readonly AgentBannerEditor* Agent = GetAgent<AgentBannerEditor>();
    private AddonBannerEditor* Addon;

    public AdvancedEditOverlay() : base(t("PortraitHelperWindows.AdvancedEditOverlay.Title"))
    {
    }

    private AgentBannerEditorState* EditorState => Agent->EditorState;
    private CharaViewPortrait* CharaView => EditorState != null ? EditorState->CharaView : null;
    private Character* Character => CharaView != null ? CharaView->Base.GetCharacter() : null;
    private HaselCharacter* HaselCharacter => (HaselCharacter*)Character;

    public override void Draw()
    {
        base.Draw();

        Addon = GetAddon<AddonBannerEditor>(AgentId.BannerEditor);

        if (Addon == null || EditorState == null || CharaView == null || Character == null)
            return;

        if (!IsWindow)
        {
            ImGuiUtils.DrawSection(
                t("PortraitHelperWindows.AdvancedEditOverlay.Title.Inner"),
                PushDown: false,
                RespectUiTheme: true,
                UIColor: 2);
        }

        using (var table = ImRaii.Table("##Table", 2))
        {
            if (table)
            {
                ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 150 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                DrawCameraOrientation();
                DrawCameraPosition();
                DrawZoomRotation();
                DrawEyeDirection();
                DrawHeadDirection();
                DrawAnimationControl();
            }
        }

        using (ImRaii.PushColor(ImGuiCol.Text, (uint)(Colors.IsLightTheme && !IsWindow ? Colors.GetUIColor(3) : Colors.Grey)))
        {
            ImGui.TextUnformatted(t("PortraitHelperWindows.AdvancedEditOverlay.Note.Label"));
            ImGuiHelpers.SafeTextWrapped(t("PortraitHelperWindows.AdvancedEditOverlay.Note.Text"));
        }
    }

    private void DrawCameraOrientation()
    {
        using (ImRaii.PushId("CameraOrientation"))
        {
            var yaw = CharaView->CameraYaw;
            var pitch = CharaView->CameraPitch;
            var distance = CharaView->CameraDistance;

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.CameraYaw.Label"));
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(t("PortraitHelperWindows.Setting.CameraYaw.Tooltip"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            if (ImGui.DragFloat($"##DragFloatYaw", ref yaw, 0.001f, 0f, 0f, "%.3f", ImGuiSliderFlags.NoInput))
            {
                var scale = 100f;

                CharaView->SetCameraYawAndPitch(
                    scale * (yaw - CharaView->CameraYaw),
                    scale * (pitch - CharaView->CameraPitch)
                );

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.CameraPitch.Label"));
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(t("PortraitHelperWindows.Setting.CameraPitch.Tooltip"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            if (ImGui.DragFloat($"##DragFloatPitch", ref pitch, 0.001f, 0f, 0f, "%.3f", ImGuiSliderFlags.NoInput))
            {
                var scale = 100f;

                CharaView->SetCameraYawAndPitch(
                    scale * (yaw - CharaView->CameraYaw),
                    scale * (pitch - CharaView->CameraPitch)
                );

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.CameraDistance.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            if (ImGui.DragFloat($"##DragFloatDistance", ref distance, 0.001f, 0.5f, 2f))
            {
                var scale = 100f;

                CharaView->SetCameraDistance(
                    scale * (distance - CharaView->CameraDistance)
                );

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }
        }
    }

    private void DrawCameraPosition()
    {
        using (ImRaii.PushId("CameraPosition"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.CameraTarget.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var pos = new Vector2(
                CharaView->CameraTarget.X,
                CharaView->CameraTarget.Y
            );
            if (ImGui.DragFloat2($"##DragFloat2", ref pos, 0.001f, 0f, 0f, "%.3f", ImGuiSliderFlags.NoInput))
            {
                var scale = 1000f;
                CharaView->SetCameraXAndY(
                    scale * (pos.X - CharaView->CameraTarget.X),
                    scale * (pos.Y - CharaView->CameraTarget.Y)
                );

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }
        }
    }

    private void DrawZoomRotation()
    {
        using (ImRaii.PushId("ZoomRotation"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.ZoomRotation.Label"));

            ImGui.TableNextColumn();

            var itemWidth = (ImGui.GetColumnWidth() - ImGui.GetStyle().ItemInnerSpacing.X) / 2f - 0.5f;
            ImGui.SetNextItemWidth(itemWidth);

            var zoom = (int)CharaView->CameraZoom;
            if (ImGui.DragInt($"##DragFloatZoom", ref zoom, 1, 0, 200))
            {
                CharaView->SetCameraZoom((byte)zoom);
                Addon->CameraZoomSlider->SetValue(zoom);

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }

            ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
            ImGui.SetNextItemWidth(itemWidth + 0.5f);

            var rotation = (int)CharaView->ImageRotation;
            if (ImGui.DragInt($"##DragFloatRotation", ref rotation, 1, -90, 90))
            {
                CharaView->ImageRotation = (short)rotation;
                Addon->ImageRotation->SetValue(rotation);

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }
        }
    }

    private void DrawEyeDirection()
    {
        using (ImRaii.PushId("EyeDirection"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.EyeDirection.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var eyeDirection = new Vector2(
                HaselCharacter->Gaze.BannerEyesDirection.X,
                HaselCharacter->Gaze.BannerEyesDirection.Y
            );

            if (ImGui.DragFloat2($"##DragFloat2", ref eyeDirection, 0.001f))
            {
                CharaView->SetEyeDirection(eyeDirection.X, eyeDirection.Y);

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }
        }
    }

    private void DrawHeadDirection()
    {
        using (ImRaii.PushId("HeadDirection"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.HeadDirection.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var headDirection = new Vector2(
                HaselCharacter->Gaze.BannerHeadDirection.X,
                HaselCharacter->Gaze.BannerHeadDirection.Y
            );

            if (ImGui.DragFloat2($"##DragFloat2", ref headDirection, 0.001f))
            {
                CharaView->SetHeadDirection(headDirection.X, headDirection.Y);

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }
        }
    }

    private void DrawAnimationControl()
    {
        var characterBase = (CharacterBase*)Character->GameObject.DrawObject;
        if (characterBase == null || characterBase->Skeleton == null || characterBase->Skeleton->PartialSkeletonCount == 0 || characterBase->Skeleton->PartialSkeletons == null)
            return;

        var partialSkeleton = characterBase->Skeleton->PartialSkeletons[0].GetHavokAnimatedSkeleton(0);
        if (partialSkeleton == null || partialSkeleton->AnimationControls.Length == 0 || partialSkeleton->AnimationControls[0].Value == null)
            return;

        var animationControl = partialSkeleton->AnimationControls[0].Value;
        if (animationControl == null || animationControl->hkaAnimationControl.Binding.ptr == null || animationControl->hkaAnimationControl.Binding.ptr->Animation.ptr == null)
            return;

        var actionTimelineDriver = (HaselActionTimelineDriver*)(nint)(&Character->ActionTimelineManager.Driver);
        var baseTimelineSlot = (HaselSchedulerTimelineSlot*)actionTimelineDriver->SchedulerTimelineSlotsSpan[(int)ActionTimelineSlots.Base];
        if (baseTimelineSlot == null || baseTimelineSlot->Ptr == null)
            return;
        var baseTimeline = baseTimelineSlot->Ptr;

        var duration = animationControl->hkaAnimationControl.Binding.ptr->Animation.ptr->Duration - 0.5f;
        var frameCount = THIRTY_FPS * duration;

        using (ImRaii.PushId("AnimationTimestamp"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.AnimationTimestamp.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var timestamp = baseTimeline->CurrentTimestamp;
            if (ImGui.DragFloat("##DragFloat", ref timestamp, 0.01f, 0, frameCount, $"%.2f / {frameCount}"))
            {
                var delta = timestamp - baseTimeline->CurrentTimestamp;
                if (delta < 0)
                {
                    var actionTimelineManager = (HaselActionTimelineManager*)(nint)(&Character->ActionTimelineManager);
                    actionTimelineManager->BannerRequestedTimestamp = timestamp;
                    actionTimelineManager->BannerFlags2 |= 2;
                }
                else
                {
                    baseTimeline->Update(delta, 0);
                }

                CharaView->Base.ToggleAnimationPlayback(true);
                Addon->PlayAnimationCheckbox->SetValue(false);

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }
        }
    }
}
