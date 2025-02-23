namespace Flexy.AssetRefs.Editor
{
	[CustomPropertyDrawer(typeof(SceneRef))]
	public class SceneRefDrawer : PropertyDrawer
	{
		private readonly Dictionary<String, (AssetRef @ref, Object? asset)> _assets = new( );
		
		public override void OnGUI ( Rect position, SerializedProperty property, GUIContent label )
		{
			label = EditorGUI.BeginProperty( position, label, property );
			
			var uidProp			= property.FindPropertyRelative( "_uid" );
			var refUid			= uidProp.hash128Value;
			
			var assetRef		= new AssetRef<SceneAsset>( refUid );
			
			if( !_assets.ContainsKey( property.propertyPath ) )
				_assets[property.propertyPath] = (assetRef, AssetsLoader.EditorLoadAsset( assetRef ) );
			
			_assets.TryGetValue( property.propertyPath, out var assetData );
			
			if( assetData.@ref != assetRef )
				assetData = _assets[property.propertyPath] = ( assetRef, AssetsLoader.EditorLoadAsset( assetRef ) );
			
			if( assetData.asset == null && refUid != default )
				assetData.asset = AssetsLoader.EditorLoadAsset( assetRef );
			
			EditorGUI.BeginChangeCheck( );
			
			var newobj		= EditorGUI.ObjectField( position, label, assetData.asset, typeof(SceneAsset), false );

			if( EditorGUI.EndChangeCheck( ) )
			{
				_assets[property.propertyPath] = (assetRef, newobj);
				
				var @ref = AssetsLoader.EditorGetAssetAddress( newobj );
				uidProp.hash128Value = @ref.Uid;
			}
			
			EditorGUI.EndProperty( );
		}
	}
}