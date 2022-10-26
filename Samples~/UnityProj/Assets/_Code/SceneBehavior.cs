using System;
using Cysharp.Threading.Tasks;
using Flexy.AssetRefs;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Progress = Cysharp.Threading.Tasks.Progress;

// public class SceneBehavior : MonoBehaviour
// {
// 	[SerializeField] AssetRef<GameObject> _light;
//
// 	public Slider _slider;
// 	public TextMeshProUGUI _textSize;
// 	public TextMeshProUGUI _textLoadProgress;
// 	private Single _downloadSize;
// 	
// 	private UniTask<Object> _downloadTask;
// 	
// 	public async void BackToMainMenu ( )
// 	{
// 		var asyncOperation = await AssetRefScene.LoadScene( "MainScene" );
//
// 	}
//
// 	public async void InstantiateLight()
// 	{
// 		_downloadTask = _light.LoadAsset( Progress.CreateOnlyValueChanged<Single>( UpdateDownloadProgress) ).AttachExternalCancellation( gameObject.GetCancellationTokenOnDestroy(  ) ).Preserve(  );
// 		var (isCanceled, result) = await _downloadTask.SuppressCancellationThrow(  );
// 		if (!isCanceled)
// 			Instantiate( result );
// 	}
// 	private void Start()
// 	{
// 		LoadLight(  ).Forget(  );
// 	}
// 	private void UpdateDownloadProgress( Single value)
// 	{
// 		if (_downloadTask.Status == UniTaskStatus.Pending)
// 		{
// 			_slider.value = value;
// 			_textLoadProgress.text = $"{Math.Round(value,2) * 100}%";
// 		}
// 	}
// 	private async UniTaskVoid LoadLight()
// 	{
// 		if (!_light.IsBundleLoaded(  ))
// 		{
// 			_textSize.text = $"{Math.Round(await _light.GetDownloadSize(  ) / 1048576f)} mb";
// 		}
// 	}
// }