﻿using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Structs;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Glamourer.Gui;

internal partial class Interface
{
    private class ActorTab
    {
        private ObjectManager.ActorData _data       = new(string.Empty, string.Empty, Actor.Null, false, Actor.Null);
        private CharacterSave           _character  = new();
        private Actor                   _nextSelect = Actor.Null;

        public void Draw()
        {
            using var tab = ImRaii.TabItem("Actors");
            if (!tab)
                return;

            DrawActorSelector();
            if (_data.Label.Length == 0)
                return;

            ImGui.SameLine();

            if (_data.Actor.IsHuman)
                DrawActorPanel();
            else
                DrawMonsterPanel();
        }

        private void DrawActorPanel()
        {
            using var group = ImRaii.Group();
            if (DrawCustomization(_character.Customize, _character.Equipment, !_data.Modifiable))
            {
                Glamourer.RedrawManager.Set(_data.Actor.Address, _character);
                Glamourer.Penumbra.RedrawObject(_data.Actor.Character, RedrawType.Redraw, true);
            }

            if (ImGui.Button("Set Machinist Goggles"))
            {
                Glamourer.RedrawManager.ChangeEquip(_data.Actor.Address, EquipSlot.Head, new CharacterArmor(265, 1, 0));
            }
        }

        private void DrawMonsterPanel()
        {
            using var group        = ImRaii.Group();
            var       currentModel = (uint)_data.Actor.ModelId;
            var       models       = GameData.Models(Dalamud.GameData);
            var       currentData  = models.Models.TryGetValue(currentModel, out var c) ? c.FirstName : $"#{currentModel}";
            using var combo        = ImRaii.Combo("Model Id", currentData);
            if (!combo)
                return;

            foreach (var (id, data) in models.Models)
            {
                if (ImGui.Selectable(data.FirstName, id == currentModel) || id == currentModel)
                {
                    _data.Actor.SetModelId((int)id);
                    Glamourer.Penumbra.RedrawObject(_data.Actor.Character, RedrawType.Redraw, true);
                }
                ImGuiUtil.HoverTooltip(data.AllNames);
            }
        }


        private LowerString _actorFilter = LowerString.Empty;

        private void DrawActorSelector()
        {
            using var group      = ImRaii.Group();
            var       oldSpacing = ImGui.GetStyle().ItemSpacing;
            using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
                .Push(ImGuiStyleVar.FrameRounding, 0);
            ImGui.SetNextItemWidth(_actorSelectorWidth);
            LowerString.InputWithHint("##actorFilter", "Filter...", ref _actorFilter, 64);
            using (var child = ImRaii.Child("##actorSelector", new Vector2(_actorSelectorWidth, -ImGui.GetFrameHeight()), true))
            {
                if (!child)
                    return;

                _data.Actor      = Actor.Null;
                _data.GPose      = Actor.Null;
                _data.Modifiable = false;

                style.Push(ImGuiStyleVar.ItemSpacing, oldSpacing);
                var skips     = ImGuiClip.GetNecessarySkips(ImGui.GetTextLineHeight());
                var remainder = ImGuiClip.FilteredClippedDraw(ObjectManager.GetEnumerator(), skips, CheckFilter, DrawSelectable);
                ImGuiClip.DrawEndDummy(remainder, ImGui.GetTextLineHeight());
                style.Pop();
            }

            DrawSelectionButtons();
        }

        private void UpdateSelection(ObjectManager.ActorData data)
        {
            _data = data;
            _character.Load(_data.Actor);
        }

        private bool CheckFilter(ObjectManager.ActorData data)
        {
            if (_nextSelect && _nextSelect == data.Actor || data.Label == _data.Label)
                UpdateSelection(data);
            return data.Label.Contains(_actorFilter.Lower, StringComparison.OrdinalIgnoreCase);
        }

        private void DrawSelectable(ObjectManager.ActorData data)
        {
            var equal = data.Label == _data.Label;
            if (ImGui.Selectable(data.Label, equal) && !equal)
                UpdateSelection(data);
        }

