using System;
using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using Flexy.Utils.Editor;
#endif

namespace Flexy.AssetRefs
{
	public class SceneResolver
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init( )
		{
			SceneLoader = null;
		}
		
		public static	Func<AsyncOperation, IProgress<Single>, UniTask<Scene>> SceneLoader;

		public virtual			UniTask			DownloadDependencies	( Hash128 address, IProgress<Single> progress )	
		{
			return UniTask.CompletedTask;
		}
		public virtual			UniTask<Int32>	GetDownloadSize			( Hash128 address )								
		{
			return UniTask.FromResult(0);
		}
		
		public virtual 			UniTask			StartLoadingSceneAsync	( ref AssetRef_Scene sceneRef, IProgress<Single> progress )		
		{
			var awaitable	= default(AsyncOperation);
			var address		= sceneRef.Uid;
			
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

			sceneRef.Scene = scene;
			
			if( SceneLoader != null )
				return SceneLoader( awaitable, progress );
			
			return awaitable.ToUniTask( );
		}
		public virtual async	UniTask<Scene>	LoadSceneAsync			( AssetRef_Scene sceneRef, IProgress<Single> progress )			
		{
			var awaitable	= default(AsyncOperation);
			var address		= sceneRef.Uid;
			
			if( AssetRef.AllowDirectAccessInEditor )
			{
				#if UNITY_EDITOR
				var path		= UnityEditor.AssetDatabase.GUIDToAssetPath( address.ToGUID( ) );
				awaitable		= UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode( path, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive } );
				#endif
			}
			else
			{
				var asset		= (ResourceRef) await UnityEngine.Resources.LoadAsync<ResourceRef>( $"AssetRefs/{address}" );
				awaitable		= SceneManager.LoadSceneAsync( asset.Name, LoadSceneMode.Additive );
			}
			
			var scene		= SceneManager.GetSceneAt( SceneManager.sceneCount - 1 );

			if( SceneLoader != null )
				return await SceneLoader( awaitable, progress );
			
			await awaitable;
			
			return scene;
		}
	}
}