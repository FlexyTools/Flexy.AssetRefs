using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs
{
	public class ResourceRef : ScriptableObject
	{
		public String Name;
		public Object Ref;
	}
}