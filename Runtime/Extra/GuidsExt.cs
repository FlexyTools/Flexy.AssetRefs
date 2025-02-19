#if UNITY_EDITOR

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEditor;

namespace Flexy.AssetRefs.Extra;

public static class GuidsExt
{
	[MethodImpl(256)] public static Hash128		ToHash	( this GUID g )		=> new GuidHashUnion( g ).SwitchBits( ).Hash;
	[MethodImpl(256)] public static GUID		ToGUID	( this Hash128 h )	=> new GuidHashUnion( h ).SwitchBits( ).Guid;
		
	[StructLayout(LayoutKind.Explicit)]
	private ref struct GuidHashUnion
	{
		[MethodImpl(256)]	public	GuidHashUnion( GUID g )		{ this = default; Guid = g; }
		[MethodImpl(256)]	public	GuidHashUnion( Hash128 h )	{ this = default; Hash = h; }
			
		[FieldOffset(0)]	public	Hash128	Hash;
		[FieldOffset(0)]	public	GUID	Guid;
		
		[FieldOffset(0)]	private UInt64	half_01;
		[FieldOffset(8)]	private UInt64	half_02;
		
		[MethodImpl(256)]	public	GuidHashUnion SwitchBits( )
		{
			var h11 = (half_01 & 0x_f0f0_f0f0_f0f0_f0f0) >> 4;
			var h12 = (half_01 & 0x_0f0f_0f0f_0f0f_0f0f) << 4;
			
			half_01 = h11 | h12;
			
			var h21 = (half_02 & 0x_f0f0_f0f0_f0f0_f0f0) >> 4;
			var h22 = (half_02 & 0x_0f0f_0f0f_0f0f_0f0f) << 4;
			
			half_02 = h21 | h22;
			
			return this;
		}
	}
}
#endif