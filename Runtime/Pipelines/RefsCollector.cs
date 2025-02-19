using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using UnityEngine.U2D;
using UnityEngine.UIElements;

namespace Flexy.AssetRefs.Pipelines
{
	public interface	IAssetRefsSource	{ List<Object> CollectAssets( ); }
	
	#if UNITY_EDITOR
	
	[CreateAssetMenu(fileName = "RefsBuilder.refs.asset", menuName = "Flexy/AssetRefs/RefsBuilder")]
	public class		RefsCollector		: Pipeline 
	{
		public static	List<Object>	CollectRefsDeep	( System.Object info, params String[]? ignoreFields )			
		{
			var result	= new List<Object>( );
			var type	= info.GetType(  );
			
			var fields	= new List<FieldInfo>( );

			do
			{
				fields.AddRange( type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList( ) );
				type = type.BaseType;
			}
			while ( type != null && type != typeof(ScriptableObject) && type != typeof(MonoBehaviour) );
			
			// DistinctBy
			{
				var uniqueNames = new HashSet<String>( );
				fields = fields.Where( f => uniqueNames.Add(f.Name) ).ToList( );
			}
			
			fields = fields.Where( f => f.FieldType is { IsEnum: false, IsPrimitive: false } && f.FieldType != typeof(String) ).ToList( );
			fields = fields.Where( f => f.IsPublic || f.GetCustomAttribute<SerializeField>(true) != null || f.GetCustomAttribute<SerializeReference>(true) != null ).ToList( ); 
			
			if( ignoreFields != null )
				fields = fields.Where( f => !ignoreFields.Contains( f.Name ) ).ToList( );
			
			foreach (var field in fields)
			{
				var fieldObj = field.GetValue( info );

				if (fieldObj == null)
					continue;
				
				if (fieldObj is IRefLike r1)
				{
					var asset = AssetsLoader.EditorLoadAsset( new( r1.Uid, r1.SubId ), typeof(Object) );
					if( asset != null )
						result.Add( asset );
				}
				else if (fieldObj is IEnumerable enumerable)
				{
					foreach (var e in enumerable)
					{
						var eType = e.GetType( );
					
						if (eType.IsPrimitive | eType == typeof(String))
							continue;
						
						if (e is IRefLike r2)
						{
							var asset = AssetsLoader.EditorLoadAsset( new( r2.Uid, r2.SubId ), typeof(Object) );
							if( asset != null )
								result.Add( asset );
						}
						else
						{
							result.AddRange( CollectRefsDeep( e ) );
						}
					}
				}
				else
				{
					result.AddRange( CollectRefsDeep( fieldObj ) );
				}
			}
			
			return result;
		}
	}
	
	public class RefsList: IEnumerable<Object>, ITasksTabView
	{
		public RefsList( ) {_refs = new( 32 ); _refsSet = default; }
		
		private List<Object> 	_refs;
		private HashSet<Object>? _refsSet;
		
		public void Add			( Object? @ref )
		{
			if( @ref == null )
				return;
			
			if ( @ref is IAssetRefsSource rs )
				foreach ( var r in rs.CollectAssets() )
					_refs.Add( r );
			else
				_refs.Add( @ref );
		}
		public void AddRange	( IEnumerable<Object?> list ) 
		{
			foreach (var o in list)
				Add( o );
		}
		public void Remove		( Object @ref ) => _refs.Remove( @ref );
		public void RemoveAt	( Int32 index ) => _refs.RemoveAt( index );
		
		public IEnumerator<Object> GetEnumerator() => _refs.GetEnumerator( );
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public static class Internal
		{
			public static void ReplaceRefs	( RefsList refs, List<Object> list )
			{
				refs._refs.Clear( );
				refs._refs.AddRange( list );
			}
		}

		public VisualElement	CreateTabGui()
		{
			var collectedRefs		= this.Where( a => a ).Select( a => ( AssetDatabase.GetAssetPath( a ), a ) ).OrderBy( i => i.Item1 ).ToList( );
			var collectedRefsGui	= new VisualElement { name = "Refs List" };
		
			const int itemHeight = 16;
			Func<VisualElement> makeItem			= ()		=>  
			{
				var row = new VisualElement{ style = { flexDirection = FlexDirection.Row }};
				row.Add( new Label {style = { width = 300 } } );
				row.Add( new Label( ) );
				return row;
			};
			Action<VisualElement, Int32> bindItem	= (e, i)	=>
			{
				(e.hierarchy[0] as Label)!.text		= Path.GetFileName( collectedRefs[i].Item1 );
				(e.hierarchy[1] as Label)!.text		= Path.GetDirectoryName( collectedRefs[i].Item1 );
			};
		
			Label			collectedCount	= new( ){ text = $"Count: {collectedRefs.Count}" };
			ListView		previewList		= new( collectedRefs, itemHeight, makeItem, bindItem ) { selectionType = SelectionType.Single };

			previewList.selectionChanged	+= objects => EditorGUIUtility.PingObject( (objects.First( ) as (String, Object)?)?.Item2  );

			// _previewList.style.flexGrow = 1.0f;
			previewList.style.maxHeight = 800;
		
			collectedRefsGui.Add(collectedCount);
			collectedRefsGui.Add(previewList);
		
			return collectedRefsGui;
		}

