using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.Math;
using ColossalFramework.Threading;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using ICities;
using UnityEngine;

namespace TreeUnlimiter
{
    internal static class LimitTreeManager
    {
        private static void AfterTerrainUpdate(TreeManager tm, TerrainArea heightArea, TerrainArea surfaceArea, TerrainArea zoneArea)
        {
            unsafe
            {
                float mMin = heightArea.m_min.x;
                float single = heightArea.m_min.z;
                float mMax = heightArea.m_max.x;
                float mMax1 = heightArea.m_max.z;
                int num = Mathf.Max((int)((mMin - 8f) / 32f + 270f), 0);
                int num1 = Mathf.Max((int)((single - 8f) / 32f + 270f), 0);
                int num2 = Mathf.Min((int)((mMax + 8f) / 32f + 270f), 539);
                int num3 = Mathf.Min((int)((mMax1 + 8f) / 32f + 270f), 539);
                for (int i = num1; i <= num3; i++)
                {
                    for (int j = num; j <= num2; j++)
                    {
                        uint mTreeGrid = tm.m_treeGrid[i * 540 + j];
                        int num4 = 0;
                        while (mTreeGrid != 0)
                        {
                            Vector3 position = tm.m_trees.m_buffer[mTreeGrid].Position;
                            if (Mathf.Max(Mathf.Max(mMin - 8f - position.x, single - 8f - position.z), Mathf.Max(position.x - mMax - 8f, position.z - mMax1 - 8f)) < 0f)
                            {
                                //try catch added 5-12-2016
                                //avoids some errors that blocks the game, and id's the tree is missing
                                //we probably shouldn't even bother doing this.
                                try
                                {
                                    tm.m_trees.m_buffer[mTreeGrid].AfterTerrainUpdated(mTreeGrid, mMin, single, mMax, mMax1);
                                }
                                catch (Exception ex) 
                                {
                                    Logger.dbgLog("AfterTerrainUpdate failed on treeidx: " + mTreeGrid.ToString() + "  TreeInstance->m_infoIndex: " + tm.m_trees.m_buffer[mTreeGrid].m_infoIndex.ToString(), ex,true);
                                }
                            }
                            mTreeGrid = tm.m_trees.m_buffer[mTreeGrid].m_nextGridTree;
                            int num5 = num4 + 1;
                            num4 = num5;
                            if (num5 < LimitTreeManager.Helper.TreeLimit)
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

        private static void CalculateAreaHeight(TreeManager tm, float minX, float minZ, float maxX, float maxZ, out int num, out float min, out float avg, out float max)
        {
            unsafe
            {
                    int num1 = Mathf.Max((int)((minX - 8f) / 32f + 270f), 0);
                    int num2 = Mathf.Max((int)((minZ - 8f) / 32f + 270f), 0);
                    int num3 = Mathf.Min((int)((maxX + 8f) / 32f + 270f), 539);
                    int num4 = Mathf.Min((int)((maxZ + 8f) / 32f + 270f), 539);
                    num = 0;    //OUT number of times hit.
                    min = 1024f; //OUT Min height seen.
                    avg = 0f;  //Out avg height seen .
                    max = 0f;  //Out Max height seen.
                    for (int i = num2; i <= num4; i++)
                    {
                        for (int j = num1; j <= num3; j++)
                        {
                            uint mTreeGrid = tm.m_treeGrid[i * 540 + j];
                            int num5 = 0;
                            while (mTreeGrid != 0)
                            {
                                Vector3 position = tm.m_trees.m_buffer[mTreeGrid].Position;
                                if (Mathf.Max(Mathf.Max(minX - 8f - position.x, minZ - 8f - position.z), Mathf.Max(position.x - maxX - 8f, position.z - maxZ - 8f)) < 0f)
                                {
                                    TreeInfo info = tm.m_trees.m_buffer[mTreeGrid].Info;
                                    if (info != null)
                                    {
                                        Randomizer randomizer = new Randomizer(mTreeGrid);
                                        float mMinScale = info.m_minScale + (float)randomizer.Int32(10000) * (info.m_maxScale - info.m_minScale) * 0.0001f;
                                        float mSize = position.y + info.m_generatedInfo.m_size.y * mMinScale * 2f;
                                        if (mSize < min)
                                        {
                                            min = mSize;
                                        }
                                        avg = avg + mSize;
                                        if (mSize > max)
                                        {
                                            max = mSize;
                                        }
                                        num = num + 1;
                                    }
                                }
                                mTreeGrid = tm.m_trees.m_buffer[mTreeGrid].m_nextGridTree;
                                int num6 = num5 + 1;
                                num5 = num6;
                                if (num6 < LimitTreeManager.Helper.TreeLimit )
                                {
                                    continue;
                                }
                                CODebugBase<LogChannel>.Error(LogChannel.Core, string.Concat("Invalid list detected!\n", Environment.StackTrace));
                                break;
                            }
                        }
                    }
                    if (avg != 0f)
                    {
                        avg = avg / (float)num;
                    }
            }
        }

        private static bool CalculateGroupData(TreeManager tm, int groupX, int groupZ, int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays)
        {
            unsafe
            {
                bool flag = false;
                if (layer != tm.m_treeLayer)
                {
                    return flag;
                }
                int num = groupX * 540 / 45;
                int num1 = groupZ * 540 / 45;
                int num2 = (groupX + 1) * 540 / 45 - 1;
                int num3 = (groupZ + 1) * 540 / 45 - 1;
                for (int i = num1; i <= num3; i++)
                {
                    for (int j = num; j <= num2; j++)
                    {
                        uint mTreeGrid = tm.m_treeGrid[i * 540 + j];
                        int num4 = 0;
                        while (mTreeGrid != 0)
                        {
                            if (tm.m_trees.m_buffer[mTreeGrid].CalculateGroupData(mTreeGrid, layer, ref vertexCount, ref triangleCount, ref objectCount, ref vertexArrays))
                            {
                                flag = true;
                            }
                            mTreeGrid = tm.m_trees.m_buffer[mTreeGrid].m_nextGridTree;
                            int num5 = num4 + 1;
                            num4 = num5;
                            if (num5 < LimitTreeManager.Helper.TreeLimit)
                            {
                                continue;
                            }
                            CODebugBase<LogChannel>.Error(LogChannel.Core, string.Concat("Invalid list detected!\n", Environment.StackTrace));
                            break;
                        }
                    }
                }
                return flag;
            }
        }

        private static bool CheckLimits(TreeManager tm)
        {
            ItemClass.Availability mMode = Singleton<ToolManager>.instance.m_properties.m_mode;
            if ((mMode & ItemClass.Availability.MapEditor) != ItemClass.Availability.None)
            {
                if (tm.m_treeCount >= LimitTreeManager.Helper.TreeLimit - 5)
                {
                    return false;
                }
            }
            else if ((mMode & ItemClass.Availability.AssetEditor) != ItemClass.Availability.None)
            {
                if (tm.m_treeCount + Singleton<PropManager>.instance.m_propCount >= 64)
                {
                    return false;
                }
            }
            else if (tm.m_treeCount >= LimitTreeManager.Helper.TreeLimit - 5)
            {
                return false;
            }
            return true;
        }

        private static void EndRenderingImpl(TreeManager tm, RenderManager.CameraInfo cameraInfo)
        {
            unsafe
            {
                FastList<RenderGroup> mRenderedGroups = Singleton<RenderManager>.instance.m_renderedGroups;
                for (int i = 0; i < mRenderedGroups.m_size; i++)
                {
                    RenderGroup mBuffer = mRenderedGroups.m_buffer[i];
                    if ((mBuffer.m_instanceMask & 1 << (tm.m_treeLayer & 31)) != 0)
                    {
                        int mX = mBuffer.m_x * 540 / 45;
                        int mZ = mBuffer.m_z * 540 / 45;
                        int num = (mBuffer.m_x + 1) * 540 / 45 - 1;
                        int mZ1 = (mBuffer.m_z + 1) * 540 / 45 - 1;
                        for (int j = mZ; j <= mZ1; j++)
                        {
                            for (int k = mX; k <= num; k++)
                            {
                                uint mTreeGrid = tm.m_treeGrid[j * 540 + k];
                                int num1 = 0;
                                while (mTreeGrid != 0)
                                {
                                    tm.m_trees.m_buffer[mTreeGrid].RenderInstance(cameraInfo, mTreeGrid, mBuffer.m_instanceMask);
                                    mTreeGrid = tm.m_trees.m_buffer[mTreeGrid].m_nextGridTree;
                                    int num2 = num1 + 1;
                                    num1 = num2;
                                    if (num2 < LimitTreeManager.Helper.TreeLimit)
                                    {
                                        continue;
                                    }
                                    Logger.dbgLog(string.Format("mTreeGrid = {0}  num2= {1}  limit= {2}", mTreeGrid.ToString(), num2, LimitTreeManager.Helper.TreeLimit.ToString()));
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, string.Concat("Invalid list detected!\n", Environment.StackTrace));
                                    break;
                                }
                            }
                        }
                    }
                }
                int num3 = PrefabCollection<TreeInfo>.PrefabCount();
                for (int l = 0; l < num3; l++)
                {
                    TreeInfo prefab = PrefabCollection<TreeInfo>.GetPrefab((uint)l);
                    if (prefab != null && prefab.m_lodCount != 0)
                    {
                        TreeInstance.RenderLod(cameraInfo, prefab);
                    }
                }

                if (Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.None)
                {
                    int mSize = tm.m_burningTrees.m_size;
                    for (int m = 0; m < mSize; m++)
                    {
                        TreeManager.BurningTree burningTree = tm.m_burningTrees.m_buffer[m];
                        if (burningTree.m_treeIndex != 0)
                        {
                            float mFireIntensity = (float)burningTree.m_fireIntensity * 0.003921569f;
                            float mFireDamage = (float)burningTree.m_fireDamage * 0.003921569f;
                            tm.RenderFireEffect(cameraInfo, burningTree.m_treeIndex, ref tm.m_trees.m_buffer[burningTree.m_treeIndex], mFireIntensity, mFireDamage);
                        }
                    }
                }
            }
        }

        private static void FinalizeTree(TreeManager tm, uint tree, ref TreeInstance data)
        {
            unsafe
            {
                int num;
                int num1;
                if (Singleton<ToolManager>.instance.m_properties.m_mode == ItemClass.Availability.AssetEditor)
                {
                    num = Mathf.Clamp((data.m_posX / 16 + 32768) * 540 / 65536, 0, 539);
                    num1 = Mathf.Clamp((data.m_posZ / 16 + 32768) * 540 / 65536, 0, 539);
                }
                else
                {
                    num = Mathf.Clamp((data.m_posX + 32768) * 540 / 65536, 0, 539);
                    num1 = Mathf.Clamp((data.m_posZ + 32768) * 540 / 65536, 0, 539);
                }
                int num2 = num1 * 540 + num;
                while (!Monitor.TryEnter(tm.m_treeGrid, SimulationManager.SYNCHRONIZE_TIMEOUT))
                {
                }
                try
                {
                    uint num3 = 0;
                    uint mTreeGrid = tm.m_treeGrid[num2];
                    int num4 = 0;
                    while (mTreeGrid != 0)
                    {
                        if (mTreeGrid != tree)
                        {
                            num3 = mTreeGrid;
                            mTreeGrid = tm.m_trees.m_buffer[mTreeGrid].m_nextGridTree;
                            int num5 = num4 + 1;
                            num4 = num5;
                            if (num5 <= LimitTreeManager.Helper.TreeLimit)
                            {
                                continue;
                            }
                            CODebugBase<LogChannel>.Error(LogChannel.Core, string.Concat("Invalid list detected!\n", Environment.StackTrace));
                            break;
                        }
                        else if (num3 == 0)
                        {
                            tm.m_treeGrid[num2] = data.m_nextGridTree;
                            break;
                        }
                        else
                        {
                            tm.m_trees.m_buffer[num3].m_nextGridTree = data.m_nextGridTree;
                            break;
                        }
                    }
                    data.m_nextGridTree = 0;
                }
                finally
                {
                    Monitor.Exit(tm.m_treeGrid);
                }
                Singleton<RenderManager>.instance.UpdateGroup(num * 45 / 540, num1 * 45 / 540, tm.m_treeLayer);
            }
        }


        private static bool HandleFireSpread(TreeManager tm,ref TreeManager.BurningTree tree)
        {
            unsafe
            {
                BuildingInfo buildingInfo;
                int num;
                int num1;
                int num2;
                Vector3 position = tm.m_trees.m_buffer[tree.m_treeIndex].Position;
                if (Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(position)) > position.y + 1f)
                {
                    return false;
                }
                if (tm.m_trees.m_buffer[tree.m_treeIndex].GrowState == 0)
                {
                    return true;
                }
                int mFireIntensity = tree.m_fireIntensity + 15 >> 4;
                Singleton<NaturalResourceManager>.instance.TryDumpResource(NaturalResourceManager.Resource.Burned, mFireIntensity, mFireIntensity, position, 20f, true);
                float single = (float)(tree.m_fireIntensity * (128 - Mathf.Abs(tree.m_fireDamage - 128)));
                InstanceID instanceID = new InstanceID()
                {
                    Tree = tree.m_treeIndex
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
                        single = single * ((float)fireSpreadProbability * 0.01f);
                    }
                }
                int num3 = Mathf.Max((int)((position.x - 32f) / 32f + 270f), 0);
                int num4 = Mathf.Max((int)((position.z - 32f) / 32f + 270f), 0);
                int num5 = Mathf.Min((int)((position.x + 32f) / 32f + 270f), 539);
                int num6 = Mathf.Min((int)((position.z + 32f) / 32f + 270f), 539);
                for (int i = num4; i <= num6; i++)
                {
                    for (int j = num3; j <= num5; j++)
                    {
                        uint mTreeGrid = tm.m_treeGrid[i * 540 + j];
                        int num7 = 0;
                        while (mTreeGrid != 0)
                        {
                            Vector3 vector3 = tm.m_trees.m_buffer[mTreeGrid].Position;
                            float single1 = Vector3.Distance(vector3, position);
                            if (single1 < 32f && (float)Singleton<SimulationManager>.instance.m_randomizer.Int32(32768) * single1 < single)
                            {
                                tm.BurnTree(mTreeGrid, group, (int)tree.m_fireIntensity);
                            }
                            mTreeGrid = tm.m_trees.m_buffer[mTreeGrid].m_nextGridTree;
                            int num8 = num7 + 1;
                            num7 = num8;
                            if (num8 < LimitTreeManager.Helper.TreeLimit)
                            {
                                continue;
                            }
                            CODebugBase<LogChannel>.Error(LogChannel.Core, string.Concat("Invalid list detected!\n", Environment.StackTrace));
                            break;
                        }
                    }
                }
                int num9 = Mathf.Max((int)((position.x - 32f - 72f) / 64f + 135f), 0);
                int num10 = Mathf.Max((int)((position.z - 32f - 72f) / 64f + 135f), 0);
                int num11 = Mathf.Min((int)((position.x + 32f + 72f) / 64f + 135f), 269);
                int num12 = Mathf.Min((int)((position.z + 32f + 72f) / 64f + 135f), 269);
                BuildingManager buildingManager = Singleton<BuildingManager>.instance;
                bool flag = false;
                object[] paramcall ;  //krn
                for (int k = num10; k <= num12; k++)
                {
                    for (int l = num9; l <= num11; l++)
                    {
                        ushort mBuildingGrid = buildingManager.m_buildingGrid[k * 270 + l];
                        int num13 = 0;
                        while (mBuildingGrid != 0)
                        {
                            Vector3 mPosition = buildingManager.m_buildings.m_buffer[mBuildingGrid].m_position;
                            float single2 = VectorUtils.LengthSqrXZ(mPosition - position);
                            if (!flag && single2 < 10000f)
                            {
                                BuildingInfo info1 = buildingManager.m_buildings.m_buffer[mBuildingGrid].Info;
                                float single3 = info1.m_buildingAI.MaxFireDetectDistance(mBuildingGrid, ref buildingManager.m_buildings.m_buffer[mBuildingGrid]);
                                if (single2 < single3 * single3 && info1.m_buildingAI.NearObjectInFire(mBuildingGrid, ref buildingManager.m_buildings.m_buffer[mBuildingGrid], instanceID, position))
                                {
                                    flag = true;
                                }
                            }
                            if (single2 < 10816f)
                            {
                                float mAngle = buildingManager.m_buildings.m_buffer[mBuildingGrid].m_angle;
                                buildingManager.m_buildings.m_buffer[mBuildingGrid].GetInfoWidthLength(out buildingInfo, out num, out num1);
                                Vector3 vector31 = new Vector3(Mathf.Cos(mAngle), 0f, Mathf.Sin(mAngle));
                                Vector3 vector32 = new Vector3(vector31.z, 0f, -vector31.x);
                                Vector3 vector33 = position - mPosition;
                                vector33 = vector33 - (Mathf.Clamp(Vector3.Dot(vector33, vector31), (float)(-num) * 4f, (float)num * 4f) * vector31);
                                vector33 = vector33 - (Mathf.Clamp(Vector3.Dot(vector33, vector32), (float)(-num1) * 4f, (float)num1 * 4f) * vector32);
                                float single4 = vector33.magnitude;
                                if (single4 < 32f && (float)Singleton<SimulationManager>.instance.m_randomizer.Int32(65536) * single4 < single)
                                {
                                    //kh - use reflection1 1.60 testing - will use BP's reverse redirect
                                    // once things seem to be working. reminder (also needed couple other places):
                                    /* http://community.simtropolis.com/forums/topic/69673-tutorial-how-to-invoke-private-methods-without-reflection/ */

                                    paramcall = new object[] { mBuildingGrid, buildingManager.m_buildings.m_buffer[mBuildingGrid], group};
                                    var x = tm.GetType().GetMethod("TrySpreadFire", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, paramcall);
                                   
                                    //original
                                   // TreeManager.TrySpreadFire(mBuildingGrid, ref buildingManager.m_buildings.m_buffer[mBuildingGrid], group);
                                }
                            }
                            mBuildingGrid = buildingManager.m_buildings.m_buffer[mBuildingGrid].m_nextGridBuilding;
                            int num14 = num13 + 1;
                            num13 = num14;
                            if (num14 < 49152)
                            {
                                continue;
                            }
                            CODebugBase<LogChannel>.Error(LogChannel.Core, string.Concat("Invalid list detected!\n", Environment.StackTrace));
                            break;
                        }
                    }
                }
                Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(ImmaterialResourceManager.Resource.FirewatchCoverage, position, out num2);
                if (Singleton<SimulationManager>.instance.m_randomizer.Int32(100) < num2)
                {
                    FastList<ushort> serviceBuildings = buildingManager.GetServiceBuildings(ItemClass.Service.FireDepartment);
                    int num15 = 0;
                    while (num15 < serviceBuildings.m_size)
                    {
                        ushort mBuffer = serviceBuildings.m_buffer[num15];
                        Vector3 mPosition1 = buildingManager.m_buildings.m_buffer[mBuffer].m_position;
                        float single5 = VectorUtils.LengthSqrXZ(mPosition1 - position);
                        BuildingInfo buildingInfo1 = buildingManager.m_buildings.m_buffer[mBuffer].Info;
                        float single6 = buildingInfo1.m_buildingAI.MaxFireDetectDistance(mBuffer, ref buildingManager.m_buildings.m_buffer[mBuffer]);
                        if (single5 >= single6 * single6 || !buildingInfo1.m_buildingAI.NearObjectInFire(mBuffer, ref buildingManager.m_buildings.m_buffer[mBuffer], instanceID, position))
                        {
                            num15++;
                        }
                        else
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                return true;
            }
        }

        //KH 11/2016: this originally here via marko, I've left it, though I just noticed
        // it doesn't appear to be needed. Something to be looked during next round post 1.6

        private static void InitializeTree(TreeManager tm, uint tree, ref TreeInstance data, bool assetEditor)
        {
            unsafe
            {
                int num;
                int num1;
                if (assetEditor)
                {
                    num = Mathf.Clamp((data.m_posX / 16 + 32768) * 540 / 65536, 0, 539);
                    num1 = Mathf.Clamp((data.m_posZ / 16 + 32768) * 540 / 65536, 0, 539);
                }
                else
                {
                    num = Mathf.Clamp((data.m_posX + 32768) * 540 / 65536, 0, 539);
                    num1 = Mathf.Clamp((data.m_posZ + 32768) * 540 / 65536, 0, 539);
                }
                int num2 = num1 * 540 + num;
                while (!Monitor.TryEnter(tm.m_treeGrid, SimulationManager.SYNCHRONIZE_TIMEOUT))
                {
                }
                try
                {
                    tm.m_trees.m_buffer[tree].m_nextGridTree = tm.m_treeGrid[num2];
                    tm.m_treeGrid[num2] = tree;
                }
                finally
                {
                    Monitor.Exit(tm.m_treeGrid);
                }
            }
        }

        private static bool OverlapQuad(TreeManager tm, Quad2 quad, float minY, float maxY,ItemClass.CollisionType collisionType, int layer, uint ignoreTree)
        {
            unsafe
            {
                Vector2 vector2 = quad.Min();
                Vector2 vector21 = quad.Max();
                int num = Mathf.Max((int)(((double)vector2.x - 8) / 32 + 270), 0);
                int num1 = Mathf.Max((int)(((double)vector2.y - 8) / 32 + 270), 0);
                int num2 = Mathf.Min((int)(((double)vector21.x + 8) / 32 + 270), 539);
                int num3 = Mathf.Min((int)(((double)vector21.y + 8) / 32 + 270), 539);
                for (int i = num1; i <= num3; i++)
                {
                    for (int j = num; j <= num2; j++)
                    {
                        uint mTreeGrid = tm.m_treeGrid[i * 540 + j];
                        int num4 = 0;
                        while (mTreeGrid != 0)
                        {
                            Vector3 position = tm.m_trees.m_buffer[mTreeGrid].Position;
                            if ((double)Mathf.Max(Mathf.Max(vector2.x - 8f - position.x, vector2.y - 8f - position.z), Mathf.Max((float)((double)position.x - (double)vector21.x - 8), (float)((double)position.z - (double)vector21.y - 8))) < 0 && tm.m_trees.m_buffer[mTreeGrid].OverlapQuad(mTreeGrid, quad, minY, maxY,collisionType))
                            {
                                return true;
                            }
                            mTreeGrid = tm.m_trees.m_buffer[mTreeGrid].m_nextGridTree;
                            int num5 = num4 + 1;
                            num4 = num5;
                            if (num5 < LimitTreeManager.Helper.TreeLimit)
                            {
                                continue;
                            }
                            CODebugBase<LogChannel>.Error(LogChannel.Core, string.Concat("Invalid list detected!\n", Environment.StackTrace));
                            break;
                        }
                    }
                }
                return false;
            }
        }

        private static void PopulateGroupData(TreeManager tm, int groupX, int groupZ, int layer, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance, ref bool requireSurfaceMaps)
        {
            unsafe
            {
                if (layer != tm.m_treeLayer)
                {
                    return;
                }
                int num = groupX * 540 / 45;
                int num1 = groupZ * 540 / 45;
                int num2 = (groupX + 1) * 540 / 45 - 1;
                int num3 = (groupZ + 1) * 540 / 45 - 1;
                for (int i = num1; i <= num3; i++)
                {
                    for (int j = num; j <= num2; j++)
                    {
                        uint mTreeGrid = tm.m_treeGrid[i * 540 + j];
                        int num4 = 0;
                        while (mTreeGrid != 0)
                        {
                            tm.m_trees.m_buffer[mTreeGrid].PopulateGroupData(mTreeGrid, layer, ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
                            mTreeGrid = tm.m_trees.m_buffer[mTreeGrid].m_nextGridTree;
                            int num5 = num4 + 1;
                            num4 = num5;
                            if (num5 < LimitTreeManager.Helper.TreeLimit)
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

        private static bool RayCast(TreeManager tm, Segment3 ray, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Layer itemLayers, TreeInstance.Flags ignoreFlags, out Vector3 hit, out uint treeIndex)
        {
            unsafe
            {
                int num;
                int num1;
                int num2;
                int num3;
                int num4;
                int num5;
                float single;
                float single1;
                Bounds bound = new Bounds(new Vector3(0f, 512f, 0f), new Vector3(17280f, 1152f, 17280f));
                if (ray.Clip(bound))
                {
                    Vector3 vector3 = ray.b - ray.a;
                    int num6 = (int)((double)ray.a.x / 32 + 270);
                    int num7 = (int)((double)ray.a.z / 32 + 270);
                    int num8 = (int)((double)ray.b.x / 32 + 270);
                    int num9 = (int)((double)ray.b.z / 32 + 270);
                    float single2 = Mathf.Abs(vector3.x);
                    float single3 = Mathf.Abs(vector3.z);
                    if ((double)single2 >= (double)single3)
                    {
                        num = ((double)vector3.x <= 0 ? -1 : 1);
                        num1 = 0;
                        if ((double)single2 != 0)
                        {
                            vector3 = vector3 * (32f / single2);
                        }
                    }
                    else
                    {
                        num = 0;
                        num1 = ((double)vector3.z <= 0 ? -1 : 1);
                        if ((double)single3 != 0)
                        {
                            vector3 = vector3 * (32f / single3);
                        }
                    }
                    float single4 = 2f;
                    float single5 = 10000f;
                    treeIndex = 0;
                    Vector3 vector31 = ray.a;
                    Vector3 vector32 = ray.a;
                    int num10 = num6;
                    int num11 = num7;
                    do
                    {
                        Vector3 vector33 = vector32 + vector3;
                        if (num != 0)
                        {
                            num2 = ((num10 != num6 || num <= 0) && (num10 != num8 || num >= 0) ? Mathf.Max(num10, 0) : Mathf.Max((int)(((double)vector33.x - 72) / 32 + 270), 0));
                            num3 = ((num10 != num6 || num >= 0) && (num10 != num8 || num <= 0) ? Mathf.Min(num10, 539) : Mathf.Min((int)(((double)vector33.x + 72) / 32 + 270), 539));
                            num4 = Mathf.Max((int)(((double)Mathf.Min(vector31.z, vector33.z) - 72f) / 32f + 270f), 0);
                            num5 = Mathf.Min((int)(((double)Mathf.Max(vector31.z, vector33.z) + 72f) / 32f + 270f), 539);
                        }
                        else
                        {
                            num4 = ((num11 != num7 || num1 <= 0) && (num11 != num9 || num1 >= 0) ? Mathf.Max(num11, 0) : Mathf.Max((int)(((double)vector33.z - 72) / 32 + 270), 0));
                            num5 = ((num11 != num7 || num1 >= 0) && (num11 != num9 || num1 <= 0) ? Mathf.Min(num11, 539) : Mathf.Min((int)(((double)vector33.z + 72) / 32 + 270), 539));
                            num2 = Mathf.Max((int)(((double)Mathf.Min(vector31.x, vector33.x) - 72f) / 32f + 270f), 0);
                            num3 = Mathf.Min((int)(((double)Mathf.Max(vector31.x, vector33.x) + 72f) / 32f + 270f), 539);
                        }
                        for (int i = num4; i <= num5; i++)
                        {
                            for (int j = num2; j <= num3; j++)
                            {
                                uint mTreeGrid = tm.m_treeGrid[i * 540 + j];
                                int num12 = 0;
                                while (mTreeGrid != 0)
                                {
                                    if ((tm.m_trees.m_buffer[mTreeGrid].m_flags & (ushort)ignoreFlags) == 0 && (double)ray.DistanceSqr(tm.m_trees.m_buffer[mTreeGrid].Position) < 2500)
                                    {
                                        TreeInfo info = tm.m_trees.m_buffer[mTreeGrid].Info;
                                        if ((service == ItemClass.Service.None || info.m_class.m_service == service) && (subService == ItemClass.SubService.None || info.m_class.m_subService == subService) && (itemLayers == ItemClass.Layer.None || (info.m_class.m_layer & itemLayers) != ItemClass.Layer.None) && tm.m_trees.m_buffer[mTreeGrid].RayCast(mTreeGrid, ray, out single, out single1) && ((double)single < (double)single4 - 9.99999974737875E-05 || (double)single < (double)single4 + 9.99999974737875E-05 && (double)single1 < (double)single5))
                                        {
                                            single4 = single;
                                            single5 = single1;
                                            treeIndex = mTreeGrid;
                                        }
                                    }
                                    mTreeGrid = tm.m_trees.m_buffer[mTreeGrid].m_nextGridTree;
                                    int num13 = num12 + 1;
                                    num12 = num13;
                                    if (num13 <= LimitTreeManager.Helper.TreeLimit)
                                    {
                                        continue;
                                    }
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, string.Concat("Invalid list detected!\n", Environment.StackTrace));
                                    break;
                                }
                            }
                        }
                        vector31 = vector32;
                        vector32 = vector33;
                        num10 = num10 + num;
                        num11 = num11 + num1;
                    }
                    while ((num10 <= num8 || num <= 0) && (num10 >= num8 || num >= 0) && (num11 <= num9 || num1 <= 0) && (num11 >= num9 || num1 >= 0));
                    if (single4 != 2f)
                    {
                        hit = ray.Position(single4);
                        return true;
                    }
                }
                hit = Vector3.zero;
                treeIndex = 0;
                return false;
            }
        }


        //redirected because we need it to call our version of finalizetree?
        private static void ReleaseTreeImplementation(TreeManager tm, uint tree, ref TreeInstance data)
        {
            if (data.m_flags != 0)
            {
                InstanceID instanceID = new InstanceID()
                {
                    Tree = tree
                };
                Singleton<InstanceManager>.instance.ReleaseInstance(instanceID);
                data.m_flags = (ushort)(data.m_flags | 2);
                data.UpdateTree(tree);
                
                //1.6 new code from c\o related to burning trees
                //why the hell they swap an empty one for old and create new empty one
                // at the tail end of the fastlist of burning trees
                //makes no logical sense to me yet. Why not just null and .remove the 
                //darn object from the list?
                if ((data.m_flags & 64) != 0)
                {
                    int mSize = tm.m_burningTrees.m_size - 1;
                    int num = 0;
                    while (num <= mSize)
                    {
                        if (tm.m_burningTrees.m_buffer[num].m_treeIndex != tree)
                        {
                            num++;
                        }
                        else
                        {
                            tm.m_burningTrees.m_buffer[num] = tm.m_burningTrees.m_buffer[mSize];
                            TreeManager.BurningTree burningTree = new TreeManager.BurningTree();
                            tm.m_burningTrees.m_buffer[mSize] = burningTree;
                            tm.m_burningTrees.m_size = mSize;
                            break;
                        }
                    }
                }


                data.m_flags = 0;
                try
                {
                    LimitTreeManager.FinalizeTree(tm, tree, ref data);
                }
                catch (Exception exception1)
                {
                    Exception exception = exception1;
                    object[] objArray = new object[] { tree, tm.m_trees.m_size, LimitTreeManager.Helper.TreeLimit, LimitTreeManager.Helper.UseModifiedTreeCap };
                    Logger.dbgLog(string.Format(" FinalizeTree exception: Releasing {0} {1} {2} {3}", objArray), exception1, true);
                }
                tm.m_trees.ReleaseItem(tree);
                tm.m_treeCount = (int)(tm.m_trees.ItemCount() - 1);
            }
        }

        private static float SampleSmoothHeight(TreeManager tm, Vector3 worldPos)
        {
            unsafe
            {
                float single = 0f;
                int num = Mathf.Max((int)(((double)worldPos.x - 32) / 32 + 270), 0);
                int num1 = Mathf.Max((int)(((double)worldPos.z - 32) / 32 + 270), 0);
                int num2 = Mathf.Min((int)(((double)worldPos.x + 32) / 32 + 270), 539);
                int num3 = Mathf.Min((int)(((double)worldPos.z + 32) / 32 + 270), 539);
                for (int i = num1; i <= num3; i++)
                {
                    for (int j = num; j <= num2; j++)
                    {
                        uint mTreeGrid = tm.m_treeGrid[i * 540 + j];
                        int num4 = 0;
                        while (mTreeGrid != 0)
                        {
                            if (tm.m_trees.m_buffer[mTreeGrid].GrowState != 0)
                            {
                                Vector3 position = tm.m_trees.m_buffer[mTreeGrid].Position;
                                Vector3 vector3 = worldPos - position;
                                float single1 = 1024f;
                                float single2 = (float)((double)vector3.x * (double)vector3.x + (double)vector3.z * (double)vector3.z);
                                if ((double)single2 < (double)single1)
                                {
                                    TreeInfo info = tm.m_trees.m_buffer[mTreeGrid].Info;
                                    float single3 = MathUtils.SmoothClamp01(1f - Mathf.Sqrt(single2 / single1));
                                    
                                    single3 = Mathf.Lerp(worldPos.y, position.y + info.m_generatedInfo.m_size.y * 1.25f, single3);
                                    
                                    //1.6.0? used to have the below but looks like I missed a change in 1.4 to math.lerp + worldpos.y
                                    //float mSize = position.y + info.m_generatedInfo.m_size.y * 1.25f * single3;

                                    if (single3 > single)
                                    {
                                        single = single3;
                                    }
                                }
                            }
                            mTreeGrid = tm.m_trees.m_buffer[mTreeGrid].m_nextGridTree;
                            int num5 = num4 + 1;
                            num4 = num5;
                            if (num5 < LimitTreeManager.Helper.TreeLimit)
                            {
                                continue;
                            }
                            CODebugBase<LogChannel>.Error(LogChannel.Core, string.Concat("Invalid list detected!\n", Environment.StackTrace));
                            break;
                        }
                    }
                }
                return single;
            }
        }

        private static void TerrainUpdated(TreeManager tm, TerrainArea heightArea, TerrainArea surfaceArea, TerrainArea zoneArea)
        {
            unsafe
            {
                float mMin = surfaceArea.m_min.x;
                float single = surfaceArea.m_min.z;
                float mMax = surfaceArea.m_max.x;
                float mMax1 = surfaceArea.m_max.z;
                int num = Mathf.Max((int)(((double)mMin - 8) / 32 + 270), 0);
                int num1 = Mathf.Max((int)(((double)single - 8) / 32 + 270), 0);
                int num2 = Mathf.Min((int)(((double)mMax + 8) / 32 + 270), 539);
                int num3 = Mathf.Min((int)(((double)mMax1 + 8) / 32 + 270), 539);
                for (int i = num1; i <= num3; i++)
                {
                    for (int j = num; j <= num2; j++)
                    {
                        uint mTreeGrid = tm.m_treeGrid[i * 540 + j];
                        int num4 = 0;
                        while (mTreeGrid != 0)
                        {
                            Vector3 position = tm.m_trees.m_buffer[mTreeGrid].Position;
                            if ((double)Mathf.Max(Mathf.Max(mMin - 8f - position.x, single - 8f - position.z), Mathf.Max((float)((double)position.x - (double)mMax - 8f), (float)((double)position.z - (double)mMax1 - 8f))) < 0)
                            {
                                tm.m_trees.m_buffer[mTreeGrid].TerrainUpdated(mTreeGrid, mMin, single, mMax, mMax1);
                            }
                            mTreeGrid = tm.m_trees.m_buffer[mTreeGrid].m_nextGridTree;
                            int num5 = num4 + 1;
                            num4 = num5;
                            if (num5 < LimitTreeManager.Helper.TreeLimit)
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


        private static void UpdateData(TreeManager tm, SimulationManager.UpdateMode mode)
        {
            Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.BeginLoading("TreeManager.UpdateData");
            if (Mod.DEBUG_LOG_ON){Logger.dbgLog(" UpdateData() calling Ensure Init");}
            LimitTreeManager.Helper.EnsureInit(3);
            
            for (int i = 1; i < LimitTreeManager.Helper.TreeLimit; i++)
            {
                if (tm.m_trees.m_buffer[i].m_flags != 0 && tm.m_trees.m_buffer[i].Info == null)
                {
                    tm.ReleaseTree((uint)i);
                }
            }
            int num = PrefabCollection<TreeInfo>.PrefabCount();
            int num1 = 1;
            while (num1 * num1 < num)
            {
                num1++;
            }
            for (int j = 0; j < num; j++)
            {
                TreeInfo prefab = PrefabCollection<TreeInfo>.GetPrefab((uint)j);
                if (prefab != null)
                {
                    prefab.SetRenderParameters(j, num1);
                }
            }
            ColossalFramework.Threading.ThreadHelper.dispatcher.Dispatch(() => {
                tm.GetType().GetField("m_lastShadowRotation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tm, new Quaternion());
                tm.GetType().GetField("m_lastCameraRotation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tm, new Quaternion());
            });
            tm.m_infoCount = num;
            Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.EndLoading();
        }

        private static void UpdateTree(TreeManager tm, uint tree)
        {
            unsafe
            {
                tm.m_updatedTrees[tree >> 6] = tm.m_updatedTrees[tree >> 6] | (ulong)1L << (int)(tree & 63);
                tm.m_treesUpdated = true;
            }
        }

        private static void UpdateTrees(TreeManager tm, float minX, float minZ, float maxX, float maxZ)
        {
            unsafe
            {
                int num = Mathf.Max((int)(((double)minX - 8f) / 32f + 270f), 0);
                int num1 = Mathf.Max((int)(((double)minZ - 8f) / 32f + 270f), 0);
                int num2 = Mathf.Min((int)(((double)maxX + 8f) / 32f + 270f), 539);
                int num3 = Mathf.Min((int)(((double)maxZ + 8f) / 32f + 270f), 539);
                for (int i = num1; i <= num3; i++)
                {
                    for (int j = num; j <= num2; j++)
                    {
                        uint mTreeGrid = tm.m_treeGrid[i * 540 + j];
                        int num4 = 0;
                        while (mTreeGrid != 0)
                        {
                            Vector3 position = tm.m_trees.m_buffer[mTreeGrid].Position;
                            if ((double)Mathf.Max(Mathf.Max(minX - 8f - position.x, minZ - 8f - position.z), Mathf.Max((float)((double)position.x - (double)maxX - 8), (float)((double)position.z - (double)maxZ - 8))) < 0f)
                            {
                                tm.m_updatedTrees[mTreeGrid >> 6] = tm.m_updatedTrees[mTreeGrid >> 6] | (ulong)1ul << (int)(mTreeGrid & 63);
                                tm.m_treesUpdated = true;
                            }
                            mTreeGrid = tm.m_trees.m_buffer[mTreeGrid].m_nextGridTree;
                            int num5 = num4 + 1;
                            num4 = num5;
                            if (num5 < LimitTreeManager.Helper.TreeLimit)
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

        internal static class CustomSerializer
        {
            internal static bool Deserialize()
            {
                unsafe
                {
                    if (Mod.DEBUG_LOG_ON){Logger.dbgLog(string.Concat(" treelimit = ", Helper.TreeLimit.ToString()));}
//9-25-2015         if (Mod.DEBUG_LOG_ON){Debug.Log("[TreeUnlimiter::CustomSerializer::Deserialize()] calling Ensure Init");}
                    LimitTreeManager.Helper.EnsureInit(2);
                    if (!LimitTreeManager.Helper.UseModifiedTreeCap) { return false; }

                    byte[] numArray = null;
                    if (!Singleton<SimulationManager>.instance.m_serializableDataStorage.TryGetValue("mabako/unlimiter", out numArray))
                    {
                        Logger.dbgLog(" No extra data saved or found with this savegame or map.");
                        return false;
                    }
                    if (Mod.DEBUG_LOG_ON)
                    {
                        object[] length = new object[] { (int)numArray.Length };
                        Logger.dbgLog(string.Format("We have {0} bytes of extra trees", length));
                    }
                    if ((int)numArray.Length < 2 || (int)numArray.Length % 2 != 0)
                    {
                        Logger.dbgLog(" Invalid chunk size");
                        return false;
                    }
                    TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                    ushort[] numArray1 = new ushort[(int)numArray.Length / 2];
                    Buffer.BlockCopy(numArray, 0, numArray1, 0, (int)numArray.Length);
                    uint num = 0;
                    uint num1 = num;
                    ushort versionnum = numArray1[num1];
                    if (versionnum != 1 & versionnum != 2 & versionnum !=3)
                    {
                        object[] objArray = new object[] { numArray1[0], versionnum, numArray[0], numArray[1] };
                        Logger.dbgLog(string.Format(" Wrong version ({0}|{1}|{2},{3}).", objArray));
                        return false;
                    }
                    int numStoredTrees = 0;
                    int headerTreeCount = 0;
                    ushort fileflags = 0;

                    if (versionnum == 1)
                    {
                        num = num1 + 1; //adjust for version header.

                        if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("save format - version 1 detected."); }
                        numStoredTrees = Mod.FormatVersion1NumOfTrees;
                    }
                    //handles new version where we've stored the number of stored trees in the save.
                    if(versionnum > 1)
                    {
                        num = num1 + 3; //adjust for version header + saved array size(v2).
                        if (versionnum == 3)
                        { num = num1 + 10; } //adjust for version+full_v3_header

                        if (Mod.DEBUG_LOG_ON) { Logger.dbgLog(string.Format("save format - version {0} detected.",versionnum.ToString())); }

                        numStoredTrees = (numArray1[1] << 16) | (numArray1[2] & 0xffff);  //tree limit figure used when saved not a tree count
                        if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("stored array count = " + numStoredTrees.ToString()); }
                        if (numStoredTrees <= 0 | numStoredTrees > 2097152)
                        { 
                            Logger.dbgLog(" *Warning* - Aborting deserialize; storedTreeArray <= 0 or > 2097152");
                            return false;
                        }
                        if (numStoredTrees > LimitTreeManager.Helper.TreeLimit)
                        {
                            Logger.dbgLog(string.Format("** WARNING ** Number of trees in file is greater then scaled value, we will only load as many trees ({0}) as will fit in currently scaled limit of {1}.",
                                numStoredTrees.ToString(),LimitTreeManager.Helper.TreeLimit.ToString()));

                            numStoredTrees = LimitTreeManager.Helper.TreeLimit;
                        }
                        //get treecount from header in v3+
                        if (versionnum > 2) 
                        { 
                            headerTreeCount = (numArray1[3] << 16) | (numArray1[4] & 0xffff);  //actual number of stored trees
                            fileflags = numArray1[9];
                            if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("stored treecount = " + headerTreeCount.ToString()); }
                        }
                    }

                    int num3 = 0; //holds our number of processed trees
                    bool flgPacked = false;
                    if ((fileflags & (ushort)Helper.SaveFlags.packed) != (ushort)Helper.SaveFlags.packed)
                    {
                        if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("Stored data is not packed. reading " + (numStoredTrees - Mod.DEFAULT_TREE_COUNT ).ToString() + " number of objects"); }
                        TreeInstance.Flags flags1;
                        for (int i = 262144; i < numStoredTrees; i++) //start at the top thier limit.
                        {
                            uint num4 = num;
                            try
                            {
                                uint num5 = num;
                                num = num5 + 1;
                                //do flags - //1.6.0 remove burning and damage from all.
                                flags1 = (TreeInstance.Flags)numArray1[num5];
                                flags1 &= ~(TreeInstance.Flags.FireDamage | TreeInstance.Flags.Burning);
                                mBuffer[i].m_flags = (ushort)flags1; 

                                if (mBuffer[i].m_flags != 0)
                                {
                                    uint num6 = num;
                                    num = num6 + 1;
                                    mBuffer[i].m_infoIndex = numArray1[num6];
                                    uint num7 = num;
                                    num = num7 + 1;
                                    mBuffer[i].m_posX = (short)numArray1[num7];
                                    //mBuffer[i].m_posY = 0; // we do later for entire buffer instead of here.
                                    uint num8 = num;
                                    num = num8 + 1;
                                    mBuffer[i].m_posZ = (short)numArray1[num8];
                                    num3++;
                                }
                                if ((ulong)num == (ulong)((int)numArray1.Length))
                                {
                                    break;
                                }
                            }
                            catch (Exception exception1)
                            {
                                Exception exception = exception1;
                                object[] objArray1 = new object[] { i, num4, (int)numArray1.Length };
                                Logger.dbgLog(string.Format("Error (non packed) - While fetching tree {0} in pos {1} of {2}", objArray1), exception1, true);
                                throw exception;
                            }
                        }
                    }
                    else //packed version.
                    {
                        flgPacked = true;
                        if (Mod.DEBUG_LOG_ON)
                        { Logger.dbgLog("Stored data is packed. reading " + headerTreeCount.ToString() + " number of objects"); }
                        if (headerTreeCount > (Helper.TreeLimit - Mod.DEFAULT_TREE_COUNT))
                        {
                            headerTreeCount = (Helper.TreeLimit - Mod.DEFAULT_TREE_COUNT);
                            Logger.dbgLog(string.Format("**Warning** Stored treecount is greater then exisiting scaled arraysize, will only load {0} number of trees", headerTreeCount.ToString()));  
                        }
                        int i = Mod.DEFAULT_TREE_COUNT;  //start adding at location 262144.
                        int j = 0;
                        TreeInstance.Flags flags2; //hold temp flag values
                        for (j = 0; j < headerTreeCount ; j++) //use our stored treecount limit.
                        {
                            uint num4 = num; //for error logging
                            try
                            {
                                uint num5 = num; //store current array index.
                                num = num5 + 1; //bump master arrayindex tracker.


                                //do flags - //1.6.0 remove burning and damage from all.
                                flags2 = (TreeInstance.Flags)numArray1[num5];
                                flags2 &= ~(TreeInstance.Flags.FireDamage | TreeInstance.Flags.Burning);
                                mBuffer[i].m_flags = (ushort)flags2; 

                               
                                if (mBuffer[i].m_flags != 0)
                                {
                                    uint num6 = num; //store current master.
                                    num = num6 + 1; //bumper master up 
                                    mBuffer[i].m_infoIndex = numArray1[num6];
                                    uint num7 = num; //store current master.
                                    num = num7 + 1; //bump master up. 
                                    mBuffer[i].m_posX = (short)numArray1[num7];
                                    //mBuffer[i].m_posY = 0; // we do later for entire buffer instead of here.
                                    uint num8 = num; //store current master
                                    num = num8 + 1;  //bump master
                                    mBuffer[i].m_posZ = (short)numArray1[num8];
                                    num3++;  //data tracker
                                }
                                //needed cause we 'for' on j, might as well stop if we're at the limit of the array.
                                if ((ulong)num == (ulong)((int)numArray1.Length))
                                {
                                    break;
                                }
                                i++;  //increment our treemanager buffer index.
                            }
                            catch (Exception exception1)
                            {
                                Exception exception = exception1;
                                object[] objArray1 = new object[] { i.ToString(), num4.ToString(), (int)numArray1.Length,j.ToString() };
                                Logger.dbgLog(string.Format("Error - While fetching packed tree i={0} j={4} in array pos {1} of {2}", objArray1), exception1, true);
                                throw exception;
                            }
                        }
                    }
                    object[] treeLimit1;
                    if (!flgPacked)
                    { treeLimit1 = new object[] { num3, (numStoredTrees - Mod.DEFAULT_TREE_COUNT),(numStoredTrees - Mod.DEFAULT_TREE_COUNT) }; }
                    else
                    { treeLimit1 = new object[] { num3, headerTreeCount ,(numStoredTrees - Mod.DEFAULT_TREE_COUNT) }; }
                    Logger.dbgLog(string.Format(" Loaded {0} trees of {1} (out of {2} possible in extra range)", treeLimit1));

                    return true;
                }
            }


            internal static bool DeserializeBurningTrees()
            {
                //addtions burning trees
                try
                {
                    if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Logger.dbgLog("threadname: " + Thread.CurrentThread.Name); }
                    UTSaveDataContainer oMasterContainer;
                    oMasterContainer = DeseralizeSaveDataContainer();  //farm it out, always returns at least bare object.
                    if (oMasterContainer == null || oMasterContainer.SaveType ==1 || oMasterContainer.m_BurningTreeData == null)
                    {
                        Logger.dbgLog("Data Container is null or there is no m_BurningTreeData data to load");
                        return false;
                    }
                    if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
                    { Logger.dbgLog(string.Format("Containername:{0} Created:{1} with GameVersionStamp:{2}", oMasterContainer.ContainerName, oMasterContainer.CreatedDate.ToString(), oMasterContainer.GameVersion)); }
                    TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                    if (oMasterContainer.m_BurningTreeData.BurningCount > 0 && oMasterContainer.m_BurningTreeData.BurningTreeList != null)
                    {
                        FastList<TreeManager.BurningTree> TMburningtrees = Singleton<TreeManager>.instance.m_burningTrees;
                        if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
                        { Logger.dbgLog(string.Format("org m_size: {0} org bufflen: {1} To-processcount: {2}", TMburningtrees.m_size.ToString(), TMburningtrees.m_buffer.Length.ToString(),oMasterContainer.m_BurningTreeData.BurningCount.ToString())); }
   
                        TreeManager.BurningTree oBurningTree = new TreeManager.BurningTree();
                        int tmpcounter = 0; int tmpaddcounter = 0;
                        foreach (UTSaveDataContainer.UTBurningTreeInstance UTburn in oMasterContainer.m_BurningTreeData.BurningTreeList)
                        {
                            //are we valid, are we going to link to an existing created tree?
                            if (UTburn.m_treeIndex != 0 && UTburn.m_treeIndex < mBuffer.Length && (mBuffer[UTburn.m_treeIndex].m_flags & (ushort)TreeInstance.Flags.Created) != 0)
                            {
                                oBurningTree.m_treeIndex = UTburn.m_treeIndex;
                                oBurningTree.m_fireDamage = UTburn.m_fireDamage;
                                oBurningTree.m_fireIntensity = UTburn.m_fireIntensity;
                                TMburningtrees.Add(oBurningTree);
                                tmpaddcounter++;
                                    
                                mBuffer[oBurningTree.m_treeIndex].m_flags = (ushort)(mBuffer[oBurningTree.m_treeIndex].m_flags | 64);

                                if (oBurningTree.m_fireIntensity != 0)
                                { mBuffer[oBurningTree.m_treeIndex].m_flags = (ushort)(mBuffer[oBurningTree.m_treeIndex].m_flags | 128); }

                            }
                            else
                            {
                                Logger.dbgLog(string.Format("Skipping extra tree location:{0}  stored m_treeindex: {1}  because = 0 or < active TreeBuffer.Length or TreeBuffer flags not created {2}", tmpcounter.ToString(), UTburn.m_treeIndex.ToString(), mBuffer[UTburn.m_treeIndex].m_flags.ToString()));
                            }
                            tmpcounter++;
                        }
                        if (Mod.DEBUG_LOG_ON)
                        { Logger.dbgLog("after m_size: " + TMburningtrees.m_size.ToString() + "  after bufflen: " + TMburningtrees.m_buffer.Length.ToString()); }
                        
                        Logger.dbgLog(string.Format("Processed {0} extra saved burning trees. Added {1} extra saved burning trees", tmpcounter.ToString(), tmpaddcounter.ToString()));
                    }
                    else
                    {
                        if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("MasterContainer.burningtreeData == null no burning tree data to add."); }
                    }

                }
                catch (Exception ex51)
                { 
                    Logger.dbgLog(ex51.ToString());
                    return false;
                }
                return true;

            }

            internal static UTSaveDataContainer DeseralizeSaveDataContainer()
            {
                UTSaveDataContainer oMasterContainer = new UTSaveDataContainer();
                byte[] ourBytes = null;
                //DataExtension._serializableData.
                bool errFlag = true;
                try
                {
                    Logger.dbgLog("threadname: " + Thread.CurrentThread.Name);
                    ourBytes = SaveDataUtils.ReadBytesFromNamedKey(UTSaveDataContainer.DefaultContainername);
                    if (ourBytes == null || ourBytes.Length < 10) //ourheaderalone is like 1k.
                    {
                        Logger.dbgLog("We could not find our named byte array. No extra burningtree data to process");

                        if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
                        {
                            int iCount = Singleton<SimulationManager>.instance.m_serializableDataStorage.Keys.Count();
                            Logger.dbgLog("Curious: keycount==" + iCount.ToString());
                            if (iCount > 0)
                            {
                                string[] thekeys = Singleton<SimulationManager>.instance.m_serializableDataStorage.Keys.ToArray();
                                if (thekeys != null)
                                {
                                    for (int i = 0; i < thekeys.Length; i++)
                                    {
                                        Logger.dbgLog(string.Format("entry: key{0}  named: {1}", i.ToString(), thekeys[i].ToString()));
                                    }
                                }
                            }
                        }
                        oMasterContainer.SaveFlags = 0;
                        oMasterContainer.SaveType = 1; //flag empty.
                        oMasterContainer.m_BurningTreeData = null;
                        errFlag = true;
                    }
                    else
                    {
                        if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("We obtained our named data array of length:" + ourBytes.Length.ToString()); }
                        errFlag = false;
                    }

                }
                catch (Exception ldEx)
                {
                    Logger.dbgLog("Error loading save container byte data:", ldEx);
                }

                if (errFlag)
                { return oMasterContainer; } //we have no data

                try
                {
                    if (ourBytes != null && ourBytes.Length != 0)  //check again.
                    {
                        MemoryStream memoryStream = new MemoryStream();
                        try
                        {
                            Logger.dbgLog("Loading our UTSaveDataContainer from bytes to objects!");
                            //memoryStream = new MemoryStream();
                            memoryStream.Write(ourBytes, 0, ourBytes.Length);
                            memoryStream.Position = 0L;
                            oMasterContainer = (UTSaveDataContainer)new BinaryFormatter().Deserialize(memoryStream);
                            oMasterContainer.SaveType = 2;
                            DataExtension.m_UTSaveDataContainer = oMasterContainer;
                        }
                        finally 
                        {
                            memoryStream.Close();
                        }
                    }
                    else
                    {
                        Logger.dbgLog("No data to deserialize!");
                    }
                }
                catch (Exception ex)
                {
                    Logger.dbgLog (string.Format("Error deserializing SaveContainer data: ", ex));
                    //flag = true;
                }

                return oMasterContainer; 
 
            }

            /// <summary>
            /// This actually runs OnSave() // which gets fired off before Data.Deserialze gets called.
            /// It handles all our tree data > 262144 if needed; Also packs and kicks off the packersavelist filling.
            /// </summary>
            internal static void Serialize()
            {
                if (!LimitTreeManager.Helper.UseModifiedTreeCap)
                {
                    return;
                }
                /* Insert possible enhancement here. Should we not check here first if the m_trees.m_buffer.length 
                 * is as large as we're about to assume it is?  I mean 99% of the time it should be.
                 * but if something before us blows up and Loader.OnCreated() never runs, and somehow redirects
                 * are still active... this will bomb out with an exception...it will not do damage persay but
                 * why not just prevent it and let the game save the first 262144.
                 */
                
                if (Mod.DEBUG_LOG_ON) { Logger.dbgLog(string.Concat(" treelimit = ", LimitTreeManager.Helper.TreeLimit.ToString() , " buffersize=" , Singleton<TreeManager>.instance.m_trees.m_size.ToString())); }

                TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                if (Loader.LastSaveList == null)
                {
                    if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("LastSaveList==null Obtaining fresh PackedList"); }
                    Loader.LastSaveList = Packer.GetPackedList();
                    if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("fresh PackedList assigned to Loader.LastSaveList "); }
                }
                else
                {
                    if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("Using Stale?? PackedList"); }
                }
                if (mBuffer.Length <= Mod.DEFAULT_TREE_COUNT || Loader.LastSaveList.Count <= Mod.DEFAULT_TREE_COUNT)
                { 
                    Logger.dbgLog("No extra tree data to save.");
                    if (Mod.DEBUG_LOG_ON)
                    {
                        Logger.dbgLog("Setting LastSaveUsedPacking = False. and returning and removing old data!");
                    }
                    Loader.LastSaveUsedPacking = false;
                    //1.6.0_build04 Bugfix for old data staying around 
                    //case is if we had >262k saved and now we don't we can't bail without wipinging here.
                    //or else we end up with "I deleted trees but they are back after save+reload!" problems.
                    if (SaveDataUtils.EraseBytesFromNamedKey(Mod.MOD_OrgDataKEYNAME))
                    { 
                        Logger.dbgLog("We found and removed old data."); 
                    }
                    Loader.LastFileClearedFlag = true;
                    return; 
                }
                if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("Setting LastSaveUsedPacking = True."); }
                Loader.LastSaveUsedPacking = true;

                List<ushort> nums = new List<ushort>();
                nums.Add(Mod.CurrentFormatVersion); //this is our internal save format version #
                
                //now add our present 32bit array size as 2 ushorts.
                uint orgint = (uint)LimitTreeManager.Helper.TreeLimit;
                ushort firsthalf = (ushort) (orgint >> 16); //right shift 16
                ushort secondhalf = (ushort)(orgint & 0xffff); //and 16bits  
                // now store them in entries 1 and 2.
                nums.Add(firsthalf);    //int32-1 arraysize at time of save.
                nums.Add(secondhalf);   //get these back later by (first <<16) | (second & 0xffff) 
                nums.Add(0);   //int32-2 actual treecount 
                nums.Add(0);   //int32-2 actual treecount
                nums.Add(0);    //int32-3 reserved for future use.
                nums.Add(0);   //int32-3 reserved for future use.
                nums.Add(0);    //int32-4 reserved for future use.
                nums.Add(0);   //int32-4 reserved for future use.
                nums.Add(0);   //ushort Flags reserved for furture use. 

                int num = 0;

                //orignal
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Logger.dbgLog("LastSaveUsedPacking: " + Loader.LastSaveUsedPacking.ToString() + " LastSaveList: " + (Loader.LastSaveList == null ? "null":"not null")); }
                if (Loader.LastSaveUsedPacking == false | Loader.LastSaveList == null)
                {
                    if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Logger.dbgLog("Start using custom seralizer.(no packing)"); }

                    for (int i = Mod.DEFAULT_TREE_COUNT; i < LimitTreeManager.Helper.TreeLimit; i++) //from top of their range to ours.
                    {
                        TreeInstance treeInstance = mBuffer[i];
                        nums.Add(treeInstance.m_flags);
                        if (treeInstance.m_flags != 0)
                        {
                            nums.Add(treeInstance.m_infoIndex);
                            nums.Add((ushort)treeInstance.m_posX);
                            nums.Add((ushort)treeInstance.m_posZ);
                            num++;

                        }
                    }
                }
                else //use packing
                {
                    if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Logger.dbgLog("Start using custom seralizer. (packing) " + Loader.LastSaveList.Count.ToString()); }

                    for (int i = Mod.DEFAULT_TREE_COUNT; i < Loader.LastSaveList.Count; i++) //from top of thier range to ours.
                    {
                        TreeInstance treeInstance = mBuffer[Loader.LastSaveList[i]];
                        nums.Add(treeInstance.m_flags);
                        if (treeInstance.m_flags != 0)
                        {
                            nums.Add(treeInstance.m_infoIndex);
                            nums.Add((ushort)treeInstance.m_posX);
                            nums.Add((ushort)treeInstance.m_posZ);
                            num++;
                        }
                    }
                }
                //save actual tree count to header.
                nums[3] = (ushort)(num >> 16);
                nums[4] = (ushort)(num & 0xffff);
                nums[9] = 0; //options
                if (Loader.LastSaveUsedPacking) //add packed flag.
                { nums[9] = (ushort)(nums[9] | (ushort)Helper.SaveFlags.packed); }

                //TODO:Add some try catches around at least this call if not the above.
                object[] treeLimit = new object[] { num, LimitTreeManager.Helper.TreeLimit - Mod.DEFAULT_TREE_COUNT, nums.Count * 2, Mod.CurrentFormatVersion.ToString() };
                Logger.dbgLog(string.Format("Saving {0} of {1} in extra trees range, size in savegame approx: {2} bytes, saveformatverion:{3}", treeLimit));
                Singleton<SimulationManager>.instance.m_serializableDataStorage["mabako/unlimiter"] = nums.SelectMany<ushort, byte>((ushort v) => BitConverter.GetBytes(v)).ToArray<byte>();

            }


            //Runs Later then the Tress guy
            //gets called during Data.Serialize()
            /// <summary>
            /// Our main wrapper function to handle the burning trees that are > 262k.
            /// We have to see if packing was used, if so we must reorder burningtree_indexes based on their 
            /// new data.  Then save all those > 262k up to Current Limit.
            /// </summary>
            internal static void SerializeBurningTreeWrapper()
            {
                //let's make sure we're enabled.
                if (!LimitTreeManager.Helper.UseModifiedTreeCap)
                {
                    return;
                }

                FastList<TreeManager.BurningTree> tmbt;
                FastList<TreeManager.BurningTree> tmbt2;
                try
                {
                    Logger.dbgLog("fired; Loader.LastSaveUsedPacking = " + Loader.LastSaveUsedPacking.ToString());

                    //triggering on lastfileclearFlag forces reordering and then should result in 0 trees and removal.
                    //we have to avoid hitting the non-packer in that case because the direct copy from TM.
                    //will copy from higher indexes.. vs copy from reordered packed list.
                    //KH Build06 12/7- You know I don't even think we need the 'else' or my brain may just be fried atm. 
                    if (Loader.LastSaveUsedPacking || Loader.LastFileClearedFlag )
                    {
                        //reorder based on LastSaveList of indexes. 
                        //we technically have already done this before so we're duplicating
                        //work unless you want to save those results in Loader.Something?? like LastSavedList? 
                        Logger.dbgLog("LastSavedPacking True - Getting a reOrderedList using existing objLastSaveList");
                        Packer.ReOrderBurningTrees(ref Loader.LastSaveList, out tmbt);

                        //Now get list from that list copy that only includes 262k+
                        Logger.dbgLog("LastSavedPacking True - Copying 262k to limit - from our re-ordered copy");
                        tmbt2 = Packer.CopyBurningTreesList(ref tmbt, 2);
                        SerializeExtraBurningTrees(true, tmbt2); //farm out the save details.

                    }
                    else //
                    {
                        //No need to reorder because we're not packed.
                        //This basically shouldn't ever happen anymore unless something goes wildly wrong.
                        //Now get list from **ORG TM copy** that only includes 262k+
                        //In theory there shouldn't be any most cases I can think of.
                        Logger.dbgLog("LastSavedPacking false and LastFileCleard not set - Copying 262k to limit -from original tree manager");
                        tmbt2 = Packer.CopyBurningTreesList(ref Singleton<TreeManager>.instance.m_burningTrees, 2);
                        SerializeExtraBurningTrees(false, tmbt2);  //farm out the save details.
                    }
                }

                catch (Exception ex)
                { Logger.dbgLog("Error during save of extra burning trees.",ex);}
 
            }


            /// <summary>
            /// Handles Actual setup of object containers, and then the syncronized writes of the objects to the byte array.
            /// </summary>
            /// <param name="isPacked">IfPackingWasUsed on tree save process</param>
            /// <param name="lstBurningTrees">The FastList of burning trees to actually try to save</param>
            private static void SerializeExtraBurningTrees(bool isPacked,FastList<TreeManager.BurningTree> lstBurningTrees) 
            {
                if(Mod.DEBUG_LOG_ON)
                {Logger.dbgLog("Serializing extra burning trees  " + (isPacked==true ? "(packed)":"(notpacked)"));}
                try
                {
                    if (lstBurningTrees == null )
                    {
                        Logger.dbgLog("listBurningTrees is null, it at least should be empty!");
                        Logger.dbgLog("We will abort the saving of burning data. and leave existing data in place.");
                        //1.6.0 we should probably.. put this back when done testing 1.6.0.-f4_build05
                        //SaveDataUtils.EraseBytesFromNamedKey(UTSaveDataContainer.DefaultContainername);
                        return;
                    }
                    else if(lstBurningTrees.m_size < 1 || lstBurningTrees.m_buffer.Length == 0)
                    {
                        Logger.dbgLog("No extra burning data " + lstBurningTrees.m_size + " burning trees to save.");
                        //Got to remove the old stuff if it exists.
                        if (Mod.DEBUG_LOG_ON)
                        {
                            Logger.dbgLog(string.Format("will attempt to remove existing {0} container if exists",UTSaveDataContainer.DefaultContainername));
                        }
                        
                        SaveDataUtils.EraseBytesFromNamedKey(UTSaveDataContainer.DefaultContainername); 
                        Loader.LastFileClearedFlag = true; //sort of redundent since it was likely set above us.

                        return;
                    }
                    
                    if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
                    {
                        Logger.dbgLog("about to serialize extra " + lstBurningTrees.m_size + " burning trees");
                    }

                    //setup the container.
                    UTSaveDataContainer toplevelContainer = new UTSaveDataContainer();
                    toplevelContainer.ContainerName = UTSaveDataContainer.DefaultContainername.ToString(); 
                    toplevelContainer.SaveFormatVersion = UTSaveDataContainer.CurrentSaveContainerFormatVersion;
                    toplevelContainer.CreatedDate = DateTime.UtcNow;
                    //setup the burningcontainer.
                    toplevelContainer.m_BurningTreeData = new UTSaveDataContainer.BurningTreeData();
                    if (isPacked)
                    { 
                        toplevelContainer.SaveFlags = 1; //packing used.
                        toplevelContainer.m_BurningTreeData.isPacked = true;
                    }

                    UTSaveDataContainer.BurningTreeData BurningContainer = toplevelContainer.m_BurningTreeData;
                    BurningContainer.SaveFormatVersion = UTSaveDataContainer.BurningTreeData.CurrentSaveBurningFormatVersion;
                    BurningContainer.BurningCount = 0;

                    List<UTSaveDataContainer.UTBurningTreeInstance> myBurningTreeList = new List<UTSaveDataContainer.UTBurningTreeInstance>(lstBurningTrees.m_size);

                    if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
                    { Logger.dbgLog("myburning list capacity is currently: " + myBurningTreeList.Capacity.ToString()); }

                    UTSaveDataContainer.UTBurningTreeInstance oTree = new UTSaveDataContainer.UTBurningTreeInstance();
                    //copy source data to our container.
                    for (uint i = 0; i < lstBurningTrees.m_buffer.Length; i++)
                    {
                        if (lstBurningTrees.m_buffer[i].m_treeIndex > 0)
                        {
                            oTree.m_treeIndex = lstBurningTrees.m_buffer[i].m_treeIndex;
                            oTree.m_fireIntensity = lstBurningTrees.m_buffer[i].m_fireIntensity;
                            oTree.m_fireDamage = lstBurningTrees.m_buffer[i].m_fireDamage;
                            oTree.idxWhenSaved = i;
                            oTree.version = 1;
                            myBurningTreeList.Add(oTree);
                        }
                    }
                    BurningContainer.BurningTreeList = myBurningTreeList;
                    BurningContainer.BurningCount = myBurningTreeList.Count;
                    if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1){ Logger.dbgLog("about to save container with : " + myBurningTreeList.Count.ToString() + " burning trees."); }
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    try
                    {
                        MemoryStream memoryStream = new MemoryStream();
                        try
                        {
                            binaryFormatter.Serialize(memoryStream, toplevelContainer);
                            memoryStream.Position = 0L;
                            Logger.dbgLog(string.Format("Saving data, byte length: {0}  ContainerName: {1}", memoryStream.Length.ToString(), toplevelContainer.ContainerName));
                            if (SaveDataUtils.WriteBytesToNamedKey(toplevelContainer.ContainerName, memoryStream.ToArray()) == false)
                            { Logger.dbgLog("saving failed."); }
                            //Singleton<SimulationManager>.instance.m_serializableDataStorage[toplevelContainer.ContainerName] = memoryStream.ToArray();
                        }
                        finally
                        {
                            memoryStream.Close();
                        }
                    }
                    catch (Exception ex10)
                    {
                        Logger.dbgLog("Unexpected error while saving data: ", ex10);
                    }

                    //store this incase we need it.actually we will eventually just not in this version... yet 
                    DataExtension.m_UTSaveDataContainer = toplevelContainer;

                }
                catch (Exception ex) { Logger.dbgLog("err: ",ex); } 

            } 

        }



        internal class Data
        {
            public Data()
            {
            }

            private static void Deserialize(TreeManager.Data data, DataSerializer s)
            {
                short num;
                short num1;
                if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("Starting detoured deseralizer. Making sure we're initialized."); }
                LimitTreeManager.Helper.EnsureInit(1);
                Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.BeginDeserialize(s, "TreeManager");
                TreeManager treeManager = Singleton<TreeManager>.instance;
                TreeInstance[] mBuffer = treeManager.m_trees.m_buffer;
                if (Mod.DEBUG_LOG_ON) { Logger.dbgLog(" mbuffersize=" + mBuffer.Length.ToString()); }
                uint[] mTreeGrid = treeManager.m_treeGrid;
                int num2 = Mod.DEFAULT_TREE_COUNT ;  //262144
                int length = (int)mTreeGrid.Length;
                treeManager.m_trees.ClearUnused();
                treeManager.m_burningTrees.Clear();  //v1.6.0 c/o
                //my personal addition because no sense in constant growth.
                if ((treeManager.m_burningTrees.m_buffer == null) == false && treeManager.m_burningTrees.m_buffer.Length > 64) 
                { treeManager.m_burningTrees.Trim(); }
                if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("m_burningTrees.Clear()'d and m_burningTrees.Trim()'d"); }

                SimulationManager.UpdateMode mUpdateMode = Singleton<SimulationManager>.instance.m_metaData.m_updateMode;
                if (Mod.DEBUG_LOG_ON) { Logger.dbgLog(string.Concat(" mUpdatemode =", mUpdateMode.ToString())); }
                bool flag = (mUpdateMode == SimulationManager.UpdateMode.NewAsset ? true : mUpdateMode == SimulationManager.UpdateMode.LoadAsset);
                for (int i = 0; i < length; i++)
                {
                    mTreeGrid[i] = 0;
                }
                EncodedArray.UShort num3 = EncodedArray.UShort.BeginRead(s);
                for (int j = 1; j < num2; j++)
                {
                    TreeInstance.Flags flags = (TreeInstance.Flags)num3.Read();
                   // reverse any burning or damaged flags : CO wisely added in 1.6.0
                    flags &= ~(TreeInstance.Flags.FireDamage | TreeInstance.Flags.Burning);
                    mBuffer[j].m_flags = (ushort)flags;
                }
                num3.EndRead();
                PrefabCollection<TreeInfo>.BeginDeserialize(s);
                for (int k = 1; k < num2; k++)
                {
                    if (mBuffer[k].m_flags != 0)
                    {
                        mBuffer[k].m_infoIndex = (ushort)PrefabCollection<TreeInfo>.Deserialize(true);
                    }
                }
                PrefabCollection<TreeInfo>.EndDeserialize(s);
                EncodedArray.Short num4 = EncodedArray.Short.BeginRead(s);
                for (int l = 1; l < num2; l++)
                {
                    if (mBuffer[l].m_flags != 0)
                    {
                        num = num4.Read();
                    }
                    else
                    {
                        num = 0;
                    }
                    mBuffer[l].m_posX = num;
                }
                num4.EndRead();
                EncodedArray.Short num5 = EncodedArray.Short.BeginRead(s);
                for (int m = 1; m < num2; m++)
                {
                    if (mBuffer[m].m_flags != 0)
                    {
                        num1 = num5.Read();
                    }
                    else
                    {
                        num1 = 0;
                    }
                    mBuffer[m].m_posZ = num1;
                }
                num5.EndRead();


                if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("main deserialize: Processed the core 262144k tree buffer, moving on to read in core burning trees"); }
                //added for 1.6.0 (burning trees)
                if (s.version >= 266)
                {
                    int numBurningTrees = (int)s.ReadUInt24();
                    if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("main deserialize: There are " + numBurningTrees.ToString() + " burning trees stored in the file."); }
                    treeManager.m_burningTrees.EnsureCapacity(numBurningTrees);
                    TreeManager.BurningTree burningTree = new TreeManager.BurningTree();
                    for (int n = 0; n < numBurningTrees; n++)
                    {
                        burningTree.m_treeIndex = s.ReadUInt24();
                        burningTree.m_fireIntensity = (byte)s.ReadUInt8();
                        burningTree.m_fireDamage = (byte)s.ReadUInt8();
                        if ((burningTree.m_treeIndex != 0) && (burningTree.m_treeIndex < mBuffer.Length))
                        {
                            treeManager.m_burningTrees.Add(burningTree);
                            mBuffer[burningTree.m_treeIndex].m_flags = (ushort)(mBuffer[burningTree.m_treeIndex].m_flags | 64);
                            if (burningTree.m_fireIntensity != 0)
                            {
                                mBuffer[burningTree.m_treeIndex].m_flags = (ushort)(mBuffer[burningTree.m_treeIndex].m_flags | 128);
                            }
                        }
                        else
                        {
                            if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("main deserialize: skipping burning tree with index " + burningTree.m_treeIndex.ToString()); }
                        }

                    }
                }
                //end 1.6.0 additions


                //go load our data if enabled.  //we load our burningtree in here too.
                if (LimitTreeManager.Helper.UseModifiedTreeCap)
                {
                    if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("Using ModifiedTreeCap - Calling Custom Tree Deserializer."); }
                    LimitTreeManager.CustomSerializer.Deserialize();
                    if (s.version >= 266)
                    {
                        if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("Using ModifiedTreeCap - Calling Custom BurningTree Deserializer."); }
                        LimitTreeManager.CustomSerializer.DeserializeBurningTrees();
                    }
                    else
                    {
                        if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("save version < 266 no need to load custom tree data. ver:" + s.version.ToString()); }
                    }
                }



