using System;
using System.Collections.Generic;
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
		
		// used to store cached objects of current SerializedObject our drawer part of
		private Dictionary<String, (AssetRef @ref, Object asset)> _assets = new( );
		
		public override void OnGUI ( Rect position, SerializedProperty property, GUIContent label )
		{
			label = EditorGUI.BeginProperty( position, label, property );
			
			var uidProp			= property.FindPropertyRelative( "_uid" );
			var subIdProp		= property.FindPropertyRelative( "_subId" );
			
			var type			= GetFieldType( fieldInfo );
			var assetRef		= new AssetRef( uidProp.hash128Value, subIdProp.longValue );
			
			if( !_assets.ContainsKey( property.propertyPath ) )
			 	_assets[property.propertyPath] = ( assetRef, AssetRef.EditorLoadAsset( assetRef, type ) );
			
			_assets.TryGetValue( property.propertyPath, out var assetData );

			if( assetData.@ref != assetRef )
				assetData = _assets[property.propertyPath] = ( assetRef, AssetRef.EditorLoadAsset( assetRef, type ) );
			
			var drawPreview		= DrawPreview( uidProp, fieldInfo ); 
			var isInline		= ArrayTableDrawer.DrawingInTableGUI;
			
			if( drawPreview & isInline )
				position.xMin	+= 80;
			
			//EditorGUI.BeginChangeCheck( );
			var newobj		= EditorGUI.ObjectField( position, label, assetData.asset, type, false );
			
			//if( EditorGUI.EndChangeCheck( ) )
			if( newobj != null )
			{
				var @ref		= AssetRef.EditorCreateAssetAddress( newobj );
				
				uidProp.hash128Value	= @ref.Uid; 
				subIdProp.longValue		= @ref.SubId;
				
				_assets[property.propertyPath] = ( @ref, newobj );
				
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
				uidProp.hash128Value			= default; 
				subIdProp.longValue				= default;
				_assets[property.propertyPath]	= default;
			}
			// Validate Reference
			
			EditorGUI.EndProperty( );
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
		
		public static Boolean DrawPreview( SerializedProperty property, FieldInfo fieldInfo )
		{
			var type = GetFieldType( fieldInfo );
			
			return type == typeof(Sprite) && property.hash128Value != default; 
		}
	 
	    public override Single GetPropertyHeight( SerializedProperty property, GUIContent label )
	    {
	        var addressProp		= property.FindPropertyRelative( "_uid" );
			
			if ( DrawPreview( addressProp, fieldInfo ) && !ArrayTableDrawer.DrawingInTableGUI )
	        {
	            return EditorGUI.GetPropertyHeight(addressProp, label, true) + _imageHeight + 10;
	        }
			
			
	        return EditorGUI.GetPropertyHeight(addressProp, label, true);
	    }
	}
}