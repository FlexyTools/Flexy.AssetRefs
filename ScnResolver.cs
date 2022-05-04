using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs
{
	public class ScnResolver
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init( )
		{
			SceneLoader = null;
		}
		
		public static	Func<String, IProgress<Single>, UniTask<Scene>> SceneLoader;
		
		public					String			Prefix					=> "scn";

		public virtual			UniTask			DownloadDependencies	( String address, IProgress<Single> progress )	
		{
			throw new NotImplementedException();
		}
		public virtual			UniTask<Int32>	GetDownloadSize			( String address )								
		{
			throw new NotImplementedException();
		}
		
		public virtual async	UniTask<Scene>	LoadSceneAsync			( String address, IProgress<Single> progress )	
		{
			if( SceneLoader != null )
				return await SceneLoader( address, progress );
			
			var awaitable	= default(AsyncOperation);
			
			#if UNITY_EDITOR
			var guid		= address.AsSpan( )[4..36].ToString( );
			var path		= UnityEditor.AssetDatabase.GUIDToAssetPath( guid );
			
			awaitable		= UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode( path, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive } );
			#else
			var sceneName	= address.AsSpan( )[37..].ToString( );
			awaitable		= SceneManager.LoadSceneAsync( sceneName );
			
			#endif			

			var scene		= SceneManager.GetSceneAt( SceneManager.sceneCount - 1 );
		
			await awaitable;
			
			return scene;
		}
		
		#if UNITY_EDITOR
		public virtual			Object			EditorLoadAsset			( String address )		
		{
			if( string.IsNullOrEmpty( address ) )
				return null;
			
			var guid = address.AsSpan( )[4..36].ToString( );
			var path = UnityEditor.AssetDatabase.GUIDToAssetPath( guid );
			
			return UnityEditor.AssetDatabase.LoadAssetAtPath<Object>( path );
		}
		public virtual			String			EditorCreateAssetPath	( Object asset )		
		{
			var guid	= UnityEditor.AssetDatabase.AssetPathToGUID( UnityEditor.AssetDatabase.GetAssetPath( asset ) );
			var mapName	= Path.GetFileNameWithoutExtension( UnityEditor.AssetDatabase.GetAssetPath( asset ) );
				
			return $"scn:{guid}:{mapName}";
		}
		#endif
	}
}