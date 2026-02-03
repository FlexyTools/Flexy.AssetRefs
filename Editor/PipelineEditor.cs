using System.Reflection;
using UnityEditor.UIElements;
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
	
		private VisualElement	_tabs			= null!;
		private VisualElement	_tabsContent	= null!;
		private VisualElement	_tabControl		= null!;

		public	VisualElement	CreatePreviewGui	( )	
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
	
		private void AddTab		( String tabName, ITasksTabView content )	
		{
			var index = _tabs.childCount;
			_tabs.Add( new Button( () => SelectTab( index ) ){ text = tabName, style = { borderBottomLeftRadius = 0, borderBottomRightRadius = 0, marginLeft = 0, marginRight = 0}} );
			_tabsContent.Add( new(){ userData =content } );
		}
		private void SelectTab	( Int32 index )								
		{
			if( _tabs.hierarchy.childCount <= index || _tabsContent.hierarchy.childCount <= index )
				return;
			
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
		public override Boolean			CanCacheInspectorGUI	( SerializedProperty property )		=> false;
		public override VisualElement	CreatePropertyGUI		( SerializedProperty property )		
		{
			var foldout = new Foldout
			{
				text = property.displayName,
				pickingMode = PickingMode.Ignore, 
				bindingPath = property.propertyPath,
			};
			
			var toggle = foldout.Q<Toggle>(null, Foldout.toggleUssClassName);
			var toggleRow = toggle.hierarchy[0];
			var checkmark = toggleRow.hierarchy[0];
			var toggleLabel = toggleRow.hierarchy[1];
			
			toggle.style.marginTop = 0;
			toggle.style.marginBottom = 0;
			toggleLabel.style.display = DisplayStyle.None;
			checkmark.style.display = DisplayStyle.None;
			foldout.value = true;
			
			var header	= new VisualElement( ){ style = { flexDirection = FlexDirection.Row, flexGrow = 1} };
			
			toggleRow.hierarchy.Add(header);
							
			BuildUI(property, foldout, header);
			
			foldout.TrackPropertyValue(property, _ => BuildUI(property, foldout, header));
			
			return foldout;
		}
		
		private			void			BuildUI					( SerializedProperty property, Foldout foldout, VisualElement header )
		{
			header	.Unbind	();
			header	.Clear	();
			foldout.contentContainer.Unbind	();
			foldout.contentContainer.Clear	();

			var taskProp	= property.FindPropertyRelative("Task");
			var val			= taskProp.managedReferenceValue;
			var displayName	= ObjectNames.NicifyVariableName( val?.GetType().Name ?? "None" );
			
			if( taskProp.propertyPath.EndsWith( "]" ) )
			{
				// This is array element
				var start	= taskProp.propertyPath.LastIndexOf('[')+1;
				var index	= taskProp.propertyPath[start..^1];
				displayName = $"{index}. " + displayName;
			}
			
			var enabledCheckbox		= new Toggle( ){bindingPath = property.FindPropertyRelative("Enabled").propertyPath, style = { marginLeft = 0, marginRight = 5}};
			var nameLabel			= new Label( displayName ){style = {unityFontStyleAndWeight = FontStyle.Bold, alignSelf = Align.Center } };
			var button				= new Button { text = "⦿", style = { width = 20, paddingLeft = 0, paddingRight = 0, marginLeft = 0, marginRight = -2} };
			
			enabledCheckbox.RegisterValueChangedCallback( v => foldout.value = true );
			
			button.clicked += () =>
			{
				var types = GetAssignableTypes( GetType( taskProp.managedReferenceFieldTypename ) ).ToArray()!;
				var names = types.Select( t => ObjectNames.NicifyVariableName( t?.Name ) ).ToArray();
			
				var gm = new GenericMenu();

				for (var i = 0; i < names.Length; i++)
					gm.AddItem(new GUIContent(names[i]), false, Choose, (i, taskProp, types));
			
				gm.DropDown(button.worldBound);
				
				static void Choose(System.Object userData)
				{
					var (newIndex, taskProp, types) = ((Int32, SerializedProperty, Type[]))userData;
					
					taskProp.managedReferenceValue = Activator.CreateInstance( types[newIndex] );
					taskProp.serializedObject.ApplyModifiedProperties();
					taskProp.serializedObject.Update();
				}
			};
			var flexibleSpace		= new VisualElement { style = { flexGrow = 1}};
			
			header.Add( enabledCheckbox );
			header.Add( nameLabel );
			header.Add( flexibleSpace );
			header.Add( button );
			
			var subProp	= taskProp.Copy();
			var depth	= subProp.depth;
        			
			for ( var enterChildren = true ; subProp.NextVisible(enterChildren) && subProp.depth > depth; enterChildren = false )
				foldout.contentContainer.Add( new PropertyField(subProp) );
			
			foldout.BindProperty( property );
			foldout.value = true;
		}
		
		private static	Type			GetType					( String typename )		
		{
			var parts		= typename.Split( ' ' );
			return Type.GetType( $"{parts[1]}, {parts[0]}", false );
		}
		private static	List<Type>		GetAssignableTypes		( Type type )			
		{
			var nonUnityTypes	= TypeCache.GetTypesDerivedFrom(type).Where(IsAssignableNonUnityType).ToList();
			nonUnityTypes.Sort( (l, r) => String.Compare( l.FullName, r.FullName, StringComparison.Ordinal) );
			return nonUnityTypes;
        
			Boolean IsAssignableNonUnityType(Type type)
			{
				return ( type.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface ) && !type.IsSubclassOf(typeof(UnityEngine.Object)) && type.GetCustomAttributes().All( a => !a.GetType().Name.Contains( "BakingType" )  );
			}
		}
	}
}
