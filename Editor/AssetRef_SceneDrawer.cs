using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Flexy.AssetRefs.Editor
{
	[CustomPropertyDrawer(typeof(AssetRef_Scene))]
	public class AssetRef_SceneDrawer : PropertyDrawer
	{
		private Object _asset;
		
		public override void OnGUI ( Rect position, SerializedProperty property, GUIContent label )
		{
			Profiler.BeginSample( "AssetRefDrawer" );
			
			// var arr			= fieldInfo.GetCustomAttributes( typeof(AssetTypeAttribute), true );
			// var attr			= (AssetTypeAttribute)( attribute ?? ( arr.Length > 0 ? arr[0] : null ) );

			var addressProp		= property.FindPropertyRelative( "_refAddress" );
			var refAddress		= addressProp.stringValue;

			var type			= typeof(SceneAsset);
			
			// 	Action<SearchItem, Boolean> asd = asasd;
			// 	//var qs	= SearchService.ShowPicker( new SearchContext( new []{new SearchProvider("p:")},  "Assets/!GDInfo"), asd );
			
			//Debug.Log			( $"[AssetRefDrawer] - OnGUI: {type}" );
			
			if( _asset == null )
				_asset = AssetRef.GetSceneResolver( ).EditorLoadAsset( refAddress );
			
			var newobj		= EditorGUI.ObjectField( position, label, _asset, type, false );

			if( newobj != null )
			{
				var resolver	= AssetRef.GetSceneResolver( );
				var path		= resolver.EditorCreateAssetPath( newobj );
				addressProp.stringValue = path;
				_asset = newobj;
			}
			
			// Validate Reference
			
			Profiler.EndSample( );
		}
	}
}