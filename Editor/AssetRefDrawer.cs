using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs.Editor
{
	[CustomPropertyDrawer(typeof(AssetRef<>))]
	public class AssetRefDrawer : PropertyDrawer
	{
		private Object _asset;
		
		public override void OnGUI ( Rect position, SerializedProperty property, GUIContent label )
		{
			Profiler.BeginSample( "AssetRefDrawer" );
			
			// var arr			= fieldInfo.GetCustomAttributes( typeof(AssetTypeAttribute), true );
			// var attr			= (AssetTypeAttribute)( attribute ?? ( arr.Length > 0 ? arr[0] : null ) );

			var addressProp		= property.FindPropertyRelative( "_refAddress" );
			var refAddress		= addressProp.stringValue;
			
			var type			= fieldInfo.FieldType.GetGenericArguments()[0];
			
			if( _asset == null )
			 	_asset = AssetRef.GetResolver( refAddress )?.EditorLoadAsset( refAddress );
			
			var newobj		= EditorGUI.ObjectField( position, label, _asset, type, false );

			if( newobj != null )
			{
				var resolver	= AssetRef.Editor.GetResolverForType( newobj.GetType(), AssetDatabase.GetAssetPath(newobj) );
				var path		= resolver.EditorCreateAssetPath( newobj );
				addressProp.stringValue = path;
				_asset = newobj;
				
				// var validateResult = resolver.Validate( );
				// if( validateResult != validateResult.None )
				// 	Draw.ErrorBox( $"ref currently is not valid: {validateResult}" );
			}
			
			// Validate Reference
			
			Profiler.EndSample( );
		}

		// private void asasd(SearchItem arg1, Boolean arg2)
		// {
		// 	
		// }
	}
	
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
	
	// [CustomPropertyDrawer(typeof(AssetRefScene))]
	// public class AssetRefSceneDrawer : PropertyDrawer 
	// {
	// 	public override void OnGUI ( Rect position, SerializedProperty property, GUIContent label )
	// 	{
	// 		var guidProp		= property.FindPropertyRelative( AssetRefScene.Internal.GuidFieldName );
	// 		var nameSceneProp	= property.FindPropertyRelative( AssetRefScene.Internal.SceneNameFiledName );
	// 		var guid			= guidProp.stringValue;
	// 		var nameScene		= nameSceneProp.stringValue;
	//
	// 		var obj			= EditorAssetBundlesDelivery.LoadAssetBypassBungles( guid, nameScene );
	// 		var newobj		= (SceneAsset) EditorGUI.ObjectField( position, label, obj, typeof(SceneAsset), false );
	//
	// 		if( obj != newobj )
	// 		{
	// 			guidProp.stringValue = EditorAssetBundlesDelivery.GetAssetRefGuid( newobj );
	// 			nameSceneProp.stringValue = newobj.name;
	// 		}
	// 	}
	// }
}