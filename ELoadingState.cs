using System;

namespace Flexy.AssetRefs;

public enum ELoadingState : Byte
{
	None,
	Download,
	Unpack,
	Load,
	Done
}