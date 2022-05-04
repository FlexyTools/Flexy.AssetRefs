using System;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs
{
	public abstract class AssetRefResolver
	{
		public abstract		String			Prefix					{ get ; }
		
		public abstract		UniTask<Object> LoadAssetAsync			( String address, IProgress<Single> progress );
		public abstract		Object			LoadAssetSync			( String address );
		
		public abstract		UniTask			DownloadDependencies	( String address, IProgress<Single> progress );
		public abstract		UniTask<Int32>	GetDownloadSize			( String address );

		public abstract		Boolean			CanHandleAsset			( Type type, String path );
		public abstract		Object			EditorLoadAsset			( String address );
		public abstract		String			EditorCreateAssetPath	( Object asset );
		
	}
}