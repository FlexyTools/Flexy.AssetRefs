#if UNITY_6000_6_OR_NEWER
using System.IO;
using Unity.Loading;

namespace Flexy.AssetRefs
{
	public class ContentService : MonoBehaviour
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static	void	ResetLoaders( )		
		{
			AssetRef.AssetLoader	= new AssetLoaderResources();
			SceneRef.SceneLoader	= new SceneLoader_Resources();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static	void	Bootstrap	( )		
		{
			SpawnContentService();
		}
	
		private const	String	PrefabResourcePath			= "Fun.Flexy/ContentServiceRef";
		private const	String	PrefabResourceDirectory		= "Assets/Resources/Fun.Flexy";
		private const	String	PrefabResourceAssetPath		= PrefabResourceDirectory + "/ContentServiceRef.asset";

		[SerializeField] EPathType	_pathType				= EPathType.StreamingAssets;
		[SerializeField] String		_contentDirectoryPath	= "MainCD";

		private ContentDirectoryHandle		_contentDirectory;
		private ContentDirectoryCatalog[]	_contentCatalogs = Array.Empty<ContentDirectoryCatalog>();

		private static	void			SpawnContentService	( )		
		{
			var serviceRef = Resources.Load<ResourceRef>(PrefabResourcePath);
			if (!serviceRef)
				return;

			var prefab = serviceRef.Ref as GameObject;
			if (!prefab)
				throw new InvalidOperationException($"Resource reference '{PrefabResourcePath}' does not point to a GameObject prefab.");

			var prefabService = prefab.GetComponent<ContentService>();
			if (!prefabService)
				throw new InvalidOperationException($"Service prefab referenced by '{PrefabResourcePath}' has no {nameof(ContentService)} component.");

			var prefabWasActive = prefab.activeSelf;
			GameObject serviceObject;

			try
			{
				prefab.SetActive(false);
				serviceObject = Instantiate(prefab);
			}
			finally
			{
				prefab.SetActive(prefabWasActive);
				
				#if UNITY_EDITOR
				if (prefab && !prefab.scene.IsValid() && !String.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(prefab)))
					UnityEditor.EditorUtility.ClearDirty(prefab);
				#endif
			}
			
			DontDestroyOnLoad(serviceObject);
			serviceObject.SetActive(true);
		}
		public virtual	void			Awake				( )		
		{
			Debug.Log($"[ContentService] Awake and start register from '{_contentDirectoryPath}'");
		
			AssetRef.AssetLoader	= new AssetLoader_ContentDirectory(this);
			SceneRef.SceneLoader	= new SceneLoader_ContentDirectory(this);
		
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

		public async	UniTask<T?>		LoadContentAssetAsync<T>	( AssetRef @ref ) where T : Object	
		{
			var asset = await GetContentLoadable(@ref).LoadAsync();
			return GetContentAsset<T>(asset);
		}
		public			T?				LoadContentAssetSync<T>		( AssetRef @ref ) where T : Object	
		{
			var asset = GetContentLoadable(@ref).Load();
			return GetContentAsset<T>(asset);
		}

		public virtual	String			GetContentSceneName			( SceneRef @ref )					
		{
			foreach (var catalog in _contentCatalogs)
				if (catalog.TryGetSceneName(@ref, out var sceneName))
					return sceneName;

			throw new KeyNotFoundException($"SceneRef '{@ref}' was not found in any registered content catalog.");
		}
		public virtual	LoadSceneTask	LoadContentSceneAsync		( SceneRef @ref, LoadSceneTask.Parameters p, GameObject context ) 
		{
			var sceneLoadOperation	= SceneManager.LoadSceneAsync(GetContentLoadable(@ref), new LoadSceneParameters(p.LoadMode, p.PhysicsMode));
			var sceneTask			= new LoadSceneTask(context, @ref, p, SceneManager.GetSceneAt(SceneManager.sceneCount - 1));

			return sceneTask.Run(SceneLoader_ContentDirectory.WaitSceneLoad(sceneLoadOperation, sceneTask));
		}

		private Loadable<Object>	GetContentLoadable	( AssetRef @ref )	
		{
			foreach (var catalog in _contentCatalogs)
				if (catalog.TryGetLoadable(@ref, out var loadable))
					return loadable;

			throw new KeyNotFoundException($"AssetRef '{@ref}' was not found in any registered content catalog.");
		}
		private LoadableSceneId		GetContentLoadable	( SceneRef @ref )	
		{
			foreach (var catalog in _contentCatalogs)
				if (catalog.TryGetLoadable(@ref, out var loadable))
					return loadable;

			throw new KeyNotFoundException($"SceneRef '{@ref}' was not found in any registered content catalog.");
		}

		protected static T?			GetContentAsset<T>	( Object asset ) where T : Object
		{
			if (asset is GameObject go && typeof(T).IsSubclassOf(typeof(Component)))
				return go.GetComponent<T>();

			return asset as T;
		}

#if UNITY_EDITOR
		public static void EditorSetupContentServiceResourceRef( ContentService servicePrefab )
		{
			if (!servicePrefab)
				throw new UnityEditor.Build.BuildFailedException($"{nameof(ContentService)} prefab reference is not assigned.");

			Directory.CreateDirectory(PrefabResourceDirectory);

			var resourceRef = UnityEditor.AssetDatabase.LoadAssetAtPath<ResourceRef>(PrefabResourceAssetPath);
			if (!resourceRef)
			{
				if (UnityEditor.AssetDatabase.LoadMainAssetAtPath(PrefabResourceAssetPath))
					throw new UnityEditor.Build.BuildFailedException($"Asset '{PrefabResourceAssetPath}' exists but is not a {nameof(AssetLoaders.ResourceRef)}.");

				resourceRef = ScriptableObject.CreateInstance<ResourceRef>();
				UnityEditor.AssetDatabase.CreateAsset(resourceRef, PrefabResourceAssetPath);
			}

			resourceRef.Name	= String.Empty;
			resourceRef.Ref		= servicePrefab.gameObject;

			UnityEditor.EditorUtility.SetDirty(resourceRef);
			UnityEditor.AssetDatabase.SaveAssets();
			UnityEditor.AssetDatabase.ImportAsset(PrefabResourceAssetPath, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
		}
#endif

		public enum EPathType: Byte
		{
			StreamingAssets,
			Data,
			Raw
		}
	}
}
#endif