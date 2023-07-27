using System;
using Cysharp.Threading.Tasks;
using Flexy.JsonXSpace;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = System.Object;

namespace Flexy.AssetRefs
{
	[Serializable]
	public struct SceneRef : ISerializeAsString
	{
		public	SceneRef ( String refAddress )	
		{
			this = default;
			FromString( refAddress );
		}
		
		[SerializeField] Hash128		_uid;

		public			Hash128			Uid						=> _uid;
		public			Boolean			IsNone					=> _uid == default;
		
		public			UniTask<Int32>	GetDownloadSize			( )		=> AssetRef.AssetsLoader.Package_GetDownloadSize( (AssetRef)this );
		public			UniTask			PreloadSceneDataAsync	( )		=> AssetRef.AssetsLoader.Package_DownloadAsync( (AssetRef)this ).ToUniTask( );
		public async	UniTask<Scene> 	LoadSceneAsync			( GameObject context )	
		{
			await AssetRef.AssetsLoader.Package_DownloadAsync( (AssetRef)this );
			await AssetRef.AssetsLoader.Package_UnpackAsync( (AssetRef)this );
			
			var (scene, ao) = AssetRef.AssetsLoader.LoadSceneAsync( this );
			
			#if FLEXY_GAMEWORLD
			GameWorld_.GameWorld.GetGameWorld( context.scene ).RegisterGameScene( scene );
			#endif
			
			await ao;
			
			return scene;
		}

		public override	String			ToString				( )					
		{
			if( _uid == default )
				return String.Empty;
			
			return _uid.ToString( );
		}
		public			void			FromString				( String data )		
		{
			if( String.IsNullOrWhiteSpace( data ) )
				_uid = default;
			
			_uid = Hash128.Parse( data );
		}
		
		public static explicit operator AssetRef( SceneRef sr ) => new AssetRef( sr._uid, 0 );
	}
	
	public struct SceneLoadingOperation
	{
		private SceneRef		_ref;
		private GameObject		_context;
		private Single			_stateProgress;
		private ELoadingState	_loadingState;

		public Single			StateProgress	=> _stateProgress;
		public ELoadingState	LoadingState	=> _loadingState;

		public async	UniTask<Scene> 			LoadSceneAsync		( GameObject context )	
		{
			await AssetRef.AssetsLoader.Package_DownloadAsync( (AssetRef)_ref );
			
			//set loading progress there
			
			var (scene, ao) = AssetRef.AssetsLoader.LoadSceneAsync( _ref );
			
#if FLEXY_GAMEWORLD
			GameWorld_.GameWorld.GetGameWorld( context.scene ).RegisterGameScene( scene );
#endif
			
			await ao;
			
			return scene;
		}
		public async	UniTask<Boolean>		StillWaiting		( )		
		{
			await UniTask.DelayFrame( 1 );
			
			if( LoadingState == ELoadingState.Done )
				return false;
			
			return true;
		}
		public			UniTask<Scene>.Awaiter	GetAwaiter			( )		
		{
			return LoadSceneAsync( _context ).GetAwaiter( );
		}
	}
}