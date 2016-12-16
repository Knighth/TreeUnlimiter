using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace TreeUnlimiter
{
    //Static class for debugging functions
	internal static class UTDebugUtils
	{

/*
        public static List<int> DumpBurningTreesToList(bool IncludeFireDamaged = false)
        {
            List<int> theList = new List<int>(32);
            try
            {
               
                TreeManager tm = Singleton<TreeManager>.instance;
                ushort sourceFlags = 0;
                int burningcounter = 0;
                TreeInstance.Flags flags;
                for (int i = 1; i < tm.m_trees.m_buffer.Length; i++)
                {
                    sourceFlags = tm.m_trees.m_buffer[i].m_flags;
                    flags = (TreeInstance.Flags)sourceFlags;
                    if(IncludeFireDamaged)
                    {
                        if (HasTreeFlags(flags, TreeInstance.Flags.Created) && (HasTreeFlags(flags, TreeInstance.Flags.Burning) | HasTreeFlags(flags, TreeInstance.Flags.FireDamage)))
                        {
                            theList.Add(i);
                            burningcounter++;
                        }
                    }
                    else
                    {
                        if (HasTreeFlags(flags, TreeInstance.Flags.Created) && HasTreeFlags(flags, TreeInstance.Flags.Burning))
                        {
                            theList.Add(i);
                            burningcounter++;
                        }
                    }
                }
                Logger.dbgLog("--done, processed " + burningcounter.ToString() + " created and burning trees");
            }
            catch (Exception ex)
            { Logger.dbgLog("error: ", ex); }

            return theList;
        }
*/

        public static void DumpBurningTrees(bool IncludeFireDamaged = false)
        {
            try
            {
                TreeManager tm = Singleton<TreeManager>.instance;
                ushort sourceFlags = 0;
                Logger.dbgLog("----- burning trees dump -----");
                byte logcounter = 0;
                int burningcounter = 0;
                string tmpstr = "";
                TreeInstance.Flags flags;
                for (int i = 1; i < tm.m_trees.m_buffer.Length; i++)
                {
                    sourceFlags = tm.m_trees.m_buffer[i].m_flags;
                     flags = (TreeInstance.Flags)sourceFlags;
                     if (HasTreeFlags(flags, TreeInstance.Flags.Created) && HasTreeFlags(flags, TreeInstance.Flags.Burning))
                    {
                        burningcounter++;
                        tmpstr = tmpstr + string.Format("Treeidx:{0} flags:{1}  ", i.ToString(), sourceFlags.ToString());
                        logcounter++;
                    }
                    if (logcounter > 3)
                    {
                        Logger.dbgLog(tmpstr, null, false, true); //dump 4 per line.
                        tmpstr = "";
                        logcounter = 0;
                    }
                }
                Logger.dbgLog("--done, processed " + burningcounter.ToString() + " created and burning trees");
            }
            catch (Exception ex)
            { Logger.dbgLog("error: ", ex); }
        }

        public static void DumpDamagedTrees()
        { 
        }

        public static void ResetAllBurningTrees(bool ClearBurningBufferToo)
        {
            try
            {
                //List<int> theList = DumpBurningTreesToList(true);
                TreeManager tm = Singleton<TreeManager>.instance;
                TreeInstance.Flags tflags;
                ushort sourceFlags = 0;
                int counter = 0; int counter2 = 0;
                for (int i = 1; i < tm.m_trees.m_buffer.Length; i++)
                {
                    sourceFlags = tm.m_trees.m_buffer[i].m_flags;
                     tflags = (TreeInstance.Flags)sourceFlags;
                     if (HasTreeFlags(tflags, TreeInstance.Flags.Created) && (HasTreeFlags(tflags, TreeInstance.Flags.Burning) | HasTreeFlags(tflags, TreeInstance.Flags.FireDamage)))
                     {
                         tflags &= ~(TreeInstance.Flags.Burning | TreeInstance.Flags.FireDamage);
                         tm.m_trees.m_buffer[i].m_flags  = (ushort)tflags;
                         counter++;
                     }
                }

                if (ClearBurningBufferToo)
                {
                    counter2 = tm.m_burningTrees.m_size;
                    tm.m_burningTrees.Clear();
                    tm.m_burningTrees.Trim();
                }
                Logger.dbgLog(string.Format("debug user reset all {0} burning trees, and {1} in the burningfastlist.", counter.ToString(),counter2.ToString())); 
            }
            catch(Exception ex)
            {
                Logger.dbgLog("Error: ", ex); 
            }


        }

        public static bool HasTreeFlags(TreeInstance.Flags somevalue, TreeInstance.Flags flagtocheck)
        {
            return (somevalue & flagtocheck) == flagtocheck;
        }

        public static void DumpBurningTreeBuffer(bool CrossRefferenceTBuff)
        { 
        }

        public static void ClearBurningTreeBuffer(bool AlsoResetToNull = false)
        { 
        }

        //Don't really need this anymore.
        //public static void DumpHelicopters()
        //{ 
        //    //dump choppers ..specifically firecopterAI ones.. 
        //    //do these have refferences to TM.burning buffer[x] somehow?
        //    int thecounter = 0;
        //    int thecounter2 = 0;
        //    try
        //    {
        //        Logger.dbgLog("\r\n---Dumping Helcopters---\r\n");
        //        VehicleManager vm = Singleton<VehicleManager>.instance;
        //        Vehicle.Flags vFlags;
        //        Vehicle v;
        //        object logobj;
        //        for (int i = 0; i < vm.m_vehicles.m_buffer.Length; i++)
        //        {
        //            vFlags = vm.m_vehicles.m_buffer[i].m_flags;
        //            if ((vFlags & Vehicle.Flags.Created) == Vehicle.Flags.Created)
        //            {
        //                thecounter++;
        //                v = vm.m_vehicles.m_buffer[i];
        //                if (v.m_infoIndex > 0)
        //                {
        //                    VehicleInfo vi = v.Info;
        //                    if (vi != null)
        //                    {
        //                        if ((vi.m_vehicleType & VehicleInfo.VehicleType.Helicopter) == vi.m_vehicleType)
        //                        {
        //                            logobj = new object[] { i.ToString(), v.m_flags.ToString(), v.m_sourceBuilding.ToString(), v.m_targetBuilding.ToString(), v.m_transferType.ToString(), vi.name.ToString(), v.m_transferSize.ToString() };
        //                            Logger.dbgLog(string.Format("vIdx:{0} flags:{1}  m_source:{2} m_target:{3} xfertype:{4} xfersize:{6} name={5}",logobj));
        //                            thecounter2++;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    { Logger.dbgLog("Error: " + thecounter.ToString(), ex); }
        //    Logger.dbgLog("Processed " + thecounter.ToString() + " number of vecs number logged: " + thecounter2.ToString()); 

        //}

        //Randomize fire fighting more?
      //  public static void ReOrderBurnTreeListToSource(FastList<TreeManager.BurningTree> SourceList, List<BurningReorderEntry> MatchingTableList, out FastList<TreeManager.BurningTree> ResultingList)
      //  {
           // ResultingList = new FastList<TreeManager.BurningTree>();
           // ResultingList.EnsureCapacity(MatchingTableList.Count);
            //if found in source copy it to REsultinglist in the buffer[x] location it's supposed to be.
            //if not in list log that
            //if entries in source exist that are not in match list... log that too.
           // MatchingTableList[0].BurningBufferDestIdx = 0;
          //  MatchingTableList[0].treeidxToMatch = 0;
      //  }

	}
}
