using System.Reflection;
using Flexy.Utils.Editor;

namespace Flexy.AssetRefs.Editor
{
	[CustomPropertyDrawer(typeof(AssetRef<>))]
	public class AssetRefDrawer : PropertyDrawer
	{
		const Single CImageHeight = 60;
		
		// used to store cached objects of current SerializedObject our drawer part of
		private readonly Dictionary<String, (AssetRef @ref, Object? asset)> _assets = new( );
		
		public override		void	OnGUI				( Rect position, SerializedProperty property, GUIContent label )	
		{
			OnGUI( position, property, label, GetRefType( fieldInfo ) );
		}
		public override		Single	GetPropertyHeight	( SerializedProperty property, GUIContent label )					
		{
			var addressProp		= property.FindPropertyRelative( "_uid" );
			
			if ( DrawPreview( addressProp, fieldInfo ) && !ArrayTableDrawer.DrawingInTableGUI )
				return EditorGUI.GetPropertyHeight( addressProp, label, true ) + CImageHeight + 10;
			
			return EditorGUI.GetPropertyHeight( addressProp, label, true );
		}
		
		protected			void	OnGUI				( Rect position, SerializedProperty property, GUIContent label, Type type )
		{	
			label				= EditorGUI.BeginProperty( position, label, property );
			
			var uidProp			= property.FindPropertyRelative( "_uid" );
			var subIdProp		= property.FindPropertyRelative( "_subId" );
			
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
			
			EditorGUI.BeginChangeCheck( );
			var newobj		= EditorGUI.ObjectField( position, label, assetData.asset, type, false );
			
			var isChanged = EditorGUI.EndChangeCheck( );
			
			if (newobj is SceneAsset)
			{
				Debug.LogError		( $"[AssetRefDrawer] - OnGUI: Asset type (Scene) not able for AssetRef, use SceneRef" );
				uidProp.hash128Value = default;
				subIdProp.longValue = default;
				EditorGUI.EndProperty( );
				return;
			}
			
			if( isChanged )
			{
				var @ref		= AssetsLoader.EditorGetAssetAddress( newobj );
				
				uidProp.hash128Value			= @ref.Uid; 
				subIdProp.longValue				= @ref.SubId;
				_assets[property.propertyPath]	= ( @ref, newobj );
				
				if( drawPreview )
				{
					var sprite		= newobj as Sprite;
					var tx			= newobj is Sprite sp ? sp.texture : newobj as Texture2D;
					
					if( isInline )
					{
						var spriteRect		= position;
						var isOdd			= ArrayTableDrawer.DrawingArrayElementOnPage % 2 == 0; 
						
						spriteRect.xMin		-= 80;
						spriteRect.width	= 40;
						spriteRect.height	= 40;
						
						if( !isOdd )
						{
							spriteRect.y	-= 20;
							spriteRect.x	+= 40;
						}
						
						if( sprite is not null )
							DrawTexturePreview( spriteRect, sprite );
						else
							GUI.DrawTexture( spriteRect, tx, ScaleMode.ScaleToFit );
					}
					else
					{
						position.y += 5;
		                position.height = CImageHeight + EditorGUI.GetPropertyHeight( property, label, true );
		                
		                if( sprite is not null )
							DrawTexturePreview( position, sprite );
						else
							GUI.DrawTexture( position, tx, ScaleMode.ScaleToFit );
					}
				}
			}
			
			EditorGUI.EndProperty( );
		}
		protected static	void	DrawTexturePreview	( Rect position, Sprite sprite )									
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
		protected static	Type	GetRefType			( FieldInfo fieldInfo )												
		{
			var type = typeof(Object);
			
			if			( type.IsArray )															type = fieldInfo.FieldType.GetElementType()!;
			else if		( type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) )	type = fieldInfo.FieldType.GetGenericArguments()[0];
			
			if( type.IsGenericType )	type = type.GetGenericArguments()[0];
			else						type = typeof(Object);
			
			return type;
		}
		protected static	Boolean	DrawPreview			( SerializedProperty property, FieldInfo fieldInfo )				
		{
			var type = GetRefType( fieldInfo );
			
			return type == typeof(Sprite) && property.hash128Value != default; 
		}
	}
}

#if !FLEXY_UTILS
namespace Flexy.Utils.Editor
{
	public static class ArrayTableDrawer
	{
		public static Boolean	DrawingInTableGUI			{ get; }
		public static Int32		DrawingArrayElementOnPage	{ get; }
	}
}
#endif