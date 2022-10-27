using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Flexy.Utils.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs.Editor
{
	[CustomPropertyDrawer(typeof(AssetRef<>))]
	public class AssetRefDrawer : PropertyDrawer
	{
		const Single _imageHeight = 60;
		
		private Dictionary<String, Object> _assets = new Dictionary<String, Object>( );
		
		public override void OnGUI ( Rect position, SerializedProperty property, GUIContent label )
		{
			Profiler.BeginSample( "AssetRefDrawer" );
			
			// var arr			= fieldInfo.GetCustomAttributes( typeof(AssetTypeAttribute), true );
			// var attr			= (AssetTypeAttribute)( attribute ?? ( arr.Length > 0 ? arr[0] : null ) );

			
			var addressProp		= property.FindPropertyRelative( "_refAddress" );
			var refAddress		= addressProp.stringValue;
			
			var type			= GetFieldType( fieldInfo );
			
			if( !_assets.ContainsKey( property.propertyPath ) )
			 	_assets[property.propertyPath] = AssetRef.GetAssetResolver( )?.EditorLoadAsset( refAddress, type );
			
			_assets.TryGetValue( property.propertyPath, out var asset );

			var drawPreview		= DrawPreview( addressProp, fieldInfo ); 
			var isInline		= ArrayTableDrawer.DrawingInTableGUI;
			
			if( drawPreview & isInline )
				position.xMin	+= 80;
			
			//EditorGUI.BeginChangeCheck( );
			var newobj		= EditorGUI.ObjectField( position, label, asset, type, false );
			
			//if( EditorGUI.EndChangeCheck( ) )
			if( newobj != null )
			{
				var resolver	= AssetRef.GetAssetResolver( );
				var path		= resolver.EditorCreateAssetPath( newobj );
				
				addressProp.stringValue = path;
				_assets[property.propertyPath] = newobj;
				
				// var validateResult = resolver.Validate( );
				// if( validateResult != validateResult.None )
				// 	Draw.ErrorBox( $"ref currently is not valid: {validateResult}" );
				if( drawPreview )
				{
					var sprite		= newobj as Sprite;
					var tx			= newobj is Sprite sp ? sp.texture : newobj as Texture2D;
					
					if( isInline )
					{
						var spriteRect		= position;
						var isOdd			= ArrayTableDrawer.DrawingArrayElementOnPage % 2 == 0; 
						
						//Debug.Log			( $"[AssetRefDrawer] - OnGUI: {(Int32)spriteRect.y}" );
						
						spriteRect.xMin		-= 80;
						spriteRect.width	= 40;
						spriteRect.height	= 40;
						
						if( !isOdd )
						{
							spriteRect.y	-= 20;
							spriteRect.x	+= 40;
						}
						
						if( sprite is {} )
							DrawTexturePreview(spriteRect, sprite );
						else
							GUI.DrawTexture(spriteRect, tx, ScaleMode.ScaleToFit);
					}
					else
					{
						position.y += 5;
		                position.height = _imageHeight + EditorGUI.GetPropertyHeight(property, label, true);
		                //EditorGUI.DrawPreviewTexture(position, sprite.texture, null, ScaleMode.ScaleToFit, 0);
		                if( sprite is {} )
							DrawTexturePreview(position, sprite );
						else
							GUI.DrawTexture(position, tx, ScaleMode.ScaleToFit);
					}
				}
			}
			else
			{
				addressProp.stringValue = null;
				_assets[property.propertyPath] = null;
			}
			// Validate Reference
			
			Profiler.EndSample( );
		}

		private void DrawTexturePreview(Rect position, Sprite sprite)
        {
            var fullSize	= new Vector2(sprite.texture.width, sprite.texture.height);
            var size		= new Vector2(sprite.textureRect.width, sprite.textureRect.height);
 
            var coords = sprite.textureRect;
            coords.x /= fullSize.x;
            coords.width /= fullSize.x;
            coords.y /= fullSize.y;
            coords.height /= fullSize.y;
 
            Vector2 ratio;
            ratio.x = position.width / size.x;
            ratio.y = position.height / size.y;
            var minRatio = Mathf.Min(ratio.x, ratio.y);
 
            var center = position.center;
            position.width = size.x * minRatio;
            position.height = size.y * minRatio;
            position.center = center;
 
            GUI.DrawTextureWithTexCoords(position, sprite.texture, coords);
        }
		
		private static Type GetFieldType( FieldInfo fieldInfo )
		{
			var type			= default(Type);
			
			if( fieldInfo.FieldType.IsArray )
				type = fieldInfo.FieldType.GetElementType()?.GetGenericArguments()[0];
			
			else if( fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>) )
				type = fieldInfo.FieldType.GetGenericArguments()[0].GetGenericArguments()[0];
					 
			else
				type = fieldInfo.FieldType.GetGenericArguments()[0];
			
			return type;
		}
		
		// private void asasd(SearchItem arg1, Boolean arg2)
		// {
		// 	
		// }
		public static Boolean DrawPreview( SerializedProperty property, FieldInfo fieldInfo )
		{
			var type			= GetFieldType( fieldInfo );
			
			return type == typeof(Sprite) && property.stringValue is {} str && !String.IsNullOrWhiteSpace( str ); 
		}
	 
	    public override float GetPropertyHeight( SerializedProperty property, GUIContent label )
	    {
	        var addressProp		= property.FindPropertyRelative( "_refAddress" );
			
			if ( DrawPreview( addressProp, fieldInfo ) && !ArrayTableDrawer.DrawingInTableGUI )
	        {
	            return EditorGUI.GetPropertyHeight(addressProp, label, true) + _imageHeight + 10;
	        }
			
			
	        return EditorGUI.GetPropertyHeight(addressProp, label, true);
	    }
	}
}