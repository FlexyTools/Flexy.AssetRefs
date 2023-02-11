using System;
using Cysharp.Threading.Tasks;
using Flexy.JsonXSpace;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Flexy.AssetRefs
{
	// See there to adjust the interface: https://github.com/mygamedevtools/scene-loader?
	
	[Serializable]
	public struct AssetRef_Scene : ISerializeAsString
	{
		public AssetRef_Scene ( String refAddress ) { _refAddress = refAddress; }
		
		[SerializeField] String			_refAddress;
		
		public			Boolean			IsNone				=> String.IsNullOrEmpty( _refAddress );
		public			String			Address				=> _refAddress;
		//public			String			SceneName			=> IsNone ? null : _refAddress.AsSpan()[37..].ToString( );

		public	async	UniTask			DownloadDependencies( IProgress<Single> progress = null )	
		{
			var resolver	= AssetRef.GetSceneResolver( );
			await resolver.DownloadDependencies( _refAddress, progress );
		}
		public	async	UniTask<Int32>	GetDownloadSize		( )										
		{
			var resolver	= AssetRef.GetSceneResolver( );
			return await resolver.GetDownloadSize( _refAddress );
		}
		
		public 			UniTask<Scene> 	LoadSceneAsync		( IProgress<Single> progress = null )	
		{
			return AssetRef.GetSceneResolver( ).LoadSceneAsync( this, progress );
		}
		
		public override	String	ToString	( )				
		{
			return _refAddress;
		}
		public			void	FromString	( String data )	
		{
			#if UNITY_EDITOR
			if( data.Length >= 4 && data[3] == ':' )
				data = data[4..];
			#endif
			
			_refAddress = data;
		}
	}
}