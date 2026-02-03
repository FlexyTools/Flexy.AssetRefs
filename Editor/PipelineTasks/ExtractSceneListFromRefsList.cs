namespace Flexy.AssetRefs.Editor.PipelineTasks;

public class ExtractSceneListFromRefsList : IPipelineTask
{
	public void Run( Pipeline ppln, Context ctx )
	{
		var refs		= ctx.Get<RefsList>();
		var scenes		= ctx.Get<SceneList>();
		var sceneList	= refs.Where(r => AssetDatabase.GetMainAssetTypeFromGUID(r.Uid.ToGUID()) == typeof(SceneAsset)).Distinct();

		foreach (var o in sceneList)
			scenes.Add(new SceneRef(o.Uid));
	}
}