                //shared


                for (int o = 1; o < LimitTreeManager.Helper.TreeLimit; o++)
                {
                    mBuffer[o].m_nextGridTree = 0;
                    mBuffer[o].m_posY = 0;
                    if (mBuffer[o].m_flags != 0)
                    {
                        LimitTreeManager.InitializeTree(treeManager, (uint)o, ref mBuffer[o], flag);
                    }
                    else
                    {
                        treeManager.m_trees.ReleaseItem((uint)o);
                    }
                }


                Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.EndDeserialize(s, "TreeManager");
            }


            //KH 5-12-2016 we had to add this guy to be able to add checking for custom tree issues
            //As earlier in the process we don't have access to 'scene_loaded data'
            private static void AfterDeserialize(TreeManager.Data data ,DataSerializer s)
            {
                Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.BeginAfterDeserialize(s, "TreeManager");
                //TreePrefabsDebug.DumpLoadedPrefabInfos(3);
                //Logger.dbgLog("before waitforload...");
                Singleton<LoadingManager>.instance.WaitUntilEssentialScenesLoaded();
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1)
                { 
                    Logger.dbgLog("EssentialScenesLoaded...");
                    TreePrefabsDebug.DumpLoadedPrefabInfos(4);
                    Logger.dbgLog("before TreeInfo BindPrefabs...");
                }

