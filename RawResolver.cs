using System;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs
{
	public class RawResolver : AssetRefResolver
	{
		[RuntimeInitializeOnLoadMethod]
		static void RegisterResolver( )
		{
			AssetRef.RegisterResolver( new RawResolver( ) );
		}
		
		public override String Prefix => "raw";

		public override Boolean CanHandleAsset(Type type, String path)
		{
			return path.StartsWith( "Assets/StreamingAssets/" );
		}

		public override Object EditorLoadAsset(String address)
		{
			return null;
		}

		public override String EditorCreateAssetPath(Object asset)
		{
			return AssetDatabase.GetAssetPath( asset ).AsSpan()["Assets/StreamingAssets".Length].ToString( );
		}

		public override UniTask<Object> LoadAssetAsync(String address, IProgress<Single> progress)
		{
			throw new NotImplementedException();
		}

		public override Object LoadAssetSync(String address)
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