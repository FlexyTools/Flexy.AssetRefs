using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Flexy.AssetRefs.LoaderResources
{
	public class AssetsLoader_Resources : AssetsLoader
	{
		protected override		Boolean					Package_IsDownloaded_Impl	( AssetRef @ref )		
		{
			return true;
		}
		protected override		UniTask<UInt64>			Package_GetDownloadSize_Impl( AssetRef @ref )		
		{
			return UniTask.FromResult( default(UInt64) );
		}
		protected override		AsyncOperation			Package_DownloadAsync_Impl	( AssetRef @ref )		
		{
			return default;
		}
		
		protected override async UniTask<T>				LoadAssetAsync_Impl<T>		( AssetRef @ref )		
		{
			var asset	= (ResourceRef) await Resources.LoadAsync<ResourceRef>( $"AssetRefs/{@ref.ToString().Replace(":", "@")}" );

			if (asset) 
				return (T)asset.Ref;
			
			if( typeof(T).IsSubclassOf(typeof(MonoBehaviour)) )
			{
				var croppedAddress = new AssetRef( @ref.Uid, default );
				asset	= (ResourceRef) await UnityEngine.Resources.LoadAsync<ResourceRef>( $"AssetRefs/{croppedAddress.ToString()}" );
					
				if( !asset )
					Debug.LogError( $"[AssetRef] - Resources Resolver - Loading asset: {@ref} failed" );
					
				return ((GameObject)asset.Ref).GetComponent<T>();
			}
					
			Debug.LogError( $"[AssetRef] - Resources Resolver - Loading asset: {@ref} failed" );

			return default;
		}
		protected override		T						LoadAssetSync_Impl<T>		( AssetRef @ref )		
		{
			var asset	= Resources.Load<ResourceRef>( $"AssetRefs/{@ref.ToString().Replace(":", "@")}" );

			if (asset) 
				return (T)asset.Ref;
			
			if( typeof(T).IsSubclassOf(typeof(MonoBehaviour)) )
			{
				var croppedAddress = new AssetRef( @ref.Uid, default );
				asset	= Resources.Load<ResourceRef>( $"AssetRefs/{croppedAddress.ToString()}" );
					
				if( !asset )
					Debug.LogError( $"[AssetRef] - Resources Resolver - Loading asset: {@ref} failed" );
					
				return ((GameObject)asset.Ref).GetComponent<T>();
			}
				
			Debug.LogError( $"[AssetRef] - Resources Resolver - Loading asset: {@ref} failed" );

			return default;
		}
		
		protected override		AsyncOperation			LoadSceneAsync_Impl			( SceneRef @ref, LoadSceneMode loadMode )		
        {
			var address		= @ref.Uid;
        	var asset		= Resources.Load<ResourceRef>( $"AssetRefs/{address}" );
        	var awaitable	= SceneManager.LoadSceneAsync( asset.Name, loadMode );
        	
        	return awaitable;
        }
	}
}