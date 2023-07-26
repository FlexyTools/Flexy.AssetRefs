using System;
using System.Collections.Generic;
using Flexy.Utils.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs.Editor
{
	[CustomPropertyDrawer(typeof(SceneRef))]
	public class AssetRef_SceneDrawer : PropertyDrawer
	{
		private Dictionary<String, Object> _assets = new( );
		
		public override void OnGUI ( Rect position, SerializedProperty property, GUIContent label )
		{
			Profiler.BeginSample( "AssetRefDrawer" );
			
			// var arr			= fieldInfo.GetCustomAttributes( typeof(AssetTypeAttribute), true );
			// var attr			= (AssetTypeAttribute)( attribute ?? ( arr.Length > 0 ? arr[0] : null ) );

			var uidProp			= property.FindPropertyRelative( "_uid" );
			var refAddress		= uidProp.hash128Value;

			var type			= typeof(SceneAsset);
			
			// 	Action<SearchItem, Boolean> asd = asasd;
			// 	//var qs	= SearchService.ShowPicker( new SearchContext( new []{new SearchProvider("p:")},  "Assets/!GDInfo"), asd );
			
			//Debug.Log			( $"[AssetRefDrawer] - OnGUI: {type}" );
			
			if( !_assets.ContainsKey( property.propertyPath ) )
				_assets[property.propertyPath] = EditorLoadAsset( refAddress );
			
			_assets.TryGetValue( property.propertyPath, out var asset );
			
			if( asset == null && refAddress != default )
				asset = EditorLoadAsset( refAddress );
			
			EditorGUI.BeginChangeCheck( );
			
			var newobj		= EditorGUI.ObjectField( position, label, asset, type, false );

			if( newobj != asset )
			{
				_assets[property.propertyPath] = newobj;
				
				if( newobj == null )
					uidProp.hash128Value = default;
				else
					uidProp.hash128Value = new GUID( AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( newobj ) ) ).ToHash( );
			}
			
			// Validate Reference
			
			Profiler.EndSample( );
		}

		private					Object			EditorLoadAsset			( Hash128 address )		
		{
			if( address == default )
				return null;
			
			var guid = address;
			var path = AssetDatabase.GUIDToAssetPath( guid.ToGUID( ) );
			
			return AssetDatabase.LoadAssetAtPath<Object>( path );
		}
	}
}