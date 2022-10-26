﻿using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
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

		#if UNITY_EDITOR
		public override Object		EditorLoadAsset	( String address, Type type )
		{
			if ( String.IsNullOrEmpty( address ) || String.Equals( address, "null", StringComparison.Ordinal ) )
				return null;

			var guid = address.AsSpan( )[4..36].ToString( );
			var path = UnityEditor.AssetDatabase.GUIDToAssetPath( guid );
			
			return UnityEditor.AssetDatabase.LoadAssetAtPath( path, type );
		}

		public override String		EditorCreateAssetPath(Object asset)
		{
			if( UnityEditor.AssetDatabase.IsMainAsset( asset ) && UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid, out long instanceId ) )
				return $"pkg:{guid}";	
			
			if( UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid2, out long instanceId2 ) )
				return $"pkg:{guid2}:{instanceId2}";	
			
			return "";
		}
		#endif

		public override async UniTask<T> LoadAssetAsync<T>(String address, IProgress<Single> progress)
		{
			#if UNITY_EDITOR
			return (T)EditorLoadAsset( address, typeof(T) );
			#else
			var guid	= address.AsSpan( )[4..36].ToString( );
			var asset	= (ResourceRef) await UnityEngine.Resources.LoadAsync<ResourceRef>( $"AssetRefs/{guid}" );

			return asset.Ref;
			#endif
		}

		public override T			LoadAssetSync<T>(String address)
		{
			#if UNITY_EDITOR
			return (T)EditorLoadAsset( address, typeof(T) );
			#else
			var guid	= address.AsSpan( )[4..36].ToString( );
			var asset	= UnityEngine.Resources.Load<ResourceRef>( $"AssetRefs/{guid}" );
			
			return asset.Ref;
			#endif
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