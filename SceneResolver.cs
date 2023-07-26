using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Flexy.AssetRefs
{
	public abstract class SceneResolver
	{
		public abstract			UniTask<Int32>			GetPreloadDataSize		( SceneRef @ref );
		public abstract 		AsyncOperation			PreloadSceneDataAsync	( SceneRef @ref );
		public abstract 		(Scene, AsyncOperation)	LoadSceneAsync			( SceneRef @ref );
	}
}