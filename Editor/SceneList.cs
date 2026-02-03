using System.Collections;

namespace Flexy.AssetRefs.Editor;

public class SceneList: IEnumerable<SceneRef>, ITasksTabView
{
	private		List<SceneRef> 		_refs		= new(32);
	private		HashSet<SceneRef>?	_refsSet	= default;
		
	public		void		Add			( SceneRef @ref )					
	{
		if (@ref.IsNone)
			return;
		
		_refs.Add(@ref);
	}
	public		void		AddRange	( IEnumerable<SceneRef> list )		
	{
		foreach (var @ref in list)
			Add(@ref);
	}
	public		Boolean		Exists		( SceneRef @ref )					
	{
		if (_refsSet == null)
			_refsSet = new(_refs);
			
		return _refsSet.Contains(@ref);
	}
	public		void		Remove		( SceneRef @ref )					
	{
		_refs.Remove(@ref);
	}
	public		void		RemoveAt	( Int32 index )						
	{
		_refs.RemoveAt(index);
	}

	public		String[]	GetBuildScenes		( )							
	{
		var buildScenes	= EditorBuildSettings.scenes;
		var scenes		= new List<String>();
        
		foreach (var scene in buildScenes)
			if (scene.enabled)
				scenes.Add(scene.path);
        
		AddScenesToList(scenes);
        
		return scenes.Distinct().ToArray();
	}
	public		void		AddScenesToList		( List<String> scenes )		
	{
		foreach (var scn in _refs)
			scenes.Add( AssetDatabase.GUIDToAssetPath(scn.Uid.ToGUID()) );
	}
	
	public IEnumerator<SceneRef>GetEnumerator	( )	=> _refs.GetEnumerator();
	IEnumerator IEnumerable		.GetEnumerator	( )	=> GetEnumerator();

	public VisualElement		CreateTabGui	( )	
	{
		var collectedRefs		= this.Where( a => !a.IsNone ).Select( a => ( AssetDatabase.GUIDToAssetPath(a.Uid.ToGUID()), a ) ).OrderBy( i => i.Item1 ).ToList();
		var collectedRefsGui	= new VisualElement { name = "Scene List" };
		
		const int itemHeight = 16;
		Func<VisualElement> makeItem			= ()		=>  
		{
			var row = new VisualElement{ style = { flexDirection = FlexDirection.Row }};
			row.Add( new Label {style = { width = 300 } } );
			row.Add( new Label() );
			return row;
		};
		Action<VisualElement, Int32> bindItem	= (e, i)	=>
		{
			(e.hierarchy[0] as Label)!.text		= Path.GetFileName( collectedRefs[i].Item1 );
			(e.hierarchy[1] as Label)!.text		= Path.GetDirectoryName( collectedRefs[i].Item1 );
		};
		
		Label		collectedCount	= new(){ text = $"Count: {collectedRefs.Count}" };
		ListView	previewList		= new( collectedRefs, itemHeight, makeItem, bindItem ) { selectionType = SelectionType.Single };

		previewList.selectionChanged	+= objects => EditorGUIUtility.PingObject( (objects.First() as (String, Object)?)?.Item2  );

		// _previewList.style.flexGrow = 1.0f;
		previewList.style.maxHeight = 800;
		
		collectedRefsGui.Add(collectedCount);
		collectedRefsGui.Add(previewList);
		
		return collectedRefsGui;
	}

	public static class Internal
	{
		public static void ReplaceRefs	( SceneList refs, List<SceneRef> list )
		{
			refs._refs.Clear();
			refs._refs.AddRange(list);
		}
	}
}