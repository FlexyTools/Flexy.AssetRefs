namespace Flexy.AssetRefs.AssetLoaders;

public class SceneLoader_ContentDirectory : SceneLoader
{
	private readonly ContentService _service;

	public SceneLoader_ContentDirectory( ContentService service ) => _service = service;

	protected override	String?			GetSceneName_Impl	( SceneRef @ref )													=> _service.GetContentSceneName		(@ref);
	protected override	LoadSceneTask	LoadSceneAsync_Impl	( SceneRef @ref, LoadSceneTask.Parameters p, GameObject context )	=> _service.LoadContentSceneAsync	(@ref, p, context);
	
	internal static		UniTask<Scene>	WaitSceneLoad		( AsyncOperation ao, LoadSceneTask sceneTask )	=> SceneLoadWaitImpl(ao, sceneTask);
}