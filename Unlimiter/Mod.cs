using CitiesSkylinesDetour;
using ColossalFramework;
using ColossalFramework.Plugins;
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
        internal const ulong MOD_WORKSHOPID = 455403039uL; //technically not needed but I store it here in case I need it.
        internal const string MOD_OFFICIAL_NAME = "Unlimited Trees Mod";
        internal const string VERSION_BUILD_NUMBER = "1.0.7.0-f5 build_01";
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
        private static Dictionary<MethodInfo, RedirectCallsState> redirectDic = new Dictionary<MethodInfo, RedirectCallsState>();  //holds our redirects.
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

        //12-22-2016 Dead code below as of 1.6.2_build10, as is my nature, leaving around for a release or two.

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
        //public static System.Collections.IEnumerator PopulateTooltips(UIComponent hlpComponent)
        //{
        //    yield return new WaitForSeconds(0.500f);
        //    try
        //    {
        //        List<UIDropDown> dd = new List<UIDropDown>();
        //        hlpComponent.GetComponentsInChildren<UIDropDown>(true, dd);
        //        if (dd.Count > 0)
        //        {
        //            dd[0].tooltip = "Sets what you want the mod to do when missing trees\n or trees with serious errors are found.\n DoNothing= Let game errors happen normally.\n ReplaceTree= Replaces any missing tree with a default game tree\n RemoveTree = Deletes the tree\n*Any changes made are commited upon you saving the file after loading\n*So you may want to disable autosave if debugging.";
        //            dd[0].selectedIndex = config.NullTreeOptionsIndex;
        //        }

        //        UICheckBox[] cbx = hlpComponent.GetComponentsInChildren<UICheckBox>(true);
        //        if (cbx != null && cbx.Length > 0)
        //        {
        //            for (int i = 0; i < (cbx.Length); i++)
        //            {
        //                switch (cbx[i].text)
        //                {
        //                    case "Enable Verbose Logging":
        //                        cbx[i].tooltip = "Enables detailed logging for debugging purposes.\nUnless there are problems you probably don't want to enable this.";
        //                        break;
        //                    case "Disable tree effects on wind":
        //                        cbx[i].tooltip = "Disable the normal game behavior of letting tree's effect \\ dilute the wind map. \n Option should be set before loading a map.";
        //                        break;
        //                    case "Enable Seperate Log":
        //                        cbx[i].tooltip = "Enables logging of UnlimitedTrees log data to a seperate file\n so you don't have to look through the game log.\nLocation can be changed, default is Tree_Unlimiter_Log.txt in game installation root folder.";
        //                        break;
        //                    case "Enable Dev Level Logging":
        //                        cbx[i].tooltip = "Enables logging of much more information to your log file\n Recommended only if you are having problems and someone\nhas asked you to enable it\nprobably in combination with custom log option.";
        //                        break;
        //                    case "Enable Ghost Mode":
        //                        cbx[i].tooltip = "(advanced) Enables Mod to stay active but act like it's not, ignoring 'extra' UT tree data during load.\n This mode only exists to allow you to load a map that has 'extra' UT data WITHOUT actually loading that data the game will act as if UT is not loaded.\n For your own safety you can not change this setting in-game.";
        //                        break;
        //                    default:
        //                        break;
        //                }
        //            }
        //        }
        //        UISlider sld = hlpComponent.GetComponentInChildren<UISlider>();
        //        if (sld != null)
        //        {
        //            sld.tooltip = "Sets the maximum # of trees in increments of 262,144.\nSetting this above 4 (1 million trees) is not recommended and depending on your hardware\n it may cause performance or rendering issues.";
        //        }

        //        //buttons.
        //        UIButton[] cbx5 = hlpComponent.GetComponentsInChildren<UIButton>(true);
        //        if (cbx5 != null && cbx5.Length > 0)
        //        {
        //            for (int i = 0; i < (cbx5.Length); i++)
        //            {
        //                switch (cbx5[i].text)
        //                {
        //                    case "ResetAllBurningTrees":
        //                        cbx5[i].tooltip = "Resets all trees to not burning and not damaged.\nAlso wipes the burningtrees array to match.";
        //                        break;
        //                    case "ClearAllOurSaveDataFromThisFile":
        //                        cbx5[i].tooltip = "Wipes all UT data from currently loaded map file.\nNote** This does not remove tree from an active map\n It will just force the mod to re-save your data if needed.\n or not write new data if <262k trees";
        //                        break;
        //                    default:
        //                        break;
        //                }
        //            }
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.dbgLog("error populating tooltips", ex, true);
        //    }

        //    yield break; //equiv to return and don't reenter.
        //}




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
                if (Mod.DEBUG_LOG_ON)
                {
                    Logger.dbgLog(string.Format("redirected from {0}.{2} to {1}.{2}", type1.Name, type2.Name, p));
                }
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

                //reverse redirects
                RedirectCalls(typeof(LimitCommonBuildingAI), typeof(CommonBuildingAI), "TrySpreadFire");
                RedirectCalls(typeof(LimitTreeManager), typeof(TreeManager), "TrySpreadFire");
                //end reverse redirects

                RedirectCalls(typeof(CommonBuildingAI), typeof(LimitCommonBuildingAI), "HandleFireSpread");
                RedirectCalls(typeof(DisasterHelpers), typeof(LimitDisasterHelpers), "DestroyTrees");
                RedirectCalls(typeof(FireCopterAI), typeof(LimitFireCopterAI), "FindBurningTree");
                RedirectCalls(typeof(ForestFireAI), typeof(LimitForestFireAI), "FindClosestTree");


                //If windoverride enabled, otherwise don't.
                if (USE_NO_WINDEFFECTS){RedirectCalls(typeof(WeatherManager), typeof(LimitWeatherManager), "CalculateSelfHeight");}

                IsSetupActive = true;
                if (DEBUG_LOG_ON) { Logger.dbgLog("Redirected calls completed."); }
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