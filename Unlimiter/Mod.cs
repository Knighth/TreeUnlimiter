using CitiesSkylinesDetour;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.Steamworks;
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
        internal const long MOD_WORKSHOPID = 455403039L;
        internal const string MOD_OFFICIAL_NAME = "Unlimited Trees Mod";
        internal const string VERSION_BUILD_NUMBER = "1.0.2.0-f3 build_004";
        internal const string MOD_DESCRIPTION = "Allows you to place way more trees!";
        public const int DEFAULT_TREE_COUNT = 262144;
        public const int DEFAULT_TREEUPDATE_COUNT = 4096;
        public const int OUR_TREEUPDATE_COUNT = 16384;
        public static readonly string MOD_CONFIGPATH = "TreeUnlimiterConfig.xml";
        public static bool IsEnabled = false;
        public static bool IsInited = false;
        public static bool IsSetupActive = false;
        public static bool DEBUG_LOG_ON = false;
        public static byte DEBUG_LOG_LEVEL = 0;
        public static bool USE_NO_WINDEFFECTS = false;

        //9-25-2015--notneeded    public static SimulationManager.UpdateMode LastMode = SimulationManager.UpdateMode.Undefined;
        private static Dictionary<MethodInfo, RedirectCallsState> redirectDic = new Dictionary<MethodInfo, RedirectCallsState>();
        public Configuration config;


        public string Name
        {
            get
            {
                if (!Mod.IsInited)
                {
                    Mod.init();
                }
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


        public Mod()
        {
            config = Configuration.Deserialize(MOD_CONFIGPATH);
            if (config == null)
            {
                config = new Configuration();
                config.DebugLogging = false;
                config.DebugLoggingLevel = 0;
                config.UseNoWindEffects = false;
            }
            if (config != null)
            {
                DEBUG_LOG_ON = config.DebugLogging;
                DEBUG_LOG_LEVEL = config.DebugLoggingLevel;
                USE_NO_WINDEFFECTS = config.UseNoWindEffects;
            }
        }


        private void LoggingChecked(bool en)
        {
            DEBUG_LOG_ON = en;
            config.DebugLogging = en;
            Configuration.Serialize(MOD_CONFIGPATH, this.config);
        }
        private void UseNoWindChecked(bool en)
        {
            USE_NO_WINDEFFECTS = en;
            config.UseNoWindEffects = en;
            Configuration.Serialize(MOD_CONFIGPATH, this.config);
        }

        //icities interface for gui option setup
        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase uIHelperBase = helper.AddGroup("Unlimited Trees Options");
            uIHelperBase.AddCheckbox("Disable tree effects on wind", Mod.USE_NO_WINDEFFECTS, new OnCheckChanged(UseNoWindChecked));
            uIHelperBase.AddCheckbox("Enable Verbose Logging", Mod.DEBUG_LOG_ON, new OnCheckChanged(LoggingChecked));
        }


        //fired when user enables\disables mod or when they leave an options screen.
        public static void PluginsChanged()
        {
            try
            {
                PluginManager.PluginInfo pluginInfo = (
                    from p in Singleton<PluginManager>.instance.GetPluginsInfo()
#if (DEBUG)  //used for local debug testing
                    where p.name.ToString() == MOD_OFFICIAL_NAME
#else   //used for steam distribution - public release.
                    where p.publishedFileID.AsUInt64 == (long)MOD_WORKSHOPID
#endif
                    select p).FirstOrDefault<PluginManager.PluginInfo>();

                if (pluginInfo == null)
                {
                    IsEnabled = false;
//                    if (IsSetupActive == true) { Mod.ReveseSetup(); }; //if we can't find ourselves let's man darn sure we're not around anymore.
                    Debug.Log("[TreeUnlimiter::PluginsChanged()] Can't find self. No idea if this mod is enabled");
                }
                else
                {
                    IsEnabled = pluginInfo.isEnabled;
                    if (IsEnabled)
                    {
                        Debug.Log("[TreeUnlimiter] v" + VERSION_BUILD_NUMBER + "  Mod is enabled, id:" + pluginInfo.publishedFileID.AsUInt64.ToString());
                        if (!DEBUG_LOG_ON)
                        {
                            //9-25-2015 I should probably dump this it's hangover i've all but removed 
                            DEBUG_LOG_LEVEL = 0;  //level must be set manually in configfile.
                        }
                        else
                        {
                            Debug.Log("[TreeUnlimiter:PluginChanged] DEBUG_LOG_LEVEL " + DEBUG_LOG_LEVEL.ToString());
                        }
                    }
                    else
                    {
                        Debug.Log("[TreeUnlimiter] Mod is disabled, id:" + pluginInfo.publishedFileID.AsUInt64.ToString());
                        if (IsSetupActive)
                        {
                            ReveseSetup();  //make sure we've cleanuped after being disabled.
                        }
                    }
                }
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                Debug.LogException(exception);
                object[] type = new object[] { "[TreeUnlimiter::PluginsChanged()] ", exception.GetType(), ": ", exception.Message };
                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning, string.Concat(type));
                Debug.Log(string.Concat("[TreeUnlimiter::PluginsChanged()] ", exception.Message.ToString()));
            }
        }

        private static void RedirectCalls(Type type1, Type type2, string p)
        {
            var bindflags1 = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var bindflags2 = BindingFlags.Static | BindingFlags.NonPublic;
            var theMethod = type1.GetMethod(p, bindflags1);
            redirectDic.Add(theMethod, RedirectionHelper.RedirectCalls(theMethod, type2.GetMethod(p, bindflags2), false));
            //RedirectionHelper.RedirectCalls(type1.GetMethod(p, bindflags1), type2.GetMethod(p, bindflags2), false);
        }

        public static void init()
        {
            if (IsInited == false)
            {
                PluginsChanged();
                Singleton<PluginManager>.instance.eventPluginsChanged += new PluginManager.PluginsChangedHandler(PluginsChanged);
                Singleton<PluginManager>.instance.eventPluginsStateChanged += new PluginManager.PluginsChangedHandler(PluginsChanged);
                IsInited = true;
            }
        }

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
                //If windoverride enabled.
                if (USE_NO_WINDEFFECTS){RedirectCalls(typeof(WindManager), typeof(LimitWindManager), "CalculateSelfHeight");}

                IsSetupActive = true;
                if (DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter::Mod.Setup()]: Redirected Calls"); }
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                //DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, exception.ToString());
                Debug.Log("[TreeUnlimiter::Mod.Setup() Exception]: " + exception.Message.ToString());
            }
        }


        public static void ReveseSetup()
        {
            if (IsSetupActive == false) { return; }
            if (redirectDic.Count == 0)
            {
                if (DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter::ReverseSetup()] No state entries exists to Revert"); }
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
                if (DEBUG_LOG_ON == true) { Debug.Log("[TreeUnlimiter::ReverseSetup()] Reverted redirected calls"); }
            }
            catch (Exception exception1)
            { Debug.Log("TreeUnlimiter::ReverseSetup()] " + exception1.Message.ToString()); }
        }

    }
}