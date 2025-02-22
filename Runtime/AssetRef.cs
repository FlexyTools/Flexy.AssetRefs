global using System;
global using System.Collections.Generic;

global using UnityEngine;
global using UnityEngine.SceneManagement;
global using UnityEngine.Pool;

global using Cysharp.Threading.Tasks;

global using Flexy.AssetRefs.AssetLoaders;
global using Flexy.Serialisation;

global using Object = UnityEngine.Object;

namespace Flexy.AssetRefs;

//[DebuggerDisplay("{ToString()} = {AssetsLoader.EditorLoadAsset()}")]
[Serializable]
public struct AssetRef<T> : IRefLike, ISerializeAsString, IEquatable<AssetRef<T>> where T: Object
{
	public	AssetRef ( Hash128 uid, Int64 subId = 0 )	{ _uid = uid; _subId = subId; }
	public	AssetRef ( String refAddress )				{ this = default; FromString( refAddress ); }
	
	[SerializeField] Hash128	_uid;
	[SerializeField] Int64		_subId;
	
	public			Hash128		Uid			=> _uid;
	public			Int64		SubId		=> _subId;
	public			Boolean		IsNone		=> this == default;
	public			AssetRef	Raw			=> this;
	
	public override Int32		GetHashCode		( )										=> _uid.GetHashCode() ^ _subId.GetHashCode( );
	public override Boolean		Equals			( System.Object obj )					=> obj is AssetRef<T> ar && this == ar;
	public			Boolean		Equals			( AssetRef<T> other )					=> _uid == other._uid & _subId == other._subId;
	public static	Boolean		operator ==		( AssetRef<T> left, AssetRef<T> right )	=> left._uid == right._uid & left._subId == right._subId;
	public static	Boolean		operator !=		( AssetRef<T> left, AssetRef<T> right )	=> !(left == right);
	
	public override	String		ToString		( )										=> Raw.ToString( );
	public 			void		FromString		( String address )						=> this = AssetRef.Parse<T>( address );

	public static implicit operator AssetRef	( AssetRef<T> art )						=> new( art._uid, art._subId );
}
	
[Serializable] 
public struct AssetRef : IRefLike, IEquatable<AssetRef>
{
	public	AssetRef	( Hash128 refAddress, Int64 subId ) { this = default; _uid = refAddress; _subId = subId; }
	public	AssetRef	( String refAddress )				{ this = default; FromString( refAddress ); }
	
	[SerializeField] Hash128	_uid;
	[SerializeField] Int64		_subId;
	
	public			Hash128		Uid			=> _uid;
	public			Int64		SubId		=> _subId;
	public			Boolean		IsNone		=> _uid == default;

	public override	Int32		GetHashCode		( )									=> _uid.GetHashCode() ^ _subId.GetHashCode( );
	public override	Boolean		Equals			( System.Object obj )				=> obj is AssetRef ar && this == ar;
	public			Boolean		Equals			( AssetRef other )					=> _uid == other._uid & _subId == other._subId;
	public static	Boolean		operator ==		( AssetRef left, AssetRef right )	=> left._uid == right._uid & left._subId == right._subId;
	public static	Boolean		operator !=		( AssetRef left, AssetRef right )	=> !(left == right);
	
	public override	String		ToString		( )									=> _uid == default ? String.Empty : _subId == 0 ? $"{_uid}" : $"{_uid}[{_subId}]";
	public 			void		FromString		( String address )					=> this = Parse<Object>( address ).Raw;

	public static	AssetRef<T>		Parse<T>( String address ) where T: Object		
	{
		if( String.IsNullOrWhiteSpace( address ) )
			return default;

		var uid		= Hash128.Parse( address[..32] ); 
		var subId	= address.Length == 32 ? 0 : Int64.Parse(address[33..^1]);
		
		return new( uid, subId );
	}
	public	static	AssetsLoader	AssetsLoader	= new AssetsLoader_Resources( );
}
	
public interface IRefLike
{
	public	Hash128		Uid		{ get; }
	public	Int64		SubId	=> default;
}