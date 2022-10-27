using System;
using Cysharp.Threading.Tasks;
using Flexy.JsonX;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = System.Object;

namespace Flexy.AssetRefs
{
	[Serializable]
	[JsonX(isInline:true)]
	public struct AssetRef_Scene : ISerializeAsString
	{
		public AssetRef_Scene ( String refAddress ) { _refAddress = refAddress; }
		
		// scn:01257675c281472ea774a69dba7d5d82:Map.Island.12 - manager that can load scene by name or guid
		[SerializeField] String			_refAddress;
		
		public			Boolean			IsNone				=> String.IsNullOrEmpty( _refAddress );
		public			String			SceneName			=> IsNone ? null : _refAddress.AsSpan()[37..].ToString( );

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
			return AssetRef.GetSceneResolver( ).LoadSceneAsync( _refAddress, progress );
		}
		
		public override	String	ToString	( )				
		{
			return _refAddress;
		}
		public			void	FromString	( String data )	
		{
			_refAddress = data;
		}
	}
}