namespace Flexy.AssetRefs.Editor.PipelineTasks;

public class ExtractSceneListFromRefsList : IPipelineTask
{
	public void Run( Pipeline ppln, Context ctx )
	{
		var refs		= ctx.Get<RefsList>();
		var scenes		= ctx.Get<SceneList>();
		var sceneList	= refs.Where(r => r is SceneAsset).Distinct().Cast<SceneAsset>();

		foreach (var o in sceneList)
			scenes.Add(o);
	}
}