namespace Flexy.AssetRefs.Editor.PipelineTasks;

public class ResourcesPopulateRefs : IPipelineTask
{
	[SerializeField]	Boolean		AddScenesToBuildSettings;

	public void Run( Pipeline ppln, Context ctx )
	{
		Debug.Log			( $"[ResourcesIRefSourceBuilder] - CreateResourcesAssetForeachAssetRefSource" );
		
		Directory.CreateDirectory( "Assets/Resources/Fun.Flexy/AssetRefs" );
		
		try						
		{
			var refs = ctx.Get<RefsList>();
				
			AssetDatabase.StartAssetEditing();
		
			var ress = refs;
			
			foreach (var @ref in ress)
			{
				if (@ref.IsNone)
				{
					Debug.LogError( $"[ResourcesIRefSourceBuilder] - CreateResourcesAssetForeachAssetRefSource: resource is null in {ppln.name} collector. Skipped", ppln );
					continue;
				}

				var assetAddress	= @ref;
				var path			= $"Assets/Resources/Fun.Flexy/AssetRefs/{assetAddress}.asset";
				
				var rref			= AssetDatabase.LoadAssetAtPath<ResourceRef>(path);
				
				if (rref == null)
				{
					rref = ScriptableObject.CreateInstance<ResourceRef>();
					
					try						{ AssetDatabase.CreateAsset( rref, path ); }
					catch (Exception ex)	{ Debug.LogException(ex); }
				}
				
				rref.Ref = AssetLoader.EditorLoadAssetRaw(@ref);
				
				if (rref.Ref is SceneAsset scn)
					rref.Name = scn.name;
				
				EditorUtility.SetDirty( rref );
				
				if (AddScenesToBuildSettings && rref.Ref is SceneAsset sa)
				{
					var scenesArray	= EditorBuildSettings.scenes;
					var scenePath	= AssetDatabase.GetAssetPath(sa);
					var isAdded		= false;
						
					foreach (var scene in scenesArray)
					{
						if (scene.path == scenePath)
						{
							isAdded = true;
							break;
						}
					}
						
					if (!isAdded)
					{
						var scenes = scenesArray.ToList();
						scenes.Add(new( scenePath, true ));
						EditorBuildSettings.scenes = scenes.ToArray();
					}
				}
				
				if (rref.Ref is not GameObject) // Resources.UnloadAsset dont like GameObjects 
					Resources.UnloadAsset(rref.Ref);
			}
		}
		finally
		{
			AssetDatabase.StopAssetEditing();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		}
	}
}