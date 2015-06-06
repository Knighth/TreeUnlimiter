using ICities;
using System;

namespace TreeUnlimiter
{
	public class DataExtension : SerializableDataExtensionBase
	{
		public DataExtension()
		{
		}

		public override void OnLoadData()
		{
		}

		public override void OnSaveData()
		{
			LimitTreeManager.CustomSerializer.Serialize();
		}
	}
}