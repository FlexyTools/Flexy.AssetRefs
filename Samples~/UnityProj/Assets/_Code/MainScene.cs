using System;
using Flexy.AssetRefs;
using UnityEngine;
using UnityEngine.EventSystems;

public class MainScene : MonoBehaviour
{
	//public	AssetRef_Bundle	_bundle;
	public  AssetRef_Scene _sceneA;
	public  AssetRef_Scene _sceneB;
	private static Boolean _bundleManagerInited;

	private Boolean UseStreamingAssets
	{
		get
		{
			#if INCLUDE_BUNDLES || UNITY_EDITOR
			return true;
			#else
			return false;
			#endif
		}
	}

	private async void Start()
	{
		//Debug.Log			( $"[MainScene] - Start: {String.Join( ", ",_bundle.BundleNames )}" );
		if (!_bundleManagerInited)
		{
			Debug.Log			( $"[MainScene] - Start: start init" );
			//await BundleManager.Init( UseStreamingAssets );
			Debug.Log			( $"[MainScene] - Start: was inited" );
			_bundleManagerInited = true;
		}
	}
	private void Update()
	{
	}
	public	void	LoadSceneA()
	{
		Debug.Log			( $"[MainScene] - LoadSceneA: load scene A" );
		_sceneA.LoadSceneAsync(  );
	}
	public	void	LoadSceneB()
	{
		Debug.Log			( $"[MainScene] - LoadSceneA: load scene B" );
		_sceneB.LoadSceneAsync(  );
	}
}
