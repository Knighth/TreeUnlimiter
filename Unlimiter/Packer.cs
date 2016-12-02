using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ColossalFramework;
using ColossalFramework.IO;
using UnityEngine;

namespace TreeUnlimiter
{
	static class Packer
	{

        /// <summary>
        /// Feed it a packed list it will filter throught he buringtree's buffer
        /// and change old indexes to match what 99.99% of the time should be the
        /// index when deserialised. Only needed when 'packing' is used.
        /// </summary>
        /// <param name="orgindex">a 'packed' list of tree indexes</param>
        /// <returns></returns>
        public static int ReOrderBuringTrees(List<int> orgindex)
        {
            int reordered = 0;
            if (orgindex == null || orgindex.Count < 2) 
            {
                if (Mod.DEBUG_LOG_ON)
                { Logger.dbgLog("orgindex was null or < 2; during call. aborting reorder;");}
                return 0; 
            }
            FastList<TreeManager.BurningTree> tmburning = Singleton<TreeManager>.instance.m_burningTrees;
            try
            {
                if (tmburning.m_size > 0 )
                {
                    for (int i = 1; i < orgindex.Count; i++)
                    {
                        //int oidx = orgindex[i];  //why do an assignment everytime?
                        for (int j = 0; j < tmburning.m_size; j++)
                        {
                            if (tmburning.m_buffer[j].m_treeIndex == orgindex[i])
                            {
                                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
                                {
                                    object[] logstring = new object[] { tmburning.m_buffer[j].m_treeIndex.ToString(), orgindex[i].ToString(), i.ToString() };
                                    Logger.dbgLog(string.Format("matched buring: {0} == orgidx: {1}  reassigned: {2}", logstring));
                                }
                                tmburning.m_buffer[j].m_treeIndex = (uint)i;
                                reordered++;
                            }
                        }

                    }
                }

                else
                {
                    if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
                    { Logger.dbgLog("No burning trees to reorder or orgindex was < 2, skipping."); }
                }
            }
            catch (Exception ex)
            { Logger.dbgLog("", ex, true); }

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


            if(Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
            {
                if (idxList == null)
                { Logger.dbgLog("idxlist is null. lastsaveusedpacking: " + Loader.LastSaveUsedPacking.ToString()); }
                else
                { Logger.dbgLog("idxlist count: " + idxList.Count + " lastsaveusedpacking: " + Loader.LastSaveUsedPacking.ToString()); }
            }

            TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
            FastList<TreeManager.BurningTree> tmburning = Singleton<TreeManager>.instance.m_burningTrees;
            int num = Mod.DEFAULT_TREE_COUNT; //262144

            //if (idxList != null && idxList.Count >= Mod.DEFAULT_TREE_COUNT) //custom one.
            if (idxList != null && idxList.Count > 1) //always use custom one unless problem.
            {
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Logger.dbgLog("Start using packer seralizer for first 262144 realones."); }
                if (Mod.DEBUG_LOG_ON && idxList.Count < 262144) 
                { Logger.dbgLog(string.Format("tree seralizer will serialize first {0} trees then dummy up the rest.",idxList.Count)); }
                Loader.LastSaveUsedPacking = true;
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
                            if ((Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1))
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
                if ((uint)tmburning.m_size > 0)
                { 
                    int retval = ReOrderBuringTrees(idxList); 
                }
                s.WriteUInt24((uint)tmburning.m_size);
                for (int m = 0; m < tmburning.m_size; m++)
                {
                    s.WriteUInt24(tmburning.m_buffer[m].m_treeIndex);
                    s.WriteUInt8(tmburning.m_buffer[m].m_fireIntensity);
                    s.WriteUInt8(tmburning.m_buffer[m].m_fireDamage);
                }
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Logger.dbgLog("Saved " + tmburning.m_size.ToString() + "buring trees"); }

                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Logger.dbgLog("End using packer seralizer."); }

            }

            else //use original.
            {
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Logger.dbgLog("Start using original seralizer."); }
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

                s.WriteUInt24((uint)tmburning.m_size);
                for (int m = 0; m < tmburning.m_size; m++)
                {
                    s.WriteUInt24(tmburning.m_buffer[m].m_treeIndex);
                    s.WriteUInt8(tmburning.m_buffer[m].m_fireIntensity);
                    s.WriteUInt8(tmburning.m_buffer[m].m_fireDamage);
                }

                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Logger.dbgLog("End using original seralizer."); }
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
            if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
            { Logger.dbgLog("Fake serialize from " + startIndex.ToString() + " to " + idxList.Count.ToString()); }

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
