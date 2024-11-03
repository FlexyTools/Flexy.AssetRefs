#if UNITY_ADDRESSABLES

namespace Flexy.AssetRefs.AssetLoaders
{
	[CreateAssetMenu( fileName = "Addressables_RefsRemap.remap.asset", menuName = "Flexy/AssetRefs/Addressables/RefsRemap" )]
	public class Addressables_RefsRemap : ScriptableObject, ISerializationCallbackReceiver
	{
		[SerializeField]	AssetRef[]	_keys	= Array.Empty<AssetRef>();
		[SerializeField]	String[]	_values = Array.Empty<String>();
		
		private Dictionary<AssetRef, String> _remap = new( 32 );

		public	void	Add		( AssetRef assetRef, String addressableKey )	
		{
			_remap[assetRef] = addressableKey;
			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty( this );
			#endif
		}
		public	String	Get		( AssetRef assetRef )							
		{
			if( _remap.TryGetValue( assetRef, out var addressableKey ) )
				return addressableKey;
			
			return assetRef.ToString( );
		}
		public	void	Clear	( )												
		{
			_remap.Clear( );
			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty( this );
			#endif
		}
		
		public	void	OnBeforeSerialize	( )		
		{
			var count = _remap.Count;
			
			_keys		= new AssetRef[count];
			_values		= new String[count];
			var i = 0;
			
			foreach (var pair in _remap)
			{
				_keys[i]	= pair.Key;
				_values[i]	= pair.Value;
				i++;
			}
		}
		public	void	OnAfterDeserialize	( )		
		{
			var count = Math.Min( _keys.Length, _values.Length );

			for (var i = 0; i < count; i++)
				_remap[_keys[i]] = _values[i];
		}

		
	}
}

#endif