using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TreeUnlimiter.Detours;
using TreeUnlimiter.RedirectionFramework;
using UnityEngine;

namespace TreeUnlimiter
{
    public class Mod : IUserMod
    {
        internal const ulong MOD_WORKSHOPID = 455403039uL; //technically not needed but I store it here in case I need it.
        internal const string MOD_OFFICIAL_NAME = "Unlimited Trees Mod";
        internal const string VERSION_BUILD_NUMBER = "1.0.8.0-f3 build_01";
        internal const string MOD_DESCRIPTION = "Allows you to place way more trees!";
        internal const string MOD_DBG_Prefix = "TreeUnlimiter";
        internal const string CURRENTMAXTREES_FORMATTEXT = "ScaleFactor: {0}   Maximum trees: {1}";
        internal const string DTMilli = "MM/dd/yyyy hh:mm:ss.fff tt";  //used for time formatting.
        internal const string MOD_OrgDataKEYNAME = "mabako/unlimiter";  //where we store treedata
        public const int MAX_NET_SEGMENTS = 36864; //used in BuildingDecorations
        public const int MAX_NET_NODES = 32768;  //used in BuildingDecorations
        public static readonly string MOD_CONFIGPATH = "TreeUnlimiterConfig.xml";
        public static readonly string MOD_DEFAULT_LOG_PATH = "TreeUnlimiter_Log.txt";
        public static int MOD_TREE_SCALE = 4;
        public const int DEFAULT_TREE_COUNT = 262144;
        public const int DEFAULT_TREEUPDATE_COUNT = 4096;
        internal const int FormatVersion1NumOfTrees = 1048576;
        internal const ushort CurrentFormatVersion = 3;  //don't change unless really have to same.
        public static int SCALED_TREE_COUNT = MOD_TREE_SCALE * DEFAULT_TREE_COUNT; //1048576
        public static int SCALED_TREEUPDATE_COUNT = MOD_TREE_SCALE * DEFAULT_TREEUPDATE_COUNT;//  16384;
        public static bool IsEnabled = false;
        public static bool IsInited = false;
        public static bool IsSetupActive = false;  //tracks if detours active.
        public static bool IsGhostMode = false;
        public static bool DEBUG_LOG_ON = false;
        public static byte DEBUG_LOG_LEVEL = 0;
        public static bool USE_NO_WINDEFFECTS = false;
        private static bool isFirstInit = true;

        public static Configuration config;  //holds static copy of our saved (xml) config data.
        internal static UTSettingsUI oSettings;  //holds an instance of our options UI stuff.

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
        /// We always spit out our version stamp to the MAIN log.
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
        /// Or that was true back in 1.1 timeframe.
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

                    Redirector<LimitBuildingDecoration>.Revert();
                    Redirector<LimitNaturalResourceManager>.Revert();
                    Redirector<LimitTreeTool>.Revert();
                    Redirector<LimitTreeManager>.Revert();
                    Redirector<LimitTreeManager.Data>.Revert();
                    Redirector<LimitCommonBuildingAI>.Revert();
                    Redirector<LimitDisasterHelpers>.Revert();
                    Redirector<LimitFireCopterAI>.Revert();
                    Redirector<LimitForestFireAI>.Revert();
                    Redirector<LimitWeatherManager>.Revert();

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
                    oSettings = null;
                    IsGhostMode = false;
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
                    Logger.dbgLog("New configuation file created. forced"); 
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
                    config.GhostModeEnabled = false;
                    Configuration.Serialize(MOD_CONFIGPATH, config);
                    if (DEBUG_LOG_ON) { Logger.dbgLog("New configuation file created."); }
                }
                if (config != null)
                {
                    Configuration.ValidateConfig(ref config);
                    DEBUG_LOG_ON = config.DebugLogging;
                    DEBUG_LOG_LEVEL = config.DebugLoggingLevel;
                    IsGhostMode = config.GhostModeEnabled;
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


        internal static void UpdateScaleFactors()
        {
            Mod.MOD_TREE_SCALE = Mod.config.ScaleFactor;
            Mod.SCALED_TREE_COUNT = Mod.MOD_TREE_SCALE * Mod.DEFAULT_TREE_COUNT; //1048576
            Mod.SCALED_TREEUPDATE_COUNT = Mod.MOD_TREE_SCALE * Mod.DEFAULT_TREEUPDATE_COUNT;//  16384;
        }


                /// <summary>
        /// Called by game upon user entering options screen.
        /// icities interface for gui option setup
        /// </summary>
        /// <param name="helper"></param>
        public void OnSettingsUI(UIHelperBase helper)
        {
            try
            {
                oSettings = new UTSettingsUI();
                oSettings.BuildSettingsUI(ref helper);
            }
            catch (Exception ex44)
            { Logger.dbgLog("Exception OnSettingsUI :", ex44, true); }
        }

        /// <summary>
        /// Sets up our redirects of our replacement methods.
        /// </summary>
        public static void Setup()
        {
            if (IsSetupActive) { return; }

            try
            {
                Redirector<LimitBuildingDecoration>.Deploy();
                Redirector<LimitNaturalResourceManager>.Deploy();
                Redirector<LimitTreeTool>.Deploy();
                Redirector<LimitTreeManager>.Deploy();
                Redirector<LimitTreeManager.Data>.Deploy();
                Redirector<LimitCommonBuildingAI>.Deploy();
                Redirector<LimitDisasterHelpers>.Deploy();
                Redirector<LimitFireCopterAI>.Deploy();
                Redirector<LimitForestFireAI>.Deploy();

                //If windoverride enabled, otherwise don't.
                if (USE_NO_WINDEFFECTS)
                {
                    Redirector<LimitWeatherManager>.Deploy();
                }

                IsSetupActive = true;
                if (DEBUG_LOG_ON) { Logger.dbgLog("Redirected calls completed."); }
            }
            catch (Exception exception1)
            {
                Logger.dbgLog("Setup error:",exception1,true);
            }
        }
    }
}