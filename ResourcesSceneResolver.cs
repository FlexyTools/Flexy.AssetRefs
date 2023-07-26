using System;
using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using Flexy.Utils.Editor;
#endif

namespace Flexy.AssetRefs
{
	// public record struct SceneLoadOperation( AsyncOperation AsyncOp )
	// {
	// 	private UniTask _task;
	// 	
	// 	public	UniTask		WaitFrame	( ) => UniTask.DelayFrame( 1 );
	// 	public	UniTask		WaitData	( ) => _task.Status == UniTaskStatus.Succeeded;
	// }
	
	public class ResourcesSceneResolver : SceneResolver
	{
		public override		UniTask<Int32>			GetPreloadDataSize		( SceneRef @ref )		
		{
			return UniTask.FromResult(0);
		}
		public override		AsyncOperation			PreloadSceneDataAsync	( SceneRef @ref )		
		{
			return default;
		}
		public override		(Scene, AsyncOperation)	LoadSceneAsync			( SceneRef @ref )		
		{
			var awaitable	= default(AsyncOperation);
			var address		= @ref.Uid;
			
			if( AssetRef.AllowDirectAccessInEditor )
			{
#if UNITY_EDITOR
				var path		= UnityEditor.AssetDatabase.GUIDToAssetPath( address.ToGUID( ) );
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
	}
}