		public Boolean Exists( Object @ref )
		{
			if( _refsSet == null )
				_refsSet = new( _refs );
			
			return _refsSet.Contains( @ref );
		}
	}

	public interface ITasksTabView
	{
		public VisualElement? CreateTabGui( );
	}
	[MovedFrom(true, sourceNamespace:"Flexy.AssetRefs")]
	public class RunSubTasks : IPipelineTask
	{
		[SerializeField]	Boolean			_reverse;
		[FormerlySerializedAs("_subCollectors")] 
		[SerializeField]	Pipeline?[]	_subTasks = null!;

		public Pipeline?[] SubTasks => _subTasks;

		public void Run(Pipeline ppln, Context ctx)
		{
			if( _reverse )
				for ( var i = _subTasks.Length - 1; i >= 0; i-- )
				{
					var subCollector = _subTasks[i];
					if( subCollector == null )
						continue;
					
					subCollector.RunTasks(ctx);
				}
			
			else
				for (var i = 0; i < _subTasks.Length; i++)
				{
					var subCollector = _subTasks[i];
					if( subCollector == null )
						continue;
					
					subCollector.RunTasks(ctx);
				}
		}
	}
	[MovedFrom(true, sourceNamespace:"Flexy.AssetRefs")]
	public class RunPipeline : IPipelineTask
	{
		[SerializeField]	Pipeline	_pipeline = null!;

		public void Run(Pipeline ppln, Context ctx) => _pipeline.RunTasks(ctx);
	}
	[MovedFrom(true, sourceNamespace:"Flexy.AssetRefs")]
	public class AddAssetsFromDirectory : IPipelineTask
	{
		[SerializeField]	DefaultAsset?	DirectoryOptional;
		[FormerlySerializedAs("TypeName")] 
		[SerializeField]	String			TypeNamesOptional	= "";
		[SerializeField]	Boolean			GoToSubdirectories	= false;
		
		public void Run(Pipeline ppln, Context ctx)
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
	
	[MovedFrom(true, sourceNamespace:"Flexy.AssetRefs")]
	public class AddRefsFromDirectReferences : IPipelineTask
	{
		[FormerlySerializedAs("DirectReferences")] public Object[]		Refs	= {};
		
		public void Run(Pipeline ppln, Context ctx)
		{
			var refs = ctx.Get<RefsList>( );
			
			refs.AddRange( Refs.Where( r => r is not DefaultAsset ) );
		}
	}
	[MovedFrom(true, sourceNamespace:"Flexy.AssetRefs")]
	public class AddRefsDeepFromDirectReferences : IPipelineTask
	{
		[FormerlySerializedAs("DirectReferences")] public Object[]		Refs	= {};
		
		public void Run(Pipeline ppln, Context ctx)
		{
			var refs = ctx.Get<RefsList>( );
			
			refs.AddRange( Refs.Where( r => r is not DefaultAsset ).SelectMany( r => RefsCollector.CollectRefsDeep( r ) ) );
		}
	}
	[MovedFrom(true, sourceNamespace:"Flexy.AssetRefs")]
	public class	AddRefsForSceneList		: IPipelineTask	
	{ 
		public void Run(Pipeline ppln, Context ctx)
		{
			var refs	= ctx.Get<RefsList>( );
		
			try
			{
				AssetDatabase.StartAssetEditing( );
				
				foreach ( var pair in SpriteUtils.GetAllSpritesInAtlases( refs ) )
				{
					var importer = (TextureImporter)AssetImporter.GetAtPath( AssetDatabase.GetAssetPath( pair.sprite ) );
					
					if( importer.textureCompression == TextureImporterCompression.Uncompressed )
						continue;
					
					importer.textureCompression = TextureImporterCompression.Uncompressed;
					importer.SaveAndReimport( );
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing( );
			}
		}
	}
	
	
	public class RemoveMeshesFromSpriteImports : IPipelineTask
	{
		public void Run(Pipeline ppln, Context ctx)
		{
			var refs	= ctx.Get<RefsList>( );
		
			try
			{
				AssetDatabase.StartAssetEditing( );
				
				foreach ( var @ref in refs.ToArray( ) )
				{
					if( @ref is not Sprite sprite )
						continue;
				
					var importer = (TextureImporter)AssetImporter.GetAtPath( AssetDatabase.GetAssetPath( sprite ) );
					var settings = new TextureImporterSettings( );
					
					importer.ReadTextureSettings( settings );
					
					if( settings is { spriteGenerateFallbackPhysicsShape: false, spriteMeshType: SpriteMeshType.FullRect } )  
						continue;
					
					settings.spriteGenerateFallbackPhysicsShape = false;
					settings.spriteMeshType = SpriteMeshType.FullRect;
					
					importer.SetTextureSettings( settings );
					importer.SaveAndReimport( );
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing( );
			}
		}
	}
	
