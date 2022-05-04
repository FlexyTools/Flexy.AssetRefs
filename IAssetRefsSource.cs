using System.Collections.Generic;
using UnityEngine;

namespace Flexy.AssetRefs
{
	public interface  IAssetRefsSource
	{
		List<Object> CollectAssets( );
	}
}