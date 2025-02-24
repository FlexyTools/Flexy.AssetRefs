using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Flexy.AssetRefs.Editor.PipelineTasks;

public class RunOnBuildPreprocess : IPipelineTask, IPreprocessBuildWithReport
{
	public Int32 callbackOrder { get; }
	public void OnPreprocessBuild( BuildReport report )
	{
		var guids = AssetDatabase.FindAssets("t:Pipeline");
		foreach ( var guid in guids )
		{
			var pipeline = AssetDatabase.LoadAssetAtPath<Pipeline>( AssetDatabase.GUIDToAssetPath( guid ) );
			
			if( pipeline.EnabledTasks[0].Task is RunOnBuildPreprocess )
			{
				Debug.Log( $"[On Pre Build] Run Pipeline: {pipeline.name}" );
				pipeline.RunTasks( );
		
				AssetDatabase.SaveAssets( );
				AssetDatabase.Refresh( );
			}
		}
	}

	public void Run( Pipeline ppl, Context ctx ) { }
}