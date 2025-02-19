global using System;
global using System.Collections.Generic;
global using Cysharp.Threading.Tasks;
global using UnityEngine;
global using UnityEngine.SceneManagement;
global using UnityEngine.Pool;
global using Object = UnityEngine.Object;

using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Flexy.AssetRefs.AssetLoaders;
using Flexy.Serialisation;
using UnityEngine.U2D;

namespace Flexy.AssetRefs;

//[DebuggerDisplay("{ToString()} = {AssetsLoader.EditorLoadAsset()}")]
[Serializable]
public struct AssetRef<T> : IRefLike, ISerializeAsString, IEquatable<AssetRef<T>> where T: Object
{
		public	AssetRef ( Hash128 uid, Int64 subId = 0 )	{ _uid = uid; _subId = subId; }
		public	AssetRef ( String refAddress )				{ this = default; FromString( refAddress ); }
		
		[SerializeField] Hash128		_uid;
		[SerializeField] Int64			_subId;
		
		public static	AssetRef<T>		None				=> default;
		
		public			Hash128			Uid					=> _uid;
		public			Int64			SubId				=> _subId;
		public			Boolean			IsNone				=> this == default;
		public			AssetRef		Raw					=> this;

		public			Boolean			Like				( String cropId ) => _uid.ToString().StartsWith( cropId );
		
		public override Int32			GetHashCode			( )										=> _uid.GetHashCode() ^ _subId.GetHashCode( );
		public override Boolean			Equals				( System.Object obj )					=> obj is AssetRef<T> ar && this == ar;
		public			Boolean			Equals				( AssetRef<T> other )					=> _uid == other._uid & _subId == other._subId;
		public static	Boolean			operator ==			( AssetRef<T> left, AssetRef<T> right )	=> left._uid == right._uid & left._subId == right._subId;
		public static	Boolean			operator !=			( AssetRef<T> left, AssetRef<T> right )	=> !(left == right);
		
		public override	String			ToString			( ) => _uid == default ? String.Empty : _subId == 0 ? $"{_uid}" : $"{_uid}[{_subId}]";
		public 			void			FromString			( String address )							
		{
			if( String.IsNullOrWhiteSpace( address ) )
			{
				this = default;
				return;
			}
			
			_uid		= Hash128.Parse( address[..32] ); 
			_subId		= address.Length == 32 ? 0 : Int64.Parse(address[33..^1]);
		}

		public static implicit operator AssetRef			( AssetRef<T> art )	=> new( art._uid, art._subId );
}
	
[Serializable] 
public struct AssetRef : IRefLike, IEquatable<AssetRef>
{
	public	AssetRef	( Hash128 refAddress, Int64 subId ) { this = default; _uid = refAddress; _subId = subId; }
	public	AssetRef	( String refAddress )				{ this = default; FromString( refAddress ); }
	
	[SerializeField] Hash128	_uid;
	[SerializeField] Int64		_subId;

	public static	AssetRef	None		=> default;
	
	public			Hash128		Uid			=> _uid;
	public			Int64		SubId		=> _subId;
	
	public			Boolean		IsNone		=> _uid == default;

	public override Int32			GetHashCode			( )									=> _uid.GetHashCode() ^ _subId.GetHashCode( );
	public override Boolean			Equals				( System.Object obj )				=> obj is AssetRef ar && this == ar;
	public			Boolean			Equals				( AssetRef other )					=> _uid == other._uid & _subId == other._subId;
	public static	Boolean			operator ==			( AssetRef left, AssetRef right )	=> left._uid == right._uid & left._subId == right._subId;
	public static	Boolean			operator !=			( AssetRef left, AssetRef right )	=> !(left == right);
	
	public override	String			ToString			( )									=> _uid == default ? String.Empty : _subId == 0 ? $"{_uid}" : $"{_uid}[{_subId}]";
	public 			void			FromString			( String address )					
	{
		if( String.IsNullOrWhiteSpace( address ) )
		{
			this = default;
			return;
		}

		var uid		= Hash128.Parse( address[..32] ); 
		var subId	= address.Length == 32 ? 0 : Int64.Parse(address[33..^1]);
		
		this = new( uid, subId );
	}
	
	public	static	AssetsLoader	AssetsLoader		= new AssetsLoader_Resources( );
}
	
public interface IRefLike
{
		public	Hash128		Uid		{ get; }
		public	Int64		SubId	=> default;
}
	
[AttributeUsage(AttributeTargets.Field)]
public class AssetTypeAttribute : PropertyAttribute
{
		public AssetTypeAttribute ( Type assetType )
		{
			AssetType = assetType;
		}

		public Type AssetType { get ; }
}