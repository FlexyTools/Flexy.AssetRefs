using System;
using Cysharp.Threading.Tasks;
using Flexy.JsonXSpace;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = System.Object;

namespace Flexy.AssetRefs
{
	// See there to adjust the interface: https://github.com/mygamedevtools/scene-loader?
	
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
		
		public static	SceneResolver	SceneResolver			= new ResourcesSceneResolver( );
		
		public			UniTask<Int32>	GetDownloadSize			( )										=> SceneResolver.GetPreloadDataSize( this );
		public			UniTask			PreloadSceneDataAsync	( IProgress<Single> progress = null )	=> SceneResolver.PreloadSceneDataAsync( this ).ToUniTask( );
		public async	UniTask<Scene> 	LoadSceneAsync			( GameObject context				)	
		{
			await SceneResolver.PreloadSceneDataAsync( this );
			
			var (scene, ao) = SceneResolver.LoadSceneAsync( this );
			
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
	}
}