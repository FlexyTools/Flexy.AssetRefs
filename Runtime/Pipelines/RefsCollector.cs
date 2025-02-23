#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;

namespace Flexy.AssetRefs.Pipelines;

public static class		RefsCollector		 
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
#endif