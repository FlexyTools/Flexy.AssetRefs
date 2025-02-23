namespace Flexy.AssetRefs.Editor.PipelineTasks;

[MovedFrom(true, sourceNamespace:"Flexy.AssetRefs.Pipelines", sourceAssembly:"Flexy.AssetRefs")]
public class AddRefsFromDirectory : IPipelineTask
{
	[SerializeField]	DefaultAsset?	DirectoryOptional;
	[FormerlySerializedAs("TypeName")] 
	[SerializeField]	String			TypeNamesOptional	= "";
	[SerializeField]	Boolean			GoToSubdirectories	= false;
		
	public	void		Run		( Pipeline ppln, Context ctx )		
	{
		var refs		= ctx.Get<RefsList>( );
			
		var currDir		= Path.GetDirectoryName( DirectoryOptional ? AssetDatabase.GetAssetPath( DirectoryOptional )+"/fake" : AssetDatabase.GetAssetPath( ppln ) );
		var noFilter	= String.IsNullOrWhiteSpace( TypeNamesOptional ); 
			
		var types		= TypeNamesOptional.Split( ',', StringSplitOptions.RemoveEmptyEntries ).Select( s => s.Trim( ) ).ToArray( );
		var assetGuids	= new List<String>( );

		if( noFilter )
			assetGuids.AddRange( AssetDatabase.FindAssets( "", new []{ currDir } ) );
		else
			foreach (var t in types) assetGuids.AddRange( AssetDatabase.FindAssets( $"t:{t}", new []{ currDir } ) );	
				
		foreach ( var assetGuid in assetGuids )
		{
			var path	= AssetDatabase.GUIDToAssetPath( assetGuid );
				
			if( !GoToSubdirectories )
				if( Path.GetDirectoryName( path ) != currDir )
					continue;
				
			if( noFilter )
			{
				var asset  = AssetDatabase.LoadMainAssetAtPath( path );
				refs.Add( asset );
			}
			else
			{
				var assets  = AssetDatabase.LoadAllAssetsAtPath( path );
						
				foreach ( var asset in assets )
				{
					var typeName = asset.GetType( ).Name; 
					if( types.Any( t => typeName.Contains( t ) ) )
					{
						refs.Add( asset );
					}
				}
			}
		}
	}
}