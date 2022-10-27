using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

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

		public virtual			UniTask			DownloadDependencies	( String address, IProgress<Single> progress )	
		{
			return UniTask.CompletedTask;
		}
		public virtual			UniTask<Int32>	GetDownloadSize			( String address )								
		{
			return UniTask.FromResult(0);
		}
		
		public virtual async	UniTask<Scene>	LoadSceneAsync			( AssetRef_Scene sceneRef, IProgress<Single> progress )	
		{
			var awaitable	= default(AsyncOperation);
			var address		= sceneRef.Address;
			
			if( AssetRef.AllowDirectAccessInEditor )
			{
				var path		= UnityEditor.AssetDatabase.GUIDToAssetPath( address );
				awaitable		= UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode( path, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive } );
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