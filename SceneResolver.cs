using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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
			
			#if UNITY_EDITOR
			var path		= UnityEditor.AssetDatabase.GUIDToAssetPath( sceneRef.Address );
			awaitable		= UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode( path, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive } );
			#else
			var asset		= (ResourceRef) await UnityEngine.Resources.LoadAsync<ResourceRef>( $"AssetRefs/{address}" );
			awaitable		= SceneManager.LoadSceneAsync( asset.Name, LoadSceneMode.Additive );
			#endif
			
			var scene		= SceneManager.GetSceneAt( SceneManager.sceneCount - 1 );

			if( SceneLoader != null )
				return await SceneLoader( awaitable, progress );
			
			await awaitable;
			
			return scene;
		}
		
		#if UNITY_EDITOR
		public virtual			Object			EditorLoadAsset			( String address )		
		{
			if( string.IsNullOrEmpty( address ) )
				return null;
			
			var guid = address.AsSpan( )[..32].ToString( );
			var path = UnityEditor.AssetDatabase.GUIDToAssetPath( guid );
			
			return UnityEditor.AssetDatabase.LoadAssetAtPath<Object>( path );
		}
		public virtual			String			EditorCreateAssetPath	( Object asset )		
		{
			var guid	= UnityEditor.AssetDatabase.AssetPathToGUID( UnityEditor.AssetDatabase.GetAssetPath( asset ) );
			
			return $"{guid}";
		}
		#endif
	}
}