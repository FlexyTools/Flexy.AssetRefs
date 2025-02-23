namespace Flexy.AssetRefs.Editor.PipelineTasks;

[MovedFrom(true, sourceNamespace:"Flexy.AssetRefs.Pipelines", sourceAssembly:"Flexy.AssetRefs")]
public class	DistinctRefs			: IPipelineTask	
{
	public	void	Run		( Pipeline ppln, Context ctx )
	{
		var refs			= ctx.Get<RefsList>( );
		var distinctList	= refs.Distinct( ).ToList( );
		
		RefsList.Internal.ReplaceRefs( refs, distinctList );
	} 
}
