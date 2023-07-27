using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Flexy.AssetRefs
{
	public class AssetsLoader_Resources : AssetsLoader
	{
		public override		UniTask<Int32>			Package_GetDownloadSize	( AssetRef @ref )		
		{
			return UniTask.FromResult(0);
		}
		public override		AsyncOperation			Package_DownloadAsync	( AssetRef @ref )		
		{
			return default;
		}
		public override		AsyncOperation			Package_UnpackAsync		( AssetRef @ref )		
		{
			return default;
		}
		
		public override async UniTask<T>			LoadAssetAsync<T>		( AssetRef @ref )		
		{
			if ( AssetRef.AllowDirectAccessInEditor )
				return (T)AssetRef.EditorLoadAsset( @ref, typeof(T) );
			
			var asset	= (ResourceRef) await UnityEngine.Resources.LoadAsync<ResourceRef>( $"AssetRefs/{@ref.ToString().Replace(":", "@")}" );
			
			if( !asset )
			{
				if( typeof(T).IsSubclassOf(typeof(MonoBehaviour)) )
				{
					var croppedAddress = new AssetRef( @ref.Uid, default );
					asset	= (ResourceRef) await UnityEngine.Resources.LoadAsync<ResourceRef>( $"AssetRefs/{croppedAddress.ToString()}" );
					
					if( !asset )
						Debug.LogError( $"[AssetRef] - Resources Resolver - Loading asset: {@ref} failed" );
					
					return ((GameObject)asset.Ref).GetComponent<T>();
				}
					
				Debug.LogError( $"[AssetRef] - Resources Resolver - Loading asset: {@ref} failed" );
			}
			
			return (T)asset.Ref;
		}
		public override		T						LoadAssetSync<T>		( AssetRef @ref )		
		{
			if( AssetRef.AllowDirectAccessInEditor  )
				return (T)AssetRef.EditorLoadAsset( @ref, typeof(T) );
			
			var asset	= UnityEngine.Resources.Load<ResourceRef>( $"AssetRefs/{@ref.ToString().Replace(":", "@")}" );
			
			if( !asset )
			{
				if( typeof(T).IsSubclassOf(typeof(MonoBehaviour)) )
				{
					var croppedAddress = new AssetRef( @ref.Uid, default );
					asset	= UnityEngine.Resources.Load<ResourceRef>( $"AssetRefs/{croppedAddress.ToString()}" );
					
					if( !asset )
						Debug.LogError( $"[AssetRef] - Resources Resolver - Loading asset: {@ref} failed" );
					
					return ((GameObject)asset.Ref).GetComponent<T>();
				}
				
				Debug.LogError( $"[AssetRef] - Resources Resolver - Loading asset: {@ref} failed" );
			}
			
			return (T)asset.Ref;
		}
		
		public override		(Scene, AsyncOperation)	LoadSceneAsync			( SceneRef @ref )		
        {
        	var awaitable	= default(AsyncOperation);
        	var address		= @ref.Uid;
        	
        	if( AssetRef.AllowDirectAccessInEditor )
        	{
#if UNITY_EDITOR
        		var path		= UnityEditor.AssetDatabase.GUIDToAssetPath( Utils.Editor.Hash128EditorGUIDExt.ToGUID(address) );
        		awaitable		= UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode( path, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive } );
#endif
        	}
        	else
        	{
        		var asset		= Resources.Load<ResourceRef>( $"AssetRefs/{address}" );
        		awaitable		= SceneManager.LoadSceneAsync( asset.Name, LoadSceneMode.Additive );
        	}
        	
        	var scene		= SceneManager.GetSceneAt( SceneManager.sceneCount - 1 );
        	
        	return (scene, awaitable);
        }
        
        // public async	UniTask<T>		LoadSceneAsync<T>		( String sceneName, IProgress<Single> progress = null ) where T: MonoBehaviour	
        // {
        // 	var scene = await LoadSceneAsync( sceneName, progress );
        //
        // 	foreach ( var go in scene.GetRootGameObjects( ) )
        // 	{
        // 		if ( !go.TryGetComponent<T>( out var component ) )
        // 			continue;
        //
        // 		return component;
        // 	}
        // 	
        // 	return null;
        // }
		
		// public record struct SceneLoadOperation( AsyncOperation AsyncOp )
		// {
		// 	private UniTask _task;
		// 	
		// 	public	UniTask		WaitFrame	( ) => UniTask.DelayFrame( 1 );
		// 	public	UniTask		WaitData	( ) => _task.Status == UniTaskStatus.Succeeded;
		// }
	}
}