	public static class SpriteUtils
	{
		public static IEnumerable<(SpriteAtlas atlas,Sprite sprite)> GetAllSpritesInAtlases( RefsList refs )
		{
			foreach ( var @ref in refs.ToArray( ) )
			{
				if( @ref is not SpriteAtlas atlas )
					continue;

				foreach ( var packable in UnityEditor.U2D.SpriteAtlasExtensions.GetPackables( atlas ) )
				{
					{
						if (packable is Sprite s)
						{
							yield return (atlas, s);
						}
						else if (packable is Texture2D texture)
						{
							var assetPath = AssetDatabase.GetAssetPath(texture);
							var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
							foreach (var subAsset in subAssets)
							{
								if (subAsset is Sprite subSprite)
								{
									yield return (atlas, subSprite);
								}
							}
						}
						else if (packable is DefaultAsset directoryAsset)
						{
							var directoryPath = AssetDatabase.GetAssetPath(directoryAsset);
							var spriteGUIDs = AssetDatabase.FindAssets("t:Sprite", new[] { directoryPath });
							
							foreach (var guid in spriteGUIDs)
							{
								var spritePath = AssetDatabase.GUIDToAssetPath(guid);
								var subAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
								
								foreach (var subAsset in subAssets)
								{
									if (subAsset is Sprite subSprite)
									{
										yield return (atlas, subSprite);
									}
								}
							}
						}
					}
				}
			}
		}
	}
	
	[MovedFrom(true, sourceNamespace:"Flexy.AssetRefs")]
	public class	DistinctRefs			: IPipelineTask	{ public void Run(Pipeline ppln, Context ctx)
	{
		var refs = ctx.Get<RefsList>( );
		
		var distinctList = refs.Distinct( ).ToList( );
		RefsList.Internal.ReplaceRefs( refs, distinctList );
	} }
	[MovedFrom(true, sourceNamespace:"Flexy.AssetRefs")]
	public class	ClearSceneRefs			: IPipelineTask	{ public void Run(Pipeline ppln, Context ctx)
	{
		var refs = ctx.Get<RefsList>( );
		
		var distinctList = refs.Where( r => r is not SceneAsset ).ToList( );
		RefsList.Internal.ReplaceRefs( refs, distinctList );
	} }
	
	// public class RefsCollectorBuildPreprocessor : IPreprocessBuildWithReport
	// {
	// 	public Int32 callbackOrder { get; }
	// 	public void OnPreprocessBuild( BuildReport report )
	// 	{
	// 		Debug.Log			( $"[RefsCollector] - OnPreprocessBuild: PreProcessBuild" );
	// 		
	// 		RunAllProcessors( );
	// 	}
	// 	
	// 	// [MenuItem( AssetsLoader_Resources.Editor.Menu + "Collect & Process AssetRefs")]
	// 	// public static void RunAllProcessors ( )
	// 	// {
	// 	// 	Debug.Log( $"[Flexy.AssetRefs] All RefCollectors - Run Processors" );
	// 	// 	var collectors = GetAllCollectors( );
	// 	// 	
	// 	// 	foreach ( var collector in collectors )
	// 	// 	{
	// 	// 		Debug.Log( $"[Flexy.AssetRefs] {collector.name} - Run Processors" );
	// 	// 		collector.RunTasks( );
	// 	// 	}
	// 	// 	
	// 	// 	return;
	// 	//
	// 	// 	static List<RefsCollector> GetAllCollectors()
	// 	// 	{
	// 	// 		var getAssets = AssetDatabase.FindAssets( $"t:{typeof(RefsCollector)}" ).Select( guid => (RefsCollector)AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( guid ), typeof(RefsCollector) ) );
	// 	// 		var assetBundleDefinitions = getAssets.Where( asset => asset != null ).ToList( );
	// 	// 		return assetBundleDefinitions;
	// 	// 	}
	// 	// }
	// }
	
	#endif
}