                PrefabCollection<TreeInfo>.BindPrefabs();
                if (Mod.DEBUG_LOG_ON)
                { 
                    Logger.dbgLog("TreeInfo prefabs bound...");
                    TreePrefabsDebug.DumpLoadedPrefabInfos(5);
                }



                TreeManager instance = Singleton<TreeManager>.instance;
                TreeInstance[] buffer = instance.m_trees.m_buffer;

                //Our additions, if enabled do our version if not just C\O's.
                if (Mod.config.NullTreeOptionsValue != TreePrefabsDebug.NullTreeOptions.DoNothing)
                {
                    if (Mod.DEBUG_LOG_ON)
                    { Logger.dbgLog("Starting custom Tree validation process..."); }
                    TreeManager treeManager = Singleton<TreeManager>.instance;
                    treeManager.m_treeCount = (int)(treeManager.m_trees.ItemCount() - 1u); //needed before getpackedlistcall

                    if (TreePrefabsDebug.ValidateAllTreeInfos())
                    {
                        if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("Tree validation process completed..."); }
                    }
                    else
                    { TreePrefabsDebug.DoOriginal(); }
                }
                else
                {
                    if (Mod.DEBUG_LOG_ON) { Logger.dbgLog("Tree validation process disabled..."); }
                    TreePrefabsDebug.DoOriginal(); //same as below
/*                    //Original untouched.
                    int num = buffer.Length;
                    for (int i = 1; i < num; i++)
                    {
                        if (buffer[i].m_flags != 0)
                        {

                            TreeInfo info = buffer[i].Info;
                            if (info != null)
                            {
                                buffer[i].m_infoIndex = (ushort)info.m_prefabDataIndex;
                            }
                        }
                    }
 */
                }
                instance.m_treeCount = (int)(instance.m_trees.ItemCount() - 1u);
                Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.EndAfterDeserialize(s, "TreeManager");
            }
            

            private static void Serialize(TreeManager.Data data, DataSerializer s)
            {
                Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.BeginSerialize(s, "TreeManager");
                //orig TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                
                //orig int num = Mod.DEFAULT_TREE_COUNT ; //262144
                try
                {
                    if (Mod.DEBUG_LOG_ON) 
                    {
                        if (Loader.LastSaveList == null)
                        {
                            Logger.dbgLog("debug: Loader.LastSaveList is == NULL before call to packer.serialize.");
                        }
                        else 
                        {
                            Logger.dbgLog("debug: Loader.LastSaveList is NOT NULL before call to packer.serialize.");
                        }
                    }
                    Logger.dbgLog("threadname: " + Thread.CurrentThread.Name + "  calling packer " + DateTime.Now.ToString(Mod.DTMilli));
                    Packer.Serialize(ref Loader.LastSaveList, ref s);
                }
                catch (Exception ex)
                { 
                    Logger.dbgLog("", ex, true);
                    Logger.dbgLog("** May have failed to save trees due to last exception inside packer.seralize.\n Please enable verbose logging option and make contact with author via Steam to help debug the problem.", ex, true);
                }

                // I really don't know why but we had to move this code to DataExtentions
                // I'm 90% sure because m_serializableStorge is already seralized by the time it gets
                // to this code, which frankly makes no sense looking at CO's source
                // since it shows it being saved last...after all data.serialize calls run 
                // However maybe there is something I'm missing in about the co-routine.
                //handle extra burning trees if any.

                /*
                try
                {
                    Logger.dbgLog("threadname: " + Thread.CurrentThread.Name + "  calling serializeburning " + DateTime.Now.ToString(Mod.DTMilli));
                    CustomSerializer.SerializeBurningTreeWrapper();

                }
                catch (Exception ex)
                {
                    Logger.dbgLog("", ex, true);
                }
                */

                //original code
                /*
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
                */ 
                Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.EndSerialize(s, "TreeManager");
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Logger.dbgLog("replaced seralizer completed."); }
                Loader.LastFileClearedFlag = false;
                if (Loader.LastSaveList != null)
                {
                    if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Logger.dbgLog("Cleaning up last save flags and list object."); }
                    Loader.LastSaveList.Clear();
                    Loader.LastSaveUsedPacking = false;
                    Loader.LastSaveList = null;
                }

            }
        }


        internal static class Helper
        {
            [Flags]
            internal enum SaveFlags :ushort
            {
                none = 0,
                packed = 1,
            }
            internal static int TreeLimit
            {
                get
                {
                    if (!LimitTreeManager.Helper.UseModifiedTreeCap)
                    {
                        return Mod.DEFAULT_TREE_COUNT ; // 262144
                    }
                    return Mod.SCALED_TREE_COUNT;
                    //return 1048576;  //1048576
                }
            }


            internal static bool UseModifiedTreeCap
            {
                get
                {
                    if (!Mod.IsEnabled)
                    {
                        return false;
                    }
                    SimulationManager.UpdateMode mUpdateMode = Singleton<SimulationManager>.instance.m_metaData.m_updateMode;
//9-25-2015         Mod.LastMode = mUpdateMode; //probably can ditch this now was used for debugging.
                    
                    if (mUpdateMode == SimulationManager.UpdateMode.LoadGame || mUpdateMode == SimulationManager.UpdateMode.LoadMap
                        || mUpdateMode == SimulationManager.UpdateMode.NewGameFromMap || mUpdateMode == SimulationManager.UpdateMode.NewGameFromScenario
                        || mUpdateMode == SimulationManager.UpdateMode.NewMap || mUpdateMode == SimulationManager.UpdateMode.LoadScenario
                        || mUpdateMode == SimulationManager.UpdateMode.NewScenarioFromGame || mUpdateMode == SimulationManager.UpdateMode.NewScenarioFromGame)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            /// <summary>
            /// Called to check or initialize if needed the increasing of the Tree buffer array size.
            /// </summary>
            /// <param name="caller">The fuction that is calling this guy, 1 = deserialize(),2=custom deserialize() 3=UpdateData()</param>
            internal static void EnsureInit(byte caller)
            {
                uint num;
                object[] objArray = new object[] { (Mod.IsEnabled ? "enabled" : "disabled"), (LimitTreeManager.Helper.UseModifiedTreeCap ? "actived" : "not-actived"), caller.ToString() };
                if (Mod.DEBUG_LOG_ON) { Logger.dbgLog(string.Format("EnsureInit({2}) This mod is {0}. Tree unlimiter mode is {1}.", objArray));}

                if (!LimitTreeManager.Helper.UseModifiedTreeCap)
                {
                    if (Mod.DEBUG_LOG_ON)
                    {
                        Logger.dbgLog(string.Format(string.Concat("EnsureInit({2}) UseModifiedTreeCap = False  TreeLimit = ", LimitTreeManager.Helper.TreeLimit), objArray));
                    }
                    /* 9-25-2015    if (Mod.DEBUG_LOG_ON)
                    {
                        Debug.LogFormat(string.Concat("[TreeUnlimiter::EnsureInit({2})] LastLoadmode = ", Mod.LastMode.ToString()), objArray);
                    }
                    */
                    return;
                }

                if ((int)Singleton<TreeManager>.instance.m_trees.m_buffer.Length != LimitTreeManager.Helper.TreeLimit)
                {
                    int length = (int)Singleton<TreeManager>.instance.m_trees.m_buffer.Length;
                    string str1 = length.ToString();
                    int treeLimit = LimitTreeManager.Helper.TreeLimit;
                    Logger.dbgLog(string.Format(string.Concat("EnsureInit({2}) Updating TreeManager's ArraySize from ",
                        str1, " to ", treeLimit.ToString()), objArray));
               
        //9-25-2015 if (Mod.DEBUG_LOG_ON) {Debug.LogFormat(string.Concat("[TreeUnlimiter::EnsureInit({2})] LastLoadmode=", Mod.LastMode.ToString()), objArray); }
                    Singleton<TreeManager>.instance.m_trees = new Array32<TreeInstance>((uint)LimitTreeManager.Helper.TreeLimit); 
                    Singleton<TreeManager>.instance.m_updatedTrees = new ulong[Mod.SCALED_TREEUPDATE_COUNT]; //16384
                    Singleton<TreeManager>.instance.m_trees.CreateItem(out num);
                }
            }
        }
    }
}