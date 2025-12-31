namespace Flexy.AssetRefs.AssetLoaders;

public class SceneLoader_Resources : SceneLoader
{
	protected override		String			GetSceneName_Impl		( SceneRef @ref )													
	{
		var address		= @ref.Uid;
		var asset		= Resources.Load<ResourceRef>($"Fun.Flexy/AssetRefs/{address}");
			
		return asset.Name ?? "";
	}
	protected override		LoadSceneTask	LoadSceneAsync_Impl		( SceneRef @ref, LoadSceneTask.Parameters p, GameObject context )	
	{
		var address			= @ref.Uid;
		var asset			= Resources.Load<ResourceRef>($"Fun.Flexy/AssetRefs/{address}");
		var sceneLoadOp		= SceneManager.LoadSceneAsync( asset.Name, new LoadSceneParameters( p.LoadMode, p.PhysicsMode ) );
		var sceneTask		= new LoadSceneTask(context, p, SceneManager.GetSceneAt(SceneManager.sceneCount - 1));

		return sceneTask.Run( SceneLoadWaitImpl(sceneLoadOp, sceneTask) );
	}
}