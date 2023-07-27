using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs
{
	public abstract class AssetsLoader
	{
		public abstract		UniTask<Int32>			Package_GetDownloadSize	( AssetRef @ref );
		public abstract 	AsyncOperation			Package_DownloadAsync	( AssetRef @ref );
		public abstract 	AsyncOperation			Package_UnpackAsync		( AssetRef @ref );
		
		public abstract 	(Scene, AsyncOperation)	LoadSceneAsync			( SceneRef @ref );
		public abstract		UniTask<T>				LoadAssetAsync<T>		( AssetRef @ref ) where T:Object;
		public abstract		T						LoadAssetSync<T>		( AssetRef @ref ) where T:Object;
	}
}