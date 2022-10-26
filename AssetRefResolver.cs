using System;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs
{
	public abstract class AssetRefResolver
	{
		public abstract		String			Prefix					{ get ; }
		
		public abstract		UniTask<T> LoadAssetAsync<T>			( String address, IProgress<Single> progress ) where T:UnityEngine.Object;
		public abstract		T			LoadAssetSync<T>			( String address ) where T:UnityEngine.Object;
		
		public abstract		UniTask			DownloadDependencies	( String address, IProgress<Single> progress );
		public abstract		UniTask<Int32>	GetDownloadSize			( String address );

		public abstract		Boolean			CanHandleAsset			( Type type, String path );
		
		#if UNITY_EDITOR
		public abstract Object EditorLoadAsset(String address, Type type);
		public abstract		String			EditorCreateAssetPath	( Object asset );
		#endif
	}
}