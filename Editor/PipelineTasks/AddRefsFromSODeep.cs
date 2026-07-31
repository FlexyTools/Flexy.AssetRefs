namespace Flexy.AssetRefs.Editor.PipelineTasks;

[Serializable]
public class AddRefsFromSODeep : IPipelineTask
{
	[SerializeField] ScriptableObject _source = null!;

	public void Run( Pipeline ppln, Context ctx )
	{
		if (!_source)
		{
			Debug.LogError($"[{nameof(AddRefsFromSODeep)}] Source ScriptableObject is not assigned.", ppln);
			return;
		}

		ctx.Get<RefsList>().AddRange(RefsCollector.CollectRefsDeep(_source));
	}
}
