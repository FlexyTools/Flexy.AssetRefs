using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using Flexy.Utils.Editor;
using UnityEditor;
#endif

using Object = UnityEngine.Object;

namespace Flexy.AssetRefs
{
	public class AssetResolver : AssetRefResolver
	{
		public override async UniTask<T>	LoadAssetAsync<T>		( AssetRef address, IProgress<Single> progress )	
		{
			if ( AssetRef.AllowDirectAccessInEditor )
				return (T)EditorLoadAsset( address, typeof(T) );
			
			var asset	= (ResourceRef) await UnityEngine.Resources.LoadAsync<ResourceRef>( $"AssetRefs/{address.ToString().Replace(":", "@")}" );
			
			if( !asset )
			{
				if( typeof(T).IsSubclassOf(typeof(MonoBehaviour)) )
				{
					var croppedAddress = new AssetRef( address.Uid, default );
					asset	= (ResourceRef) await UnityEngine.Resources.LoadAsync<ResourceRef>( $"AssetRefs/{croppedAddress.ToString()}" );
					
					if( !asset )
						Debug.LogError( $"[AssetRef] - Resources Resolver - Loading asset: {address} failed" );
					
					return ((GameObject)asset.Ref).GetComponent<T>();
				}
					
				Debug.LogError( $"[AssetRef] - Resources Resolver - Loading asset: {address} failed" );
			}
			
			return (T)asset.Ref;
		}
		public override T					LoadAssetSync<T>		( AssetRef address )								
		{
			if( AssetRef.AllowDirectAccessInEditor  )
				return (T)EditorLoadAsset( address, typeof(T) );
			
			var asset	= UnityEngine.Resources.Load<ResourceRef>( $"AssetRefs/{address.ToString().Replace(":", "@")}" );
			
			if( !asset )
			{
				if( typeof(T).IsSubclassOf(typeof(MonoBehaviour)) )
				{
					var croppedAddress = new AssetRef( address.Uid, default );
					asset	= UnityEngine.Resources.Load<ResourceRef>( $"AssetRefs/{croppedAddress.ToString()}" );
					
					if( !asset )
						Debug.LogError( $"[AssetRef] - Resources Resolver - Loading asset: {address} failed" );
					
					return ((GameObject)asset.Ref).GetComponent<T>();
				}
				
				Debug.LogError( $"[AssetRef] - Resources Resolver - Loading asset: {address} failed" );
			}
			
			return (T)asset.Ref;
		}
		public override UniTask				DownloadDependencies	( AssetRef address, IProgress<Single> progress )	
		{
			return UniTask.CompletedTask;
		}
		public override UniTask<Int32>		GetDownloadSize			( AssetRef address )								
		{
			return UniTask.FromResult(0);
		}
		
		public override Object				EditorLoadAsset			( AssetRef address, Type type )						
		{
			#if UNITY_EDITOR
			if ( address.IsNone )
				return null;

			if( address.SubId == 0 ) //pure giud
			{
				var path = AssetDatabase.GUIDToAssetPath( address.Uid.ToGUID( ) );
			
				return AssetDatabase.LoadAssetAtPath( path, type );
			}
			else
			{
				var path		= AssetDatabase.GUIDToAssetPath( address.Uid.ToGUID( ) );
				
				foreach ( var asset in AssetDatabase.LoadAllAssetsAtPath( path ) )
				{
					if ( !asset || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid2, out Int64 instanceId ) ) 
						continue;
					
					if( address.SubId == instanceId )
							return asset;
					}
				}
			#endif
			
			return null;
		}
		public override AssetRef				EditorCreateAssetAddress( Object asset )									
		{
			#if UNITY_EDITOR
			
			if( AssetDatabase.IsMainAsset( asset ) && AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid, out Int64 instanceId ) )
				return new AssetRef( new GUID( guid ).ToHash( ), 0 );	
			
			if( AssetDatabase.TryGetGUIDAndLocalFileIdentifier( asset, out var guid2, out long instanceId2 ) )
				return new AssetRef( new GUID( guid2 ).ToHash( ), instanceId2 );
			
			#endif
			
			return default;
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