namespace Flexy.AssetRefs.Editor.PipelineTasks;

[Serializable]
public class ReadAppRevisionAndBranchFromGit : IPipelineTask
{
	public void Run( Pipeline ppln, Context ctx )
	{
		var buildArtifactsDir	= "Assets/Resources/Fun.Flexy/BuildArtifacts";
		var revisionPath		= buildArtifactsDir + "/Revision.txt";
		var branchPath			= buildArtifactsDir + "/Branch.txt";
		var (revision, branch)	= GetGitRevisionAndBranch();
		
		if (!Directory.Exists(buildArtifactsDir))
			Directory.CreateDirectory(buildArtifactsDir);
		
		File.WriteAllText(revisionPath, revision);
		File.WriteAllText(branchPath, branch);
		
		Debug.Log( $"[Set AppRevision and Branch From Git] - Got revision {revision} and branch {branch}" );
		AssetDatabase.ImportAsset(revisionPath, ImportAssetOptions.ForceSynchronousImport);
		AssetDatabase.ImportAsset(branchPath,	ImportAssetOptions.ForceSynchronousImport);
	}
	
	public static (String?, String?) GetGitRevisionAndBranch( )
	{
		DirectoryInfo? gitDir = null;
		try
		{
			var currentDir = Directory.GetParent(Application.dataPath);
        
			// Search for .git directory up to 3 levels up
			for (var i = 0; i < 3; i++)
			{
				if (currentDir == null)
					break;
                
				var gitPath = Path.Combine(currentDir.FullName, ".git");
				if (Directory.Exists(gitPath))
				{
					gitDir = new DirectoryInfo(gitPath);
					break;
				}
            
				currentDir = currentDir.Parent;
			}
        
			if (gitDir == null)
			{
				Debug.LogWarning("Git repository not found");
				return ("", "");
			}
        
			// Read HEAD file
			var headPath = Path.Combine(gitDir.FullName, "HEAD");
			if (!File.Exists(headPath))
			{
				Debug.LogWarning("HEAD file not found");
				return ("", "");
			}
        
			var headContent = File.ReadAllText(headPath).Trim();
        
			// HEAD contains the revision directly (detached HEAD state)
			if (!headContent.StartsWith("ref: ")) 
				return (headContent, "");

			// HEAD contains a reference. Extract the reference path by removing "ref: " prefix
			var branch		= Path.GetFileName(headContent[5..]);
			var refFilePath	= Path.Combine(gitDir.FullName, headContent[5..]);
            
			if (File.Exists(refFilePath))
				return (File.ReadAllText(refFilePath).Trim(), branch);

			Debug.LogWarning($"Reference file not found: {refFilePath}");
			return ("", "");
		}
		catch (Exception ex)
		{
			Debug.LogError($"Error reading git revision: {ex.Message}");
			return ("", "");
		}
	}
}