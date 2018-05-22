using ColossalFramework;
using ColossalFramework.Math;
using System;
using System.Reflection;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace TreeUnlimiter
{
    internal static class LimitCommonBuildingAI
    {
/*        public void suck(CommonBuildingAI CBAI)
        {
            //CommonBuildingAI CBAI;
            BuildingInfo  bldgInfo;
            bldgInfo = (BuildingInfo)CBAI.GetType().GetField("m_info", BindingFlags.Instance | BindingFlags.Public).GetValue(CBAI);
            string tmp = bldgInfo.m_size.y.ToString();
            var x = CBAI.GetType().GetMethod("TrySpreadFire", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        
        }
 */
        //redirect reverse this fucker.
        [MethodImpl(MethodImplOptions.NoInlining)] //to prevent inlining
        private static void TrySpreadFire(Quad2 quad, float minY, float maxY, ushort buildingID, ref Building buildingData, InstanceManager.Group group)
        {
            //this should never get reached.
            if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("try spread fire"); }
        }

        private static void HandleFireSpread(CommonBuildingAI CBAI,ushort buildingID, ref Building buildingData, int fireDamage)
        {
            unsafe
            {

                Quad2 quad2 = new Quad2();
                int width = buildingData.Width;
                int length = buildingData.Length;
                Vector2 vector2 = VectorUtils.XZ(buildingData.m_position);
                Vector2 vector21 = new Vector2(Mathf.Cos(buildingData.m_angle), Mathf.Sin(buildingData.m_angle));
                Vector2 vector22 = new Vector2(vector21.y, -vector21.x);
                float single = (float)Singleton<SimulationManager>.instance.m_randomizer.Int32(8, 32);
                quad2.a = (vector2 - (((float)width * 4f + single) * vector21)) - (((float)length * 4f + single) * vector22);
                quad2.b = (vector2 + (((float)width * 4f + single) * vector21)) - (((float)length * 4f + single) * vector22);
                quad2.c = (vector2 + (((float)width * 4f + single) * vector21)) + (((float)length * 4f + single) * vector22);
                quad2.d = (vector2 - (((float)width * 4f + single) * vector21)) + (((float)length * 4f + single) * vector22);
                Vector2 vector23 = quad2.Min();
                Vector2 vector24 = quad2.Max();
                float mPosition = buildingData.m_position.y - (float)buildingData.m_baseHeight;

                //krn
                //CBAI.m_info is private\instance /use reflection, should do reverse redirect.
                BuildingInfo bldgInfo;
                bldgInfo = (BuildingInfo)CBAI.GetType().GetField("m_info", BindingFlags.Instance | BindingFlags.Public).GetValue(CBAI);
                if (bldgInfo == null && bldgInfo.m_size == null) 
                { Logger.dbgLog("bldgInfo was null"); }
                
                float mPosition1 = buildingData.m_position.y + bldgInfo.m_size.y;

                //org
                //float mPosition1 = buildingData.m_position.y + this.m_info.m_size.y;
                //end org
                float mFireIntensity = (float)(buildingData.m_fireIntensity * (64 - Mathf.Abs(fireDamage - 192)));
                InstanceID instanceID = new InstanceID()
                {
                    Building = buildingID
                };
                InstanceManager.Group group = Singleton<InstanceManager>.instance.GetGroup(instanceID);
                if (group != null)
                {
                    ushort disaster = group.m_ownerInstance.Disaster;
                    if (disaster != 0)
                    {
                        DisasterManager disasterManager = Singleton<DisasterManager>.instance;
                        DisasterInfo info = disasterManager.m_disasters.m_buffer[disaster].Info;
                        int fireSpreadProbability = info.m_disasterAI.GetFireSpreadProbability(disaster, ref disasterManager.m_disasters.m_buffer[disaster]);
                        mFireIntensity = mFireIntensity * ((float)fireSpreadProbability * 0.01f);
                    }
                }
                int num = Mathf.Max((int)((vector23.x - 72f) / 64f + 135f), 0);
                int num1 = Mathf.Max((int)((vector23.y - 72f) / 64f + 135f), 0);
                int num2 = Mathf.Min((int)((vector24.x + 72f) / 64f + 135f), 269);
                int num3 = Mathf.Min((int)((vector24.y + 72f) / 64f + 135f), 269);
                BuildingManager buildingManager = Singleton<BuildingManager>.instance;
                for (int i = num1; i <= num3; i++)
                {
                    for (int j = num; j <= num2; j++)
                    {
                        ushort mBuildingGrid = buildingManager.m_buildingGrid[i * 270 + j];
                        int num4 = 0;
                        object[] paramcall;
                        while (mBuildingGrid != 0)
                        {
                            //Should we change this 262144?
                            if (mBuildingGrid != buildingID && (float)Singleton<SimulationManager>.instance.m_randomizer.Int32(262144) * single < mFireIntensity)
                            {
                                //Logger.dbgLog("Handlefire1"); 

                                paramcall = new object[]{quad2, mPosition, mPosition1, mBuildingGrid, buildingManager.m_buildings.m_buffer[mBuildingGrid], group};

                                //var x = CBAI.GetType().GetMethod("TrySpreadFire", BindingFlags.Static | BindingFlags.NonPublic).Invoke(CBAI, paramcall);
                                
                                LimitCommonBuildingAI.TrySpreadFire(quad2, mPosition, mPosition1, mBuildingGrid, ref buildingManager.m_buildings.m_buffer[mBuildingGrid], group);

                                //Logger.dbgLog("Handlefire2"); 

                                //orginal
                                //CommonBuildingAI.TrySpreadFire(quad2, mPosition, mPosition1, mBuildingGrid, ref buildingManager.m_buildings.m_buffer[mBuildingGrid], group);
                            }
                            mBuildingGrid = buildingManager.m_buildings.m_buffer[mBuildingGrid].m_nextGridBuilding;
                            int num5 = num4 + 1;
                            num4 = num5;
                            if (num5 < 49152)
                            {
                                continue;
                            }
                            CODebugBase<LogChannel>.Error(LogChannel.Core, string.Concat("Invalid list detected!\n", Environment.StackTrace));
                            break;
                        }
                    }
                }
                Vector3 vector3 = VectorUtils.X_Y(vector21);
                Vector3 vector31 = VectorUtils.X_Y(vector22);
                int num6 = Mathf.Max((int)((vector23.x - 32f) / 32f + 270f), 0);
                int num7 = Mathf.Max((int)((vector23.y - 32f) / 32f + 270f), 0);
                int num8 = Mathf.Min((int)((vector24.x + 32f) / 32f + 270f), 539);
                int num9 = Mathf.Min((int)((vector24.y + 32f) / 32f + 270f), 539);
                TreeManager treeManager = Singleton<TreeManager>.instance;
                for (int k = num7; k <= num9; k++)
                {
                    for (int l = num6; l <= num8; l++)
                    {
                        uint mTreeGrid = treeManager.m_treeGrid[k * 540 + l];
                        int num10 = 0;
                        while (mTreeGrid != 0)
                        {
                            Vector3 position = treeManager.m_trees.m_buffer[mTreeGrid].Position;
                            Vector3 mPosition2 = position - buildingData.m_position;
                            mPosition2 = mPosition2 - (Mathf.Clamp(Vector3.Dot(mPosition2, vector3), (float)(-width) * 4f, (float)width * 4f) * vector3);
                            mPosition2 = mPosition2 - (Mathf.Clamp(Vector3.Dot(mPosition2, vector31), (float)(-length) * 4f, (float)length * 4f) * vector31);
                            float single1 = mPosition2.magnitude;
                            //Should we change this 131072?
                            //Logger.dbgLog("Handlefire3"); 

                            if (single1 < 32f && (float)Singleton<SimulationManager>.instance.m_randomizer.Int32(131072) * single1 < mFireIntensity)
                            {
                                treeManager.BurnTree(mTreeGrid, group, (int)buildingData.m_fireIntensity);
                            }
                            mTreeGrid = treeManager.m_trees.m_buffer[mTreeGrid].m_nextGridTree;
                            int num11 = num10 + 1;
                            num10 = num11;
                            if (num11 < LimitTreeManager.Helper.TreeLimit)
                            {
                                continue;
                            }
                            CODebugBase<LogChannel>.Error(LogChannel.Core, string.Concat("Invalid list detected!\n", Environment.StackTrace));
                            break;
                        }
                    }
                }
            }
        }
    }
}