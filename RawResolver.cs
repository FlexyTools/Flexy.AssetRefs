using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs
{
	// public class RawResolver : AssetRefResolver
	// {
	// 	[RuntimeInitializeOnLoadMethod]
	// 	static void RegisterResolver( )
	// 	{
	// 		AssetRef.RegisterResolver( new RawResolver( ) );
	// 	}
	// 	
	// 	public override String Prefix => "raw";
	//
	// 	public override Boolean CanHandleAsset(Type type, String path)
	// 	{
	// 		return path.StartsWith( "Assets/StreamingAssets/" );
	// 	}
	//
	// 	#if UNITY_EDITOR
	// 	public override Object EditorLoadAsset(String address, Type type)
	// 	{
	// 		return null;
	// 	}
	//
	// 	public override String EditorCreateAssetPath(Object asset)
	// 	{
	// 		
	// 		return UnityEditor.AssetDatabase.GetAssetPath( asset ).AsSpan()["Assets/StreamingAssets".Length].ToString( );
	// 	}
	// 	#endif
	//
	// 	public override UniTask<T> LoadAssetAsync<T>(String address, IProgress<Single> progress)
	// 	{
	// 		throw new NotImplementedException();
	// 	}
	//
	// 	public override T LoadAssetSync<T>(String address)
	// 	{
	// 		throw new NotImplementedException();
	// 	}
	//
	// 	public override UniTask DownloadDependencies(String address, IProgress<Single> progress)
	// 	{
	// 		throw new NotImplementedException();
	// 	}
	//
	// 	public override UniTask<Int32> GetDownloadSize(String address)
	// 	{
	// 		throw new NotImplementedException();
	// 	}
	// }
}