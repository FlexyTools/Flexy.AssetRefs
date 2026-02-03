#if UNITY_EDITOR
namespace Flexy.AssetRefs;

public static class EditorBehaviourAndMenu
{
	private static	Boolean?	_runtimeBehaviorEnabled;
	public static	Boolean		RuntimeBehaviorEnabled		
	{
		get => _runtimeBehaviorEnabled ??= UnityEditor.EditorPrefs.GetBool( Application.productName + "=>Fun.Flexy/AssetRefs/RuntimeBehaviorEnabled" ); 
		set => UnityEditor.EditorPrefs.SetBool( Application.productName + "=>Fun.Flexy/AssetRefs/RuntimeBehaviorEnabled", (_runtimeBehaviorEnabled = value).Value );
	}

	public const String Menu = "Tools/Fun.Flexy/Asset Refs/";
	[UnityEditor.MenuItem( Menu+"Enable Runtime Behavior", secondaryPriority = 101)]	static void		EnableRuntimeBehavior			( ) => RuntimeBehaviorEnabled = true;
	[UnityEditor.MenuItem( Menu+"Disable Runtime Behavior", secondaryPriority = 100)]	static void		DisableRuntimeBehavior			( ) => RuntimeBehaviorEnabled = false;
	[UnityEditor.MenuItem( Menu+"Enable Runtime Behavior", true)]						static Boolean	EnableRuntimeBehaviorValidate	( ) => !RuntimeBehaviorEnabled;
	[UnityEditor.MenuItem( Menu+"Disable Runtime Behavior", true)]						static Boolean	DisableRuntimeBehaviorValidate	( ) => RuntimeBehaviorEnabled;

}
#endif