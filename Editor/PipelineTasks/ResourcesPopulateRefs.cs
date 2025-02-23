namespace Flexy.AssetRefs.Editor.PipelineTasks;

public class ResourcesPopulateRefs : IPipelineTask
{
	public void Run( Pipeline ppln, Context ctx )
	{
		Debug.Log			( $"[ResourcesIRefSourceBuilder] - CreateResourcesAssetForeachAssetRefSource" );
		
		Directory.CreateDirectory( "Assets/Resources/Fun.Flexy/AssetRefs" );
		
		try						
		{
			var refs = ctx.Get<RefsList>( );
				
			AssetDatabase.StartAssetEditing( );
		
			var ress = refs;
			
			foreach ( var r in ress )
			{
				if ( !r )
				{
					Debug.LogError( $"[ResourcesIRefSourceBuilder] - CreateResourcesAssetForeachAssetRefSource: resource is null in {ppln.name} collector. Skipped", ppln );
					continue;
				}

				var assetAddress	= AssetsLoader.EditorGetAssetAddress( r );
				var path			= $"Assets/Resources/Fun.Flexy/AssetRefs/{assetAddress}.asset";
				
				var rref			= AssetDatabase.LoadAssetAtPath<ResourceRef>( path );
				
				if( rref == null )
				{
					rref = ScriptableObject.CreateInstance<ResourceRef>( );
					
					try						{ AssetDatabase.CreateAsset( rref, path ); }
					catch (Exception ex)	{ Debug.LogException( ex ); }
				}
				
				rref.Ref = r;
				EditorUtility.SetDirty( rref );
				
				if( r is SceneAsset sa )
				{
					rref.Name = sa.name;
						
					var scenesArray	= EditorBuildSettings.scenes;
					var scenePath	= AssetDatabase.GetAssetPath( sa );
					var isAdded		= false;
						
					foreach ( var scene in scenesArray )
					{
						if( scene.path == scenePath )
						{
							isAdded = true;
							break;
						}
					}
						
					if ( !isAdded )
					{
						var scenes = scenesArray.ToList( );
						scenes.Add( new( scenePath, true ) );
						EditorBuildSettings.scenes = scenes.ToArray( );
					}
				}
			}
		}
		finally
		{
			AssetDatabase.StopAssetEditing( );
			AssetDatabase.SaveAssets( );
			AssetDatabase.Refresh( );
		}
	}
}