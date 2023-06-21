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
	public struct AssetRef_Scene : ISerializeAsString
	{
		public	AssetRef_Scene ( String refAddress )	
		{
			this = default;
			FromString( refAddress );
		}
		
		[SerializeField] Hash128		_uid;

		public			Hash128			Uid					=> _uid;
		public			Boolean			IsNone				=> _uid == default;
		public			Scene			Scene				{ get; set; }

		public	async	UniTask			DownloadDependencies( IProgress<Single> progress = null )	
		{
			var resolver	= AssetRef.GetSceneResolver( );
			await resolver.DownloadDependencies( _uid, progress );
		}
		public	async	UniTask<Int32>	GetDownloadSize		( )										
		{
			var resolver	= AssetRef.GetSceneResolver( );
			return await resolver.GetDownloadSize( _uid );
		}
		
		public 			UniTask<Scene> 	LoadSceneAsync		( IProgress<Single> progress = null )	
		{
			return AssetRef.GetSceneResolver( ).LoadSceneAsync( this, progress );
		}
		public 			UniTask		 	StartLoadingSceneAsync		( IProgress<Single> progress = null )	
		{
			return AssetRef.GetSceneResolver( ).StartLoadingSceneAsync( ref this, progress );
		}
		
		public override	String	ToString	( )				
		{
			if( _uid == default )
				return String.Empty;
			
			return _uid.ToString( );
		}
		public			void	FromString	( String data )	
		{
			if( String.IsNullOrWhiteSpace( data ) )
				_uid = default;
			
			_uid = Hash128.Parse( data );
		}
	}
}