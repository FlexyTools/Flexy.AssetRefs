using System.Reflection;

namespace Flexy.AssetRefs;

[Serializable]
public struct SceneRef : IRefLike, IEquatable<SceneRef>
{
	public	SceneRef ( Hash128 uid )		{ _uid = uid; }
	public	SceneRef ( String refAddress )	{ this = default; FromString( refAddress ); }
		
	[SerializeField] Hash128		_uid;
		
	public			Hash128			Uid				=> _uid;
	public			Boolean			IsNone			=> this == default(SceneRef);
	public static	SceneRef		None			=> default;
	public			AssetRef		Raw				=> new( _uid, 0 );

	public	static	SceneLoader		SceneLoader		= new SceneLoader_Resources();

	public override Int32			GetHashCode		( )									=> _uid.GetHashCode();
	public override	Boolean			Equals			( System.Object? obj )				=> obj is SceneRef sr && this == sr;
	public			Boolean			Equals			( SceneRef other )					=> _uid == other._uid;
	public static	Boolean			operator ==		( SceneRef left, SceneRef right )	=> left._uid == right._uid;
	public static	Boolean			operator !=		( SceneRef left, SceneRef right )	=> !(left == right);
		
	public override	String			ToString		( )					=> _uid == default ? String.Empty : _uid.ToString( );
	public			void			FromString		( String address )	=> _uid = String.IsNullOrWhiteSpace( address ) ? default : Hash128.Parse( address );
	
	public static	SceneRef		Parse			( String address )	
	{
		if (String.IsNullOrWhiteSpace( address ))
			return default;

		return new SceneRef(Hash128.Parse( address[..32]));
	}
	
	public static 	Boolean 		operator ==		( Scene s, SceneRef sr ) => s.GetRef() == sr;
	public static 	Boolean 		operator ==		( SceneRef sr, Scene s ) => sr == s.GetRef();
	public static 	Boolean 		operator !=		( Scene s, SceneRef sr ) => !(s == sr);
	public static 	Boolean 		operator !=		( SceneRef sr, Scene s ) => !(sr == s);
}

public static class SceneExt
{
	static SceneExt( )
	{
		var getGuidMI = typeof(Scene).GetMethod("GetGUIDInternal", BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
		var setGuidMI = typeof(Scene).GetMethod("SetPathAndGUIDInternal", BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
	
		#if UNITY_6000_3_OR_NEWER
		_getGuid = (Func<SceneHandle, String>)			Delegate.CreateDelegate(typeof(Func<SceneHandle, String>), getGuidMI);
		_setGuid = (Action<SceneHandle, String, String>)Delegate.CreateDelegate(typeof(Action<SceneHandle, String, String>), setGuidMI);
		#else	
		_getGuid = (Func<Int32, String>)			Delegate.CreateDelegate(typeof(Func<Int32, String>), getGuidMI);
		_setGuid = (Action<Int32, String, String>)	Delegate.CreateDelegate(typeof(Action<Int32, String, String>), setGuidMI);
		#endif
	}
	
	#if UNITY_6000_3_OR_NEWER
	private static readonly Func<SceneHandle, String>			_getGuid;
	private static readonly Action<SceneHandle, String, String>	_setGuid;
	#else
	private static readonly Func<Int32, String>				_getGuid;
	private static readonly Action<Int32, String, String>	_setGuid;
	#endif
	
	public static	String		GetGuid		( this Scene scene )				=> _getGuid(scene.handle);
	public static	void		SetGuid		( this Scene scene, String guid)	=> _setGuid(scene.handle, scene.path, guid);
	public static	SceneRef	GetRef		( this Scene scene )				=> SceneRef.Parse(_getGuid(scene.handle));
}