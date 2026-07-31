using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Flexy.AssetRefs.Editor.PipelineTasks;

[Serializable]
public class RunOnBuildPreprocess : IPipelineTask, IPreprocessBuildWithReport
{
	public static Boolean DisableRunOnce;
	public Int32 callbackOrder { get; }
	
	public			void	OnPreprocessBuild		( BuildReport report )
	{
		if (DisableRunOnce)
		{
			DisableRunOnce = false;
			return;
		}
	
		var cur = EditorBuildSettings.scenes;
		FindAndRunAssetPipeline();
		
		var collected = EditorBuildSettings.scenes;
		if (collected.Length != cur.Length) 
			ThrowException( cur, collected );
		
		for (var i = 0; i < collected.Length; i++)
		{
			if (collected[i].path.Equals(cur[i].path) && collected[i].enabled == cur[i].enabled)
				continue;
				
			ThrowException( cur, collected );
		}
	}
	private			void	ThrowException			( EditorBuildSettingsScene[] oldScenes, EditorBuildSettingsScene[] newScenes )
	{
		var cur = "\nCurrent Scenes";
		foreach (var sc in oldScenes)
			cur += $"\n{sc.path} enabled:{sc.enabled}";
		
		var collected = "\nCollected Scenes";
		foreach (var sc in newScenes)
			collected += $"\n{sc.path} enabled:{sc.enabled}";
	
		throw new BuildFailedException( "Scene list can not be changed from inside build" + cur + collected );
	}
	public static	void	FindAndRunAssetPipeline	( )
	{
		var guids = AssetDatabase.FindAssets("t:Pipeline");
		foreach (var guid in guids)
		{
			var pipeline = AssetDatabase.LoadAssetAtPath<Pipeline>( AssetDatabase.GUIDToAssetPath(guid) );

			if (pipeline.DisablePipeline || pipeline.EnabledTasks[0].Task is not RunOnBuildPreprocess || !pipeline.EnabledTasks[0].Enabled) 
				continue;
			
			Debug.Log( $"[On Pre Build] Run Pipeline: {pipeline.name}" );
			pipeline.RunTasks();
		
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}

	public void Run( Pipeline ppl, Context ctx ) { }
}