using CitiesSkylinesDetour;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.Steamworks;
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
        internal const int MOD_TREE_SCALE = 4;
        internal const ulong MOD_WORKSHOPID = 455403039uL;
        internal const string MOD_OFFICIAL_NAME = "Unlimited Trees Mod";
        internal const string VERSION_BUILD_NUMBER = "1.0.2.1-f1 build_002";
        internal const string MOD_DESCRIPTION = "Allows you to place way more trees!";
        internal const string MOD_DBG_Prefix = "TreeUnlimiter";
        public static readonly string MOD_CONFIGPATH = "TreeUnlimiterConfig.xml";
        public static readonly string MOD_DEFAULT_LOG_PATH = "TreeUnlimiter_Log.txt";
        public const int DEFAULT_TREE_COUNT = 262144;
        public const int DEFAULT_TREEUPDATE_COUNT = 4096;
        public const int OUR_TREEUPDATE_COUNT = 16384;
        public static bool IsEnabled = false;
        public static bool IsInited = false;
        public static bool IsSetupActive = false;
        public static bool DEBUG_LOG_ON = false;
        public static byte DEBUG_LOG_LEVEL = 0;
        public static bool USE_NO_WINDEFFECTS = false;
        private static bool isFirstInit = true;
        //9-25-2015--notneeded    public static SimulationManager.UpdateMode LastMode = SimulationManager.UpdateMode.Undefined;
        private static Dictionary<MethodInfo, RedirectCallsState> redirectDic = new Dictionary<MethodInfo, RedirectCallsState>();
        public static Configuration config;


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
            if (!Mod.IsInited)
            {
                //we hard code this one, assuming there is a problem with our own logger just so we know we're loaded.
                Debug.Log("[TreeUnlimiter] v" + VERSION_BUILD_NUMBER + " Mod has been loaded."); 
                Mod.init();
            }
        }



        /// <summary>
        /// Fired when when mod is loaded and usersetting have us marked as enabled, OR when the user specifically enables us.
        /// Timing wise during game bootup - this fires later in the load process - ie after all dll's have been 'loaded'.
        /// Note: the C\O call backs must not be static due to the way CO looks for them via reflection.
        /// </summary>
        public void OnEnabled()
        {
            IsEnabled = true;
            Logger.dbgLog(string.Concat("v", VERSION_BUILD_NUMBER, " Mod has been enabled. ",DateTime.Now.ToString()));
            if (IsInited == false)
            {
                init();  //init will do a data pull from config off disk
            }
            if (!isFirstInit)
            {
                ReloadConfiguationData(false, false); //always regrab\refresh our info on-enabled.
                //EXCEPT when it's our very first time around where idfirstinit will be true.
                //handles case of user loading with enabled, then disabling, changing config txt manually, then re-enabling.
                // ..edge case of course but why make them restart to pick up custom config change or pull from disk twice
                //during our first load just to save a few bytes?
            }
            else
            {
                isFirstInit = false; // our very first load flag 
            }
        }

        public static void init()
        {
            try
            {
                if (IsInited == false)
                {
                    ReloadConfiguationData(false,false);  //we should only need this if it's the first time around.
                    PluginsChanged();  //Go find out if we're enabled first time before subscribing.
                    IsInited = true;
                    Singleton<PluginManager>.instance.eventPluginsChanged += new PluginManager.PluginsChangedHandler(PluginsChanged);
                    Singleton<PluginManager>.instance.eventPluginsStateChanged += new PluginManager.PluginsChangedHandler(PluginsChanged);
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
        public void OnRemoved()
        {
            IsEnabled = false;
            if (IsInited)
            {
                un_init();
                Logger.dbgLog(string.Concat("v", VERSION_BUILD_NUMBER, " Mod has been unloaded, disabled or game exiting."));
            }
        }

        public static void un_init()
        {
            try
            {
                if (IsInited == true)
                {
                    Singleton<PluginManager>.instance.eventPluginsChanged -= new PluginManager.PluginsChangedHandler(PluginsChanged);
                    Singleton<PluginManager>.instance.eventPluginsStateChanged -= new PluginManager.PluginsChangedHandler(PluginsChanged);
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
                    Configuration.Serialize(MOD_CONFIGPATH, config);
                    if (DEBUG_LOG_ON) { Logger.dbgLog("New configuation file created."); }
                }
                if (config != null)
                {
                    DEBUG_LOG_ON = config.DebugLogging;
                    DEBUG_LOG_LEVEL = config.DebugLoggingLevel;
                    USE_NO_WINDEFFECTS = config.UseNoWindEffects;
                }
                if (DEBUG_LOG_ON) { Logger.dbgLog("Configuration data loaded or refreshed."); }
            }
            catch (Exception ex)
            {
                Logger.dbgLog("ReloadConfig Exceptioned:", ex, true);
            }

        }


        private void LoggingChecked(bool en)
        {
            DEBUG_LOG_ON = en;
            config.DebugLogging = en;
            Configuration.Serialize(MOD_CONFIGPATH, Mod.config);
        }

        private void UseNoWindChecked(bool en)
        {
            USE_NO_WINDEFFECTS = en;
            config.UseNoWindEffects = en;
            Configuration.Serialize(MOD_CONFIGPATH, Mod.config);
        }


        /// <summary>
        /// Called by game upon user entering options screen.
        /// icities interface for gui option setup
        /// </summary>
        /// <param name="helper"></param>
        public void OnSettingsUI(UIHelperBase helper)
        {
            //for setting up tooltips; let's subscribe to visibiliy event.
            UIHelper hp = (UIHelper)helper;
            UIScrollablePanel panel = (UIScrollablePanel)hp.self;
            panel.eventVisibilityChanged += eventVisibilityChanged;
            //regular
            UIHelperBase uIHelperBase = helper.AddGroup("Unlimited Trees Options");
            uIHelperBase.AddCheckbox("Disable tree effects on wind", Mod.USE_NO_WINDEFFECTS, new OnCheckChanged(UseNoWindChecked));
            uIHelperBase.AddCheckbox("Enable Verbose Logging", Mod.DEBUG_LOG_ON, new OnCheckChanged(LoggingChecked));
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
        /// Unless you actually want something off-thread in which case the approach would be different and more complex.
        /// </summary>
        /// <param name="hlpComponent">The UIComponent that was triggered.</param>
        /// <returns></returns>
        public static System.Collections.IEnumerator PopulateTooltips(UIComponent hlpComponent)
        {
            yield return new WaitForSeconds(0.500f);
            try
            {
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
                            default:
                                break;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                /* Doesn't really matter but let's log it anyway. */
                Logger.dbgLog("", ex, true);
            }

            yield break; //equiv to return and don't reenter.
        }


        /// <summary>
        /// Fired when user enables\disables mod, subscribes\unsubscribes, or after each plugin actually gets loaded.
        /// The only reason we track this is really because maybe some other mod upon loading might want to disable us.
        /// or maybe we want to disable ourselves or them. ie we could look for their id and act accordingly.
        /// 
        /// KH 10-6-2015: This construct really isn't needed atm. I'm leaving it for now though
        /// for max compatibility, the only down side is this fires off on every change to the user's plugins
        /// and costs a few nanoseconds.
        /// </summary>
        public static void PluginsChanged()
        {
            try
            {
                PluginManager.PluginInfo pluginInfo = (
                    from p in Singleton<PluginManager>.instance.GetPluginsInfo()
#if (DEBUG)  //used for local debug testing
                    where p.name.ToString() == MOD_OFFICIAL_NAME
#else   //used for steam distribution - public release.
                    where p.publishedFileID.AsUInt64 == MOD_WORKSHOPID
#endif
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
            redirectDic.Add(theMethod, RedirectionHelper.RedirectCalls(theMethod, type2.GetMethod(p, bindflags2), false)); //makes the actual detour and stores the callstate info.
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
                RedirectCalls(typeof(TreeManager.Data), typeof(LimitTreeManager.Data), "Serialize");
                RedirectCalls(typeof(TreeManager.Data), typeof(LimitTreeManager.Data), "Deserialize");
                MethodInfo[] methods = typeof(LimitTreeManager).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic);
                for (int i = 0; i < (int)methods.Length; i++)
                {
                    MethodInfo methodInfo = methods[i];
                    RedirectCalls(typeof(TreeManager), typeof(LimitTreeManager), methodInfo.Name);
                }
                //If windoverride enabled, otherwise don't.
                if (USE_NO_WINDEFFECTS){RedirectCalls(typeof(WindManager), typeof(LimitWindManager), "CalculateSelfHeight");}

                IsSetupActive = true;
                if (DEBUG_LOG_ON) { Logger.dbgLog("Redirected calls."); }
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
            if (IsSetupActive == false) { return; }
            if (redirectDic.Count == 0)
            {
                if (DEBUG_LOG_ON) { Logger.dbgLog("No state entries exists to revert."); }
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