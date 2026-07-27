using Unity.Loading;

namespace Flexy.AssetRefs.AssetLoaders
{
	public class ContentDirectoryCatalog : ScriptableObject
	{
		[SerializeField] AssetRef[]			_assetRefs			= Array.Empty<AssetRef>();
		[SerializeField] Loadable<Object>[]	_assetLoadables		= Array.Empty<Loadable<Object>>();
		[SerializeField] SceneRef[]			_sceneRefs			= Array.Empty<SceneRef>();
		[SerializeField] LoadableSceneId[]	_sceneLoadables		= Array.Empty<LoadableSceneId>();
		[SerializeField] String[]			_sceneNames			= Array.Empty<String>();

		private Dictionary<AssetRef, Loadable<Object>>?	_assetsByRef;
		private Dictionary<SceneRef, LoadableSceneId>?	_scenesByRef;
		private Dictionary<SceneRef, String>?			_sceneNamesByRef;

		public Loadable<Object>	GetLoadable		( AssetRef @ref )									=> (_assetsByRef ??= BuildLookup(_assetRefs, _assetLoadables, "asset"))[@ref];
		public LoadableSceneId	GetLoadable		( SceneRef @ref )									=> (_scenesByRef ??= BuildLookup(_sceneRefs, _sceneLoadables, "scene"))[@ref];
		public String			GetSceneName	( SceneRef @ref )									=> (_sceneNamesByRef ??= BuildLookup(_sceneRefs, _sceneNames, "scene name"))[@ref];
		public Boolean			TryGetLoadable	( AssetRef @ref, out Loadable<Object> loadable )	=> (_assetsByRef ??= BuildLookup(_assetRefs, _assetLoadables, "asset")).TryGetValue(@ref, out loadable);
		public Boolean			TryGetLoadable	( SceneRef @ref, out LoadableSceneId loadable )		=> (_scenesByRef ??= BuildLookup(_sceneRefs, _sceneLoadables, "scene")).TryGetValue(@ref, out loadable);
		public Boolean			TryGetSceneName	( SceneRef @ref, out String sceneName )				=> (_sceneNamesByRef ??= BuildLookup(_sceneRefs, _sceneNames, "scene name")).TryGetValue(@ref, out sceneName!);

		public	void	SetLoadables( AssetRef[] assetRefs, Loadable<Object>[] assetLoadables, SceneRef[] sceneRefs, LoadableSceneId[] sceneLoadables, String[] sceneNames )	
		{
			if (assetRefs.Length != assetLoadables.Length || sceneRefs.Length != sceneLoadables.Length || sceneRefs.Length != sceneNames.Length)
				throw new InvalidOperationException($"Content catalog arrays have different lengths");
	
			_assetRefs			= assetRefs;
			_assetLoadables		= assetLoadables;
			_sceneRefs			= sceneRefs;
			_sceneLoadables		= sceneLoadables;
			_sceneNames			= sceneNames;
			_assetsByRef		= null;
			_scenesByRef		= null;
			_sceneNamesByRef	= null;
		}
		private static	Dictionary<TRef, TLoadable> BuildLookup<TRef, TLoadable>( TRef[] refs, TLoadable[] loadables, String entryType ) where TRef : notnull					
		{
			if (refs.Length != loadables.Length)
				throw new InvalidOperationException($"Content catalog arrays have different lengths");

			var result = new Dictionary<TRef, TLoadable>(refs.Length);
			for (var i = 0; i < refs.Length; i++)
				if (!result.TryAdd(refs[i], loadables[i]))
					throw new InvalidOperationException($"Content catalog contains a duplicate {entryType} ref: '{refs[i]}'.");

			return result;
		}
	}
}
