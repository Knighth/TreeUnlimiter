using ColossalFramework;
using System;
using UnityEngine;

namespace TreeUnlimiter
{
	public static class LimitBuildingDecoration
	{
		private static void ClearDecorations()
		{
			NetManager netManager = Singleton<NetManager>.instance;
			for (int i = 1; i < 32768; i++)
			{
				if (netManager.m_segments.m_buffer[i].m_flags != NetSegment.Flags.None)
				{
					netManager.ReleaseSegment((ushort)i, true);
				}
			}
			for (int j = 1; j < 32768; j++)
			{
				if (netManager.m_nodes.m_buffer[j].m_flags != NetNode.Flags.None)
				{
					netManager.ReleaseNode((ushort)j);
				}
			}
			PropManager propManager = Singleton<PropManager>.instance;
			for (int k = 1; k < 65536; k++)
			{
				if (propManager.m_props.m_buffer[k].m_flags != 0)
				{
					propManager.ReleaseProp((ushort)k);
				}
			}
			TreeManager treeManager = Singleton<TreeManager>.instance;
			for (int l = 1; l < LimitTreeManager.Helper.TreeLimit; l++)
			{
				if (treeManager.m_trees.m_buffer[l].m_flags != 0)
				{
					treeManager.ReleaseTree((uint)l);
				}
			}
		}

		private static void SaveProps(BuildingInfo info, ushort buildingID, ref Building data)
		{
			FastList<BuildingInfo.Prop> fastList = new FastList<BuildingInfo.Prop>();
			Vector3 mPosition = data.m_position;
			Quaternion quaternion = Quaternion.AngleAxis(data.m_angle * 57.29578f, Vector3.down);
			Matrix4x4 matrix4x4 = new Matrix4x4();
			matrix4x4.SetTRS(mPosition, quaternion, Vector3.one);
			matrix4x4 = matrix4x4.inverse;
			PropManager propManager = Singleton<PropManager>.instance;
			for (int i = 0; i < 65536; i++)
			{
				if ((propManager.m_props.m_buffer[i].m_flags & 67) == 1)
				{
                    BuildingInfo.Prop prop = new BuildingInfo.Prop();

                    prop.m_prop = propManager.m_props.m_buffer[i].Info;
                    prop.m_finalProp = prop.m_prop;
                    prop.m_position = matrix4x4.MultiplyPoint(propManager.m_props.m_buffer[i].Position);
                    prop.m_radAngle = propManager.m_props.m_buffer[i].Angle - data.m_angle;
                    prop.m_angle = 57.29578f * prop.m_radAngle;
                    prop.m_fixedHeight = propManager.m_props.m_buffer[i].FixedHeight;
                    prop.m_probability = 100;
					
					fastList.Add(prop);
				}
			}
			TreeManager treeManager = Singleton<TreeManager>.instance;
			for (int j = 0; j < LimitTreeManager.Helper.TreeLimit; j++)
			{
				if ((treeManager.m_trees.m_buffer[j].m_flags & 3) == 1 && treeManager.m_trees.m_buffer[j].GrowState != 0)
				{
                    BuildingInfo.Prop prop1 = new BuildingInfo.Prop();
                        prop1.m_tree = treeManager.m_trees.m_buffer[j].Info;
                        prop1.m_finalTree = prop1.m_tree;
                        prop1.m_position = matrix4x4.MultiplyPoint(treeManager.m_trees.m_buffer[j].Position);
                        prop1.m_fixedHeight = treeManager.m_trees.m_buffer[j].FixedHeight;
                        prop1.m_probability = 100;
					fastList.Add(prop1);
				}
			}
			info.m_props = fastList.ToArray();
		}
	}
}