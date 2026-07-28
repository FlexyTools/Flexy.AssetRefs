using System.IO;
using Unity.Loading;

namespace Flexy.AssetRefs
{
	public class ContentService_LocalContentDirectory : ContentService
	{
#if UNITY_6000_6_OR_NEWER
		[SerializeField] EPathType	_pathType				= EPathType.StreamingAssets;
		[SerializeField] String		_contentDirectoryPath	= "MainCD";
		
		private ContentDirectoryHandle		_contentDirectory;
		private ContentDirectoryCatalog[]	_contentCatalogs = Array.Empty<ContentDirectoryCatalog>();

		protected override void			Awake		( )		
		{
			base.Awake();

			#if UNITY_EDITOR
			foreach (var h in ContentLoadManager.GetContentDirectories())
				ContentLoadManager.UnregisterContentDirectory(h);
			#endif
		
			Debug.Log($"[ContentService] Awake and start register from '{_contentDirectoryPath}'");
		
			if (String.IsNullOrWhiteSpace(_contentDirectoryPath))
				throw new InvalidOperationException("Content directory path can not be empty.");

			var path = _pathType switch 
			{
				EPathType.StreamingAssets	=> Application.streamingAssetsPath + "/" + _contentDirectoryPath,
				EPathType.Data				=> Application.persistentDataPath + "/" + _contentDirectoryPath,
				_							=> _contentDirectoryPath,
			};
			
			var contentDirectoryPath = Path.GetFullPath(path);
			if (!Directory.Exists(contentDirectoryPath))
				throw new DirectoryNotFoundException($"Content directory was not found at '{contentDirectoryPath}'.");

				_contentDirectory = ContentLoadManager.RegisterContentDirectory(contentDirectoryPath);
			if (!_contentDirectory.IsValid)
				throw new InvalidOperationException($"Content directory could not be registered from '{contentDirectoryPath}'.");

			try
			{
				_contentCatalogs = ContentLoadManager.GetRootAssets<ContentDirectoryCatalog>(_contentDirectory);
				if (_contentCatalogs.Length == 0)
					throw new InvalidOperationException($"Content directory '{contentDirectoryPath}' contains no {nameof(ContentDirectoryCatalog)} roots.");
			}
			catch
			{
				ContentLoadManager.UnregisterContentDirectory(_contentDirectory);
				_contentDirectory = default;
				throw;
			}

			Debug.Log($"[ContentService] Registered content directory '{_contentDirectory.BuildName}' from '{contentDirectoryPath}'. Catalogs: {_contentCatalogs.Length}.");
		}
		protected override void			OnDestroy	( )		
		{
			base.OnDestroy();
		
			if (_contentDirectory.IsValid)
				ContentLoadManager.UnregisterContentDirectory(_contentDirectory);
		}

		public override			T?				LoadAssetSync<T>	( AssetRef @ref ) where T : class	
		{
			var asset = GetContentLoadable(@ref).Load();
			return GetSpecializedAsset<T>(asset);
		}
		public override async	UniTask<T?>		LoadAssetAsync<T>	( AssetRef @ref ) where T : class	
		{
			var asset = await GetContentLoadable(@ref).LoadAsync();
			return GetSpecializedAsset<T>(asset);
		}
		
		public override	String			GetSceneName		( SceneRef @ref )					
		{
			foreach (var catalog in _contentCatalogs)
				if (catalog.TryGetSceneName(@ref, out var sceneName))
					return sceneName;

			throw new KeyNotFoundException($"SceneRef '{@ref}' was not found in any registered content catalog.");
		}
		public override	LoadSceneTask	LoadSceneAsync		( SceneRef @ref, LoadSceneTask.Parameters p, GameObject context ) 
		{
			var sceneLoadOperation	= SceneManager.LoadSceneAsync(GetContentLoadable(@ref), new LoadSceneParameters(p.LoadMode, p.PhysicsMode));
			var sceneTask			= new LoadSceneTask(context, @ref, p, SceneManager.GetSceneAt(SceneManager.sceneCount - 1));

			return sceneTask.Run(WaitSceneLoad(sceneLoadOperation, sceneTask));
		}

		private		Loadable<Object>	GetContentLoadable	( AssetRef @ref )	
		{
			foreach (var catalog in _contentCatalogs)
				if (catalog.TryGetLoadable(@ref, out var loadable))
					return loadable;

			throw new KeyNotFoundException($"AssetRef '{@ref}' was not found in any registered content catalog.");
		}
		private		LoadableSceneId		GetContentLoadable	( SceneRef @ref )	
		{
			foreach (var catalog in _contentCatalogs)
				if (catalog.TryGetLoadable(@ref, out var loadable))
					return loadable;

			throw new KeyNotFoundException($"SceneRef '{@ref}' was not found in any registered content catalog.");
		}

		public enum EPathType: Byte
		{
			StreamingAssets,
			Data,
			Raw
		}
		
#else
		public override void			Awake				( )	=> throw new NotImplementedException();
		public override T?				LoadAssetSync<T>	( AssetRef @ref ) where T : class => throw new NotImplementedException();
		public override UniTask<T?>		LoadAssetAsync<T>	( AssetRef @ref ) where T : class => throw new NotImplementedException();
		public override String			GetSceneName		( SceneRef @ref ) => throw new NotImplementedException();
		public override LoadSceneTask	LoadSceneAsync		( SceneRef @ref, LoadSceneTask.Parameters p, GameObject context ) => throw new NotImplementedException();
#endif
	}
}