        private void DrawSelectionButtons()
        {
            _nextSelect = Actor.Null;
            using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
                .Push(ImGuiStyleVar.FrameRounding, 0);
            var buttonWidth = new Vector2(_actorSelectorWidth / 2, 0);
            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.UserCircle.ToIconString(), buttonWidth
                    , "Select the local player character.", !ObjectManager.Player, true))
                _nextSelect = _inGPose ? ObjectManager.GPosePlayer : ObjectManager.Player;
            ImGui.SameLine();
            Actor targetActor = Dalamud.Targets.Target?.Address;
            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.HandPointer.ToIconString(), buttonWidth,
                    "Select the current target, if it is in the list.", _inGPose || !targetActor, true))
                _nextSelect = targetActor;
        }
    }

    private readonly ActorTab _actorTab = new();
}

//internal partial class Interface
//{
//    private readonly CharacterSave _currentSave   = new();
//    private          string        _newDesignName = string.Empty;
//    private          bool          _keyboardFocus;
//    private          bool          _holdShift;
//    private          bool          _holdCtrl;
//    private const    string        DesignNamePopupLabel = "Save Design As...";
//    private const    uint          RedHeaderColor       = 0xFF1818C0;
//    private const    uint          GreenHeaderColor     = 0xFF18C018;
//
//    private void DrawPlayerHeader()
//    {
//        var color       = _player == null ? RedHeaderColor : GreenHeaderColor;
//        var buttonColor = ImGui.GetColorU32(ImGuiCol.FrameBg);
//        using var c = ImRaii.PushColor(ImGuiCol.Text, color)
//            .Push(ImGuiCol.Button,        buttonColor)
//            .Push(ImGuiCol.ButtonHovered, buttonColor)
//            .Push(ImGuiCol.ButtonActive,  buttonColor);
//        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
//            .Push(ImGuiStyleVar.FrameRounding, 0);
//        ImGui.Button($"{_currentLabel}##playerHeader", -Vector2.UnitX * 0.0001f);
//    }
//
//    private static void DrawCopyClipboardButton(CharacterSave save)
//    {
//        ImGui.PushFont(UiBuilder.IconFont);
//        if (ImGui.Button(FontAwesomeIcon.Clipboard.ToIconString()))
//            ImGui.SetClipboardText(save.ToBase64());
//        ImGui.PopFont();
//        ImGuiUtil.HoverTooltip("Copy customization code to clipboard.");
//    }
//
//    private static unsafe void ConditionalApply(CharacterSave save, FFXIVClientStructs.FFXIV.Client.Game.Character.Character* player)
//    {
//        //if (ImGui.GetIO().KeyShift)
//        //    save.ApplyOnlyCustomizations(player);
//        //else if (ImGui.GetIO().KeyCtrl)
//        //    save.ApplyOnlyEquipment(player);
//        //else
//        //    save.Apply(player);
//    }
//
//    private static unsafe void ConditionalApply(CharacterSave save, Character player)
//        => ConditionalApply(save, (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)player.Address);
//
//    private static CharacterSave ConditionalCopy(CharacterSave save, bool shift, bool ctrl)
//    {
//        var copy = save.Copy();
//        if (shift)
//        {
//            copy.Load(new CharacterEquipment());
//            copy.SetHatState    = false;
//            copy.SetVisorState  = false;
//            copy.SetWeaponState = false;
//            copy.WriteEquipment = CharacterEquipMask.None;
//        }
//        else if (ctrl)
//        {
//            copy.Load(CharacterCustomization.Default);
//            copy.SetHatState         = false;
//            copy.SetVisorState       = false;
//            copy.SetWeaponState      = false;
//            copy.WriteCustomizations = false;
//        }
//
//        return copy;
//    }
//
//    private bool DrawApplyClipboardButton()
//    {
//        ImGui.PushFont(UiBuilder.IconFont);
//        var applyButton = ImGui.Button(FontAwesomeIcon.Paste.ToIconString()) && _player != null;
//        ImGui.PopFont();
//        ImGuiUtil.HoverTooltip(
//            "Apply customization code from clipboard.\nHold Shift to apply only customizations.\nHold Control to apply only equipment.");
//
//        if (!applyButton)
//            return false;
//
//        try
//        {
//            var text = ImGui.GetClipboardText();
//            if (!text.Any())
//                return false;
//
//            var save = CharacterSave.FromString(text);
//            ConditionalApply(save, _player!);
//        }
//        catch (Exception e)
//        {
//            PluginLog.Information($"{e}");
//            return false;
//        }
//
//        return true;
//    }
//
//    private void DrawSaveDesignButton()
//    {
//        ImGui.PushFont(UiBuilder.IconFont);
//        if (ImGui.Button(FontAwesomeIcon.Save.ToIconString()))
//            OpenDesignNamePopup(DesignNameUse.SaveCurrent);
//
//        ImGui.PopFont();
//        ImGuiUtil.HoverTooltip("Save the current design.\nHold Shift to save only customizations.\nHold Control to save only equipment.");
//
//        DrawDesignNamePopup(DesignNameUse.SaveCurrent);
//    }
//
//    private void DrawTargetPlayerButton()
//    {
//        if (ImGui.Button("Target Player"))
//            Dalamud.Targets.SetTarget(_player);
//    }
//
//    private unsafe void DrawApplyToPlayerButton(CharacterSave save)
//    {
//        if (!ImGui.Button("Apply to Self"))
//            return;
//
//        var player = _inGPose
//            ? (Character?)Dalamud.Objects[GPoseObjectId]
//            : Dalamud.ClientState.LocalPlayer;
//        var fallback = _inGPose ? Dalamud.ClientState.LocalPlayer : null;
//        if (player == null)
//            return;
//
//        ConditionalApply(save, (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)player.Address);
//        if (_inGPose)
//            ConditionalApply(save, (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)fallback!.Address);
//        Glamourer.Penumbra.UpdateCharacters(player, fallback);
//    }
//
//
//    private static unsafe FFXIVClientStructs.FFXIV.Client.Game.Character.Character* TransformToCustomizable(
//        FFXIVClientStructs.FFXIV.Client.Game.Character.Character* actor)
//    {
//        if (actor == null)
//            return null;
//
//        if (actor->ModelCharaId == 0)
//            return actor;
//
//        actor->ModelCharaId = 0;
//        CharacterCustomization.Default.Write(actor);
//        return actor;
//    }
//
//    private static unsafe FFXIVClientStructs.FFXIV.Client.Game.Character.Character* Convert(GameObject? actor)
//    {
//        return actor switch
//        {
//            null              => null,
//            PlayerCharacter p => (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)p.Address,
//            BattleChara b     => (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)b.Address,
//            _ => actor.ObjectKind switch
//            {
//                ObjectKind.BattleNpc => (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)actor.Address,
//                ObjectKind.Companion => (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)actor.Address,
//                ObjectKind.Retainer  => (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)actor.Address,
//                ObjectKind.EventNpc  => (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)actor.Address,
//                _                    => null,
//            },
//        };
//    }
//
//    private unsafe void DrawApplyToTargetButton(CharacterSave save)
//    {
//        if (!ImGui.Button("Apply to Target"))
//            return;
//
//        var player = TransformToCustomizable(Convert(Dalamud.Targets.Target));
//        if (player == null)
//            return;
//
//        var fallBackCharacter = _gPoseActors.TryGetValue(new Utf8String(player->GameObject.Name).ToString(), out var f) ? f : null;
//        ConditionalApply(save, player);
//        if (fallBackCharacter != null)
//            ConditionalApply(save, fallBackCharacter!);
//        //Glamourer.Penumbra.UpdateCharacters(player, fallBackCharacter);
//    }
//
//    private void DrawRevertButton()
//    {
//        if (!ImGuiUtil.DrawDisabledButton("Revert", Vector2.Zero, string.Empty, _player == null))
//            return;
//
//        Glamourer.RevertableDesigns.Revert(_player!);
//        var fallBackCharacter = _gPoseActors.TryGetValue(_player!.Name.ToString(), out var f) ? f : null;
//        if (fallBackCharacter != null)
//            Glamourer.RevertableDesigns.Revert(fallBackCharacter);
//        Glamourer.Penumbra.UpdateCharacters(_player, fallBackCharacter);
//    }
//
//    private void SaveNewDesign(CharacterSave save)
//    {
//        try
//        {
//            var (folder, name) = _designs.FileSystem.CreateAllFolders(_newDesignName);
//            if (!name.Any())
//                return;
//
//            var newDesign = new Design(folder, name) { Data = save };
//            folder.AddChild(newDesign);
//            _designs.Designs[newDesign.FullName()] = save;
//            _designs.SaveToFile();
//        }
//        catch (Exception e)
//        {
//            PluginLog.Error($"Could not save new design {_newDesignName}:\n{e}");
//        }
//    }
//
//    private unsafe void DrawMonsterPanel()
//    {
//        if (DrawApplyClipboardButton())
//            Glamourer.Penumbra.UpdateCharacters(_player!);
//
//        ImGui.SameLine();
//        if (ImGui.Button("Convert to Character"))
//        {
//            //TransformToCustomizable(_player);
//            _currentLabel = _currentLabel.Replace("(Monster)", "(NPC)");
//            Glamourer.Penumbra.UpdateCharacters(_player!);
//        }
//
//        if (!_inGPose)
//        {
//            ImGui.SameLine();
//            DrawTargetPlayerButton();
//        }
//
//        var       currentModel = ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)_player!.Address)->ModelCharaId;
//        using var combo        = ImRaii.Combo("Model Id", currentModel.ToString());
//        if (!combo)
//            return;
//
//        foreach (var (id, _) in _models.Skip(1))
//        {
//            if (!ImGui.Selectable($"{id:D6}##models", id == currentModel) || id == currentModel)
//                continue;
//
//            ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)_player!.Address)->ModelCharaId = 0;
//            Glamourer.Penumbra.UpdateCharacters(_player!);
//        }
//    }
//
//    private void DrawPlayerPanel()
//    {
//        DrawCopyClipboardButton(_currentSave);
//        ImGui.SameLine();
//        var changes = !_currentSave.WriteProtected && DrawApplyClipboardButton();
//        ImGui.SameLine();
//        DrawSaveDesignButton();
//        ImGui.SameLine();
//        DrawApplyToPlayerButton(_currentSave);
//        if (!_inGPose)
//        {
//            ImGui.SameLine();
//            DrawApplyToTargetButton(_currentSave);
//            if (_player != null && !_currentSave.WriteProtected)
//            {
//                ImGui.SameLine();
//                DrawTargetPlayerButton();
//            }
//        }
//
//        var data = _currentSave;
//        if (!_currentSave.WriteProtected)
//        {
//            ImGui.SameLine();
//            DrawRevertButton();
//        }
//        else
//        {
//            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.8f);
//            data = data.Copy();
//        }
//
//        if (DrawCustomization(ref data.Customizations) && _player != null)
//        {
//            Glamourer.RevertableDesigns.Add(_player);
//            _currentSave.Customizations.Write(_player.Address);
//            changes = true;
//        }
//
//        changes |= DrawEquip(data.Equipment);
//        changes |= DrawMiscellaneous(data, _player);
//
//        if (_player != null && changes)
//            Glamourer.Penumbra.UpdateCharacters(_player);
//        if (_currentSave.WriteProtected)
//            ImGui.PopStyleVar();
//    }
//
//    private unsafe void DrawActorPanel()
//    {
//        using var group = ImRaii.Group();
//        DrawPlayerHeader();
//        using var child = ImRaii.Child("##playerData", -Vector2.One, true);
//        if (!child)
//            return;
//
//        if (_player == null || ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)_player.Address)->ModelCharaId == 0)
//            DrawPlayerPanel();
//        else
//            DrawMonsterPanel();
//    }
//}
