using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ColossalFramework;
using ColossalFramework.IO;
using UnityEngine;
using System.Diagnostics;
using System.Threading;
using TreeUnlimiter.Detours;
using TreeUnlimiter.OptionsFramework;

namespace TreeUnlimiter
{
	static class Packer
	{

        static private Stopwatch m_PerfMonitor = new Stopwatch();

        /// <summary>
        /// Copies from original list to a new list based on treeindex values, based upon the flag sent.
        /// ie copies entries with 0 to 262144 only or that have 262144 to 'limit', or just all.
        /// </summary>
        /// <param name="orgBurningList">The orginal source list</param>
        /// <param name="bCopyFlag">0= CopyAllEntries; 1= 0 to 262144; 2= 262144 to 'limit'</param>
        /// <returns>a new FastList of treemanager.burningtrees, returns an empty list on none or error.</returns>
        public static FastList<TreeManager.BurningTree> CopyBurningTreesList(ref FastList<TreeManager.BurningTree> orgBurningList,byte bCopyFlag)
        {
            FastList<TreeManager.BurningTree> newlist = new FastList<TreeManager.BurningTree>();
            newlist.Clear();
            try
            {
                if (orgBurningList != null)
                {

                    int orgcount = orgBurningList.m_size;
                    newlist.EnsureCapacity(orgcount);
                    TreeManager.BurningTree tmpTree = new TreeManager.BurningTree();

                    int tmpcounter = 0;
                    int MinValue = 0;
                    int MaxValue = 0;
                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("CopyFlag = " + bCopyFlag.ToString()); }
                    switch (bCopyFlag)
                    {
                        //0-262144 mainserialze()
                        case 1: 
                            MinValue = 0;
                            MaxValue = Mod.DEFAULT_TREE_COUNT;
                            break;
                        //262144 to activelimit  customserialze( not packed)
                        case 2: 
                            MinValue = Mod.DEFAULT_TREE_COUNT;
                            MaxValue = LimitTreeManager.Helper.TreeLimit;
                            break;
                        //262144 to lastsavecount.count customseralize(packed)???
                        case 3:
                            MinValue = Mod.DEFAULT_TREE_COUNT;
                            MaxValue = LoadingExtension.LastSaveList.Count;
                            break;
                        //just copy all of them.
                        default:
                            MinValue = 0;
                            MaxValue = LimitTreeManager.Helper.TreeLimit;
                            break;
                    }
                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) 
                    { Logger.dbgLog(string.Concat("copying from: ", MinValue.ToString(), " to ",MaxValue.ToString())); }

                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) 
                    { m_PerfMonitor.Reset(); m_PerfMonitor.Start(); }
                    
                    foreach (TreeManager.BurningTree orgTree in orgBurningList)
                    {
                        if (orgTree.m_treeIndex > 0 && (orgTree.m_treeIndex >= MinValue & orgTree.m_treeIndex < MaxValue))
                        {
                            //copy tree
                            tmpTree.m_treeIndex = orgTree.m_treeIndex;
                            tmpTree.m_fireDamage = orgTree.m_fireDamage;
                            tmpTree.m_fireIntensity = orgTree.m_fireIntensity;
                            newlist.Add(tmpTree);
                            tmpcounter++;
                        }
                    }
                    newlist.Trim();
                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { m_PerfMonitor.Stop(); Logger.dbgLog(string.Concat("Copy time took (ticks):", m_PerfMonitor.ElapsedTicks.ToString())); }
                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) {Logger.dbgLog(string.Concat("orgCount(m_size):",orgcount.ToString()," copycount:",tmpcounter.ToString()) + " new_msize:" + newlist.m_size.ToString() ); }
                }
                else 
                {
                    Logger.dbgLog("orgBurningList is Null!");
                    return newlist;
                }
            }
            catch (Exception ex)
            { Logger.dbgLog(ex.ToString()); }

            return newlist;
 
        }

