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
	[CustomPropertyDrawer(typeof(AssetRef))]
	[CustomPropertyDrawer(typeof(AssetRef<>))]
	public class AssetRefDrawer : PropertyDrawer
	{
		const Single ImageHeight = 60;
		
		// used to store cached objects of current SerializedObject our drawer part of
		private readonly Dictionary<String, (AssetRef @ref, Object? asset)> _assets = new( );
		
		public override void OnGUI ( Rect position, SerializedProperty property, GUIContent label )
		{
		    if (property.serializedObject.isEditingMultipleObjects)
		    {
		        EditorGUI.BeginProperty(position, label, property);
		        Rect fieldRect = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
		        GUI.Box(fieldRect, "—", EditorStyles.textField);
		        EditorGUI.EndProperty();
		        return;
		    }	
			
			var arr			= fieldInfo.GetCustomAttributes( typeof(AssetTypeAttribute), true );
			var attr		= (AssetTypeAttribute?)( attribute ?? ( arr.Length > 0 ? arr[0] : null ) );
			
			label			= EditorGUI.BeginProperty( position, label, property );
			
			var uidProp			= property.FindPropertyRelative( "_uid" );
			var subIdProp		= property.FindPropertyRelative( "_subId" );
			
			var type			= attr != null ? attr.AssetType : GetRefType( fieldInfo );
			var assetRef		= new AssetRef( uidProp.hash128Value, subIdProp.longValue );
			
			if( !_assets.ContainsKey( property.propertyPath ) )
			 	_assets[property.propertyPath] = ( assetRef, AssetsLoader.EditorLoadAsset( assetRef, type ) );
			
			_assets.TryGetValue( property.propertyPath, out var assetData );

			if( assetData.@ref != assetRef )
				assetData = _assets[property.propertyPath] = ( assetRef, AssetsLoader.EditorLoadAsset( assetRef, type ) );
			
			var drawPreview		= DrawPreview( uidProp, fieldInfo ); 
			var isInline		= ArrayTableDrawer.DrawingInTableGUI;
			
			if( drawPreview & isInline )
				position.xMin	+= 80;
			
			//EditorGUI.BeginChangeCheck( );
			var newobj		= EditorGUI.ObjectField( position, label, assetData.asset, type, false );
			
			if (newobj is SceneAsset)
			{
				Debug.LogError		( $"[AssetRefDrawer] - OnGUI: Asset type (Scene) not able for AssetRef, use AssetRefScene" );
				uidProp.hash128Value = default;
				subIdProp.longValue = default;
				EditorGUI.EndProperty( );
				return;
			}
			
			//if( EditorGUI.EndChangeCheck( ) )
			if( newobj )
			{
				var @ref		= AssetsLoader.EditorGetAssetAddress( newobj );
				
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
		                position.height = ImageHeight + EditorGUI.GetPropertyHeight(property, label, true);
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
		
		private static Type GetRefType( FieldInfo fieldInfo )
		{
			var type = fieldInfo.FieldType;
			
			if			( type.IsArray )															type = fieldInfo.FieldType.GetElementType()!;
			else if		( type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) )	type = fieldInfo.FieldType.GetGenericArguments()[0];
			
			if( type.IsGenericType )	type = type.GetGenericArguments()[0];
			else						type = typeof(Object);
			
			return type;
		}
		
		public static Boolean DrawPreview( SerializedProperty property, FieldInfo fieldInfo )
		{
			var type = GetRefType( fieldInfo );
			
			return type == typeof(Sprite) && property.hash128Value != default; 
		}
	 
	    public override Single GetPropertyHeight( SerializedProperty property, GUIContent label )
	    {
	        var addressProp		= property.FindPropertyRelative( "_uid" );
			
			if ( DrawPreview( addressProp, fieldInfo ) && !ArrayTableDrawer.DrawingInTableGUI )
	        {
	            return EditorGUI.GetPropertyHeight(addressProp, label, true) + ImageHeight + 10;
	        }
			
			
	        return EditorGUI.GetPropertyHeight(addressProp, label, true);
	    }
	}
}