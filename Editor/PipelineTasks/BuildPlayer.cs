using UnityEditor.Build.Reporting;

namespace Flexy.AssetRefs.Editor.PipelineTasks;

public class BuildPlayer : IPipelineTask
{
	[SerializeField] private String		_outputDirectory	= "Builds";
	[SerializeField] private String		_buildName			= "MyGame";
	[SerializeField] private Boolean	_disablePreprocessPipelines = false;
	[Header("Options")]
	[SerializeField] private Boolean 	_developmentBuild;
	[Header("Additions")]
	[SerializeField] private Boolean 	_nameAddTime;
	[SerializeField] private Boolean 	_nameAddHash; 

	public void Run( Pipeline ppln, Context ctx )
	{
		var fullBuildName	= $"{_buildName} {Application.version}";
		var outoutExtension = "";
		var outputDirectory = _outputDirectory;
        
#if UNITY_STANDALONE_WIN
		outoutExtension = ".exe";
		outputDirectory = Path.Combine(outputDirectory, fullBuildName);
		fullBuildName	= _buildName;
#elif UNITY_STANDALONE_OSX
		outoutExtension = ".app";
		outputDirectory = Path.Combine(outputDirectory, fullBuildName);
		fullBuildName	= _buildName;
#elif UNITY_STANDALONE_LINUX
		outoutExtension = "";
		outputDirectory = Path.Combine(outputDirectory, fullBuildName);
		fullBuildName	= _buildName;
#elif UNITY_IOS
		outoutExtension = ".ipa"; 
#elif UNITY_ANDROID
		outoutExtension = ".apk";
#elif UNITY_WEBGL
		outoutExtension = "";
		outputDirectory = Path.Combine(outputDirectory, fullBuildName);
		fullBuildName	= _buildName;
#endif
        
        if (_nameAddTime)
			fullBuildName += $" {DateTime.Now.Hour:00}.{DateTime.Now.Minute:00}.{DateTime.Now.Second:00}";
			
        if (_nameAddHash)
			try { fullBuildName += $" {File.ReadAllText($"Assets/Resources/Fun.Flexy/BuildArtifacts/Revision.txt")[..7]}"; } catch {}
        
		var outputPath	= Path.Combine(outputDirectory, fullBuildName);
        outputPath		+= outoutExtension;
        
		if (!Directory.Exists(outputDirectory))
			Directory.CreateDirectory(outputDirectory);
        
		var scenes = ctx.Get<SceneList>().GetBuildScenes();
        
		var buildPlayerOptions = new BuildPlayerOptions
		{
			scenes = scenes,
			locationPathName = outputPath,
			target = EditorUserBuildSettings.activeBuildTarget,
			options = _developmentBuild ? BuildOptions.Development : BuildOptions.None
		};

		if (_disablePreprocessPipelines)
			RunOnBuildPreprocess.DisableRunOnce = true;
			
		var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
		
		if (_disablePreprocessPipelines)
			RunOnBuildPreprocess.DisableRunOnce = false;
		
		var summary = report.summary;
		
		if (summary.result == BuildResult.Succeeded)
		{
			Debug.Log($"Build succeeded: {summary.totalSize} bytes");
			Debug.Log($"Build location: {outputPath}");
			
			if (!Application.isBatchMode)
				Application.OpenURL( outputDirectory );
		}
		else if (summary.result == BuildResult.Failed)
		{
			Debug.LogError("Build failed");
		}
	}
}