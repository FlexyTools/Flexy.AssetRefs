using System;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs
{
	public abstract class AssetRefResolver
	{
		public abstract		UniTask<T>		LoadAssetAsync<T>		( AssetRef address, IProgress<Single> progress ) where T:Object;
		public abstract		T				LoadAssetSync<T>		( AssetRef address ) where T:Object;
		
		public abstract		UniTask			DownloadDependencies	( AssetRef address, IProgress<Single> progress );
		public abstract		UniTask<Int32>	GetDownloadSize			( AssetRef address );
		
		public abstract		Object			EditorLoadAsset			( AssetRef address, Type type );
		public abstract		AssetRef		EditorCreateAssetAddress( Object asset );
	}
}