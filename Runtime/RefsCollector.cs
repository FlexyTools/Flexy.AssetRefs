using UnityEngine.Serialization;

namespace Flexy.AssetRefs
{
#if UNITY_EDITOR
	
	using System.Collections;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using AssetLoaders;
	using UnityEditor;
	using UnityEditor.Build;
	using UnityEditor.Build.Reporting;
	
	
	[CreateAssetMenu(fileName = "Collector.refs.asset", menuName = "Flexy/AssetRefs/RefsCollector")]
	public class RefsCollector : ScriptableObject
	{
		[FormerlySerializedAs("Enabled")] 
		public Boolean			IsEnabled;
		public Object[]			DirectReferences	= {};
		[SerializeReference]
		public IRefsProcessor[]	Processors			= {};

		public			List			RunProcessors	( Boolean preview = false )		
		{
			if( !IsEnabled )
			{
				foreach ( var processor in Processors ) 
				{
					if (processor == null)
						continue;
				
					processor.ProcessDisabled( this, preview );
				}
				
				return new( 0 );
			}	
			
			var list = new List( 256 ); 
			
			list.AddRange( DirectReferences.Where( r => r is not DefaultAsset ) );

			if ( Processors.Length <= 0 ) 
				return list;
			
			foreach ( var processor in Processors ) 
			{
				if (processor == null)
					continue;
				
				processor.Process( this, list, preview );
			}
			
			return list;
		}
		public static	List<Object>	CollectRefsDeep	( System.Object info )			
		{
			var result	= new List<Object>( );
			var type	= info.GetType(  );
			
			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList(  );

			while (type.BaseType != null && (type.BaseType != typeof(TypeInfo) || type.BaseType != typeof(ScriptableObject) ))
			{
				type = type.BaseType;
				fields.AddRange( type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) );
			}
			
			foreach (var field in fields)
			{
				if (field.FieldType.IsPrimitive | field.FieldType == typeof(String))
					continue;

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
					if (field.IsPublic || field.GetCustomAttribute<SerializeReference>(true) != null || field.GetCustomAttribute<SerializeField>(true) != null)
						result.AddRange( CollectRefsDeep( fieldObj ) );
				}
			}
			
			return result;
		}
		
		public struct List: IEnumerable<Object>
		{
			public List( Int32 capacity ) => _refs = new( capacity );
			private List<Object> 	_refs;
		
			public void Add			( Object? @ref )
			{
				if( @ref == null )
					return;
			
				if( @ref is IAssetRefsSource rs )
				{
					foreach ( var r in rs.CollectAssets( ) )
					{
						_refs.Add( r );
					}	
				}
				else
				{
					_refs.Add( @ref );
				}
			}
			public void AddRange	( IEnumerable<Object?> list ) 
			{
				foreach (var o in list)
					Add( o );
			}
			public void Remove		( Object? @ref ) => _refs.Remove( @ref! );
			public void RemoveAt	( Int32 index ) => _refs.RemoveAt( index );
		
			public IEnumerator<Object> GetEnumerator() => _refs.GetEnumerator( );
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			public static class Internal
			{
				public static void ReplaceRefs	( List refs, List<Object> list )
				{
					refs._refs.Clear( );
					refs._refs.AddRange( list );
				}
			}
		}

		public T? GetProcessor<T>() where T:class,IRefsProcessor
		{
			foreach (var refsProcessor in Processors)
			{
				if( refsProcessor is T result )
					return result;
			}
			
			return default;
		}
	}
	
	public interface IRefsProcessor
	{
		public void Process			( RefsCollector collector, RefsCollector.List refs, Boolean isPreview );
		public void ProcessDisabled ( RefsCollector collector, Boolean isPreview ) { }
	}
	
	public class AddAssetsFromSubCollectors : IRefsProcessor
	{
		[SerializeField]	Boolean			_reverse;
		[SerializeField]	RefsCollector[] _subCollectors = null!;
	
		public void Process( RefsCollector collector, RefsCollector.List refs, Boolean isPreview )
		{
			if( _subCollectors == null )
				return;

			if( _reverse )
				for (var i = _subCollectors.Length - 1; i >= 0; i--)
				{
					var subCollector = _subCollectors[i];
					refs.AddRange(subCollector.RunProcessors(isPreview));
				}
			
			else
				for (var i = 0; i < _subCollectors.Length; i++)
				{
					var subCollector = _subCollectors[i];
					refs.AddRange(subCollector.RunProcessors(isPreview));
				}
		}
	}
	
	public class AddAssetsFromDirectory : IRefsProcessor
	{
		[SerializeField]	DefaultAsset?	DirectoryOptional;
		[FormerlySerializedAs("TypeName")] 
		[SerializeField]	String			TypeNamesOptional	= "";
		[SerializeField]	Boolean			GoToSubdirectories	= false;
		
		public void Process( RefsCollector collector, RefsCollector.List refs, Boolean isPreview )
		{
			var currDir		= Path.GetDirectoryName( DirectoryOptional ? AssetDatabase.GetAssetPath( DirectoryOptional )+"/fake" : AssetDatabase.GetAssetPath( collector ) );
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
	
	public class AddRefsForSceneList : IRefsProcessor
	{
		public void Process( RefsCollector collector, RefsCollector.List refs, Boolean isPreview )
		{
			foreach ( var s in EditorBuildSettings.scenes )
			{
				if ( !s.enabled )
					continue;

				refs.Add( AssetDatabase.LoadAssetAtPath<SceneAsset>( s.path ) );
			}
		}
	}
	
	public class DistinctRefs : IRefsProcessor
	{
		public void Process( RefsCollector collector, RefsCollector.List refs, Boolean isPreview )
		{
			var distinctList = refs.Distinct( ).ToList( );
			RefsCollector.List.Internal.ReplaceRefs( refs, distinctList );
		}
	}
	
	public class ClearSceneRefs : IRefsProcessor
	{
		public void Process( RefsCollector collector, RefsCollector.List refs, Boolean isPreview )
		{
			var distinctList = refs.Where( r => r is not SceneAsset ).ToList( );
			RefsCollector.List.Internal.ReplaceRefs( refs, distinctList );
		}
	}
	
	public class RefsCollectorBuildPreprocessor : IPreprocessBuildWithReport
	{
		public Int32 callbackOrder { get; }
		public void OnPreprocessBuild( BuildReport report )
		{
			Debug.Log			( $"[RefsCollector] - OnPreprocessBuild: PreProcessBuild" );
			
			RunAllProcessors( );
		}
		
		[MenuItem( AssetsLoader_Resources.Editor.Menu + "Collect & Process AssetRefs")]
		public static void RunAllProcessors ( )
		{
			Debug.Log( $"[Flexy.AssetRefs] All RefCollectors - Run Processors" );
			var collectors = GetAllCollectors( );
			
			foreach ( var collector in collectors )
			{
				Debug.Log( $"[Flexy.AssetRefs] {collector.name} - Run Processors" );
				collector.RunProcessors( );
			}
			
			return;

			static List<RefsCollector> GetAllCollectors()
			{
				var getAssets = AssetDatabase.FindAssets( $"t:{typeof(RefsCollector)}" ).Select( guid => (RefsCollector)AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( guid ), typeof(RefsCollector) ) );
				var assetBundleDefinitions = getAssets.Where( asset => asset != null ).ToList( );
				return assetBundleDefinitions;
			}
		}
	}
	
#endif
	
	public interface  IAssetRefsSource
	{
		List<Object> CollectAssets( );
	}
}