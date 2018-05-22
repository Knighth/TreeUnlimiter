using ColossalFramework;
using ColossalFramework.Math;
using System;
using System.Reflection;
using UnityEngine;

namespace TreeUnlimiter
{
    internal static class LimitTreeTool
    {
        private static void ApplyBrush(TreeTool tt)
        {
            //uint useless1 = 0;
            unsafe
            {
                float single;
                float single1;
                uint num;
                int num1;
                Vector3 vector3 = new Vector3();
                Randomizer value = (Randomizer)tt.GetType().GetField("m_randomizer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tt);
                ToolController toolController = (ToolController)tt.GetType().GetField("m_toolController", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tt);
                Vector3 value1 = (Vector3)tt.GetType().GetField("m_mousePosition", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tt);
                bool flag = (bool)tt.GetType().GetField("m_mouseLeftDown", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tt);
                bool flag1 = (bool)tt.GetType().GetField("m_mouseRightDown", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tt);
                TreeInfo treeInfo = (TreeInfo)tt.GetType().GetField("m_treeInfo", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tt);
                float[] brushData = toolController.BrushData;
                float mBrushSize = tt.m_brushSize * 0.5f;
                float single2 = 32f;
                int num2 = 540;
                TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                uint[] mTreeGrid = Singleton<TreeManager>.instance.m_treeGrid;
                float mStrength = tt.m_strength;
                int num3 = Mathf.Max((int)((value1.x - mBrushSize) / single2 + (float)num2 * 0.5f), 0);
                int num4 = Mathf.Max((int)((value1.z - mBrushSize) / single2 + (float)num2 * 0.5f), 0);
                int num5 = Mathf.Min((int)((value1.x + mBrushSize) / single2 + (float)num2 * 0.5f), num2 - 1);
                int num6 = Mathf.Min((int)((value1.z + mBrushSize) / single2 + (float)num2 * 0.5f), num2 - 1);
                for (int i = num4; i <= num6; i++)
                {
                    float mBrushSize1 = (float)(((float)i - (float)num2 * 0.5f + 0.5f) * single2 - value1.z + mBrushSize) / tt.m_brushSize * 64f - 0.5f;
                    int num7 = Mathf.Clamp(Mathf.FloorToInt(mBrushSize1), 0, 63);
                    int num8 = Mathf.Clamp(Mathf.CeilToInt(mBrushSize1), 0, 63);
                    for (int j = num3; j <= num5; j++)
                    {
                        float mBrushSize2 = (float)(((float)j - (float)num2 * 0.5f + 0.5f) * single2 - value1.x + mBrushSize) / tt.m_brushSize * 64f - 0.5f;
                        int num9 = Mathf.Clamp(Mathf.FloorToInt(mBrushSize2), 0, 63);
                        int num10 = Mathf.Clamp(Mathf.CeilToInt(mBrushSize2), 0, 63);
                        float single3 = brushData[num7 * 64 + num9];
                        float single4 = brushData[num7 * 64 + num10];
                        float single5 = brushData[num8 * 64 + num9];
                        float single6 = brushData[num8 * 64 + num10];
                        float single7 = single3 + (float)((single4 - single3) * (mBrushSize2 - (float)num9));
                        float single8 = single5 + (float)((single6 - single5) * (mBrushSize2 - (float)num9));
                        float single9 = single7 + (float)((single8 - single7) * (mBrushSize1 - (float)num7));
                        int num11 = (int)(mStrength * (single9 * 1.2f - 0.2f) * 10000f);
                        if (flag && tt.m_prefab != null)
                        {
                            if (value.Int32(10000) < num11)
                            {
                                TreeInfo treeInfo1 = ((Singleton<ToolManager>.instance.m_properties.m_mode & ItemClass.Availability.AssetEditor) == ItemClass.Availability.None ? tt.m_prefab.GetVariation(ref value) : tt.m_prefab);
                                vector3.x = ((float)j - (float)num2 * 0.5f) * single2;
                                vector3.z = ((float)i - (float)num2 * 0.5f) * single2;
                                vector3.x = vector3.x + (float)(((float)value.Int32(10000) + 0.5f) * (single2 / 10000f));
                                vector3.z = vector3.z + (float)(((float)value.Int32(10000) + 0.5f) * (single2 / 10000f));
                                vector3.y = 0f;
                                vector3.y = Singleton<TerrainManager>.instance.SampleDetailHeight(vector3, out single, out single1);
                                if (Mathf.Max(Mathf.Abs(single), Mathf.Abs(single1)) < (float)value.Int32(10000) * 5E-05f)
                                {
                                    float mSize = treeInfo.m_generatedInfo.m_size.y;
                                    float mMinScale = treeInfo.m_minScale;
                                    Randomizer randomizer = new Randomizer(Singleton<TreeManager>.instance.m_trees.NextFreeItem(ref value));
                                    mMinScale = mMinScale + (float)randomizer.Int32(10000) * ((treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f);
                                    mSize = mSize * mMinScale;
                                    float single11 = 4.5f;
                                    Vector2 vector2 = VectorUtils.XZ(vector3);
                                    Quad2 quad22 = new Quad2()
                                    {
                                        a = vector2 + new Vector2(-single11, -single11),
                                        b = vector2 + new Vector2(-single11, single11),
                                        c = vector2 + new Vector2(single11, single11),
                                        d = vector2 + new Vector2(single11, -single11)
                                    };
                                    Quad2 quad2 = quad22;
                                    Quad2 quad23 = new Quad2()
                                    {
                                        a = vector2 + new Vector2(-8f, -8f),
                                        b = vector2 + new Vector2(-8f, 8f),
                                        c = vector2 + new Vector2(8f, 8f),
                                        d = vector2 + new Vector2(8f, -8f)
                                    };
                                    Quad2 quad21 = quad23;
                                    float single12 = value1.y - 1000f;
                                    float single13 = value1.y + mSize;
                                    ItemClass.CollisionType collisionType = ItemClass.CollisionType.Terrain;
                                    if (!Singleton<PropManager>.instance.OverlapQuad(quad2, single12, single13, collisionType, 0, 0) && !Singleton<TreeManager>.instance.OverlapQuad(quad21, single12, single13, collisionType, 0, 0) && !Singleton<NetManager>.instance.OverlapQuad(quad2, single12, single13, collisionType, treeInfo1.m_class.m_layer, 0, 0, 0) && !Singleton<BuildingManager>.instance.OverlapQuad(quad2, single12, single13, collisionType, treeInfo1.m_class.m_layer, 0, 0, 0) && !Singleton<TerrainManager>.instance.HasWater(vector2) && !Singleton<GameAreaManager>.instance.QuadOutOfArea(quad2) && !Singleton<TreeManager>.instance.CreateTree(out num, ref value, treeInfo1, vector3, false))
                                    {
                                    }
                                }
                            }
                        }
                        else if (flag1 || tt.m_prefab == null)
                        {
                            uint num12 = mTreeGrid[i * num2 + j];
                            int num13 = 0;
                            do
                            {
                                if (num12 == 0)
                                {
                                    goto Label10;
                                }
                                uint mNextGridTree = mBuffer[num12].m_nextGridTree;
                                if (value.Int32(10000) < num11)
                                {
                                    Singleton<TreeManager>.instance.ReleaseTree(num12);
                                }
                                num12 = mNextGridTree;
                                num1 = num13 + 1;
                                num13 = num1;
                            }
                            while (num1 < LimitTreeManager.Helper.TreeLimit);
                            CODebugBase<LogChannel>.Error(LogChannel.Core, string.Concat("Invalid list detected!\n", Environment.StackTrace));
                        }

                    Label10:
                        ;//useless1++;
                    }
                }
            }
        }
    }
}
