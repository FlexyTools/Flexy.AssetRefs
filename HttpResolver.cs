using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs
{
	public class HttpResolver : AssetRefResolver
	{
		[RuntimeInitializeOnLoadMethod]
		static void RegisterResolver( )
		{
			AssetRef.RegisterResolver( new HttpResolver( ) );
		}
		
		public override String Prefix => "http";

		public override Boolean CanHandleAsset(Type type, String path)
		{
			return path.StartsWith( "http://" );
		}

		#if UNITY_EDITOR
		public override Object EditorLoadAsset(String address, Type type)
		{
			return null;
		}

		public override String EditorCreateAssetPath(Object asset)
		{
			return null;			
		}
		#endif

		public override UniTask<T> LoadAssetAsync<T>(String address, IProgress<Single> progress)
		{
			throw new NotImplementedException();
		}

		public override T LoadAssetSync<T>(String address)
		{
			throw new NotImplementedException();
		}

		public override UniTask DownloadDependencies(String address, IProgress<Single> progress)
		{
			throw new NotImplementedException();
		}

		public override UniTask<Int32> GetDownloadSize(String address)
		{
			throw new NotImplementedException();
		}
	}
}