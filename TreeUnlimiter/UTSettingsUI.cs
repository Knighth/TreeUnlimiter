using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace TreeUnlimiter
{
	public class UTSettingsUI
	{
        private static UILabel maxTreeLabel; //stores ref to last generated option panel uilabel for maxtrees#.
        internal static UISlider maxTreeSlider; //stores ref to laste generated option panel slider for treescale setting.
        internal static UICheckBox GhostModechkbox; //stores ref to this label, need it to disable it if in-game. 
        internal static UICheckBox UILoggingChecked; //stores ref to this chkbox, need it to overide user actions. 
        internal static UICheckBox UIExtraLoggingChecked; //stores ref to this chkbox, need it overide user actions. 
        internal static UIButton tmpUIButton1; //these hold our hide\unhide debug values.
        internal static UIButton tmpUIButton2;
        internal static UIButton tmpUIButton3;
        internal static UIButton tmpUIButton4;
        internal static UITextField tmpTextField1; //dbglevel
        internal static UIHelper uIHelperBase2;
        internal static bool InUIChangeLoop = false; //stops possible loop cycle when manually setting Checked-Values.
        internal static bool RadioMastFix = true;




        private void UseNoWindChecked(bool en)
        {
            Mod.USE_NO_WINDEFFECTS = en;
            Mod.config.UseNoWindEffects = en;
            Configuration.Serialize(Mod.MOD_CONFIGPATH, Mod.config);
        }

        private void OnScaleFactorChange(float val)
        {
            Mod.config.ScaleFactor = (int)val;
            Configuration.Serialize(Mod.MOD_CONFIGPATH, Mod.config);
            Mod.UpdateScaleFactors();
            if (maxTreeLabel != null)
            {
                maxTreeLabel.text = string.Format(Mod.CURRENTMAXTREES_FORMATTEXT, Mod.config.ScaleFactor.ToString(), Mod.SCALED_TREE_COUNT.ToString());
            }
        }


        private void NullTreeOptionsChanged(int en)
        {
            TreePrefabsDebug.NullTreeOptions tmp;
            switch (en)
            {
                case 0:
                    tmp = TreePrefabsDebug.NullTreeOptions.DoNothing;
                    break;
                case 1:
                    tmp = TreePrefabsDebug.NullTreeOptions.ReplaceTree;
                    break;
                case 2:
                    tmp = TreePrefabsDebug.NullTreeOptions.RemoveTree;
                    break;

                default:
                    tmp = TreePrefabsDebug.NullTreeOptions.DoNothing;
                    break;
            }

            Mod.config.NullTreeOptionsValue = tmp;
            Mod.config.NullTreeOptionsIndex = en;
            Configuration.Serialize(Mod.MOD_CONFIGPATH, Mod.config);
        }


        //basic logging
        private void LoggingChecked(bool en)
        {
            if (InUIChangeLoop) { return; }
            if (Mod.config.ExtraLogDataChecked & en == false)
            {
                if (UILoggingChecked != null)
                { InUIChangeLoop = true; UILoggingChecked.isChecked = true; InUIChangeLoop = false; }
                return;  //ignore it don't allow turning off if extended debugging still on.
            }
            Mod.DEBUG_LOG_ON = en;
            if (en)
            {
                Mod.DEBUG_LOG_LEVEL = 1;
                if (UILoggingChecked != null && Mod.config.ExtraLogDataChecked == true)
                { InUIChangeLoop = true; UILoggingChecked.isChecked = true; InUIChangeLoop = false; }//force it.
            }
            else
            {
                Mod.DEBUG_LOG_LEVEL = 0;
            }
            Mod.config.DebugLogging = en;
            Mod.config.DebugLoggingLevel = Mod.DEBUG_LOG_LEVEL;
            Configuration.Serialize(Mod.MOD_CONFIGPATH, Mod.config);

        }


        //extended logging (level2)
        private void ExtraLogDataChecked(bool en)
        {
            Mod.config.ExtraLogDataChecked = en;
            if (en == true)
            {
                if (Mod.config.DebugLogging == false)
                {
                    LoggingChecked(true);
                }
                Mod.DEBUG_LOG_LEVEL = 2;
                Mod.config.DebugLoggingLevel = 2;
            }
            else
            {
                //false turn off
                if (Mod.config.DebugLogging)
                {
                    LoggingChecked(true);
                }
                else //should not be the case but..
                {
                    Mod.DEBUG_LOG_LEVEL = 0;
                    LoggingChecked(false);
                }
            }
            Configuration.Serialize(Mod.MOD_CONFIGPATH, Mod.config);
            eventVisibilityChanged(null, en);
        }

        private void SeperateLogChecked(bool en)
        {
            Mod.config.UseCustomLogFile = en;
            Configuration.Serialize(Mod.MOD_CONFIGPATH, Mod.config);
        }

        private void OnToggleGhostMode(bool en)
        {
            try
            {
                if (en)
                { Mod.IsGhostMode = true; Mod.config.GhostModeEnabled = true; }
                else
                { Mod.IsGhostMode = false; Mod.config.GhostModeEnabled = false; }
                Configuration.Serialize(Mod.MOD_CONFIGPATH, Mod.config);
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex); }
        }

        private void OnDumpAllBurningTrees()
        {
            try
            {
                UTDebugUtils.DumpBurningTrees();
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex); }
        }


        private void ClearAllBurningDamaged()
        {
            try
            {
                UTDebugUtils.ResetAllBurningTrees(true);
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex); }
        }

        private void ClearAllSaveDataFromFile()
        {
            try
            {
                SaveDataUtils.EraseBytesFromNamedKey(Mod.MOD_OrgDataKEYNAME); // "mabako/unlimiter" 
                SaveDataUtils.EraseBytesFromNamedKey(UTSaveDataContainer.DefaultContainername); //"KH_UnlimitedTrees_v1_0"
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex); }
        }

        private void OnDbgLevelTextChanged(string text)
        { }

        private void OnDbgLevelTextSubmit(string text)
        {
            try
            {
                int a = 0;
                if (Int32.TryParse(text, out a))
                {
                    Mod.DEBUG_LOG_LEVEL = (byte)a;
                    Logger.dbgLog("Log level manually changed to " + a.ToString());
                }
                else { }
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex); }
        }

        private void OnListAllCustomDataInFile()
        {
            try
            {
                SaveDataUtils.ListDataKeysToLog();
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex); }

        }

        private void OnRadioMastFix(bool en)
        {
            try
            {
                RadioMastFix = en;
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex); }

        }

        /// <summary>
        /// Called by game upon user entering options screen.
        /// icities interface for gui option setup
        /// </summary>
        /// <param name="helper">the ref helper from orginal OnSettingsUI</param>
        public void BuildSettingsUI(ref UIHelperBase helper)
        {
            //1.6 lets us know sort what the user is doing
            //SimulationManager.UpdateMode tmpUpdateMode = SimulationManager.UpdateMode.Undefined;
            //try
            //{
            //    if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
            //    {
            //        SimulationManager SMgr = Singleton<SimulationManager>.instance;
            //        if (SMgr != null && SMgr.m_metaData != null)
            //        {
            //            tmpUpdateMode = SMgr.m_metaData.m_updateMode;
            //            Logger.dbgLog("lastupdatemode: " + SMgr.m_metaData.m_updateMode.ToString());
            //        }
            //        else
            //        {
            //            Logger.dbgLog("lastupdatemode: null or simmanager null");
            //        }
            //    }
            //}
            //catch (Exception ex44)
            //{ Logger.dbgLog("Exception BuildSettingsUI :", ex44, true); }


            try
            {
                //for setting up tooltips; let's subscribe to visibiliy event.
                UIHelper hp = (UIHelper)helper;
                UIScrollablePanel panel = (UIScrollablePanel)hp.self; //root panel it's actually "ScrollContent"
                panel.eventVisibilityChanged += eventVisibilityChanged;
                //regular
                //UIHelperBase uIHelperBase = helper.AddGroup("Unlimited Trees Options");
                UIHelper firstpanel = (UIHelper)hp.AddGroup("Unlimited Trees Options"); //returns helper with .self == uipanel for the 'group'

                UICheckBox tmpUIChk; UIDropDown tmpUIDropDown;
                tmpUIChk = (UICheckBox)firstpanel.AddCheckbox("Disable tree effects on wind", Mod.USE_NO_WINDEFFECTS, new OnCheckChanged(UseNoWindChecked));
                tmpUIChk.tooltip = "Disable the normal game behavior of letting tree's height effect \\ dilute the wind map. \n Option should be set before loading a map.";
                UILoggingChecked = (UICheckBox)firstpanel.AddCheckbox("Enable Basic Logging", Mod.DEBUG_LOG_ON, new OnCheckChanged(LoggingChecked));
                UILoggingChecked.tooltip = "Enables logging for basic debugging and informational purposes.\nUnless there are problems you probably don't need to enable this.";

                string[] sOptions = new string[] { "DoNothing (Default)", "Replace ", "Remove" };
                tmpUIDropDown = (UIDropDown)firstpanel.AddDropdown("In case of tree errors:", sOptions, Mod.config.NullTreeOptionsIndex, NullTreeOptionsChanged);
                tmpUIDropDown.width = tmpUIDropDown.width + 24.0f;
                tmpUIDropDown.tooltip = "Sets what you want the mod to do when missing trees\n or trees with serious errors are found.\n DoNothing= Let game errors happen normally.\n ReplaceTree= Replaces any missing tree with a default game tree\n RemoveTree = Deletes the tree\n*Any changes made are commited upon you saving the file after loading\n*So you may want to disable autosave if debugging.";
                tmpUIDropDown.selectedIndex = Mod.config.NullTreeOptionsIndex;

                tmpUIChk = (UICheckBox)firstpanel.AddCheckbox("Enable Seperate Log", Mod.config.UseCustomLogFile, new OnCheckChanged(SeperateLogChecked));
                tmpUIChk.tooltip = "Enables logging of UnlimitedTrees log data to a seperate file\n so you don't have to look through the game log.\nLocation can be changed, default is TreeUnlimiter_Log.txt in game installation root folder.";
                UIExtraLoggingChecked = (UICheckBox)firstpanel.AddCheckbox("Enable Dev Level Logging", Mod.config.ExtraLogDataChecked, new OnCheckChanged(ExtraLogDataChecked));
                UIExtraLoggingChecked.tooltip = "Enables logging of much more information to your log file\n Recommended only if you are having problems and someone\nhas asked you to enable it\nprobably in combination with custom log option.";
                GhostModechkbox = (UICheckBox)firstpanel.AddCheckbox("Enable Ghost Mode", Mod.config.GhostModeEnabled, OnToggleGhostMode);
                GhostModechkbox.tooltip = "(advanced) Enables Mod to stay active but act like it's not, ignoring 'extra' UT tree data during load.\n This mode only exists to allow you to load a map that has 'extra' UT data WITHOUT actually loading that data the game will act as if UT is not loaded.\n For your own safety you can not change this setting in-game.";

                GenerateMaxTreeSliderandLablel(ref firstpanel);
                UIPanel outerpanel1 = (UIPanel)firstpanel.self;
                GenerateCurrentMaxTreeLabel(ref outerpanel1);

                //panel2
                uIHelperBase2 = (UIHelper)hp.AddGroup("In-Game-Only Debug Functions (Not meant for users)");
                tmpUIChk = (UICheckBox)uIHelperBase2.AddCheckbox("Don't burn Radio Masts", RadioMastFix, new OnCheckChanged(OnRadioMastFix));
                tmpUIChk.tooltip = "Does not allows tree fires to catch onto Radio Masts.\n This option helps fix a bug in the game, where choppers can't put out those fires.";

                tmpUIButton1 = (UIButton)uIHelperBase2.AddButton("ResetAllBurningTrees", new OnButtonClicked(ClearAllBurningDamaged));
                tmpUIButton1.tooltip = "Resets all trees to not burning and not damaged.\nAlso wipes the burningtrees array to match.";
                tmpUIButton2 = (UIButton)uIHelperBase2.AddButton("ClearAllOurSaveDataFromThisFile", new OnButtonClicked(ClearAllSaveDataFromFile));
                tmpUIButton2.tooltip = "Wipes all UT data from currently loaded map file.\nNote** This does not remove tree from an active map\n It will just force the mod to re-save your data if needed.\n or not write new data if <262k trees";
                tmpUIButton3 = (UIButton)uIHelperBase2.AddButton("List all custom data to log", OnListAllCustomDataInFile);
                tmpUIButton3.tooltip = "dumps to your log file a list and size of all custom data entries (beyond just UT) \n in the m_serializableDataStorage dictionary with-in simulation manager.";
                tmpUIButton4 = (UIButton)uIHelperBase2.AddButton("dmp allburningtrees", new OnButtonClicked(OnDumpAllBurningTrees));
                tmpUIButton4.tooltip = "Dumps a list of all burning tree data to the log.\n YOU DONT NEED THIS.";
                tmpTextField1 = (UITextField)uIHelperBase2.AddTextfield("dbglevel:", Mod.DEBUG_LOG_LEVEL.ToString(), new OnTextChanged(OnDbgLevelTextChanged), new OnTextSubmitted(OnDbgLevelTextSubmit));

            }
            catch (Exception ex)
            { Logger.dbgLog("Exception setting options gui: ", ex, true); }
        }


        private void GenerateMaxTreeSliderandLablel(ref UIHelper oUIHelperBase)
        {
            try
            {
                if (oUIHelperBase != null)
                {
                    maxTreeSlider = (UISlider)oUIHelperBase.AddSlider("Max # of trees scaling factor", 4.0f, 8.0f, 1.0f, (float)Mod.config.ScaleFactor, OnScaleFactorChange);
                    if (maxTreeSlider != null)
                    {
                        maxTreeSlider.tooltip = "Sets the maximum # of trees in increments of 262,144.\nSetting this above 4 (1 million trees) is not recommended and depending on your hardware\n it may cause performance or rendering issues.";
                        maxTreeSlider.disabledColor = new Color32(180, 180, 180, 255);
                    }
                    oUIHelperBase.AddSpace(40);
                }
            }
            catch (Exception ex)
            { Logger.dbgLog("Exception setting options slider gui: ", ex, true); }
        }


        private void GenerateCurrentMaxTreeLabel(ref UIPanel oPanel)
        {
            try
            {
                if (oPanel != null & maxTreeSlider != null)
                {
                    //oPanel.autoLayout = false;
                    maxTreeLabel = oPanel.AddUIComponent<UILabel>();
                    maxTreeLabel.name = "CurrentMaxTrees";
                    //maxTreeLabel.absolutePosition = new Vector3(maxTreeSlider.absolutePosition.x, maxTreeSlider.absolutePosition.y + 28f);
                    maxTreeLabel.text = string.Format(Mod.CURRENTMAXTREES_FORMATTEXT, Mod.config.ScaleFactor.ToString(), Mod.SCALED_TREE_COUNT.ToString());
                }
            }
            catch (Exception ex)
            { Logger.dbgLog("Exception setting MaxTreeLabel: ", ex, true); }
        }


        /// <summary>
        /// Our event handler for our options screen being shown.
        /// </summary>
        /// <param name="component">The UIComponent that fired the event.</param>
        /// <param name="value">Visability state - true when isVisiable</param>
        private static void eventVisibilityChanged(UIComponent component, bool value)
        {
            try
            {
                UIPanel panel;
                if (uIHelperBase2 != null)
                {
                    panel = (UIPanel)uIHelperBase2.self;
                    //KRN removed 1.6.2-f1 build009
                    //Changed to use normal tooltip process which seems to work without needing delay now.
                    //component.eventVisibilityChanged -= eventVisibilityChanged; //we only want to fire once so unsub.
                    //component.parent.StartCoroutine(PopulateTooltips(component));

                    //Added 1.6.2-f1 Build009
                    if (value && uIHelperBase2 != null)
                    {
                        if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
                        {
                            panel.parent.Show(); //do the entire panel not just content.
                            if (tmpTextField1 != null) { tmpTextField1.text = Mod.DEBUG_LOG_LEVEL.ToString(); }

                        }
                    }
                    else if (uIHelperBase2 != null)
                    {
                        if (Mod.DEBUG_LOG_ON == false || Mod.DEBUG_LOG_LEVEL < 2)
                        {
                            panel.parent.Hide(); //do the entire panel not just content.
                        }
                    }
                }
            }
            catch (Exception ex)
            { Logger.dbgLog("Exception gui: ", ex, true); }

        }
	}
}
