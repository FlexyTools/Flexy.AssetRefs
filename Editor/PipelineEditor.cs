using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Flexy.AssetRefs.Pipelines;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Flexy.AssetRefs.Editor
{
	[CustomEditor( typeof(Pipeline), true), CanEditMultipleObjects]
	public class PipelineEditor : UnityEditor.Editor
	{
		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement { name = "Root" } ;
			InspectorElement.FillDefaultInspector( root, serializedObject , this );
			root.Add( CreatePreviewGui() );
			return root;
		}
	
		private VisualElement			_tabs			= null!;
		private VisualElement			_tabsContent	= null!;
		private VisualElement			_tabControl		= null!;

		public VisualElement CreatePreviewGui( )
		{
			var root	= new VisualElement { name = "Additional UI" };
			var buttons	= new VisualElement { name = "Buttons", style = { flexDirection = FlexDirection.Row, marginBottom = 15} };
		
			buttons.Add( new Button( RunTasks )	{ text = "Run" } );
		
			root.Add( buttons );
		
			_tabControl		= new( );
			_tabs			= new( ){ style = { flexDirection = FlexDirection.Row }};
			_tabsContent	= new( ){ style = { borderTopWidth = 1, borderTopColor = Color.white, marginLeft = 1, marginRight = 1, marginTop = -1, paddingLeft = 1, paddingRight = 1, paddingTop = 5}};
		
			_tabControl.Add( _tabs );
			_tabControl.Add( _tabsContent );
			root.Add( _tabControl );
		
			return root;

			void RunTasks( )
			{
				var ctx = GenericPool<Context>.Get( );
				ctx.Clear( );
				
				try
				{
					((Pipeline)target).RunTasks( ctx ); 
					
					RebuildTabs( ctx );
				}
				finally
				{
					GenericPool<Context>.Release( ctx );
				}
			}
		
			void RebuildTabs( Context ctx )
			{
				_tabs.Clear( );
				_tabsContent.Clear( );

				foreach ( var view in ctx.Values.Where( v => v is ITasksTabView ).Cast<ITasksTabView>( ) )
				{
					var gui = view.CreateTabGui( );
					if( gui != null )
						AddTab( gui.name, view );
				}
			
				_tabs.Add( new(){ style = { flexGrow = 1 } } );
			
				SelectTab( 0 );
			}
		}
	
		private void AddTab( String tabName, ITasksTabView content )
		{
			var index = _tabs.childCount;
			_tabs.Add( new Button( () => SelectTab( index ) ){ text = tabName, style = { borderBottomLeftRadius = 0, borderBottomRightRadius = 0, marginLeft = 0, marginRight = 0}} );
			_tabsContent.Add( new(){ userData =content } );
		}
		private void SelectTab( Int32 index )
		{
			ColorUtility.TryParseHtmlString("#242424", out var color );
			for (var i = 0; i < _tabsContent.childCount; i++)
			{
				_tabs.hierarchy[i].style.borderBottomWidth = new(StyleKeyword.Null);
				_tabs.hierarchy[i].style.borderBottomColor = new(StyleKeyword.Null);
				_tabsContent.hierarchy[i].style.display = DisplayStyle.None;
			}
		
			_tabs.hierarchy[index].style.borderBottomWidth = 2;
			_tabs.hierarchy[index].style.borderBottomColor = Color.white;
			_tabsContent.hierarchy[index].style.display = DisplayStyle.Flex;
			_tabsContent.hierarchy[index].Clear( );
			
			var view = (ITasksTabView)_tabsContent.hierarchy[index].userData;
			
			var gui = view.CreateTabGui( );
			if( gui != null )
				_tabsContent.hierarchy[index].Add( gui );
		}
	}

	[CustomPropertyDrawer(typeof(Pipeline.EnabledTask))]
	public class EnabledTaskDrawer : PropertyDrawer
	{
		private static Type[]		_types	= null!;
		private static String[]		_names	= null!;
	
		public override Boolean CanCacheInspectorGUI(SerializedProperty property) => false;

		public override VisualElement CreatePropertyGUI( SerializedProperty property )
		{
			//var enabledProp = property.FindPropertyRelative("Enabled");
			var taskProp	= property.FindPropertyRelative("Task");
			
			// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
			if (_types == null)
			{
				_types = GetAssignableTypes( GetType( taskProp.managedReferenceFieldTypename ) ).Prepend(null).ToArray()!;
				_names = _types.Select( t => ObjectNames.NicifyVariableName( t?.Name ?? "⦿" ) ).ToArray( );
			}

			var rootVe				= new VisualElement( ){ style = { flexDirection = FlexDirection.Row } };
			var enabledVertical		= new VisualElement( );
			var enabledCheckbox		= new Toggle(){bindingPath = "Enabled", style = { marginLeft = -11, marginRight = 2}};
			var drawerImgui			= new IMGUIContainer( new PropDrawer( taskProp ).OnGui ){ style = { flexGrow = 1}};
			
			enabledVertical.Add( enabledCheckbox );
			rootVe.Add( enabledVertical );
			rootVe.Add( drawerImgui );
			
			return rootVe;
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

		private class PropDrawer
		{
			public PropDrawer(  SerializedProperty prop ) { _prop = prop; }
			private readonly SerializedProperty _prop;
			
			public void OnGui( )
			{
				try
				{
				
					GUILayout.BeginHorizontal( );
				
					var property = _prop.Copy();

					if( property.propertyPath.EndsWith( "]" ) )
					{
						// This is array element
						var start	= property.propertyPath.LastIndexOf('[')+1;
						var index	= property.propertyPath[start..^1];
						GUILayout.Label( $"{index}. ", EditorStyles.boldLabel );
					}
				
					var val		= property.managedReferenceValue;
					var name	= ObjectNames.NicifyVariableName( val?.GetType().Name ?? "None" );

					// Debug.Log(property.CountInProperty());
					// if( property.CountInProperty() == 2 )
					// {
					// 	EditorGUILayout.PrefixLabel( name, EditorStyles.boldLabel );
					// 	DrawProperties( );
					// 	return;
					// }
					// else
					{
						//EditorGUILayout.PrefixLabel( name, null, EditorStyles.boldLabel );
						GUILayout.Label( name, EditorStyles.boldLabel );
					}
				
					//if( val == null )
					{
						GUILayout.FlexibleSpace( );
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
			
				DrawProperties( );
			}

			private void DrawProperties( )
			{
				//Properties
				{
					EditorGUI.indentLevel++;
					var property	= _prop.Copy( );
					var depth		= property.depth;
        			
					for ( var enterChildren = true ; property.NextVisible( enterChildren ) && property.depth > depth; enterChildren = false )
					{
						EditorGUILayout.PropertyField( property );
					}
					EditorGUI.indentLevel--;
				}
        	
				if( _prop.propertyPath.Contains("Array.data") )
					GUILayout.Space( 10 );
			
				_prop.serializedObject.ApplyModifiedProperties( );
				_prop.serializedObject.Update( );
			}
		}
	}
}