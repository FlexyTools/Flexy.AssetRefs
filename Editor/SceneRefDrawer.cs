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
	public class SceneRefDrawer : PropertyDrawer
	{
		private readonly Dictionary<String, (AssetRef @ref, Object? asset)> _assets = new( );
		
		public override void OnGUI ( Rect position, SerializedProperty property, GUIContent label )
		{
			label = EditorGUI.BeginProperty( position, label, property );
			
			// var arr			= fieldInfo.GetCustomAttributes( typeof(AssetTypeAttribute), true );
			// var attr			= (AssetTypeAttribute)( attribute ?? ( arr.Length > 0 ? arr[0] : null ) );

			var uidProp			= property.FindPropertyRelative( "_uid" );
			var refUid			= uidProp.hash128Value;

			var type			= typeof(SceneAsset);
			
			// 	Action<SearchItem, Boolean> asd = asasd;
			// 	//var qs	= SearchService.ShowPicker( new SearchContext( new []{new SearchProvider("p:")},  "Assets/!GDInfo"), asd );
			
			//Debug.Log			( $"[AssetRefDrawer] - OnGUI: {type}" );

			var assetRef		= new AssetRef( refUid, 0 );
			
			if( !_assets.ContainsKey( property.propertyPath ) )
				_assets[property.propertyPath] = (assetRef, EditorLoadAsset( refUid ) );
			
			_assets.TryGetValue( property.propertyPath, out var assetData );
			
			if( assetData.@ref != assetRef )
				assetData = _assets[property.propertyPath] = ( assetRef, EditorLoadAsset( refUid ) );
			
			if( assetData.asset == null && refUid != default )
				assetData.asset = EditorLoadAsset( refUid );
			
			EditorGUI.BeginChangeCheck( );
			
			var newobj		= EditorGUI.ObjectField( position, label, assetData.asset, type, false );

			if( newobj != assetData.asset )
			{
				_assets[property.propertyPath] = (assetRef, newobj);
				
				if( newobj == null )
					uidProp.hash128Value = default;
				else
					uidProp.hash128Value = new GUID( AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( newobj ) ) ).ToHash( );
			}
			
			// Validate Reference
			
			EditorGUI.EndProperty( );
		}

		private					Object?			EditorLoadAsset			( Hash128 address )		
		{
			if( address == default )
				return null;
			
			var guid = address;
			var path = AssetDatabase.GUIDToAssetPath( guid.ToGUID( ) );
			
			return AssetDatabase.LoadAssetAtPath<Object>( path );
		}
	}
}