using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Label = UnityEngine.UIElements.Label;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs.Editor;

[CustomEditor( typeof(RefsCollector), true), CanEditMultipleObjects]
public class RefsCollectorEditor : UnityEditor.Editor
{
	public override VisualElement CreateInspectorGUI()
	{
		var root = new VisualElement { name = "Root" } ;
		InspectorElement.FillDefaultInspector( root, serializedObject , this );
		root.Add( CreatePreviewGui() );
		return root;
	}
	
	private List<(String,Object)>	_previewObjects = new( );
	private Vector2					_scrollPosition;
	private ListView				_previewList = null!;
	private Label					_collectedCount = null!;
	
	public VisualElement CreatePreviewGui( )
	{
		var root = new VisualElement { name = "Additional UI" };
		
		var buttons = new VisualElement { name = "Buttons", style = { flexDirection = FlexDirection.Row, marginBottom = 15} };
		
		buttons.Add( new Button( ButtonPreview_Clicked )		{ text = "Preview"			} );
		buttons.Add( new Button( ButtonRunProcesors_Clicked )	{ text = "Run Processors"	} );
		
		root.Add( buttons );
		
		const int itemHeight = 16;
		Func<VisualElement> makeItem			= ()		=>  
		{
			var row = new VisualElement{ style = { flexDirection = FlexDirection.Row }};
			row.Add( new Label( ){style = { width = 300 } } );
			row.Add( new Label( ) );
			return row;
		};
		Action<VisualElement, Int32> bindItem	= (e, i)	=>
		{
			(e.hierarchy[0] as Label)!.text		= Path.GetFileName( _previewObjects[i].Item1 );
			(e.hierarchy[1] as Label)!.text		= Path.GetDirectoryName( _previewObjects[i].Item1 );
		};
		
		_collectedCount	= new( );
		_previewList	= new( _previewObjects, itemHeight, makeItem, bindItem ) { selectionType = SelectionType.Single };

		_previewList.selectionChanged	+= objects => EditorGUIUtility.PingObject( (objects.First( ) as (String, Object)?)?.Item2  );

		// _previewList.style.flexGrow = 1.0f;
		_previewList.style.maxHeight = 800;

		
		root.Add(_collectedCount);
		root.Add(_previewList);
		
		void ButtonRunProcesors_Clicked	( ) => ((RefsCollector)target).RunProcessors( );
		void ButtonPreview_Clicked		( ) 
		{
			_previewObjects.Clear( ); 
			_previewObjects.AddRange( ((RefsCollector)target).RunProcessors( true ).Where( a => a ).Select( a => ( AssetDatabase.GetAssetPath( a ), a ) ).OrderBy( i => i.Item1 ) ); 
			_previewList.RefreshItems( ); 
			_collectedCount.text = $"Count: {_previewObjects.Count}"; 
		}
		
		return root;
	}
}

[CustomPropertyDrawer(typeof(IRefsProcessor))]
public class RefsProcessorDrawer : PropertyDrawer
{
	private static Type[]		_types	= null!;
	private static String[]		_names	= null!;
	
	public override Boolean CanCacheInspectorGUI(SerializedProperty property) => false;

	public override VisualElement CreatePropertyGUI( SerializedProperty property )
	{
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (_types == null)
		{
			_types = GetAssignableTypes( GetType( property.managedReferenceFieldTypename ) ).Prepend(null).ToArray()!;
			_names = _types.Select( t => ObjectNames.NicifyVariableName( t?.Name ?? "⦿" ) ).ToArray( );
		}
		
		var drawer = new PropDrawer( property );
		return new IMGUIContainer( drawer.OnGui ){ style = { marginLeft = -8}};
	}
	
	private static Type			GetType				( String typename )		
	{
		var parts		= typename.Split( ' ' );
		return Type.GetType( $"{parts[1]}, {parts[0]}", false );
	}
	private static List<Type>	GetAssignableTypes	( Type type )			
	{
		var nonUnityTypes	= TypeCache.GetTypesDerivedFrom(type).Where(IsAssignableNonUnityType).ToList();
		nonUnityTypes.Sort( (l, r) => String.Compare( l.FullName, r.FullName, StringComparison.Ordinal) );
		nonUnityTypes.Insert(0, null);
		return nonUnityTypes;
        
		Boolean IsAssignableNonUnityType(Type type)
		{
			return ( type.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface ) && !type.IsSubclassOf(typeof(UnityEngine.Object)) && type.GetCustomAttributes().All( a => !a.GetType().Name.Contains( "BakingType" )  );
		}
	}

	private record class PropDrawer( SerializedProperty Prop )
	{
		public void OnGui( )
		{
			try
			{
				
				GUILayout.BeginHorizontal( );
				
				var property = Prop.Copy();

				if( property.propertyPath.EndsWith( "]" ) )
				{
					// This is array element
					var start	= property.propertyPath.LastIndexOf('[')+1;
					var index	= property.propertyPath[start..^1];
					GUILayout.Label( $"{index}. ", EditorStyles.boldLabel );
				}
				
				var val		= property.managedReferenceValue;
				var name	= ObjectNames.NicifyVariableName( val?.GetType().Name ?? "None" );
        			
				GUILayout.Label( name, EditorStyles.boldLabel );
				GUILayout.FlexibleSpace( );
				
				if( val == null )
				{
					var newIndex = EditorGUILayout.Popup( 0, _names, GUILayout.Width( 35 ) );
        				
					if( newIndex != 0 )
					{
						property.managedReferenceValue = Activator.CreateInstance( _types[newIndex] );
						property.serializedObject.ApplyModifiedProperties( );
						property.serializedObject.Update( );
					}
				}
			}
			catch
			{
				return;
			}
			finally
			{
				GUILayout.EndHorizontal( );
			}
			
			//Properties
			{
				EditorGUI.indentLevel++;
				var property	= Prop.Copy( );
				var depth		= property.depth;
        			
				for ( var enterChildren = true ; property.NextVisible( enterChildren ) && property.depth > depth; enterChildren = false )
				{
					EditorGUILayout.PropertyField( property );
				}
				EditorGUI.indentLevel--;
			}
        	
			if( Prop.propertyPath.Contains("Array.data") )
				GUILayout.Space( 10 );
			
			Prop.serializedObject.ApplyModifiedProperties( );
			Prop.serializedObject.Update( );
		}
	}
}