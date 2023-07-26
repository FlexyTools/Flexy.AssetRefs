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
	public class ResourcesAssetResolver : AssetRefResolver
	{
		public override async UniTask<T>	LoadAssetAsync<T>		( AssetRef address, IProgress<Single> progress )	
		{
			if ( AssetRef.AllowDirectAccessInEditor )
				return (T)AssetRef.EditorLoadAsset( address, typeof(T) );
			
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
				return (T)AssetRef.EditorLoadAsset( address, typeof(T) );
			
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