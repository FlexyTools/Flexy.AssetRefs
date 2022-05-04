using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = System.Object;

namespace Flexy.AssetRefs
{
	[Serializable]
	public struct AssetRef_Scene
	{
		public AssetRef_Scene ( String refAddress ) { _refAddress = refAddress; }
		
		// scn:01257675c281472ea774a69dba7d5d82:Map.Island.12 - manager that can load scene by name or guid
		[SerializeField] String			_refAddress;
		
		public			Boolean			IsNone				=> String.IsNullOrEmpty( _refAddress );
		public			String			SceneName			=> _refAddress.AsSpan()[37..].ToString( );

		public	async	UniTask			DownloadDependencies( IProgress<Single> progress = null )	
		{
			var resolver	= AssetRef.GetResolver( _refAddress );
			await resolver.DownloadDependencies( _refAddress, progress );
		}
		public	async	UniTask<Int32>	GetDownloadSize		( )										
		{
			var resolver	= AssetRef.GetResolver( _refAddress );
			return await resolver.GetDownloadSize( _refAddress );
		}
		
		public 			UniTask<Scene> 	LoadSceneAsync		( IProgress<Single> progress = null )	
		{
			return AssetRef.GetSceneResolver( ).LoadSceneAsync( _refAddress, progress );
		}
	}
}