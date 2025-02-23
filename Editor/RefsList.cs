using System.Collections;

namespace Flexy.AssetRefs.Editor;

public class RefsList: IEnumerable<Object>, ITasksTabView
{
	private	List<Object> 		_refs		= new( 32 );
	private	HashSet<Object>?	_refsSet	= default;
		
	public	void	Add			( Object? @ref )				
	{
		if( @ref == null )
			return;
			
		if ( @ref is IAssetRefsSource rs )
			foreach ( var r in rs.CollectAssets() )
				_refs.Add( r );
		else
			_refs.Add( @ref );
	}
	public	void	AddRange	( IEnumerable<Object?> list )	
	{
		foreach (var o in list)
			Add( o );
	}
	public	Boolean	Exists		( Object @ref )					
	{
		if( _refsSet == null )
			_refsSet = new( _refs );
			
		return _refsSet.Contains( @ref );
	}
	public	void	Remove		( Object @ref )					
	{
		_refs.Remove( @ref );
	}
	public	void	RemoveAt	( Int32 index )					
	{
		_refs.RemoveAt( index );
	}
	
	public IEnumerator<Object>	GetEnumerator	( )	=> _refs.GetEnumerator( );
	IEnumerator IEnumerable		.GetEnumerator	( )	=> GetEnumerator( );

	public VisualElement		CreateTabGui	( )	
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

	public static class Internal
	{
		public static void ReplaceRefs	( RefsList refs, List<Object> list )
		{
			refs._refs.Clear( );
			refs._refs.AddRange( list );
		}
	}
}