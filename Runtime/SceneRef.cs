namespace Flexy.AssetRefs;

[Serializable]
public struct SceneRef : IRefLike, IEquatable<SceneRef>
{
	public	SceneRef ( Hash128 uid )		{ _uid = uid; }
	public	SceneRef ( String refAddress )	{ this = default; FromString( refAddress ); }
		
	[SerializeField] Hash128		_uid;
		
	public			Hash128			Uid					=> _uid;
	public			Boolean			IsNone				=> this == default;
	public static	SceneRef		None				=> default;
	public			AssetRef		Raw					=> new( _uid, 0 );

	public override Int32			GetHashCode			( )									=> _uid.GetHashCode();
	public override	Boolean			Equals				( System.Object obj )				=> obj is SceneRef sr && this == sr;
	public			Boolean			Equals				( SceneRef other )					=> _uid == other._uid;
	public static	Boolean			operator ==			( SceneRef left, SceneRef right )	=> left._uid == right._uid;
	public static	Boolean			operator !=			( SceneRef left, SceneRef right )	=> !(left == right);
		
	public override	String			ToString			( )					=> _uid == default ? String.Empty : _uid.ToString( );
	public			void			FromString			( String address )	=> _uid = String.IsNullOrWhiteSpace( address ) ? default : Hash128.Parse( address );
	
	public static	SceneRef		Parse( String address )		
	{
		if (String.IsNullOrWhiteSpace( address ))
			return default;

		return new SceneRef(Hash128.Parse( address[..32]));
	}

	public	static	SceneLoader		SceneLoader	= new SceneLoader_Resources();
}