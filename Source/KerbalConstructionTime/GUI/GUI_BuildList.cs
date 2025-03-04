﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalConstructionTime
{
    public static partial class KCT_GUI
    {
        public enum VesselPadStatus { InStorage, RollingOut, RolledOut, RollingBack, Recovering };

        public static Rect BuildListWindowPosition = new Rect(Screen.width - 400, 40, 400, 1);
        public static Rect EditorBuildListWindowPosition = new Rect(Screen.width - 400, 40, 400, 1);

        private static List<string> _launchSites = new List<string>();
        private static int _mouseOnRolloutButton = -1;
        private static int _mouseOnAirlaunchButton = -1;
        private static bool _combineVabAndSph, _isVABSelected, _isSPHSelected, _isTechSelected;
        private static Vector2 _launchSiteScrollView;
        private static Guid _selectedVesselId = new Guid();
        private static double _costOfNewLP = int.MinValue;

        private static GUIStyle _redText, _yellowText, _greenText, _yellowButton, _redButton, _greenButton;
        private static GUIContent _settingsTexture, _planeTexture, _rocketTexture;
        private const int _width1 = 120;
        private const int _width2 = 100;
        private const int _butW = 20;

        private static bool IsRolloutEnabled => PresetManager.Instance.ActivePreset.GeneralSettings.ReconditioningTimes &&
                                                PresetManager.Instance.ActivePreset.TimeSettings.RolloutReconSplit > 0;

        public static void SelectList(string list)
        {
            BuildListWindowPosition.height = EditorBuildListWindowPosition.height = 1;
            bool isCommon = PresetManager.Instance?.ActivePreset?.GeneralSettings.CommonBuildLine ?? false;
            switch (list)
            {
                case "Combined":
                    _combineVabAndSph = isCommon && !_combineVabAndSph;
                    _isVABSelected = false;
                    _isSPHSelected = false;
                    _isTechSelected = false;
                    break;
                case "VAB":
                    _combineVabAndSph = isCommon;
                    _isVABSelected = !isCommon && !_isVABSelected;
                    _isSPHSelected = false;
                    _isTechSelected = false;
                    break;
                case "SPH":
                    _combineVabAndSph = isCommon;
                    _isVABSelected = false;
                    _isSPHSelected = !isCommon && !_isSPHSelected;
                    _isTechSelected = false;
                    break;
                case "Tech":
                    _combineVabAndSph = false;
                    _isVABSelected = false;
                    _isSPHSelected = false;
                    _isTechSelected = !_isTechSelected;
                    break;
                default:
                    _combineVabAndSph = _isTechSelected = _isVABSelected = _isSPHSelected = false;
                    break;
            }
        }

        public static void ResetBLWindow(bool deselectList = true)
        {
            BuildListWindowPosition.height = EditorBuildListWindowPosition.height = 1;
            BuildListWindowPosition.width = EditorBuildListWindowPosition.width = 500;
            if (deselectList)
                SelectList("None");
        }

        public static void InitBuildListVars()
        {
            KCTDebug.Log("InitBuildListVars");
            _redText = new GUIStyle(GUI.skin.label);
            _redText.normal.textColor = Color.red;
            _yellowText = new GUIStyle(GUI.skin.label);
            _yellowText.normal.textColor = Color.yellow;
            _greenText = new GUIStyle(GUI.skin.label);
            _greenText.normal.textColor = Color.green;

            _yellowButton = new GUIStyle(GUI.skin.button);
            _yellowButton.normal.textColor = Color.yellow;
            _yellowButton.hover.textColor = Color.yellow;
            _yellowButton.active.textColor = Color.yellow;
            _redButton = new GUIStyle(GUI.skin.button);
            _redButton.normal.textColor = Color.red;
            _redButton.hover.textColor = Color.red;
            _redButton.active.textColor = Color.red;

            _greenButton = new GUIStyle(GUI.skin.button);
            _greenButton.normal.textColor = Color.green;
            _greenButton.hover.textColor = Color.green;
            _greenButton.active.textColor = Color.green;

            _settingsTexture = new GUIContent(GameDatabase.Instance.GetTexture("RP-0/Resources/KCT_settings16", false));
            _planeTexture = new GUIContent(GameDatabase.Instance.GetTexture("RP-0/Resources/KCT_flight16", false));
            _rocketTexture = new GUIContent(GameDatabase.Instance.GetTexture("RP-0/Resources/KCT_rocket16", false));
        }

        public static void DrawBuildListWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Next:", _windowSkin.label);
            IKCTBuildItem buildItem = Utilities.GetNextThingToFinish();
            if (buildItem != null)
            {
                string txt = buildItem.GetItemName(), locTxt = "VAB";
                if (buildItem.GetListType() == BuildListVessel.ListType.Reconditioning)
                {
                    ReconRollout reconRoll = buildItem as ReconRollout;
                    if (reconRoll.RRType == ReconRollout.RolloutReconType.Reconditioning)
                    {
                        txt = "Reconditioning";
                        locTxt = reconRoll.LaunchPadID;
                    }
                    else if (reconRoll.RRType == ReconRollout.RolloutReconType.Rollout)
                    {
                        BuildListVessel associated = reconRoll.KSC.VABWarehouse.FirstOrDefault(blv => blv.Id.ToString() == reconRoll.AssociatedID);
                        txt = $"{associated.ShipName} Rollout";
                        locTxt = reconRoll.LaunchPadID;
                    }
                    else if (reconRoll.RRType == ReconRollout.RolloutReconType.Rollback)
                    {
                        BuildListVessel associated = reconRoll.KSC.VABWarehouse.FirstOrDefault(blv => blv.Id.ToString() == reconRoll.AssociatedID);
                        txt = $"{associated.ShipName} Rollback";
                        locTxt = reconRoll.LaunchPadID;
                    }
                    else
                    {
                        locTxt = "VAB";
                    }
                }
                else if (buildItem.GetListType() == BuildListVessel.ListType.VAB)
                {
                    locTxt = "VAB";
                }
                else if (buildItem.GetListType() == BuildListVessel.ListType.SPH)
                {
                    locTxt = "SPH";
                }
                else if (buildItem.GetListType() == BuildListVessel.ListType.TechNode)
                {
                    locTxt = "Tech";
                }
                else if (buildItem.GetListType() == BuildListVessel.ListType.KSC)
                {
                    locTxt = "KSC";
                }

                GUILayout.Label(txt);
                GUILayout.Label(locTxt, _windowSkin.label);
                GUILayout.Label(MagiCore.Utilities.GetColonFormattedTime(buildItem.GetTimeLeft()));

                if (!HighLogic.LoadedSceneIsEditor && TimeWarp.CurrentRateIndex == 0 && GUILayout.Button($"Warp to{Environment.NewLine}Complete"))
                {
                    KCTWarpController.Create(buildItem);
                }
                else if (!HighLogic.LoadedSceneIsEditor && TimeWarp.CurrentRateIndex > 0 && GUILayout.Button($"Stop{Environment.NewLine}Warp"))
                {
                    KCTWarpController.Instance?.StopWarp();
                    TimeWarp.SetRate(0, true);  // If the controller doesn't exist, stop warp anyway.
                }

                if (KCTGameStates.Settings.AutoKACAlarms && KACWrapper.APIReady && buildItem.GetTimeLeft() > 30)    //don't check if less than 30 seconds to completion. Might fix errors people are seeing
                {
                    double UT = Utilities.GetUT();
                    if (!Utilities.IsApproximatelyEqual(KCTGameStates.KACAlarmUT - UT, buildItem.GetTimeLeft()))
                    {
                        KCTDebug.Log("KAC Alarm being created!");
                        KCTGameStates.KACAlarmUT = buildItem.GetTimeLeft() + UT;
                        KACWrapper.KACAPI.KACAlarm alarm = KACWrapper.KAC.Alarms.FirstOrDefault(a => a.ID == KCTGameStates.KACAlarmId);
                        if (alarm == null)
                        {
                            alarm = KACWrapper.KAC.Alarms.FirstOrDefault(a => a.Name.StartsWith("KCT: "));
                        }
                        if (alarm != null)
                        {
                            KCTDebug.Log("Removing existing alarm");
                            KACWrapper.KAC.DeleteAlarm(alarm.ID);
                        }
                        txt = "KCT: ";
                        if (buildItem.GetListType() == BuildListVessel.ListType.Reconditioning)
                        {
                            ReconRollout reconRoll = buildItem as ReconRollout;
                            if (reconRoll.RRType == ReconRollout.RolloutReconType.Reconditioning)
                            {
                                txt += $"{reconRoll.LaunchPadID} Reconditioning";
                            }
                            else if (reconRoll.RRType == ReconRollout.RolloutReconType.Rollout)
                            {
                                BuildListVessel associated = reconRoll.KSC.VABWarehouse.FirstOrDefault(blv => blv.Id.ToString() == reconRoll.AssociatedID);
                                txt += $"{associated.ShipName} rollout at {reconRoll.LaunchPadID}";
                            }
                            else if (reconRoll.RRType == ReconRollout.RolloutReconType.Rollback)
                            {
                                BuildListVessel associated = reconRoll.KSC.VABWarehouse.FirstOrDefault(blv => blv.Id.ToString() == reconRoll.AssociatedID);
                                txt += $"{associated.ShipName} rollback at {reconRoll.LaunchPadID}";
                            }
                            else
                            {
                                txt += $"{buildItem.GetItemName()} Complete";
                            }
                        }
                        else
                            txt += $"{buildItem.GetItemName()} Complete";
                        KCTGameStates.KACAlarmId = KACWrapper.KAC.CreateAlarm(KACWrapper.KACAPI.AlarmTypeEnum.Raw, txt, KCTGameStates.KACAlarmUT);
                        KCTDebug.Log($"Alarm created with ID: {KCTGameStates.KACAlarmId}");
                    }
                }
            }
            else
            {
                GUILayout.Label("No Active Projects");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (PresetManager.Instance.ActivePreset.GeneralSettings.CommonBuildLine)
            {
                bool commonSelectedNew = GUILayout.Toggle(_combineVabAndSph, "Vessels", GUI.skin.button);
                if (commonSelectedNew != _combineVabAndSph)
                    SelectList("Combined");
            }
            else
            {
                bool VABSelectedNew = GUILayout.Toggle(_isVABSelected, "VAB", GUI.skin.button);
                bool SPHSelectedNew = GUILayout.Toggle(_isSPHSelected, "SPH", GUI.skin.button);
                if (VABSelectedNew != _isVABSelected)
                    SelectList("VAB");
                else if (SPHSelectedNew != _isSPHSelected)
                    SelectList("SPH");
            }

            bool techSelectedNew = false;
            if (Utilities.CurrentGameHasScience())
                techSelectedNew = GUILayout.Toggle(_isTechSelected, "Tech", GUI.skin.button);

            if (techSelectedNew != _isTechSelected)
                SelectList("Tech");
            if (GUILayout.Button("Plans"))
            {
                GUIStates.ShowBuildPlansWindow = !GUIStates.ShowBuildPlansWindow;
            }
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                if (GUILayout.Button("Upgrades", AvailablePoints > 0 ? _greenButton : GUI.skin.button))
                {
                    GUIStates.ShowUpgradeWindow = true;
                    GUIStates.ShowBuildList = false;
                    GUIStates.ShowBLPlus = false;
                }

                if (GUILayout.Button(_settingsTexture, GUILayout.ExpandWidth(false)))
                {
                    GUIStates.ShowBuildList = false;
                    GUIStates.ShowBLPlus = false;
                    ShowSettings();
                }
            }
            GUILayout.EndHorizontal();

            if (_combineVabAndSph)
            {
                RenderCombinedBuildList();
            }
            else if(_isVABSelected)
            {
                RenderVABBuildList();
            }
            else if (_isSPHSelected)
            {
                RenderSPHBuildList();
            }
            else if (_isTechSelected)
            {
                RenderTechList();
            }

            GUILayout.EndVertical();

            if (!Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
                GUI.DragWindow();

            ref Rect pos = ref (HighLogic.LoadedSceneIsEditor ? ref EditorBuildListWindowPosition : ref BuildListWindowPosition);
            ClampWindow(ref pos, strict: true);
        }

        private static void RenderVABBuildList()
        {
            List<BuildListVessel> buildList = KCTGameStates.ActiveKSC.VABList;

            RenderBuildlistHeader();
            RenderRollouts();

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(250));
            {
                RenderVesselsBeingBuilt(buildList);
                RenderVabWarehouse();
            }
            GUILayout.EndScrollView();

            RenderLaunchPadControls();
        }

        private static void RenderSPHBuildList()
        {
            List<BuildListVessel> buildList = KCTGameStates.ActiveKSC.SPHList;

            RenderBuildlistHeader();

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(250));
            {
                RenderVesselsBeingBuilt(buildList);
                RenderSphWarehouse();
            }
            GUILayout.EndScrollView();
        }

        private static void RenderTechList()
        {
            List<FacilityUpgrade> KSCList = KCTGameStates.ActiveKSC.KSCTech;
            KCTObservableList<TechItem> techList = KCTGameStates.TechList;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:");
            GUILayout.Label("Progress:", GUILayout.Width(_width1 / 2));
            GUILayout.Label("Time Left:", GUILayout.Width(_width1));
            GUILayout.Space(70);
            GUILayout.EndHorizontal();
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(250));

            if (Utilities.CurrentGameIsCareer())
            {
                if (KSCList.Count == 0)
                    GUILayout.Label("No KSC upgrade projects are currently underway.");
                foreach (FacilityUpgrade KCTTech in KSCList)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(KCTTech.GetItemName());
                    GUILayout.Label($"{Math.Round(100 * KCTTech.Progress / KCTTech.BP, 2)} %", GUILayout.Width(_width1 / 2));
                    GUILayout.Label(MagiCore.Utilities.GetColonFormattedTime(KCTTech.GetTimeLeft()), GUILayout.Width(_width1));
                    if (!HighLogic.LoadedSceneIsEditor && GUILayout.Button("Warp", GUILayout.Width(70)))
                    {
                        KCTWarpController.Create(KCTTech);
                    }
                    else if (HighLogic.LoadedSceneIsEditor)
                        GUILayout.Space(70);
                    GUILayout.EndHorizontal();
                }
            }

            if (techList.Count == 0)
                GUILayout.Label("No tech nodes are being researched!\nBegin research by unlocking tech in the R&D building.");
            bool forceRecheck = false;
            int cancelID = -1;
            for (int i = 0; i < techList.Count; i++)
            {
                TechItem t = techList[i];
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("X", GUILayout.Width(_butW)))
                {
                    forceRecheck = true;
                    cancelID = i;
                    DialogGUIBase[] options = new DialogGUIBase[2];
                    options[0] = new DialogGUIButton("Yes", () => { CancelTechNode(cancelID); });
                    options[1] = new DialogGUIButton("No", RemoveInputLocks);
                    MultiOptionDialog diag = new MultiOptionDialog("cancelNodePopup", $"Are you sure you want to stop researching {t.TechName}?\n\nThis will also cancel any dependent techs.", "Cancel Node?", null, 300, options);
                    PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), diag, false, HighLogic.UISkin);
                }

                // Can move up if item above is not a parent.
                List<string> parentList = KerbalConstructionTimeData.techNameToParents[t.TechID];
                bool canMoveUp = i > 0 && (parentList == null || !parentList.Contains(techList[i - 1].TechID));

                // Can move down if item below is not a child.
                List<string> nextParentList = i < techList.Count - 1 ? KerbalConstructionTimeData.techNameToParents[techList[i + 1].TechID] : null;
                bool canMoveDown = nextParentList == null || !nextParentList.Contains(t.TechID);

                if (i > 0 && t.BuildRate != techList[0].BuildRate)
                {
                    GUI.enabled = canMoveUp;
                    if (i > 0 && GUILayout.Button("^", GUILayout.Width(_butW)))
                    {
                        techList.RemoveAt(i);
                        if (GameSettings.MODIFIER_KEY.GetKey())
                        {
                            // Find furthest postion tech can be moved to.
                            int newLocation = i - 1;
                            while (newLocation >= 0)
                            {
                                if (parentList != null && parentList.Contains(techList[newLocation].TechID))
                                    break;
                                --newLocation;
                            }
                            ++newLocation;

                            techList.Insert(newLocation, t);
                        }
                        else
                        {
                            techList.Insert(i - 1, t);
                        }
                        forceRecheck = true;
                    }
                    GUI.enabled = true;
                }

                if ((i == 0 && t.BuildRate != techList[techList.Count - 1].BuildRate) || t.BuildRate != techList[techList.Count - 1].BuildRate)
                {
                    GUI.enabled = canMoveDown;
                    if (i < techList.Count - 1 && GUILayout.Button("v", GUILayout.Width(_butW)))
                    {
                        techList.RemoveAt(i);
                        if (GameSettings.MODIFIER_KEY.GetKey())
                        {
                            // Find furthest postion tech can be moved to.
                            int newLocation = i + 1;
                            while (newLocation < techList.Count)
                            {
                                nextParentList = KerbalConstructionTimeData.techNameToParents[techList[newLocation].TechID];
                                if (nextParentList != null && nextParentList.Contains(t.TechID))
                                    break;
                                ++newLocation;
                            }

                            techList.Insert(newLocation, t);
                        }
                        else
                        {
                            techList.Insert(i + 1, t);
                        }
                        forceRecheck = true;
                    }
                    GUI.enabled = true;
                }

                if (forceRecheck)
                {
                    forceRecheck = false;
                    for (int j = 0; j < techList.Count; j++)
                        techList[j].UpdateBuildRate(j);
                }

                string blockingPrereq = t.GetBlockingTech(techList);

                GUILayout.Label(t.TechName);
                GUILayout.Label($"{Math.Round(100 * t.Progress / t.ScienceCost, 2)} %", GUILayout.Width(_width1 / 2));
                if (t.BuildRate > 0)
                {
                    if (blockingPrereq == null)
                        GUILayout.Label(MagiCore.Utilities.GetColonFormattedTime(t.TimeLeft), GUILayout.Width(_width1));
                    else
                        GUILayout.Label("Waiting for PreReq", GUILayout.Width(_width1));
                }
                else
                {
                    GUILayout.Label($"Est: {MagiCore.Utilities.GetColonFormattedTime(t.EstimatedTimeLeft)}", GUILayout.Width(_width1));
                }
                if (t.BuildRate > 0 && blockingPrereq == null)
                {
                    if (!HighLogic.LoadedSceneIsEditor && GUILayout.Button("Warp", GUILayout.Width(45)))
                    {
                        KCTWarpController.Create(t);
                    }
                    else if (HighLogic.LoadedSceneIsEditor)
                        GUILayout.Space(45);
                }
                else
                    GUILayout.Space(45);

                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private static void RenderCombinedBuildList()
        {
            List<BuildListVessel> buildList = KCTGameStates.ActiveKSC.BuildList;

            RenderBuildlistHeader();
            RenderRollouts();

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(275));
            {
                RenderVesselsBeingBuilt(buildList);
                RenderVabWarehouse();
                RenderSphWarehouse();
            }
            GUILayout.EndScrollView();

            RenderLaunchPadControls();
        }

        private static void RenderBuildlistHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:");
            GUILayout.Label("Progress:", GUILayout.Width(_width1 / 2));
            GUILayout.Label("Time Left:", GUILayout.Width(_width2));
            GUILayout.EndHorizontal();
        }

        private static void RenderRollouts()
        {
            foreach (ReconRollout reconditioning in KCTGameStates.ActiveKSC.Recon_Rollout.FindAll(r => r.RRType == ReconRollout.RolloutReconType.Reconditioning))
            {
                GUILayout.BeginHorizontal();
                if (!HighLogic.LoadedSceneIsEditor && GUILayout.Button("Warp To", GUILayout.Width((_butW + 4) * 3)))
                {
                    KCTWarpController.Create(reconditioning);
                }

                GUILayout.Label($"Reconditioning: {reconditioning.LaunchPadID}");
                GUILayout.Label($"{reconditioning.ProgressPercent()}%", GUILayout.Width(_width1 / 2));
                GUILayout.Label(MagiCore.Utilities.GetColonFormattedTime(reconditioning.GetTimeLeft()), GUILayout.Width(_width2));

                GUILayout.EndHorizontal();
            }
        }

        private static void RenderVesselsBeingBuilt(List<BuildListVessel> buildList)
        {
            if (buildList.Count == 0)
            {
                GUILayout.Label("No vessels under construction! Go to the Editor to build more.");
            }
            for (int i = 0; i < buildList.Count; i++)
            {
                BuildListVessel b = buildList[i];
                if (!b.AllPartsValid)
                    continue;
                GUILayout.BeginHorizontal();

                if (!HighLogic.LoadedSceneIsEditor && GUILayout.Button("*", GUILayout.Width(_butW)))
                {
                    if (_selectedVesselId == b.Id)
                        GUIStates.ShowBLPlus = !GUIStates.ShowBLPlus;
                    else
                        GUIStates.ShowBLPlus = true;
                    _selectedVesselId = b.Id;
                }
                else if (HighLogic.LoadedSceneIsEditor)
                {
                    if (GUILayout.Button("X", GUILayout.Width(_butW)))
                    {
                        InputLockManager.SetControlLock(ControlTypes.EDITOR_SOFT_LOCK, "KCTPopupLock");
                        _selectedVesselId = b.Id;
                        DialogGUIBase[] options = new DialogGUIBase[2];
                        options[0] = new DialogGUIButton("Yes", ScrapVessel);
                        options[1] = new DialogGUIButton("No", RemoveInputLocks);
                        MultiOptionDialog diag = new MultiOptionDialog("scrapVesselPopup", "Are you sure you want to scrap this vessel?", "Scrap Vessel", null, options: options);
                        PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), diag, false, HighLogic.UISkin);
                    }
                }

                if (i > 0 && GUILayout.Button("^", GUILayout.Width(_butW)))
                {
                    buildList.RemoveAt(i);
                    buildList.Insert(GameSettings.MODIFIER_KEY.GetKey() ? 0 : i - 1, b);
                }

                if (i < buildList.Count - 1 && GUILayout.Button("v", GUILayout.Width(_butW)))
                {
                    buildList.RemoveAt(i);
                    if (GameSettings.MODIFIER_KEY.GetKey())
                    {
                        buildList.Add(b);
                    }
                    else
                    {
                        buildList.Insert(i + 1, b);
                    }
                }

                GUIContent typeIcon = b.GetListType() == BuildListVessel.ListType.VAB ? _rocketTexture : _planeTexture;
                GUILayout.Label(typeIcon, GUILayout.ExpandWidth(false));
                GUILayout.Label(b.ShipName);
                GUILayout.Label($"{Math.Round(b.ProgressPercent(), 2)}%", GUILayout.Width(_width1 / 2));
                if (b.BuildRate > 0)
                {
                    string timeLeft = MagiCore.Utilities.GetColonFormattedTime(b.TimeLeft);
                    GUILayout.Label(timeLeft, GUILayout.Width(_width2));
                }
                else
                {
                    double bpLeft = b.BuildPoints + b.IntegrationPoints - b.Progress;
                    double buildRate = Utilities.GetBuildRate(0, b.GetListType(), null);
                    string timeLeft = MagiCore.Utilities.GetColonFormattedTime(bpLeft / buildRate);
                    GUILayout.Label($"Est: {timeLeft}", GUILayout.Width(_width2));
                }
                GUILayout.EndHorizontal();
            }
        }

        private static void RenderVabWarehouse()
        {
            List<BuildListVessel> buildList = KCTGameStates.ActiveKSC.VABWarehouse;
            GUILayout.Label("__________________________________________________");
            GUILayout.BeginHorizontal();
            GUILayout.Label(_rocketTexture, GUILayout.ExpandWidth(false));
            GUILayout.Label("VAB Storage");
            GUILayout.EndHorizontal();
            if (Utilities.IsVabRecoveryAvailable() && GUILayout.Button("Recover Active Vessel To VAB"))
            {
                if (!Utilities.RecoverActiveVesselToStorage(BuildListVessel.ListType.VAB))
                {
                    PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "vesselRecoverErrorPopup", "Error!", "There was an error while recovering the ship. Sometimes reloading the scene and trying again works. Sometimes a vessel just can't be recovered this way and you must use the stock recover system.", "OK", false, HighLogic.UISkin);
                }
            }
            if (buildList.Count == 0)
            {
                GUILayout.Label("No vessels in storage!\nThey will be stored here when they are complete.");
            }

            for (int i = 0; i < buildList.Count; i++)
            {
                BuildListVessel b = buildList[i];
                RenderVabWarehouseRow(b, i);
            }
        }

        private static void RenderVabWarehouseRow(BuildListVessel b, int listIdx)
        {
            if (!b.AllPartsValid)
                return;
            string launchSite = b.LaunchSite;
            if (launchSite == "LaunchPad")
            {
                if (b.LaunchSiteID >= 0)
                    launchSite = KCTGameStates.ActiveKSC.LaunchPads[b.LaunchSiteID].name;
                else
                    launchSite = KCTGameStates.ActiveKSC.ActiveLPInstance.name;
            }
            ReconRollout rollout = KCTGameStates.ActiveKSC.GetReconRollout(ReconRollout.RolloutReconType.Rollout, launchSite);
            ReconRollout rollback = KCTGameStates.ActiveKSC.Recon_Rollout.FirstOrDefault(r => r.AssociatedID == b.Id.ToString() && r.RRType == ReconRollout.RolloutReconType.Rollback);
            ReconRollout recovery = KCTGameStates.ActiveKSC.Recon_Rollout.FirstOrDefault(r => r.AssociatedID == b.Id.ToString() && r.RRType == ReconRollout.RolloutReconType.Recovery);
            GUIStyle textColor = new GUIStyle(GUI.skin.label);
            GUIStyle buttonColor = new GUIStyle(GUI.skin.button);

            VesselPadStatus padStatus = VesselPadStatus.InStorage;
            if (rollback != null)
                padStatus = VesselPadStatus.RollingBack;
            if (recovery != null)
                padStatus = VesselPadStatus.Recovering;

            string status = "In Storage";
            if (rollout != null && rollout.AssociatedID == b.Id.ToString())
            {
                padStatus = VesselPadStatus.RollingOut;
                status = $"Rolling Out to {launchSite}";
                textColor = _yellowText;
                if (rollout.IsComplete())
                {
                    padStatus = VesselPadStatus.RolledOut;
                    status = $"At {launchSite}";
                    textColor = _greenText;
                }
            }
            else if (rollback != null)
            {
                status = $"Rolling Back from {launchSite}";
                textColor = _yellowText;
            }
            else if (recovery != null)
            {
                status = "Recovering";
                textColor = _redText;
            }

            GUILayout.BeginHorizontal();
            if (!HighLogic.LoadedSceneIsEditor && (padStatus == VesselPadStatus.InStorage || padStatus == VesselPadStatus.RolledOut))
            {
                if (GUILayout.Button("*", GUILayout.Width(_butW)))
                {
                    if (_selectedVesselId == b.Id)
                        GUIStates.ShowBLPlus = !GUIStates.ShowBLPlus;
                    else
                        GUIStates.ShowBLPlus = true;
                    _selectedVesselId = b.Id;
                }
            }
            else
                GUILayout.Space(_butW + 4);

            GUILayout.Label(b.ShipName, textColor);
            GUILayout.Label($"{status}   ", textColor, GUILayout.ExpandWidth(false));
            bool siteHasActiveRolloutOrRollback = rollout != null || KCTGameStates.ActiveKSC.GetReconRollout(ReconRollout.RolloutReconType.Rollback, launchSite) != null;
            if (IsRolloutEnabled && !HighLogic.LoadedSceneIsEditor && recovery == null && !siteHasActiveRolloutOrRollback) //rollout if the pad isn't busy
            {
                bool hasRecond = false;
                bool isUpgrading = KCTGameStates.KSCs.Find(ksc =>
                    ksc == KCTGameStates.ActiveKSC
                    && ksc.KSCTech.Find(ub =>
                        ub.IsLaunchpad
                        && ub.LaunchpadID == KCTGameStates.ActiveKSC.LaunchPads.IndexOf(KCTGameStates.ActiveKSC.ActiveLPInstance)) != null) != null;
                GUIStyle btnColor = _greenButton;
                if (KCTGameStates.ActiveKSC.ActiveLPInstance.IsDestroyed || isUpgrading)
                    btnColor = _redButton;
                else if (hasRecond = KCTGameStates.ActiveKSC.GetReconditioning(KCTGameStates.ActiveKSC.ActiveLPInstance.name) != null)
                    btnColor = _yellowButton;
                else if (b.MeetsFacilityRequirements(false).Count != 0)
                    btnColor = _yellowButton;
                ReconRollout tmpRollout = new ReconRollout(b, ReconRollout.RolloutReconType.Rollout, b.Id.ToString(), launchSite);
                if (tmpRollout.Cost > 0d)
                    GUILayout.Label("√" + tmpRollout.Cost.ToString("N0"));
                string rolloutText = listIdx == _mouseOnRolloutButton ? MagiCore.Utilities.GetColonFormattedTime(tmpRollout.GetTimeLeft()) : "Rollout";
                if (GUILayout.Button(rolloutText, btnColor, GUILayout.ExpandWidth(false)))
                {
                    if (PresetManager.Instance.ActivePreset.GeneralSettings.ReconditioningBlocksPad && hasRecond)
                    {
                        PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "cannotRollOutReconditioningPopup", "Cannot Roll out!", "You must finish reconditioning the launchpad before you can roll out to it!", "Acknowledged", false, HighLogic.UISkin);
                    }
                    else
                    {
                        List<string> facilityChecks = b.MeetsFacilityRequirements(false);
                        if (facilityChecks.Count == 0)
                        {
                            if (!KCTGameStates.ActiveKSC.ActiveLPInstance.IsDestroyed)
                            {
                                if (!isUpgrading)
                                {
                                    b.LaunchSiteID = KCTGameStates.ActiveKSC.ActiveLaunchPadID;

                                    if (rollout != null)
                                    {
                                        rollout.SwapRolloutType();
                                    }
                                    // tmpRollout.launchPadID = KCT_GameStates.ActiveKSC.ActiveLPInstance.name;
                                    KCTGameStates.ActiveKSC.Recon_Rollout.Add(tmpRollout);
                                }
                                else
                                {
                                    PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "cannotLaunchUpgradePopup",
                                        "Cannot Launch!",
                                        "You must finish upgrading the launchpad before you can launch a vessel from it!",
                                        "Acknowledged", false, HighLogic.UISkin);
                                }
                            }
                            else
                            {
                                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "cannotLaunchRepairPopup", "Cannot Launch!", "You must repair the launchpad before you can launch a vessel from it!", "Acknowledged", false, HighLogic.UISkin);
                            }
                        }
                        else
                        {
                            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "cannotLaunchEditorChecksPopup", "Cannot Launch!", "Warning! This vessel did not pass the editor checks! Until you upgrade the VAB and/or Launchpad it cannot be launched. Listed below are the failed checks:\n" + string.Join("\n", facilityChecks.Select(s => $"• {s}").ToArray()), "Acknowledged", false, HighLogic.UISkin);
                        }
                    }
                }
                if (Event.current.type == EventType.Repaint)
                    if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                        _mouseOnRolloutButton = listIdx;
                    else if (listIdx == _mouseOnRolloutButton)
                        _mouseOnRolloutButton = -1;
            }
            else if (IsRolloutEnabled && !HighLogic.LoadedSceneIsEditor && recovery == null && rollback == null &&
                     rollout != null && b.Id.ToString() == rollout.AssociatedID && !rollout.IsComplete() &&
                     GUILayout.Button(MagiCore.Utilities.GetColonFormattedTime(rollout.GetTimeLeft()), GUILayout.ExpandWidth(false)))    //swap rollout to rollback
            {
                rollout.SwapRolloutType();
            }
            else if (IsRolloutEnabled && !HighLogic.LoadedSceneIsEditor && recovery == null && rollback != null && !rollback.IsComplete())
            {
                if (rollout == null)
                {
                    if (GUILayout.Button(MagiCore.Utilities.GetColonFormattedTime(rollback.GetTimeLeft()), GUILayout.ExpandWidth(false)))    //switch rollback back to rollout
                        rollback.SwapRolloutType();
                }
                else
                {
                    GUILayout.Label(MagiCore.Utilities.GetColonFormattedTime(rollback.GetTimeLeft()), GUILayout.ExpandWidth(false));
                }
            }
            else if (HighLogic.LoadedScene != GameScenes.TRACKSTATION && recovery == null &&
                     (!IsRolloutEnabled || (rollout != null && b.Id.ToString() == rollout.AssociatedID && rollout.IsComplete())))
            {
                KCT_LaunchPad pad = KCTGameStates.ActiveKSC.LaunchPads.Find(lp => lp.name == launchSite);
                bool operational = pad != null ? !pad.IsDestroyed : !KCTGameStates.ActiveKSC.ActiveLPInstance.IsDestroyed;
                GUIStyle btnColor = _greenButton;
                string launchTxt = "Launch";
                if (!operational)
                {
                    launchTxt = "Repairs Required";
                    btnColor = _redButton;
                }
                else if (Utilities.ReconditioningActive(null, launchSite))
                {
                    launchTxt = "Reconditioning";
                    btnColor = _yellowButton;
                }
                if (IsRolloutEnabled && GameSettings.MODIFIER_KEY.GetKey() && GUILayout.Button("Roll Back", GUILayout.ExpandWidth(false)))
                {
                    rollout.SwapRolloutType();
                }
                else if (!GameSettings.MODIFIER_KEY.GetKey() && GUILayout.Button(launchTxt, btnColor, GUILayout.ExpandWidth(false)))
                {
                    if (b.LaunchSiteID >= 0)
                    {
                        KCTGameStates.ActiveKSC.SwitchLaunchPad(b.LaunchSiteID);
                    }
                    b.LaunchSiteID = KCTGameStates.ActiveKSC.ActiveLaunchPadID;

                    List<string> facilityChecks = b.MeetsFacilityRequirements(false);
                    if (facilityChecks.Count == 0)
                    {
                        if (!operational)
                        {
                            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "cannotLaunchRepairPopup", "Cannot Launch!", "You must repair the launchpad before you can launch a vessel from it!", "Acknowledged", false, HighLogic.UISkin);
                        }
                        else if (Utilities.ReconditioningActive(null, launchSite))
                        {
                            ScreenMessage message = new ScreenMessage($"[KCT] Cannot launch while LaunchPad is being reconditioned. It will be finished in {MagiCore.Utilities.GetFormattedTime(KCTGameStates.ActiveKSC.GetReconditioning(launchSite).GetTimeLeft())}", 4f, ScreenMessageStyle.UPPER_CENTER);
                            ScreenMessages.PostScreenMessage(message);
                        }
                        else
                        {
                            KCTGameStates.LaunchedVessel = b;
                            KCTGameStates.LaunchedVessel.KSC = null;
                            if (ShipConstruction.FindVesselsLandedAt(HighLogic.CurrentGame.flightState, b.LaunchSite).Count == 0)
                            {
                                GUIStates.ShowBLPlus = false;
                                if (!IsCrewable(b.ExtractedParts))
                                    b.Launch();
                                else
                                {
                                    GUIStates.ShowBuildList = false;

                                    KCTGameStates.ToolbarControl?.SetFalse();

                                    _centralWindowPosition.height = 1;
                                    AssignInitialCrew();
                                    GUIStates.ShowShipRoster = true;
                                }
                            }
                            else
                            {
                                GUIStates.ShowBuildList = false;
                                GUIStates.ShowClearLaunch = true;
                            }
                        }
                    }
                    else
                    {
                        PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "cannotLaunchEditorChecksPopup", "Cannot Launch!", "Warning! This vessel did not pass the editor checks! Until you upgrade the VAB and/or Launchpad it cannot be launched. Listed below are the failed checks:\n" + string.Join("\n", facilityChecks.Select(s => $"• {s}").ToArray()), "Acknowledged", false, HighLogic.UISkin);
                    }
                }
            }
            else if (!HighLogic.LoadedSceneIsEditor && recovery != null)
            {
                GUILayout.Label(MagiCore.Utilities.GetColonFormattedTime(recovery.GetTimeLeft()), GUILayout.ExpandWidth(false));
            }

            GUILayout.EndHorizontal();
        }

        private static void RenderSphWarehouse()
        {
            List<BuildListVessel> buildList = KCTGameStates.ActiveKSC.SPHWarehouse;
            GUILayout.Label("__________________________________________________");
            GUILayout.BeginHorizontal();
            GUILayout.Label(_planeTexture, GUILayout.ExpandWidth(false));
            GUILayout.Label("SPH Storage");
            GUILayout.EndHorizontal();
            if (Utilities.IsSphRecoveryAvailable() && GUILayout.Button("Recover Active Vessel To SPH"))
            {
                if (!Utilities.RecoverActiveVesselToStorage(BuildListVessel.ListType.SPH))
                {
                    PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "recoverShipErrorPopup", "Error!", "There was an error while recovering the ship. Sometimes reloading the scene and trying again works. Sometimes a vessel just can't be recovered this way and you must use the stock recover system.", "OK", false, HighLogic.UISkin);
                }
            }
            if (buildList.Count == 0)
            {
                GUILayout.Label("No vessels in storage!\nThey will be stored here when they are complete.");
            }

            for (int i = 0; i < buildList.Count; i++)
            {
                BuildListVessel b = buildList[i];
                RenderSphWarehouseRow(b, i);
            }
        }

        private static void RenderSphWarehouseRow(BuildListVessel b, int listIdx)
        {
            if (!b.AllPartsValid)
                return;
            string status = string.Empty;
            GUIStyle textColor = new GUIStyle(GUI.skin.label);

            ReconRollout recovery = KCTGameStates.ActiveKSC.Recon_Rollout.FirstOrDefault(r => r.AssociatedID == b.Id.ToString() && r.RRType == ReconRollout.RolloutReconType.Recovery);
            if (recovery != null)
                status = "Recovering";

            AirlaunchPrep airlaunchPrep = KCTGameStates.ActiveKSC.AirlaunchPrep.FirstOrDefault(r => r.AssociatedID == b.Id.ToString());
            if (airlaunchPrep != null)
            {
                if (airlaunchPrep.IsComplete())
                {
                    status = "Ready";
                    textColor = _greenText;
                }
                else
                {
                    status = airlaunchPrep.GetItemName();
                    textColor = _yellowText;
                }
            }

            GUILayout.BeginHorizontal();
            if (!HighLogic.LoadedSceneIsEditor && status == string.Empty)
            {
                if (GUILayout.Button("*", GUILayout.Width(_butW)))
                {
                    if (_selectedVesselId == b.Id)
                        GUIStates.ShowBLPlus = !GUIStates.ShowBLPlus;
                    else
                        GUIStates.ShowBLPlus = true;
                    _selectedVesselId = b.Id;
                }
            }
            else
                GUILayout.Space(_butW + 4);

            GUILayout.Label(b.ShipName, textColor);
            GUILayout.Label(status + "   ", GUILayout.ExpandWidth(false));

            if (HighLogic.LoadedScene != GameScenes.EDITOR && recovery == null && airlaunchPrep == null && AirlaunchTechLevel.AnyUnlocked())
            {
                var tmpPrep = new AirlaunchPrep(b, b.Id.ToString());
                if (tmpPrep.Cost > 0d)
                    GUILayout.Label("√" + tmpPrep.Cost.ToString("N0"));
                string airlaunchText = listIdx == _mouseOnAirlaunchButton ? MagiCore.Utilities.GetColonFormattedTime(tmpPrep.GetTimeLeft()) : "Prep for airlaunch";
                if (GUILayout.Button(airlaunchText, GUILayout.ExpandWidth(false)))
                {
                    AirlaunchTechLevel lvl = AirlaunchTechLevel.GetCurrentLevel();
                    if (!lvl.CanLaunchVessel(b, out string failedReason))
                    {
                        ScreenMessages.PostScreenMessage($"Vessel failed validation: {failedReason}", 6f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    else
                    {
                        KCTGameStates.ActiveKSC.AirlaunchPrep.Add(tmpPrep);
                    }
                }
                if (Event.current.type == EventType.Repaint)
                    if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                        _mouseOnAirlaunchButton = listIdx;
                    else if (listIdx == _mouseOnAirlaunchButton)
                        _mouseOnAirlaunchButton = -1;
            }
            else if (HighLogic.LoadedScene != GameScenes.EDITOR && recovery == null && airlaunchPrep != null)
            {
                string btnText = airlaunchPrep.IsComplete() ? "Unmount" : MagiCore.Utilities.GetColonFormattedTime(airlaunchPrep.GetTimeLeft());
                if (GUILayout.Button(btnText, GUILayout.ExpandWidth(false)))
                {
                    airlaunchPrep.SwitchDirection();
                }
            }

            string launchBtnText = airlaunchPrep != null ? "Airlaunch" : "Launch";
            if (HighLogic.LoadedScene != GameScenes.TRACKSTATION && recovery == null && (airlaunchPrep == null || airlaunchPrep.IsComplete()) &&
                GUILayout.Button(launchBtnText, GUILayout.ExpandWidth(false)))
            {
                List<string> facilityChecks = b.MeetsFacilityRequirements(false);
                if (facilityChecks.Count == 0)
                {
                    bool operational = Utilities.IsLaunchFacilityIntact(BuildListVessel.ListType.SPH);
                    if (!operational)
                    {
                        ScreenMessages.PostScreenMessage("You must repair the runway prior to launch!", 4f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    else
                    {
                        GUIStates.ShowBLPlus = false;
                        KCTGameStates.LaunchedVessel = b;
                        KCTGameStates.LaunchedVessel.KSC = null;

                        if (ShipConstruction.FindVesselsLandedAt(HighLogic.CurrentGame.flightState, "Runway").Count == 0)
                        {
                            if (airlaunchPrep != null)
                            {
                                GUIStates.ShowBuildList = false;
                                GUIStates.ShowAirlaunch = true;
                            }
                            else if (!IsCrewable(b.ExtractedParts))
                            {
                                b.Launch();
                            }
                            else
                            {
                                GUIStates.ShowBuildList = false;
                                KCTGameStates.ToolbarControl?.SetFalse();
                                _centralWindowPosition.height = 1;
                                AssignInitialCrew();
                                GUIStates.ShowShipRoster = true;
                            }
                        }
                        else
                        {
                            GUIStates.ShowBuildList = false;
                            GUIStates.ShowClearLaunch = true;
                            GUIStates.ShowAirlaunch = airlaunchPrep != null;
                        }
                    }
                }
                else
                {
                    PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "cannotLaunchEditorChecksPopup", "Cannot Launch!", "Warning! This vessel did not pass the editor checks! Until you upgrade the SPH and/or Runway it cannot be launched. Listed below are the failed checks:\n" + string.Join("\n", facilityChecks.Select(s => $"• {s}").ToArray()), "Acknowledged", false, HighLogic.UISkin);
                }
            }
            else if (recovery != null)
            {
                GUILayout.Label(MagiCore.Utilities.GetColonFormattedTime(recovery.GetTimeLeft()), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();
        }

        private static void RenderLaunchPadControls()
        {
            GUILayout.BeginHorizontal();
            int lpCount = KCTGameStates.ActiveKSC.LaunchPadCount;
            if (lpCount > 1 && GUILayout.Button("<<", GUILayout.ExpandWidth(false)))
            {
                KCTGameStates.ActiveKSC.SwitchToPrevLaunchPad();
                if (HighLogic.LoadedSceneIsEditor)
                {
                    Utilities.RecalculateEditorBuildTime(EditorLogic.fetch.ship);
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Current: {KCTGameStates.ActiveKSC.ActiveLPInstance.name} ({KCTGameStates.ActiveKSC.ActiveLPInstance.level + 1})");
            if (_costOfNewLP == int.MinValue)
            {
                _costOfNewLP = MathParser.GetStandardFormulaValue("NewLaunchPadCost", new Dictionary<string, string>
                {
                    { "N", KCTGameStates.ActiveKSC.LaunchPads.Count.ToString() }
                });
            }

            if (GUILayout.Button("Rename", GUILayout.ExpandWidth(false)))
            {
                _isRenamingLaunchPad = true;
                _newName = KCTGameStates.ActiveKSC.ActiveLPInstance.name;
                GUIStates.ShowDismantlePad = false;
                GUIStates.ShowNewPad = false;
                GUIStates.ShowRename = true;
                GUIStates.ShowBuildList = false;
                GUIStates.ShowBLPlus = false;
            }
            if (_costOfNewLP >= 0 && GUILayout.Button("New", GUILayout.ExpandWidth(false)))
            {
                _newName = $"LaunchPad {(KCTGameStates.ActiveKSC.LaunchPads.Count + 1)}";
                GUIStates.ShowDismantlePad = false;
                GUIStates.ShowNewPad = true;
                GUIStates.ShowRename = false;
                GUIStates.ShowBuildList = false;
                GUIStates.ShowBLPlus = false;
            }
            if (lpCount > 1 && GUILayout.Button("Dismantle", GUILayout.ExpandWidth(false)))
            {
                GUIStates.ShowDismantlePad = true;
                GUIStates.ShowNewPad = false;
                GUIStates.ShowRename = false;
                GUIStates.ShowBuildList = false;
                GUIStates.ShowBLPlus = false;
            }
            GUILayout.FlexibleSpace();
            if (lpCount > 1 && GUILayout.Button(">>", GUILayout.ExpandWidth(false)))
            {
                KCTGameStates.ActiveKSC.SwitchToNextLaunchPad();
                if (HighLogic.LoadedSceneIsEditor)
                {
                    Utilities.RecalculateEditorBuildTime(EditorLogic.fetch.ship);
                }
            }
            GUILayout.EndHorizontal();
        }

        public static void CancelTechNode(int index)
        {
            if (KCTGameStates.TechList.Count > index)
            {
                TechItem node = KCTGameStates.TechList[index];
                KCTDebug.Log($"Cancelling tech: {node.TechName}");

                // cancel children
                for (int i = 0; i < KCTGameStates.TechList.Count; i++)
                {
                    List<string> parentList = KerbalConstructionTimeData.techNameToParents[KCTGameStates.TechList[i].TechID];
                    if (parentList.Contains(node.TechID))
                    {
                        CancelTechNode(i);
                        // recheck list in case multiple levels of children were deleted.
                        i = -1;
                        index = KCTGameStates.TechList.FindIndex(t => t.TechID == node.TechID);
                    }
                }

                if (Utilities.CurrentGameHasScience())
                {
                    bool valBef = KCTGameStates.IsRefunding;
                    KCTGameStates.IsRefunding = true;
                    try
                    {
                        ResearchAndDevelopment.Instance.AddScience(node.ScienceCost, TransactionReasons.None);    //Should maybe do tech research as the reason
                    }
                    finally
                    {
                        KCTGameStates.IsRefunding = valBef;
                    }
                }
                node.DisableTech();
                KCTGameStates.TechList.RemoveAt(index);
            }
        }

        private static void DrawBLPlusWindow(int windowID)
        {
            Rect parentPos = HighLogic.LoadedSceneIsEditor ? EditorBuildListWindowPosition : BuildListWindowPosition;
            _blPlusPosition.yMin = parentPos.yMin;
            _blPlusPosition.height = 225;
            BuildListVessel b = Utilities.FindBLVesselByID(_selectedVesselId);
            GUILayout.BeginVertical();
            string launchSite = b.LaunchSite;

            if (launchSite == "LaunchPad")
            {
                if (b.LaunchSiteID >= 0)
                    launchSite = b.KSC.LaunchPads[b.LaunchSiteID].name;
                else
                    launchSite = b.KSC.ActiveLPInstance.name;
            }
            ReconRollout rollout = KCTGameStates.ActiveKSC.GetReconRollout(ReconRollout.RolloutReconType.Rollout, launchSite);
            bool onPad = rollout != null && rollout.IsComplete() && rollout.AssociatedID == b.Id.ToString();
            //This vessel is rolled out onto the pad

            // 1.4 Addition
            if (!onPad && GUILayout.Button("Select LaunchSite"))
            {
                _launchSites = Utilities.GetLaunchSites(b.Type == BuildListVessel.ListType.VAB);
                if (_launchSites.Any())
                {
                    GUIStates.ShowBLPlus = false;
                    GUIStates.ShowLaunchSiteSelector = true;
                    _centralWindowPosition.width = 300;
                }
                else
                {
                    PopupDialog.SpawnPopupDialog(new MultiOptionDialog("KCTNoLaunchsites", "No launch sites available to choose from. Try visiting an editor first.", "No Launch Sites", null, new DialogGUIButton("OK", () => { })), false, HighLogic.UISkin);
                }
            }

            if (!onPad && GUILayout.Button("Scrap"))
            {
                InputLockManager.SetControlLock(ControlTypes.KSC_ALL, "KCTPopupLock");
                DialogGUIBase[] options = new DialogGUIBase[2];
                options[0] = new DialogGUIButton("Yes", ScrapVessel);
                options[1] = new DialogGUIButton("No", RemoveInputLocks);
                MultiOptionDialog diag = new MultiOptionDialog("scrapVesselConfirmPopup", "Are you sure you want to scrap this vessel?", "Scrap Vessel", null, 300, options);
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), diag, false, HighLogic.UISkin);
                GUIStates.ShowBLPlus = false;
                ResetBLWindow();
            }

            if (!onPad && GUILayout.Button("Edit"))
            {
                GUIStates.ShowBLPlus = false;
                EditorWindowPosition.height = 1;
                string tempFile = $"{KSPUtil.ApplicationRootPath}saves/{HighLogic.SaveFolder}/Ships/temp.craft";
                b.ShipNode.Save(tempFile);
                GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
                KCTGameStates.EditedVessel = b;
                KCTGameStates.EditedVessel.KSC = null;
                KCTGameStates.EditorShipEditingMode = true;

                InputLockManager.SetControlLock(ControlTypes.EDITOR_EXIT, "KCTEditExit");
                InputLockManager.SetControlLock(ControlTypes.EDITOR_NEW, "KCTEditNew");
                InputLockManager.SetControlLock(ControlTypes.EDITOR_LAUNCH, "KCTEditLaunch");

                EditorDriver.StartAndLoadVessel(tempFile, b.Type == BuildListVessel.ListType.VAB ? EditorFacility.VAB : EditorFacility.SPH);
            }

            if (GUILayout.Button("Rename"))
            {
                _centralWindowPosition.width = 360;
                _centralWindowPosition.x = (Screen.width - 360) / 2;
                _centralWindowPosition.height = 1;
                GUIStates.ShowBuildList = false;
                GUIStates.ShowBLPlus = false;
                GUIStates.ShowNewPad = false;
                GUIStates.ShowRename = true;
                _newName = b.ShipName;
                _isRenamingLaunchPad = false;
            }

            if (GUILayout.Button("Duplicate"))
            {
                Utilities.AddVesselToBuildList(b.CreateCopy(true));
            }

            if (GUILayout.Button("Add to Plans"))
            {
                AddVesselToPlansList(b.CreateCopy(true));
            }

            if (KCTGameStates.ActiveKSC.Recon_Rollout.Find(rr => rr.RRType == ReconRollout.RolloutReconType.Rollout && rr.AssociatedID == b.Id.ToString()) != null && GUILayout.Button("Rollback"))
            {
                KCTGameStates.ActiveKSC.Recon_Rollout.Find(rr => rr.RRType == ReconRollout.RolloutReconType.Rollout && rr.AssociatedID == b.Id.ToString()).SwapRolloutType();
                GUIStates.ShowBLPlus = false;
            }

            if (!b.IsFinished && GUILayout.Button("Warp To"))
            {
                KCTWarpController.Create(b);
                GUIStates.ShowBLPlus = false;
            }

            if (!b.IsFinished && GUILayout.Button("Move to Top"))
            {
                if (_combineVabAndSph)
                {
                    if (KCTGameStates.ActiveKSC.BuildList.Remove(b))
                        KCTGameStates.ActiveKSC.BuildList.Insert(0, b);
                }
                else if (b.Type == BuildListVessel.ListType.VAB)
                {
                    b.RemoveFromBuildList();
                    KCTGameStates.ActiveKSC.VABList.Insert(0, b);
                }
                else if (b.Type == BuildListVessel.ListType.SPH)
                {
                    b.RemoveFromBuildList();
                    KCTGameStates.ActiveKSC.SPHList.Insert(0, b);
                }
            }

            if (!b.IsFinished &&
                (PresetManager.Instance.ActivePreset.GeneralSettings.MaxRushClicks == 0 || b.RushBuildClicks < PresetManager.Instance.ActivePreset.GeneralSettings.MaxRushClicks) &&
                GUILayout.Button($"Rush Build 10%\n√{Math.Round(b.GetRushCost())}"))
            {
                b.DoRushBuild();
            }

            if (GUILayout.Button("Close"))
            {
                GUIStates.ShowBLPlus = false;
            }

            GUILayout.EndVertical();

            float width = _blPlusPosition.width;
            _blPlusPosition.x = parentPos.x - width;
            _blPlusPosition.width = width;
        }

        public static void DrawLaunchSiteChooser(int windowID)
        {
            GUILayout.BeginVertical();
            _launchSiteScrollView = GUILayout.BeginScrollView(_launchSiteScrollView, GUILayout.Height((float)Math.Min(Screen.height * 0.75, 25 * _launchSites.Count + 10)));

            foreach (string launchsite in _launchSites)
            {
                if (GUILayout.Button(launchsite))
                {
                    //Set the chosen vessel's launch site to the selected site
                    BuildListVessel blv = Utilities.FindBLVesselByID(_selectedVesselId);
                    blv.LaunchSite = launchsite;
                    GUIStates.ShowLaunchSiteSelector = false;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            CenterWindow(ref _centralWindowPosition);
        }

        private static void ScrapVessel()
        {
            InputLockManager.RemoveControlLock("KCTPopupLock");
            BuildListVessel b = Utilities.FindBLVesselByID(_selectedVesselId);
            if (b == null)
            {
                KCTDebug.Log("Tried to remove a vessel that doesn't exist!");
                return;
            }
            KCTDebug.Log($"Scrapping {b.ShipName}");
            if (!b.IsFinished)
            {
                List<ConfigNode> parts = b.ExtractedPartNodes;
                b.RemoveFromBuildList();

                //only add parts that were already a part of the inventory
                if (ScrapYardWrapper.Available)
                {
                    List<ConfigNode> partsToReturn = new List<ConfigNode>();
                    foreach (ConfigNode partNode in parts)
                    {
                        if (ScrapYardWrapper.PartIsFromInventory(partNode))
                        {
                            partsToReturn.Add(partNode);
                        }
                    }
                    if (partsToReturn.Any())
                    {
                        ScrapYardWrapper.AddPartsToInventory(partsToReturn, false);
                    }
                }
            }
            else
            {
                b.RemoveFromBuildList();
                ScrapYardWrapper.AddPartsToInventory(b.ExtractedPartNodes, false);    //don't count as a recovery
            }
            ScrapYardWrapper.SetProcessedStatus(ScrapYardWrapper.GetPartID(b.ExtractedPartNodes[0]), false);
            Utilities.AddFunds(b.GetTotalCost(), TransactionReasons.VesselRollout);
        }
    }
}

/*
    KerbalConstructionTime (c) by Michael Marvin, Zachary Eck

    KerbalConstructionTime is licensed under a
    Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.

    You should have received a copy of the license along with this
    work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
*/
