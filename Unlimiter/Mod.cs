using CitiesSkylinesDetour;
using ColossalFramework;
using ColossalFramework.Plugins;
//using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TreeUnlimiter
{
    public class Mod : IUserMod
    {
        internal const ulong MOD_WORKSHOPID = 455403039uL;
        internal const string MOD_OFFICIAL_NAME = "Unlimited Trees Mod";
        internal const string VERSION_BUILD_NUMBER = "1.0.6.0-f4 build_006";
        internal const string MOD_DESCRIPTION = "Allows you to place way more trees!";
        internal const string MOD_DBG_Prefix = "TreeUnlimiter";
        internal const string CURRENTMAXTREES_FORMATTEXT = "ScaleFactor: {0}   Maximum trees: {1}";
        internal const string DTMilli = "MM/dd/yyyy hh:mm:ss.fff tt";  //used for time formatting.
        internal const string MOD_OrgDataKEYNAME = "mabako/unlimiter";
        public const int MAX_NET_SEGMENTS = 36864; //used in BuildingDecorations
        public const int MAX_NET_NODES = 32768;  //used in BuildingDecorations
        public static readonly string MOD_CONFIGPATH = "TreeUnlimiterConfig.xml";
        public static readonly string MOD_DEFAULT_LOG_PATH = "TreeUnlimiter_Log.txt";
        public static int MOD_TREE_SCALE = 4;
        public const int DEFAULT_TREE_COUNT = 262144;
        public const int DEFAULT_TREEUPDATE_COUNT = 4096;
        internal const int FormatVersion1NumOfTrees = 1048576;
        internal const ushort CurrentFormatVersion = 3;  //don't change unless really have to.
        public static int SCALED_TREE_COUNT = MOD_TREE_SCALE * DEFAULT_TREE_COUNT; //1048576
        public static int SCALED_TREEUPDATE_COUNT = MOD_TREE_SCALE * DEFAULT_TREEUPDATE_COUNT;//  16384;
        public static bool IsEnabled = false;
        public static bool IsInited = false;
        public static bool IsSetupActive = false;
        public static bool DEBUG_LOG_ON = false;
        public static byte DEBUG_LOG_LEVEL = 0;
        public static bool USE_NO_WINDEFFECTS = false;
        private static bool isFirstInit = true;
        private static Dictionary<MethodInfo, RedirectCallsState> redirectDic = new Dictionary<MethodInfo, RedirectCallsState>();
        public static Configuration config;
        private static UILabel maxTreeLabel; //stores ref to last generated option panel uilabel for maxtrees#.
        internal static UISlider maxTreeSlider; //stores ref to laste generated option panel slider for treescale setting.


        public string Name
        {
            get
            {
                return MOD_OFFICIAL_NAME;
            }
        }

        public string Description
        {
            get
            {
                return MOD_DESCRIPTION;
            }
        }

        /// <summary>
        /// Our constructor. Fired upon Colossal first loading our dll. 
        /// </summary>
        public Mod()
        {
            try
            {
                if (!Mod.IsInited)
                {
                    //we hard code this one to output_log so there is no confusion in a startup log that may be passed along for debugging.
                    Debug.Log("[TreeUnlimiter] v" + VERSION_BUILD_NUMBER + " Mod has been loaded.");
                    Mod.init();
                }
            }
            catch(Exception ex)
            {
                Debug.Log("[TreeUnlimiter] v" + VERSION_BUILD_NUMBER + " ,Error in construction\nPlease report this to mod author, thank you.\r\n" + ex.ToString());
            }
        }



        /// <summary>
        /// Fired when when mod is loaded and usersetting have us marked as enabled, OR when the user specifically enables us.
        /// Timing wise during game bootup - this fires later in the load process - ie after all dll's have been 'loaded'.
        /// Note: the C\O call backs must not be static due to the way CO looks for them via reflection.
        /// </summary>
        public void OnEnabled()
        {
            try
            {
                IsEnabled = true;
                Logger.dbgLog(string.Concat("v", VERSION_BUILD_NUMBER, " Mod has been enabled. ", DateTime.Now.ToString()));
                Mod.Setup(); //1.6.0
                if (IsInited == false)
                {
                    init();  //init will do a data pull from config off disk
                }
                if (!isFirstInit)
                {
                    ReloadConfiguationData(false, false); //always regrab\refresh our info on-enabled.
                    //EXCEPT when it's our very first time around where isfirstinit will be true.
                    //handles case of user loading with enabled, then disabling, changing config txt manually, then re-enabling.
                    // ..edge case of course but why make them restart to pick up custom config change or pull from disk twice
                    //during our first load just to save a few bytes?
                }
                else
                {
                    isFirstInit = false; // our very first load flag 
                }
            }
            catch(Exception ex)
            { Logger.dbgLog("", ex, true); }

        }


        public static void init()
        {
            try
            {
                if (IsInited == false)
                {
                    ReloadConfiguationData(false,false);  //we should only need this if it's the first time around.
                    //PluginsChanged();  //Go find out if we're enabled first time before subscribing.
                    IsInited = true;
                    //Singleton<PluginManager>.instance.eventPluginsChanged += new PluginManager.PluginsChangedHandler(PluginsChanged);
                    //Singleton<PluginManager>.instance.eventPluginsStateChanged += new PluginManager.PluginsChangedHandler(PluginsChanged);
                    if (DEBUG_LOG_ON) Logger.dbgLog("Mod has been initialized.");
                }
            }
            catch (Exception ex)
            {
                Logger.dbgLog("Error in init", ex, true);
            }
        }


        /// <summary>
        /// Fired when we are 'disabled' by the user and \ or when the game exits. 
        /// Note: the C\O call backs must not be static due to the way CO looks for them via reflection.
        /// </summary>
        public void OnDisabled()
        {
            try
            {
                IsEnabled = false;
                if (IsInited)
                {
                    un_init();

                    //1.6.0 - added next line to remove detours
                    ReveseSetup();

                    Logger.dbgLog(string.Concat("v", VERSION_BUILD_NUMBER, " Mod has been unloaded, disabled or game exiting."));
                }
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex, true); }
        }

        public static void un_init()
        {
            try
            {
                if (IsInited == true)
                {
                    //Singleton<PluginManager>.instance.eventPluginsChanged -= new PluginManager.PluginsChangedHandler(PluginsChanged);
                    //Singleton<PluginManager>.instance.eventPluginsStateChanged -= new PluginManager.PluginsChangedHandler(PluginsChanged);
                    IsInited = false;
                    isFirstInit = true;
                    if (DEBUG_LOG_ON) Logger.dbgLog("Mod has been un-initialized.");
                }
            }
            catch (Exception ex)
            {
                Logger.dbgLog("Error in un_init", ex, true);
            }
        }





        /// <summary>
        /// Called to go fetch the information from the config file and populate\refresh our public vars.
        /// 
        /// KRN: 10-6-2015: Probably can remove the use of the public vars and just use mod.config.xxx but
        /// don't want to change yet in this build enough churn already.
        /// </summary>
        /// <param name="bForceNewConfig"></param>
        private static void ReloadConfiguationData(bool bNoReReadConfig = false, bool bForceNewConfig = false)
        {
            try
            {
                if (bForceNewConfig) //manual create new one and save it.
                {
                    config = new Configuration();
                    Configuration.Serialize(MOD_CONFIGPATH, config);
                    if (DEBUG_LOG_ON) { Logger.dbgLog("New configuation file force created."); }
                }

                if(bNoReReadConfig==false)
                {
                    config = Configuration.Deserialize(MOD_CONFIGPATH);
                }
                if (config == null)
                {
                    //auto create new one and save it; we save it just so that we don't have to hit this again in the case some never touches 'options'.
                    //if save exceptions we in theory should still function due to setting things before the exception would happen - in theory.
                    config = new Configuration();
                    config.DebugLogging = false;
                    config.DebugLoggingLevel = 0;
                    config.UseNoWindEffects = false;
                    config.NullTreeOptionsIndex = 0;
                    config.EmergencyOnly_RemoveAllTrees = false;
                    config.UseCustomLogFile = false;
                    config.ExtraLogDataChecked = false;
                    Configuration.Serialize(MOD_CONFIGPATH, config);
                    if (DEBUG_LOG_ON) { Logger.dbgLog("New configuation file created."); }
                }
                if (config != null)
                {
                    Configuration.ValidateConfig(ref config);
                    DEBUG_LOG_ON = config.DebugLogging;
                    DEBUG_LOG_LEVEL = config.DebugLoggingLevel;
                    USE_NO_WINDEFFECTS = config.UseNoWindEffects;
                    UpdateScaleFactors();
                }
                if (DEBUG_LOG_ON) { Logger.dbgLog("Configuration data loaded or refreshed."); }
            }
            catch (Exception ex)
            {
                Logger.dbgLog("ReloadConfig Exceptioned:", ex, true);
            }

        }

        private static void UpdateScaleFactors()
        {
            MOD_TREE_SCALE = config.ScaleFactor;
            SCALED_TREE_COUNT = MOD_TREE_SCALE * DEFAULT_TREE_COUNT; //1048576
            SCALED_TREEUPDATE_COUNT = MOD_TREE_SCALE * DEFAULT_TREEUPDATE_COUNT;//  16384;
        }

        private void LoggingChecked(bool en)
        {
            if (config.ExtraLogDataChecked)
            { return; } //ignore it.
            DEBUG_LOG_ON = en;
            if (en)
            {
                DEBUG_LOG_LEVEL = 1;
            }
            else
            {
                DEBUG_LOG_LEVEL = 0;
            }
            config.DebugLogging = en;
            config.DebugLoggingLevel = DEBUG_LOG_LEVEL;
            Configuration.Serialize(MOD_CONFIGPATH, Mod.config);
        }

        private void UseNoWindChecked(bool en)
        {
            USE_NO_WINDEFFECTS = en;
            config.UseNoWindEffects = en;
            Configuration.Serialize(MOD_CONFIGPATH, Mod.config);
        }

        private void OnScaleFactorChange(float val)
        {
            config.ScaleFactor = (int)val;
            Configuration.Serialize(MOD_CONFIGPATH, Mod.config);
            UpdateScaleFactors();
            if (maxTreeLabel != null) 
            {
                maxTreeLabel.text = string.Format(CURRENTMAXTREES_FORMATTEXT, config.ScaleFactor.ToString(), SCALED_TREE_COUNT.ToString());
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

            config.NullTreeOptionsValue = tmp;
            config.NullTreeOptionsIndex = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }

        private void SeperateLogChecked(bool en)
        {
            config.UseCustomLogFile = en;
            Configuration.Serialize(MOD_CONFIGPATH, Mod.config);
        }

        private void ExtraLogDataChecked(bool en)
        {
            config.ExtraLogDataChecked = en;
            if (en == true)
            {
                if (config.DebugLogging == false)
                {
                    LoggingChecked(true);
                }
                Mod.DEBUG_LOG_LEVEL = 2;
                config.DebugLoggingLevel = 2;
            }
            //turn off
            if (en == false) 
            {
                if (config.DebugLogging)
                {
                    LoggingChecked(true);
                }
                else //should not be the case but..
                {
                    Mod.DEBUG_LOG_LEVEL = 0;
                    LoggingChecked(false);
                }
            }
            Configuration.Serialize(MOD_CONFIGPATH, Mod.config);
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
        private void OnDumpHelicopters()
        {
            try
            {
                UTDebugUtils.DumpHelicopters();
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
                SaveDataUtils.EraseBytesFromNamedKey(UTSaveDataContainer.DefaultContainername);
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
                    DEBUG_LOG_LEVEL = (byte)a;
                    Logger.dbgLog("Log level manually changed to " + a.ToString());
                }
                else { }
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex); }
        }


        /// <summary>
        /// Called by game upon user entering options screen.
        /// icities interface for gui option setup
        /// </summary>
        /// <param name="helper"></param>
        public void OnSettingsUI(UIHelperBase helper)
        {
            //1.6 debugging grrrr
            SimulationManager.UpdateMode tmpUpdateMode = SimulationManager.UpdateMode.Undefined; 
            try
            {
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
                {
                    SimulationManager SMgr = Singleton<SimulationManager>.instance;
                    if (SMgr != null && SMgr.m_metaData != null)
                    {
                        tmpUpdateMode = SMgr.m_metaData.m_updateMode;
                        Logger.dbgLog("updatemode: " + SMgr.m_metaData.m_updateMode.ToString());
                    }
                    else
                    {
                        Logger.dbgLog("updatemode: null or simmanager null");
                    }
 
                }
 
            }
            catch( Exception ex44)
            { Logger.dbgLog("Exception OnSettingsUI :", ex44, true); }


            try
            {
                //for setting up tooltips; let's subscribe to visibiliy event.
                UIHelper hp = (UIHelper)helper;
                UIScrollablePanel panel = (UIScrollablePanel)hp.self;
                panel.eventVisibilityChanged += eventVisibilityChanged;
                //regular
                UIHelperBase uIHelperBase = helper.AddGroup("Unlimited Trees Options");
                uIHelperBase.AddCheckbox("Disable tree effects on wind", Mod.USE_NO_WINDEFFECTS, new OnCheckChanged(UseNoWindChecked));
                uIHelperBase.AddCheckbox("Enable Verbose Logging", Mod.DEBUG_LOG_ON, new OnCheckChanged(LoggingChecked));
                string[] sOptions = new string[] { "DoNothing (Default)", "Replace ", "Remove" };
                uIHelperBase.AddDropdown("In case of tree errors:", sOptions, config.NullTreeOptionsIndex, NullTreeOptionsChanged);
                uIHelperBase.AddCheckbox("Enable Seperate Log", config.UseCustomLogFile, new OnCheckChanged(SeperateLogChecked));
                uIHelperBase.AddCheckbox("Enable Dev Level Logging", config.ExtraLogDataChecked, new OnCheckChanged(ExtraLogDataChecked));

                if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL > 1)
                {
                    UIHelperBase uIHelperBase2 = helper.AddGroup("In-Game-Only Debug Functions (Not meant for users)");

                    uIHelperBase2.AddButton("dmp allburningtrees", new OnButtonClicked(OnDumpAllBurningTrees));
                    uIHelperBase2.AddButton("dmp helicopters", new OnButtonClicked(OnDumpHelicopters));
                    uIHelperBase2.AddButton("ResetAllBurningTrees", new OnButtonClicked(ClearAllBurningDamaged));
                    uIHelperBase2.AddButton("ClearAllOurSaveDataFromThisFile", new OnButtonClicked(ClearAllSaveDataFromFile));
                    uIHelperBase2.AddTextfield("dbglevel:",Mod.DEBUG_LOG_LEVEL.ToString(),new OnTextChanged(OnDbgLevelTextChanged),new OnTextSubmitted(OnDbgLevelTextSubmit));
                }

                GenerateMaxTreeSliderandLablel(ref uIHelperBase, ref panel);
                

            }
            catch (Exception ex)
            { Logger.dbgLog("Exception setting options gui:",ex,true); }
        }

        private void GenerateMaxTreeSliderandLablel(ref UIHelperBase oUIHelperBase, ref UIScrollablePanel oPanel)
        {
            if (oUIHelperBase != null && oPanel !=null)
            {
                oUIHelperBase.AddSlider("Max # of trees scaling factor", 4.0f, 8.0f, 1.0f, (float)config.ScaleFactor, OnScaleFactorChange);
                oUIHelperBase.AddSpace(60);
                UISlider sld = oPanel.Find<UISlider>("Slider");
                if (sld != null)
                {
                    maxTreeSlider = sld;  //store so onlevelloaded can hide it.
                    oPanel.autoLayout = false;
                    maxTreeLabel = oPanel.AddUIComponent<UILabel>();
                    maxTreeLabel.name = "CurrentMaxTrees";
                    maxTreeLabel.absolutePosition = new Vector3(sld.absolutePosition.x, sld.absolutePosition.y + 28f);
                    maxTreeLabel.text = string.Format(CURRENTMAXTREES_FORMATTEXT, config.ScaleFactor.ToString(), SCALED_TREE_COUNT.ToString());
                }
            }
        }


        /// <summary>
        /// Our event handler for our options screen being shown.
        /// </summary>
        /// <param name="component">The UIComponent that fired the event.</param>
        /// <param name="value">Visability state - true when isVisiable</param>
        private static void eventVisibilityChanged(UIComponent component, bool value)
        {
            if (value)
            {
                component.eventVisibilityChanged -= eventVisibilityChanged; //we only want to fire once so unsub.
                component.parent.StartCoroutine(PopulateTooltips(component));
            }
        }


        /// <summary>
        /// Coroutine to populate tooltips information. Used to fire only ONCE about 1/2 a second after options dialog is made visable.
        /// Dev note:
        /// Unity coroutines are not really async, they don't fire off on some other thread, they are just a construct
        /// for either doing limited work per-frame (and return to where you left off upon the next frame), 
        /// or as in this cases a helpfull way of of saying...hey wait till xyz no matter how
        /// many frames have passed then start doing this on whatever frame is active at that point in time.
        /// saves you from having to implement your own time tracking\frame counting system.
        /// I'm using this delay because some UI items take a few ms to initialize before acting or reporting
        /// there values correctly.
        /// </summary>
        /// <param name="hlpComponent">The UIComponent that was triggered.</param>
        /// <returns></returns>
        public static System.Collections.IEnumerator PopulateTooltips(UIComponent hlpComponent)
        {
            yield return new WaitForSeconds(0.500f);
            try
            {
                List<UIDropDown> dd = new List<UIDropDown>();
                hlpComponent.GetComponentsInChildren<UIDropDown>(true, dd);
                if (dd.Count > 0)
                {
                    dd[0].tooltip = "Sets what you want the mod to do when missing trees\n or trees with serious errors are found.\n DoNothing= Let game errors happen normally.\n ReplaceTree= Replaces any missing tree with a default game tree\n RemoveTree = Deletes the tree\n*Any changes made are commited upon you saving the file after loading\n*So you may want to disable autosave if debugging.";
                    dd[0].selectedIndex = config.NullTreeOptionsIndex;
                }

                UICheckBox[] cbx = hlpComponent.GetComponentsInChildren<UICheckBox>(true);
                if (cbx != null && cbx.Length > 0)
                {
                    for (int i = 0; i < (cbx.Length); i++)
                    {
                        switch (cbx[i].text)
                        {
                            case "Enable Verbose Logging":
                                cbx[i].tooltip = "Enables detailed logging for debugging purposes.\nUnless there are problems you probably don't want to enable this.";
                                break;
                            case "Disable tree effects on wind":
                                cbx[i].tooltip = "Disable the normal game behavior of letting tree's effect \\ dilute the wind map. \n Option should be set before loading a map.";
                                break;
                            case "Enable Seperate Log":
                                cbx[i].tooltip = "Enables logging of UnlimitedTrees log data to a seperate file\n so you don't have to look through the game log.\nLocation can be changed, default is Tree_Unlimiter_Log.txt in game installation root folder.";
                                break;
                            case "Enable Dev Level Logging":
                                cbx[i].tooltip = "Enables logging of much more information to your log file\n Recommended only if you are having problems and someone\nhas asked you to enable it\nprobably in combination with custom log option.";
                                break;
                            default:
                                break;
                        }
                    }
                }
                UISlider sld = hlpComponent.GetComponentInChildren<UISlider>();
                if (sld != null)
                {
                    sld.tooltip = "Sets the maximum # of trees in increments of 262,144.\nSetting this above 4 (1 million trees) is not recommended and depending on your hardware\n it may cause performance or rendering issues.";
                }

                //buttons.
                UIButton[] cbx5 = hlpComponent.GetComponentsInChildren<UIButton>(true);
                if (cbx5 != null && cbx5.Length > 0)
                {
                    for (int i = 0; i < (cbx5.Length); i++)
                    {
                        switch (cbx5[i].text)
                        {
                            case "ResetAllBurningTrees":
                                cbx5[i].tooltip = "Resets all trees to not burning and not damaged.\nAlso wipes the burningtrees array to match.";
                                break;
                            case "ClearAllOurSaveDataFromThisFile":
                                cbx5[i].tooltip = "Wipes all UT data from currently loaded map file.\nNote** This does not remove tree from an active map\n It will just force the mod to re-save your data if needed.\n or not write new data if <262k trees";
                                break;
                            default:
                                break;
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                Logger.dbgLog("error populating tooltips", ex, true);
            }

            yield break; //equiv to return and don't reenter.
        }

//TODO:  Dead code - Remove this code in next release.
  /*
        /// <summary>
        /// Fired when user enables\disables mod, subscribes\unsubscribes, or after each plugin actually gets loaded.
        /// The only reason we track this is really because maybe some other mod upon loading might want to disable us.
        /// or maybe we want to disable ourselves or them. ie we could look for their id and act accordingly.
        /// 
        /// KH 10-6-2015: This construct really isn't needed atm. I'm leaving it for now though
        /// for max compatibility, the only down side is this fires off on every change to the user's plugins
        /// and costs a few nanoseconds.
        /// </summary>
*/
/*        public static void PluginsChanged()
        {
            try
            {
                PluginManager.PluginInfo pluginInfo = (
                    from p in Singleton<PluginManager>.instance.GetPluginsInfo()
#if (DEBUG)  //used for local debug testing
                    where p.name.ToString() == MOD_OFFICIAL_NAME
//#else   //used for steam distribution - public release.
                    where p.publishedFileID.AsUInt64 == MOD_WORKSHOPID
//#endif
                    select p).FirstOrDefault<PluginManager.PluginInfo>();

                if (pluginInfo == null)
                {
                    //If we can't find ourselves let's make darn sure we're not around anymore if we're still in ram.
                    //In theory we shouldn't really ever need this since we should kill our stuff
                    //During Mod.OnRemoved.... that said better to be safe right?
                    IsEnabled = false;
                    if (IsSetupActive == true) { Mod.ReveseSetup(); };
                    if (IsInited) { un_init(); }
                    Logger.dbgLog("Can't find self. No idea if this mod is enabled");
                }
                else
                {
                    IsEnabled = pluginInfo.isEnabled;
                    if (IsEnabled)
                    {
                        if (!DEBUG_LOG_ON)
                        {
                            DEBUG_LOG_LEVEL = 0;  //level must be set manually in configfile.
                        }
                        else
                        {
                            if (DEBUG_LOG_LEVEL > 0) Logger.dbgLog(string.Concat(DateTime.Now.ToString(), "  Mod is enabled, wkshpid:", pluginInfo.publishedFileID.AsUInt64.ToString(), " plugcount:", Singleton<PluginManager>.instance.GetPluginsInfo().Count().ToString(), " DebugLogLevel:", DEBUG_LOG_LEVEL.ToString()));
                        }

                    }
                    else //we're disabled
                    {
                        if (DEBUG_LOG_LEVEL > 0) Logger.dbgLog(string.Concat(DateTime.Now.ToString(), "  Mod is now disabled, wkshpid:", pluginInfo.publishedFileID.AsUInt64.ToString(), " plugcount:", Singleton<PluginManager>.instance.GetPluginsInfo().Count().ToString()));
                        if (IsSetupActive)
                        {
                            ReveseSetup();  //make sure we've cleanuped after being disabled.
                        }
                        if (IsInited) { un_init(); }
                    }
                }
            }
            catch (Exception ex1)
            {
                Logger.dbgLog("PluginChanged exception:", ex1, true);
            }
        }
        */
        //End Dead Code


        /// <summary>
        /// This guy is our wrapper to doing the detours. it does the detour and then adds the returned
        /// RedirectCallState object too our dictionary for later reversal.
        /// </summary>
        /// <param name="type1">The original type of the method we're detouring</param>
        /// <param name="type2">Our replacement type of the method we're detouring</param>
        /// <param name="p">The original method\function name</param>
        private static void RedirectCalls(Type type1, Type type2, string p)
        {
            var bindflags1 = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var bindflags2 = BindingFlags.Static | BindingFlags.NonPublic;
            var theMethod = type1.GetMethod(p, bindflags1);
            //var replacementMethod = type2.GetMethod(p,bindflags2);
            //if (theMethod == null || replacementMethod == null)
            //{
            //    Logger.dbgLog("Failed to locate function: " + p + ((theMethod == null) ? "  orignal":"  replacement"));
            //}
            //if (Mod.DEBUG_LOG_ON)
            //{
                //redirectDic.Add(theMethod, RedirectionHelper.RedirectCalls(theMethod, type2.GetMethod(p, bindflags2), true)); //makes the actual detour and stores the callstate info.
            //}
            //else 
            //{
                redirectDic.Add(theMethod, RedirectionHelper.RedirectCalls(theMethod, type2.GetMethod(p, bindflags2), false)); //makes the actual detour and stores the callstate info.
            //}

                //if (Mod.DEBUG_LOG_ON)
                //{
                    //Logger.dbgLog(p.ToString() + " redirected");
                //}

            //RedirectionHelper.RedirectCalls(type1.GetMethod(p, bindflags1), type2.GetMethod(p, bindflags2), false);
        }



        /// <summary>
        /// Sets up our redirects of our replacement methods.
        /// </summary>
        public static void Setup()
        {
            if (IsSetupActive) { return; }

            try
            {
                RedirectCalls(typeof(BuildingDecoration), typeof(LimitBuildingDecoration), "ClearDecorations");
                RedirectCalls(typeof(BuildingDecoration), typeof(LimitBuildingDecoration), "SaveProps");
                RedirectCalls(typeof(NaturalResourceManager), typeof(LimitNaturalResourceManager), "TreesModified");
                RedirectCalls(typeof(TreeTool), typeof(LimitTreeTool), "ApplyBrush");

               
                RedirectCalls(typeof(TreeManager.Data), typeof(LimitTreeManager.Data), "Deserialize");
                RedirectCalls(typeof(TreeManager.Data), typeof(LimitTreeManager.Data), "Serialize");
                RedirectCalls(typeof(TreeManager.Data), typeof(LimitTreeManager.Data), "AfterDeserialize");

                MethodInfo[] methods = typeof(LimitTreeManager).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic);
                for (int i = 0; i < (int)methods.Length; i++)
                {
                    MethodInfo methodInfo = methods[i];
                    RedirectCalls(typeof(TreeManager), typeof(LimitTreeManager), methodInfo.Name);
                }

                RedirectCalls(typeof(LimitCommonBuildingAI), typeof(CommonBuildingAI), "TrySpreadFire");
                RedirectCalls(typeof(CommonBuildingAI), typeof(LimitCommonBuildingAI), "HandleFireSpread");
                RedirectCalls(typeof(DisasterHelpers), typeof(LimitDisasterHelpers), "DestroyTrees");
                RedirectCalls(typeof(FireCopterAI), typeof(LimitFireCopterAI), "FindBurningTree");
                RedirectCalls(typeof(ForestFireAI), typeof(LimitForestFireAI), "FindClosestTree");


                //If windoverride enabled, otherwise don't.
                if (USE_NO_WINDEFFECTS){RedirectCalls(typeof(WeatherManager), typeof(LimitWeatherManager), "CalculateSelfHeight");}

                IsSetupActive = true;
                if (DEBUG_LOG_ON) { Logger.dbgLog("Redirected calls."); }
                if (DEBUG_LOG_ON) 
                {
                    foreach (var keypair in redirectDic)
                    {
                        Logger.dbgLog(keypair.Key.Name + " " + keypair.Value.f.ToString());
                    }
                }
            }
            catch (Exception exception1)
            {
                Logger.dbgLog("Setup error:",exception1,true);
            }
        }

        /// <summary>
        /// Reverses our redirects from ours back to C/O's
        /// </summary>
        public static void ReveseSetup()
        {
            if (IsSetupActive == false) 
            {
                if (DEBUG_LOG_ON) { Logger.dbgLog("Redirects are not active no need to reverse."); }
                return; 
            }
            if (redirectDic.Count == 0)
            {
                if (DEBUG_LOG_ON) { Logger.dbgLog("No state entries exists to revert. clearing state?"); }
                //added 1.6.0 don't we need this, was there a reason we didn't before?
                IsSetupActive = false; 
                //end 1.6.0 add
                return;
            }
            try
            {
                foreach (var keypair in redirectDic)
                {
                    RedirectionHelper.RevertRedirect(keypair.Key, keypair.Value);
                }
                redirectDic.Clear();
                IsSetupActive = false;
                if (DEBUG_LOG_ON) { Logger.dbgLog("Reverted redirected calls."); }
            }
            catch (Exception exception1)
            { Logger.dbgLog("ReverseSetup error:",exception1,true); }
        }

    }
}