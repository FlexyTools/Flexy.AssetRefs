#if UNITY_6000_6_OR_NEWER
using System.Runtime.InteropServices;
using Flexy.AssetRefs.Extra;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Loading;

namespace Flexy.AssetRefs.AssetLoaders
{
	public class ContentDirectoryCatalog : ScriptableObject
	{
		[SerializeField] AssetRef[]			_assetRefs			= Array.Empty<AssetRef>();
		[SerializeField] LoadableObjectId[]	_assetLoadables		= Array.Empty<LoadableObjectId>();
		[SerializeField] LoadableSceneId[]	_sceneLoadables		= Array.Empty<LoadableSceneId>();
		[SerializeField] String[]			_sceneNames			= Array.Empty<String>();

		private Dictionary<AssetRef, Loadable<Object>>	_assetsByRef = null!;
		private Dictionary<SceneRef, LoadableSceneId>	_scenesByRef = null!;
		private Dictionary<SceneRef, String>			_sceneNamesByRef = null!;

		public Loadable<Object>	GetLoadable		( AssetRef @ref )									{BuildDicts(); return _assetsByRef[@ref]; }
		public LoadableSceneId	GetLoadable		( SceneRef @ref )									{BuildDicts(); return _scenesByRef[@ref]; }
		public String			GetSceneName	( SceneRef @ref )									{BuildDicts(); return _sceneNamesByRef[@ref]; }
		public Boolean			TryGetLoadable	( AssetRef @ref, out Loadable<Object> loadable )	{BuildDicts(); return _assetsByRef.TryGetValue(@ref, out loadable); }
		public Boolean			TryGetLoadable	( SceneRef @ref, out LoadableSceneId loadable )		{BuildDicts(); return _scenesByRef.TryGetValue(@ref, out loadable); }
		public Boolean			TryGetSceneName	( SceneRef @ref, out String sceneName )				{BuildDicts(); return _sceneNamesByRef.TryGetValue(@ref, out sceneName); }

		public	void	SetLoadables( AssetRef[] assetRefs, LoadableObjectId[] assetLoadables, LoadableSceneId[] sceneLoadables, String[] sceneNames )
		{
			if (assetRefs.Length != assetLoadables.Length || sceneLoadables.Length != sceneNames.Length)
				throw new InvalidOperationException($"Content catalog arrays have different lengths");
	
			_assetLoadables		= assetLoadables;
			_sceneLoadables		= sceneLoadables;
			_sceneNames			= sceneNames;
			_assetsByRef		= null!;
			_scenesByRef		= null!;
			_sceneNamesByRef	= null!;
		}

		private void BuildDicts ( )
		{
			if (_assetsByRef != null)
				return;

			var assetsByRef = _assetsByRef = new Dictionary<AssetRef, Loadable<Object>>((Int32)(_assetLoadables.Length * 1.3f));
			for (var i = 0; i < _assetLoadables.Length; i++)
			{
				var @ref = _assetRefs[i];
				if (!assetsByRef.TryAdd(@ref, new Loadable<Object>(_assetLoadables[i])))
					Debug.LogError($"Content catalog contains a duplicate asset ref: '{@ref}'.");
			}

			var scenesByRef		= _scenesByRef		= new Dictionary<SceneRef, LoadableSceneId>((Int32)(_sceneLoadables.Length * 1.3f));
			var sceneNamesByRef	= _sceneNamesByRef	= new Dictionary<SceneRef, String>((Int32)(_sceneLoadables.Length * 1.3f));
			
			for (var i = 0; i < _sceneLoadables.Length; i++)
			{
				ref var id	= ref UnsafeUtility.As<LoadableSceneId, LoadableSceneIdInternal>(ref _sceneLoadables[i]);
				var @ref	= new SceneRef(id.Guid.ToHash());
				if (!scenesByRef.TryAdd(@ref, _sceneLoadables[i]))
					Debug.LogError($"Content catalog contains a duplicate scene ref: '{@ref}'.");
				else
					sceneNamesByRef.Add(@ref, _sceneNames[i]);
			}
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct LoadableObjectIdInternal
		{
			[FieldOffset(0)]	internal GUID Guid;
			[FieldOffset(24)]	internal Int64 LocalIdentifierInFile;
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct LoadableSceneIdInternal
		{
			[FieldOffset(0)]	internal GUID Guid;
		}
	}
}
#endif
