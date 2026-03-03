using System.IO;
using Flexy.AssetRefs.Extra;
using Unity.Properties;

namespace Flexy.AssetRefs.AssetLoaders;

public class SceneLoader_Resources : SceneLoader
{
	protected override		String			GetSceneName_Impl		( SceneRef @ref )													
	{
		var asset		= Resources.Load<ResourceRef>($"Fun.Flexy/AssetRefs/{@ref.Uid}");
			
		return asset.Name ?? "";
	}
	protected override		LoadSceneTask	LoadSceneAsync_Impl		( SceneRef @ref, LoadSceneTask.Parameters p, GameObject context )	
	{
		var asset			= Resources.Load<ResourceRef>($"Fun.Flexy/AssetRefs/{@ref.Uid}");
	
		#if UNITY_EDITOR
		var path			= UnityEditor.AssetDatabase.GUIDToAssetPath( @ref.Uid.ToGUID() );
		if (asset == null || Path.GetFileNameWithoutExtension(path) != asset.Name)
			throw new InvalidOperationException($"AssetRef {@ref} is invalid.");
		
		var sceneLoadOp		= UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode( path, new(p.LoadMode, p.PhysicsMode) );
		#else
		var sceneLoadOp		= SceneManager.LoadSceneAsync( asset.Name, new LoadSceneParameters( p.LoadMode, p.PhysicsMode ) );
		#endif
		
		var sceneTask		= new LoadSceneTask(context, p, SceneManager.GetSceneAt(SceneManager.sceneCount - 1));
		sceneTask.Scene.SetGuid(@ref.Uid.ToString());
		return sceneTask.Run( SceneLoadWaitImpl(sceneLoadOp, sceneTask) );
	}
}