using System;
using Cysharp.Threading.Tasks;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs
{
	public class PkgResolver : AssetRefResolver
	{
		public override String		Prefix => "pkg";

		public override Boolean		CanHandleAsset	( Type type, String path )	
		{
			return true;
		}

		public override Object		EditorLoadAsset	( String address )			
		{
			var guid = address.AsSpan( )[4..36].ToString( );
			var path = AssetDatabase.GUIDToAssetPath( guid );
			
			return AssetDatabase.LoadAssetAtPath<Object>( path );
		}

		public override String		EditorCreateAssetPath(Object asset)
		{
			if( AssetDatabase.IsMainAsset( asset ) && AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid, out long instanceId ) )
				return $"pkg:{guid}";	
			
			if( AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid2, out long instanceId2 ) )
				return $"pkg:{guid2}:{instanceId2}";	
			
			return "";
		}

		public override async UniTask<Object> LoadAssetAsync(String address, IProgress<Single> progress)
		{
			// #if UNITY_EDITOR
			// return EditorLoadAsset( address );
			// #else
			var guid	= address.AsSpan( )[4..36].ToString( );
			var asset	= (ResourceRef) await UnityEngine.Resources.LoadAsync<ResourceRef>( $"AssetRefs/{guid}" );

			return asset.Ref;
			//#endif
		}

		public override Object			LoadAssetSync(String address)
		{
			return EditorLoadAsset( address );
		}

		public override UniTask			DownloadDependencies(String address, IProgress<Single> progress)
		{
			throw new NotImplementedException();
		}

		public override UniTask<Int32>	GetDownloadSize(String address)
		{
			throw new NotImplementedException();
		}
		
		// public static Object	LoadAssetBypassBungles				( String assetGuid, String subObjectName )		
		// {
		// 	if( String.IsNullOrEmpty( assetGuid ) )
		// 		return null;
		// 	
		// 	var path = AssetDatabase.GUIDToAssetPath( assetGuid );
		// 	var assetAtPath = AssetDatabase.LoadAssetAtPath( path, typeof(Object) );
		// 	
		// 	if ( assetAtPath is SceneAsset || String.IsNullOrEmpty( subObjectName ) )
		// 		return assetAtPath;
		// 	
		// 	var allAssetsAtPath   = AssetDatabase.LoadAllAssetsAtPath( path ).Where( o => o !=null && (AssetDatabase.IsMainAsset( o ) || (AssetDatabase.IsSubAsset( o ) && !(o is GameObject))) ).OrderBy( AssetDatabase.IsMainAsset ).ToList(  );
		// 	
		// 	if (allAssetsAtPath.Count ==0)
		// 		return null;
		// 	
		// 	var resultAsset = allAssetsAtPath.FirstOrDefault(x => x.name == subObjectName);
		// 	return resultAsset;
		// }
	}
}