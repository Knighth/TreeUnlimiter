using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using ColossalFramework.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
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
							if ((double)Mathf.Max(Mathf.Max(mMin - 8f - position.x, single - 8f - position.z), Mathf.Max((float)((double)position.x - (double)mMax - 8), (float)((double)position.z - (double)mMax1 - 8))) < 0)
							{
								tm.m_trees.m_buffer[mTreeGrid].AfterTerrainUpdated(mTreeGrid, mMin, single, mMax, mMax1);
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
				int num1 = Mathf.Max((int)(((double)minX - 8) / 32 + 270), 0);
				int num2 = Mathf.Max((int)(((double)minZ - 8) / 32 + 270), 0);
				int num3 = Mathf.Min((int)(((double)maxX + 8) / 32 + 270), 539);
				int num4 = Mathf.Min((int)(((double)maxZ + 8) / 32 + 270), 539);
				num = 0;
				min = 1024f;
				avg = 0f;
				max = 0f;
				for (int i = num2; i <= num4; i++)
				{
					for (int j = num1; j <= num3; j++)
					{
						uint mTreeGrid = tm.m_treeGrid[i * 540 + j];
						int num5 = 0;
						while (mTreeGrid != 0)
						{
							Vector3 position = tm.m_trees.m_buffer[mTreeGrid].Position;
							if ((double)Mathf.Max(Mathf.Max(minX - 8f - position.x, minZ - 8f - position.z), Mathf.Max((float)((double)position.x - (double)maxX - 8), (float)((double)position.z - (double)maxZ - 8))) < 0)
							{
								TreeInfo info = tm.m_trees.m_buffer[mTreeGrid].Info;
								if (info != null)
								{
									Randomizer randomizer = new Randomizer(mTreeGrid);
									float mMinScale = info.m_minScale + (float)((double)randomizer.Int32(10000) * ((double)info.m_maxScale - (double)info.m_minScale) * 9.99999974737875E-05);
									float mSize = position.y + (float)((double)info.m_generatedInfo.m_size.y * (double)mMinScale * 2);
									if ((double)mSize < (double)min)
									{
										min = mSize;
									}
									avg = avg + mSize;
									if ((double)mSize > (double)max)
									{
										max = mSize;
									}
									num = num + 1;
								}
							}
							mTreeGrid = tm.m_trees.m_buffer[mTreeGrid].m_nextGridTree;
							int num6 = num5 + 1;
							num5 = num6;
							if (num6 < LimitTreeManager.Helper.TreeLimit)
							{
								continue;
							}
							CODebugBase<LogChannel>.Error(LogChannel.Core, string.Concat("Invalid list detected!\n", Environment.StackTrace));
							break;
						}
					}
				}
				if ((double)avg == 0)
				{
					return;
				}
				avg = avg / (float)num;
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
				if (tm.m_treeCount >= 250000)
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
				if (Input.GetKeyDown(KeyCode.F5) && Event.current.control)
				{
					DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, string.Format("TreeLimit: TreeCount={0}, TreeLimit={1}, CanPlaceMoreTrees={2}", tm.m_treeCount, LimitTreeManager.Helper.TreeLimit, tm.CheckLimits()));
					Array32<TreeInstance> mTrees = tm.m_trees;
					DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, string.Format("TreeLimit: ArraySize={0}, ItemCount={1}, UnusedCount={2}", mTrees.m_size, mTrees.ItemCount(), mTrees.GetType().GetField("m_unusedCount", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(mTrees)));
				}
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
			}
		}

		private static void InitializeTree(TreeManager tm, uint tree, ref TreeInstance data, bool assetEditor)
		{
			unsafe
			{
				int num;
				int num1;
				if (!assetEditor)
				{
					num = Mathf.Clamp((data.m_posX + 32768) * 540 / 65536, 0, 539);
					num1 = Mathf.Clamp((data.m_posZ + 32768) * 540 / 65536, 0, 539);
				}
				else
				{
					num = Mathf.Clamp((data.m_posX / 16 + 32768) * 540 / 65536, 0, 539);
					num1 = Mathf.Clamp((data.m_posZ / 16 + 32768) * 540 / 65536, 0, 539);
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

		private static bool OverlapQuad(TreeManager tm, Quad2 quad, float minY, float maxY, int layer, uint ignoreTree)
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
							if ((double)Mathf.Max(Mathf.Max(vector2.x - 8f - position.x, vector2.y - 8f - position.z), Mathf.Max((float)((double)position.x - (double)vector21.x - 8), (float)((double)position.z - (double)vector21.y - 8))) < 0 && tm.m_trees.m_buffer[mTreeGrid].OverlapQuad(mTreeGrid, quad, minY, maxY))
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
					if ((double)single2 < (double)single3)
					{
						num = 0;
						num1 = ((double)vector3.z <= 0 ? -1 : 1);
						if ((double)single3 != 0)
						{
							vector3 = vector3 * (32f / single3);
						}
					}
					else
					{
						num = ((double)vector3.x <= 0 ? -1 : 1);
						num1 = 0;
						if ((double)single2 != 0)
						{
							vector3 = vector3 * (32f / single2);
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
						if (num == 0)
						{
							num4 = ((num11 != num7 || num1 <= 0) && (num11 != num9 || num1 >= 0) ? Mathf.Max(num11, 0) : Mathf.Max((int)(((double)vector33.z - 72) / 32 + 270), 0));
							num5 = ((num11 != num7 || num1 >= 0) && (num11 != num9 || num1 <= 0) ? Mathf.Min(num11, 539) : Mathf.Min((int)(((double)vector33.z + 72) / 32 + 270), 539));
							num2 = Mathf.Max((int)(((double)Mathf.Min(vector31.x, vector33.x) - 72) / 32 + 270), 0);
							num3 = Mathf.Min((int)(((double)Mathf.Max(vector31.x, vector33.x) + 72) / 32 + 270), 539);
						}
						else
						{
							num2 = ((num10 != num6 || num <= 0) && (num10 != num8 || num >= 0) ? Mathf.Max(num10, 0) : Mathf.Max((int)(((double)vector33.x - 72) / 32 + 270), 0));
							num3 = ((num10 != num6 || num >= 0) && (num10 != num8 || num <= 0) ? Mathf.Min(num10, 539) : Mathf.Min((int)(((double)vector33.x + 72) / 32 + 270), 539));
							num4 = Mathf.Max((int)(((double)Mathf.Min(vector31.z, vector33.z) - 72) / 32 + 270), 0);
							num5 = Mathf.Min((int)(((double)Mathf.Max(vector31.z, vector33.z) + 72) / 32 + 270), 539);
						}
						for (int i = num4; i <= num5; i++)
						{
							for (int j = num2; j <= num3; j++)
							{
								uint mTreeGrid = tm.m_treeGrid[i * 540 + j];
								int num12 = 0;
								while (mTreeGrid != 0)
								{
									if ((tm.m_trees.m_buffer[mTreeGrid].m_flags & (ushort)ignoreFlags) == (ushort)TreeInstance.Flags.None && (double)ray.DistanceSqr(tm.m_trees.m_buffer[mTreeGrid].Position) < 2500)
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
					if ((double)single4 != 2)
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

		private static void ReleaseTreeImplementation(TreeManager tm, uint tree, ref TreeInstance data)
		{
			unsafe
			{
				int num;
				int num1;
				if (data.m_flags == 0)
				{
					return;
				}
				Singleton<InstanceManager>.instance.ReleaseInstance(new InstanceID()
				{
					Tree = tree
				});
				data.m_flags = (ushort)(data.m_flags | 2);
				data.UpdateTree(tree);
				data.m_flags = 0;
				if (Singleton<ToolManager>.instance.m_properties.m_mode != ItemClass.Availability.AssetEditor)
				{
					num = Mathf.Clamp((data.m_posX + 32768) * 540 / 65536, 0, 539);
					num1 = Mathf.Clamp((data.m_posZ + 32768) * 540 / 65536, 0, 539);
				}
				else
				{
					num = Mathf.Clamp((data.m_posX / 16 + 32768) * 540 / 65536, 0, 539);
					num1 = Mathf.Clamp((data.m_posZ / 16 + 32768) * 540 / 65536, 0, 539);
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
						else if (num3 != 0)
						{
							tm.m_trees.m_buffer[num3].m_nextGridTree = data.m_nextGridTree;
							break;
						}
						else
						{
							tm.m_treeGrid[num2] = data.m_nextGridTree;
							break;
						}
					}
					data.m_nextGridTree = 0;
				}
				finally
				{
					Monitor.Exit(tm.m_treeGrid);
				}
				try
				{
					tm.m_trees.ReleaseItem(tree);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					object[] objArray = new object[] { tree, tm.m_trees.m_size, LimitTreeManager.Helper.TreeLimit, LimitTreeManager.Helper.UseModifiedTreeCap };
					Debug.LogFormat("Treexception: Releasing {0} {1} {2} {3}", objArray);
					Debug.LogException(exception);
				}
				LimitTreeManager.UpdateTree(tm, tree);
				tm.m_treeCount = (int)(tm.m_trees.ItemCount() - 1);
				Singleton<RenderManager>.instance.UpdateGroup(num * 45 / 540, num1 * 45 / 540, tm.m_treeLayer);
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
									float mSize = position.y + info.m_generatedInfo.m_size.y * 1.25f * single3;
									if ((double)mSize > (double)single)
									{
										single = mSize;
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
							if ((double)Mathf.Max(Mathf.Max(mMin - 8f - position.x, single - 8f - position.z), Mathf.Max((float)((double)position.x - (double)mMax - 8), (float)((double)position.z - (double)mMax1 - 8))) < 0)
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
			LimitTreeManager.Helper.EnsureInit();
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
				tm.m_updatedTrees[tree >> 6] = tm.m_updatedTrees[tree >> 6] | (ulong)1 << (int)(tree & 63);
				tm.m_treesUpdated = true;
			}
		}

		private static void UpdateTrees(TreeManager tm, float minX, float minZ, float maxX, float maxZ)
		{
			unsafe
			{
				int num = Mathf.Max((int)(((double)minX - 8) / 32 + 270), 0);
				int num1 = Mathf.Max((int)(((double)minZ - 8) / 32 + 270), 0);
				int num2 = Mathf.Min((int)(((double)maxX + 8) / 32 + 270), 539);
				int num3 = Mathf.Min((int)(((double)maxZ + 8) / 32 + 270), 539);
				for (int i = num1; i <= num3; i++)
				{
					for (int j = num; j <= num2; j++)
					{
						uint mTreeGrid = tm.m_treeGrid[i * 540 + j];
						int num4 = 0;
						while (mTreeGrid != 0)
						{
							Vector3 position = tm.m_trees.m_buffer[mTreeGrid].Position;
							if ((double)Mathf.Max(Mathf.Max(minX - 8f - position.x, minZ - 8f - position.z), Mathf.Max((float)((double)position.x - (double)maxX - 8), (float)((double)position.z - (double)maxZ - 8))) < 0)
							{
								tm.m_updatedTrees[mTreeGrid >> 6] = tm.m_updatedTrees[mTreeGrid >> 6] | (ulong)1 << (int)(mTreeGrid & 63);
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
					LimitTreeManager.Helper.EnsureInit();
					if (!LimitTreeManager.Helper.UseModifiedTreeCap)
					{
						return false;
					}
					byte[] numArray = null;
					if (!Singleton<SimulationManager>.instance.m_serializableDataStorage.TryGetValue("mabako/unlimiter", out numArray))
					{
						Debug.Log("TreeLimit: no extra data saved with this savegame.");
						return false;
					}
					object[] length = new object[] { (int)numArray.Length };
					Debug.LogFormat("TreeLimit: we have {0} bytes of extra trees", length);
					if ((int)numArray.Length < 2 || (int)numArray.Length % 2 != 0)
					{
						Debug.Log("TreeLimit: Invalid chunk size");
						return false;
					}
					TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
					ushort[] numArray1 = new ushort[(int)numArray.Length / 2];
					Buffer.BlockCopy(numArray, 0, numArray1, 0, (int)numArray.Length);
					uint num = 0;
					uint num1 = num;
					num = num1 + 1;
					ushort num2 = numArray1[num1];
					if (num2 != 1)
					{
						object[] objArray = new object[] { numArray1[0], num2, numArray[0], numArray[1] };
						Debug.LogFormat("TreeLimit: Wrong version ({0}|{1}|{2},{3}).", objArray);
						return false;
					}
					int num3 = 0;
					for (int i = 262144; i < LimitTreeManager.Helper.TreeLimit; i++)
					{
						uint num4 = num;
						try
						{
							uint num5 = num;
							num = num5 + 1;
							mBuffer[i].m_flags = numArray1[num5];
							if (mBuffer[i].m_flags != 0)
							{
								uint num6 = num;
								num = num6 + 1;
								mBuffer[i].m_infoIndex = numArray1[num6];
								uint num7 = num;
								num = num7 + 1;
								mBuffer[i].m_posX = (short)numArray1[num7];
								mBuffer[i].m_posY = 0;
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
							object[] length1 = new object[] { i, num4, (int)numArray1.Length };
							Debug.LogFormat("While fetching tree {0} in pos {1} of {2}", length1);
							Debug.LogException(exception);
							throw exception;
						}
					}
					object[] treeLimit = new object[] { num3, LimitTreeManager.Helper.TreeLimit - 262144 };
					Debug.LogFormat("TreeLimit: Loaded {0} additional trees (out of {1} possible)", treeLimit);
					return true;
				}
			}

			internal static void Serialize()
			{
				if (!LimitTreeManager.Helper.UseModifiedTreeCap)
				{
					return;
				}
				TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
				List<ushort> nums = new List<ushort>()
				{
					1
				};
				int num = 0;
				for (int i = 262144; i < LimitTreeManager.Helper.TreeLimit; i++)
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
				object[] treeLimit = new object[] { num, LimitTreeManager.Helper.TreeLimit - 262144, nums.Count * 2 };
				Debug.LogFormat("TreeLimit: used {0} of {1} extra trees, size in savegame: {2} bytes", treeLimit);
				Singleton<SimulationManager>.instance.m_serializableDataStorage["mabako/unlimiter"] = nums.SelectMany<ushort, byte>((ushort v) => BitConverter.GetBytes(v)).ToArray<byte>();
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
				LimitTreeManager.Helper.EnsureInit();
				Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.BeginDeserialize(s, "TreeManager");
				TreeManager treeManager = Singleton<TreeManager>.instance;
				TreeInstance[] mBuffer = treeManager.m_trees.m_buffer;
				uint[] mTreeGrid = treeManager.m_treeGrid;
				int num2 = 262144;
				int length = (int)mTreeGrid.Length;
				treeManager.m_trees.ClearUnused();
				SimulationManager.UpdateMode mUpdateMode = Singleton<SimulationManager>.instance.m_metaData.m_updateMode;
				bool flag = (mUpdateMode == SimulationManager.UpdateMode.NewAsset ? true : mUpdateMode == SimulationManager.UpdateMode.LoadAsset);
				for (int i = 0; i < length; i++)
				{
					mTreeGrid[i] = 0;
				}
				EncodedArray.UShort num3 = EncodedArray.UShort.BeginRead(s);
				for (int j = 1; j < num2; j++)
				{
					mBuffer[j].m_flags = num3.Read();
				}
				num3.EndRead();
				PrefabCollection<TreeInfo>.BeginDeserialize(s);
				for (int k = 1; k < num2; k++)
				{
					if (mBuffer[k].m_flags != 0)
					{
						mBuffer[k].m_infoIndex = (ushort)PrefabCollection<TreeInfo>.Deserialize();
					}
				}
				PrefabCollection<TreeInfo>.EndDeserialize(s);
				EncodedArray.Short num4 = EncodedArray.Short.BeginRead(s);
				for (int l = 1; l < num2; l++)
				{
					if (mBuffer[l].m_flags == 0)
					{
						num = 0;
					}
					else
					{
						num = num4.Read();
					}
					mBuffer[l].m_posX = num;
				}
				num4.EndRead();
				EncodedArray.Short num5 = EncodedArray.Short.BeginRead(s);
				for (int m = 1; m < num2; m++)
				{
					if (mBuffer[m].m_flags == 0)
					{
						num1 = 0;
					}
					else
					{
						num1 = num5.Read();
					}
					mBuffer[m].m_posZ = num1;
				}
				num5.EndRead();
				for (int n = 1; n < num2; n++)
				{
					mBuffer[n].m_nextGridTree = 0;
					mBuffer[n].m_posY = 0;
				}
				if (LimitTreeManager.Helper.UseModifiedTreeCap)
				{
					LimitTreeManager.CustomSerializer.Deserialize();
				}
				treeManager.m_trees.ClearUnused();
				for (int o = 0; o < LimitTreeManager.Helper.TreeLimit; o++)
				{
					if (mBuffer[o].m_flags == 0)
					{
						treeManager.m_trees.ReleaseItem((uint)o);
					}
					else
					{
						LimitTreeManager.InitializeTree(treeManager, (uint)o, ref mBuffer[o], flag);
					}
				}
				Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.EndDeserialize(s, "TreeManager");
			}

			private static void Serialize(TreeManager.Data data, DataSerializer s)
			{
				Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.BeginSerialize(s, "TreeManager");
				TreeInstance[] mBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
				int num = 262144;
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
				Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.EndSerialize(s, "TreeManager");
			}
		}

		internal static class Helper
		{
			internal static int TreeLimit
			{
				get
				{
					if (!LimitTreeManager.Helper.UseModifiedTreeCap)
					{
						return 262144;
					}
					return 1048576;
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
					if (mUpdateMode == SimulationManager.UpdateMode.LoadGame)
					{
						return true;
					}
					return mUpdateMode == SimulationManager.UpdateMode.NewGame;
				}
			}

			internal static void EnsureInit()
			{
				uint num;
				object[] objArray = new object[] { (Mod.IsEnabled ? "enabled" : "disabled"), (LimitTreeManager.Helper.UseModifiedTreeCap ? "enabled" : "disabled") };
				Debug.LogFormat("[TreeLimit] This mod is {0}. Tree limit is {0}.", objArray);
				if (!LimitTreeManager.Helper.UseModifiedTreeCap)
				{
					return;
				}
				if ((int)Singleton<TreeManager>.instance.m_trees.m_buffer.Length != LimitTreeManager.Helper.TreeLimit)
				{
					Debug.LogFormat("Updating TreeManager", new object[0]);
					Singleton<TreeManager>.instance.m_trees = new Array32<TreeInstance>((uint)LimitTreeManager.Helper.TreeLimit);
					Singleton<TreeManager>.instance.m_updatedTrees = new ulong[16384];
					Singleton<TreeManager>.instance.m_trees.CreateItem(out num);
				}
			}
		}
	}
}