/*
        /// <summary>
        /// It's complicated.
        /// </summary>
        /// <param name="idxReorderedList"></param>
        /// <param name="sourceBuringTrees"></param>
        /// <param name="lowerBurning"></param>
        /// <param name="upperBurning"></param>
        /// <returns></returns>
        /// 
        public static int FilterBurningTreeList(ref List<int> idxReorderedList, ref FastList<TreeManager.BurningTree> sourceBuringTrees, out FastList<TreeManager.BurningTree> lowerBurning, out FastList<TreeManager.BurningTree> upperBurning)
        {
            int retvalue = 0;

            lowerBurning = new FastList<TreeManager.BurningTree>();
            upperBurning = new FastList<TreeManager.BurningTree>();
            try 
            {
                if (sourceBuringTrees != null)
                {
                    lowerBurning = CopyBuringTreesList(ref sourceBuringTrees, 1);
                    upperBurning = CopyBuringTreesList(ref sourceBuringTrees, 2);
                    TreeManager.BurningTree tmptree = new TreeManager.BurningTree();
                    foreach(int tmpInt in idxReorderedList )
                    {

                    }
                    for (int i = 0; i < sourceBuringTrees.m_buffer.Length;i++ )
                    {

                    }
                    
                }
                else
                { Logger.dbgLog("sourceBuringTrees is null! wtf?"); }

            }
            catch (Exception ex)
            { Logger.dbgLog(ex.ToString()); }

            return retvalue;
        }
*/


        /// <summary>
        /// Feed it a packed list it will filter throught the burningtree's buffer
        /// and change old indexes to match what 99.99% of the time should be the
        /// packed index when deserialised. Only needed when 'packing' is used.
        /// Works on TreeManager.m_burningtress but returns a COPY not the original.
        /// </summary>
        /// <param name="orgindex">a 'packed' list of tree indexes</param>
        /// <returns>(int) number of indexes reordered </returns>
        public static int ReOrderBurningTrees(ref List<int> orgindex,out FastList<TreeManager.BurningTree> tmburning)
        {
            tmburning = new FastList<TreeManager.BurningTree>();
            int reordered = 0;
            if (orgindex == null || orgindex.Count < 2) 
            {
                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled())
                { Logger.dbgLog("orgindex was null or < 2; during call. aborting reorder;");}
                return 0; 
            }

            //kr 12.2.2016 don't modifiy m_burningtrees live.
            //go get a copy from original that includes all valid burning trees 0 to whatever and fk with that copy.
            tmburning = CopyBurningTreesList(ref Singleton<TreeManager>.instance.m_burningTrees, 0);
            try
            {

                if (tmburning.m_size > 0 )
                {
                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1)
                    { Logger.dbgLog(string.Format("reorder will filter though {0} trees, looking for matches to reorder in {1} burning trees",tmburning.m_size.ToString(),orgindex.Count.ToString())); }
                    object[] logstring;
                    for (int i = 1; i < orgindex.Count; i++)
                    {
                        //int oidx = orgindex[i];  //why do an assignment everytime?
                        for (int j = 0; j < tmburning.m_size; j++)
                        {
                            if (tmburning.m_buffer[j].m_treeIndex == orgindex[i])
                            {
                                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 2)
                                {
                                    logstring = new object[] { tmburning.m_buffer[j].m_treeIndex.ToString(), orgindex[i].ToString(), i.ToString(),j.ToString(),(orgindex[i] == i) ? " same":" changed"};
                                    Logger.dbgLog(string.Format("matched burning: {0} == orgidx: {1}  reassigned: {2} status: {4} orgburnidx: {3}", logstring));
                                }
                                tmburning.m_buffer[j].m_treeIndex = (uint)i;
                                reordered++;
                            }
                        }

                    }
                }

                else
                {
                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1)
                    { Logger.dbgLog("No burning trees to reorder or tmburning.m_size was < 1, skipping."); }
                }
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex, true); }
            if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("Number of reordered trees: " + reordered.ToString()); }
            return reordered;
        }

        /// <summary>
        /// Returns a list of indexes where trees actually exist and hence need to be saved\processed.
        /// Used by serialization process; Also I used this in Treeinfo validate upon load to save duplicating code.
        /// </summary>
        /// <returns>list object of int's</returns>
        public static List<int> GetPackedList()
        {
            List<int> treeidx;
            TreeManager TM = Singleton<TreeManager>.instance;
            try 
            {
                if (TM != null && TM.m_treeCount > 0)
                {
                    treeidx = new List<int>(TM.m_treeCount);
                    //Sadly we have to jackup entry [0] to account for buffer[0] and 1 based entries in serialize\deserialize  
                    treeidx.Add(0); //add dummy entry at list index[0]
                    for(int i = 0 ; i < TM.m_trees.m_size;i++)
                    {
                        if (TM.m_trees.m_buffer[i].m_flags != 0)
                        {
                            treeidx.Add(i);
                        }
                    }
                    return treeidx;
                }
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex, true); }
            return new List<int>();
        }


        /// <summary>
        /// Custom serialize call
        /// </summary>
        /// <param name="idxList"> ByRef to the List of ints representing tree indexes that should be serialised</param>
        /// <param name="s">ByRef to The DataSerializer object </param>
        internal static void Serialize(ref List<int> idxList, ref DataSerializer s)
        {
            
            //Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.BeginSerialize(s, "TreeManager");


            if(OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1)
            {
                if (idxList == null)
                { Logger.dbgLog("idxlist is null. lastsaveusedpacking: " + LoadingExtension.LastSaveUsedPacking.ToString()); }
                else
                { Logger.dbgLog("idxlist count: " + idxList.Count + " lastsaveusedpacking: " + LoadingExtension.LastSaveUsedPacking.ToString()); }

                if (OptionsWrapper<Configuration>.Options.GhostModeEnabled) { Logger.dbgLog("Ghost Mode is activated!"); }
            }

            TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
            FastList<TreeManager.BurningTree> tmburning = Singleton<TreeManager>.instance.m_burningTrees;
            int num = Mod.DEFAULT_TREE_COUNT; //262144

            //if (idxList != null && idxList.Count >= Mod.DEFAULT_TREE_COUNT) //custom one.
            if (idxList != null && idxList.Count > 1) //always use custom one unless problem.
            {
                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("Start using packer seralizer for first 262144 realones."); }
                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && idxList.Count < 262144) 
                { Logger.dbgLog(string.Format("tree seralizer will serialize first {0} trees then dummy up the rest.",idxList.Count)); }
                LoadingExtension.LastSaveUsedPacking = true;
                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("Loader.LastSaveUsedPacking now set to True."); }

                EncodedArray.UShort num1 = EncodedArray.UShort.BeginWrite(s);

                //remember idxlist index entry 0 is also zeroed'd to account for treebuffer starting at 1 not 0)
                for (int i = 1; i < num; i++)
                {
                    if (i < idxList.Count)
                    { num1.Write(mBuffer[idxList[i]].m_flags); }
                    else { num1.Write(0); }
                }
                num1.EndWrite();
                //now lets process the treeinfo
                try
                {
                    PrefabCollection<TreeInfo>.BeginSerialize(s);
                    //
                    int limit = Math.Min(num, idxList.Count);
                    for (int j = 1; j < limit; j++)
                    {
                        if (j < idxList.Count)  //pretty sure I had this here for a reason as count could be 0 or 1 in cases of error or no real trees.
                        {
                            if (mBuffer[idxList[j]].m_flags != 0)
                            {
                                PrefabCollection<TreeInfo>.Serialize(mBuffer[idxList[j]].m_infoIndex);
                            }
                        }
                        else
                        { Logger.dbgLog("j >= idxList.Count Do we ever hit this?? No real trees to save."); }
                    }

                    //Thale5's cool refcounter work around --THANKS!!
                    // This is the easiest (and only) way to create a dummy DataSerializer that writes to Stream.Null.
                    // This will simply call RefCounter.Serialize() below.
                    try
                    {
                        if (idxList.Count > limit)
                        {
                            if ((OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1))
                            { Logger.dbgLog("Incrementing related reference counts for prefabs for trees > " + limit.ToString()); }

                            DataSerializer.Serialize(System.IO.Stream.Null, DataSerializer.Mode.Memory, 0, new RefCounter(limit, idxList));
                        }
                    }
                    catch (Exception ex)
                    { Logger.dbgLog("RefCounter caused an error:", ex, true); }
                }
                finally
                {
                    PrefabCollection<TreeInfo>.EndSerialize(s);
                }

                EncodedArray.Short num2 = EncodedArray.Short.BeginWrite(s);
                for (int k = 1; k < num; k++)
                {
                    if (k < idxList.Count)
                    {
                        if (mBuffer[idxList[k]].m_flags != 0)
                        {
                            num2.Write(mBuffer[idxList[k]].m_posX);
                        }
                    }
                }
                num2.EndWrite();

                EncodedArray.Short num3 = EncodedArray.Short.BeginWrite(s);
                for (int l = 1; l < num; l++)
                {
                    if (l < idxList.Count)
                    {
                        if (mBuffer[idxList[l]].m_flags != 0)
                        {
                            num3.Write(mBuffer[idxList[l]].m_posZ);
                        }
                    }
                }
                num3.EndWrite();

                //reorder if neccessary
                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("reordering burning trees indexes for our seralizer. (packed)"); }

                if ((uint)tmburning.m_size > 0)
                {
                    FastList<TreeManager.BurningTree> returnedMatchingBurnList;
                    int retval = ReOrderBurningTrees(ref idxList, out returnedMatchingBurnList);
                    FastList<TreeManager.BurningTree> orgBurningTrees; //used below holds tmp copy of burningtrees;

                    //we only need burning tress with indexes < 262144k.
                    orgBurningTrees = CopyBurningTreesList(ref returnedMatchingBurnList, 1);
                    //create 2  fastlists of burning trees from idxlist
                    //one with indexes 0 -> 262143  one with indexes > 262144
                    //save 0-262k list as usual
                    //save 262k+ list to our own storage devices via customer serializer class that gets called before this.

                    if(orgBurningTrees ==null)
                    {
                        Logger.dbgLog("orgBurningTrees is null, copyburningtrees retuned null array.");
                        orgBurningTrees = new FastList<TreeManager.BurningTree>();
                        Logger.dbgLog("We dummied up a fake 0 count one.");
                    }
                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("Adjusted burning trees for our seralizer.(packed-reorderedlist)"); }
                    //
                    s.WriteUInt24((uint)orgBurningTrees.m_size);
                    for (int m = 0; m < orgBurningTrees.m_size; m++)
                    {
                        s.WriteUInt24(orgBurningTrees.m_buffer[m].m_treeIndex);
                        s.WriteUInt8(orgBurningTrees.m_buffer[m].m_fireIntensity);
                        s.WriteUInt8(orgBurningTrees.m_buffer[m].m_fireDamage);
                    }
                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("Saved " + orgBurningTrees.m_size.ToString() + " packed burning trees"); }
                }
                else
                {
                    //There are supposedly no burningtree so just exec the original code untouched.
                    //org
                    s.WriteUInt24((uint)tmburning.m_size);
                    for (int m = 0; m < tmburning.m_size; m++)
                    {
                        s.WriteUInt24(tmburning.m_buffer[m].m_treeIndex);
                        s.WriteUInt8(tmburning.m_buffer[m].m_fireIntensity);
                        s.WriteUInt8(tmburning.m_buffer[m].m_fireDamage);
                    }
                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("Saved " + tmburning.m_size.ToString() + " org burning trees (pack but empty)"); }
                }

                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("End using packer seralizer.  "  +DateTime.Now.ToString(Mod.DTMilli)); }
                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled()) { Logger.dbgLog("Tree saving process completed. (packed) " + DateTime.Now.ToString(Mod.DTMilli)); }

            }

            else //use original. we can hit this when we're active but packer either didn't get used or we are in ghost mode.
            {
                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("idxList was Null or idxList.Count was < 2"); }
                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("Start using original seralizer."); }
                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("Loader.LastSaveUsedPacking state = " + LoadingExtension.LastSaveUsedPacking.ToString()); }
                EncodedArray.UShort num1 = EncodedArray.UShort.BeginWrite(s);
                for (int i = 1; i < num; i++)
                {
                    num1.Write(mBuffer[i].m_flags);
                }
                num1.EndWrite();
                try
                {
                    PrefabCollection<TreeInfo>.BeginSerialize(s);
                    for (int j = 1; j < num; j++)
                    {
                        if (mBuffer[j].m_flags != 0)
                        {
                            PrefabCollection<TreeInfo>.Serialize(mBuffer[j].m_infoIndex);
                        }
                    }
                }
                finally
                {
                    PrefabCollection<TreeInfo>.EndSerialize(s);
                }
                
                EncodedArray.Short num2 = EncodedArray.Short.BeginWrite(s);
                for (int k = 1; k < num; k++)
                {
                    if (mBuffer[k].m_flags != 0)
                    {
                        num2.Write(mBuffer[k].m_posX);
                    }
                }
                num2.EndWrite();
                EncodedArray.Short num3 = EncodedArray.Short.BeginWrite(s);
                for (int l = 1; l < num; l++)
                {
                    if (mBuffer[l].m_flags != 0)
                    {
                        num3.Write(mBuffer[l].m_posZ);
                    }
                }
                num3.EndWrite();

                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("Adjusting burning trees for original seralizer."); }
                //12-4 KH - you know we could ecapsilate this with above.
                // save some code, for now leaving it though.

                //Since we are not packing we don't need to reorder, but we do 
                //need just trees < 262144k

                //we feed it the real TreeManager.burningtrees. get back 0-262k
                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("No reordering of trees indexes used."); }
                if (tmburning.m_size > 0)
                {
                    FastList<TreeManager.BurningTree> orgBurningTrees2; //used below holds tmp copy of burningtrees;
                    orgBurningTrees2 = CopyBurningTreesList(ref tmburning, 1);

                    if (orgBurningTrees2 != null)
                    { 
                        Logger.dbgLog("orgBurningTrees2 is null, copyburning returned null? Dummy up new one don't save burning"); 
                        orgBurningTrees2 = new FastList<TreeManager.BurningTree>();
                    }

                    s.WriteUInt24((uint)orgBurningTrees2.m_size);
                    for (int m = 0; m < orgBurningTrees2.m_size; m++)
                    {
                        s.WriteUInt24(orgBurningTrees2.m_buffer[m].m_treeIndex);
                        s.WriteUInt8(orgBurningTrees2.m_buffer[m].m_fireIntensity);
                        s.WriteUInt8(orgBurningTrees2.m_buffer[m].m_fireDamage);
                    }
                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("Saved " + orgBurningTrees2.m_size.ToString() + " org burning trees"); }

                }
                else 
                {
                    //use real original cause it' empty anyway?
                    s.WriteUInt24((uint)tmburning.m_size);
                    for (int m = 0; m < tmburning.m_size; m++)
                    {
                        s.WriteUInt24(tmburning.m_buffer[m].m_treeIndex);
                        s.WriteUInt8(tmburning.m_buffer[m].m_fireIntensity);
                        s.WriteUInt8(tmburning.m_buffer[m].m_fireDamage);
                    }
                    if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("Saved " + tmburning.m_size.ToString() + " org burning trees (org empty)"); }
                }
                
                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1) { Logger.dbgLog("End using original seralizer.  " + DateTime.Now.ToString(Mod.DTMilli)); }
                if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled()) { Logger.dbgLog("Tree saving process completed. (original) " + DateTime.Now.ToString(Mod.DTMilli)); }
            }
           // Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.EndSerialize(s, "TreeManager");
        }

	}

    class RefCounter : IDataContainer
    {
        readonly List<int> idxList;
        readonly int startIndex;

        public RefCounter(int startIndex, List<int> idxList)
        {
            this.startIndex = startIndex;
            this.idxList = idxList;
        }

        // The given DataSerializer is the dummy one that writes to Stream.Null.
        public void Serialize(DataSerializer s)
        {
            if (OptionsWrapper<Configuration>.Options.IsLoggingEnabled() && OptionsWrapper<Configuration>.Options.DebugLoggingLevel > 1)
            { 
                Logger.dbgLog("Fake serialize from " + startIndex.ToString() + " to " + idxList.Count.ToString());
            }

            // Setup a dummy PrefabCollection<TreeInfo>.m_encodedArray so that we can continue refcounting.
            FieldInfo encoderField = typeof(PrefabCollection<TreeInfo>).GetField("m_encodedArray", BindingFlags.NonPublic | BindingFlags.Static);
            EncodedArray.UShort original = (EncodedArray.UShort) encoderField.GetValue(null);

            try
            {
                EncodedArray.UShort dummy = EncodedArray.UShort.BeginWrite(s);
                encoderField.SetValue(null, dummy);

                // Now that the do-nothing m_encodedArray has been set, we can safely continue refcounting.
                TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                int limit = idxList.Count;

                for (int j = startIndex; j < limit; j++)
                {
                    if (mBuffer[idxList[j]].m_flags != 0)
                    {
                        PrefabCollection<TreeInfo>.Serialize(mBuffer[idxList[j]].m_infoIndex);
                    }
                }
            }
            catch (Exception ex) 
            {
                Logger.dbgLog("", ex, true);
            }

            finally
            {
                encoderField.SetValue(null, original);
            }
        }

        public void AfterDeserialize(DataSerializer s) { }
        public void Deserialize(DataSerializer s) { }
    }
}
