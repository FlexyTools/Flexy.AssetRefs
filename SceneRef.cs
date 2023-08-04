using Flexy.JsonXSpace;
using UnityEngine.SceneManagement;

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
		
		public			UniTask<UInt64>	GetDownloadSize			( )		=> AssetRef.AssetsLoader.Package_GetDownloadSize( (AssetRef)this );
		public			UniTask			DownloadAsync			( )		=> AssetRef.AssetsLoader.Package_DownloadAsync( (AssetRef)this ).ToUniTask( );
		public			LoadTask<Scene> LoadSceneAsync			( GameObject context, LoadSceneMode loadMode = LoadSceneMode.Additive )						=> AssetRef.AssetsLoader.LoadSceneAsync( this, loadMode, context );
		public static	LoadTask<Scene> LoadSceneAsyncByName	( GameObject context, String sceneName, LoadSceneMode loadMode = LoadSceneMode.Additive  )	=> AssetRef.AssetsLoader.LoadSceneByNameAsync( sceneName, loadMode, context );

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
}