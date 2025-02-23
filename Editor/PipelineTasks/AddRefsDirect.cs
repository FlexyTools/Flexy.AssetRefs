namespace Flexy.AssetRefs.Editor.PipelineTasks;

[MovedFrom(true, sourceNamespace:"Flexy.AssetRefs.Pipelines", sourceAssembly:"Flexy.AssetRefs")]
public class AddRefsDirect : IPipelineTask
{
	[FormerlySerializedAs("DirectReferences")]
	[FormerlySerializedAs("Refs")] 
	[SerializeField] Object[]	_refs = { };
		
	public void Run( Pipeline ppln, Context ctx )
	{
		var refs = ctx.Get<RefsList>( );
			
		refs.AddRange( _refs.Where( r => r is not DefaultAsset ) );
	}
}
