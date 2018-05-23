using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using TreeUnlimiter.OptionsFramework.Attibutes;
using UnityEngine;

namespace TreeUnlimiter
{
    [Options("TreeUnlimiter", "TreeUnlimiterConfig")]
    public class Configuration
    {
        private const string UnlimitedTreesOptions = "Unlimited Trees Options";
        private const string InGameOnlyDebugFunctions = "In-Game-Only Debug Functions (Not meant for users)";

        [Description("Disable the normal game behavior of letting tree's height effect \\ dilute the wind map. \n Option should be set before loading a map.")]
        [Checkbox("Disable tree effects on wind", UnlimitedTreesOptions)]
        public bool UseNoWindEffects { get; set; } = false;

        [Description("None= Logging disabled\nBasic= Enables logging for basic debugging and informational purposes.\nUnless there are problems you probably don't need to enable this.\nAdvanced= Enables logging of much more information to your log file\n Recommended only if you are having problems and someone\nhas asked you to enable it\nprobably in combination with custom log option.")]
        [EnumDropDown("Log level", typeof(Logger.LoggingLevel), UnlimitedTreesOptions)]
        public int DebugLoggingLevel { get; set; } = 0;

        [Description("Enables logging of UnlimitedTrees log data to a seperate file\n so you don't have to look through the game log.\nLocation can be changed, default is TreeUnlimiter_Log.txt in game installation root folder.")]
        [Checkbox("Enable Seperate Log", UnlimitedTreesOptions)]
        public bool UseCustomLogFile { get; set; } = false;

        public string CustomLogFilePath { get; set; } = Mod.MOD_DEFAULT_LOG_PATH;

        [Description("Sets what you want the mod to do when missing trees\n or trees with serious errors are found.\n DoNothing= Let game errors happen normally.\n ReplaceTree= Replaces any missing tree with a default game tree\n RemoveTree = Deletes the tree\n*Any changes made are commited upon you saving the file after loading\n*So you may want to disable autosave if debugging.")]
        [EnumDropDown("In case of tree errors:", typeof(TreePrefabsDebug.NullTreeOptions), UnlimitedTreesOptions)]
        public int NullTreeOptionsIndex { get; set; } = 0;

        [Description("(advanced) Enables Mod to stay active but act like it's not, ignoring 'extra' UT tree data during load.\n This mode only exists to allow you to load a map that has 'extra' UT data WITHOUT actually loading that data the game will act as if UT is not loaded.\n For your own safety you can not change this setting in-game.")]
        [Checkbox("Enable Ghost Mode", UnlimitedTreesOptions)]
        public bool GhostModeEnabled { get; set; } = false; //TODO hide in-game

        [Description("Sets the maximum # of trees in increments of 262,144.\nSetting this above 4 (1 million trees) is not recommended and depending on your hardware\n it may cause performance or rendering issues.")]
        [Slider("Max # of trees scaling factor", 4.0f, 8.0f, 1.0f, UnlimitedTreesOptions, typeof(UTSettingsUI), nameof(UTSettingsUI.OnScaleFactorChange))]
        public float ScaleFactor { get; set; } = 4.0f; //TODO hide in-game

        //TODO: restore tree count label generation

        [XmlIgnore]
        [Description("Does not allows tree fires to catch onto Radio Masts.\n This option helps fix a bug in the game, where choppers can't put out those fires.")]
        [Checkbox("Don't burn Radio Masts", InGameOnlyDebugFunctions)]
        public bool RadioMastFix { get; set; } = true;

        public bool EmergencyOnly_RemoveAllTrees = false;

        [XmlIgnore]
        [Description("Resets all trees to not burning and not damaged.\nAlso wipes the burningtrees array to match.")]
        [Button("Reset all burning trees", InGameOnlyDebugFunctions, typeof(UTSettingsUI), nameof(UTSettingsUI.ClearAllBurningDamaged))]
        public object ClearAllBurningDamaged { get; } = null;

        [XmlIgnore]
        [Description("Wipes all UT data from currently loaded map file.\nNote** This does not remove tree from an active map\n It will just force the mod to re-save your data if needed.\n or not write new data if <262k trees")]
        [Button("Clear all our save data from this file", InGameOnlyDebugFunctions, typeof(UTSettingsUI), nameof(UTSettingsUI.ClearAllSaveDataFromFile))]
        public object ClearAllSaveDataFromFile { get; } = null;

        [XmlIgnore]
        [Description("dumps to your log file a list and size of all custom data entries (beyond just UT) \n in the m_serializableDataStorage dictionary with-in simulation manager.")]
        [Button("List all custom data to log", InGameOnlyDebugFunctions, typeof(UTSettingsUI), nameof(UTSettingsUI.OnListAllCustomDataInFile))]
        public object OnListAllCustomDataInFile { get; } = null;

        [XmlIgnore]
        [Description("Dumps a list of all burning tree data to the log.\n YOU DONT NEED THIS.")]
        [Button("Dump all burning trees to log", InGameOnlyDebugFunctions, typeof(UTSettingsUI), nameof(UTSettingsUI.OnDumpAllBurningTrees))]
        public object OnDumpAllBurningTrees { get; } = null;

        public bool IsLoggingEnabled()
        {
            return DebugLoggingLevel > 0;
        }

        public int GetScaledTreeCount()
        {
            return (int) (ScaleFactor * Mod.DEFAULT_TREE_COUNT); //For scale 4: 1048576
        }

        public int GetTreeUpdateCount()
        {
            return (int)(ScaleFactor * Mod.DEFAULT_TREEUPDATE_COUNT); //For scale 4: 16384;
        }
    }
}
