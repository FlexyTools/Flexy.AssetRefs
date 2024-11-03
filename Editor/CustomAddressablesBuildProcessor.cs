#if UNITY_ADDRESSABLES

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEngine;

namespace Flexy.AssetRefs.Editor;


public class CustomAddressablesBuildProcessor : AddressablesPlayerBuildProcessor
{
	[InitializeOnLoadMethod]
	private static void CleanTemporaryPlayerBuildData()
	{
		AddressablesPlayerBuildProcessor.BuildAddressablesOverride += Override;
	}

	private static AddressablesPlayerBuildResult Override(AddressableAssetSettings arg)
	{
		var settings = AddressableAssetSettingsDefaultObject.Settings;

		// Modify resource locations or catalog entries
		foreach (var group in settings.groups)
		{
			ModifyGroupAssets(group);
		}

		Debug.Log("Custom build processing compsdsdsdlete.");
		
		return null!;
	}


	public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
	{
		// Load the Addressable settings
		var settings = AddressableAssetSettingsDefaultObject.Settings;

		// Modify resource locations or catalog entries
		foreach (var group in settings.groups)
		{
			ModifyGroupAssets(group);
		}

		Debug.Log("Custom build processing complete.");
		
		base.PrepareForBuild(buildPlayerContext);
	}

	private static void ModifyGroupAssets(AddressableAssetGroup group)
	{
		// Iterate over assets in the group
		foreach (var entry in group.entries)
		{
			Debug.Log( entry.address );
			
			// Modify each entry's asset location as per your custom logic
			// Example: change the address or custom metadata
			//entry.address = ModifyAddress(entry.address);
		}
	}

	private string ModifyAddress(string originalAddress)
	{
		// Add custom logic here to modify the asset address during the build
		// Example: change the URL or path of assets based on some custom rule
		return "custom_path/" + originalAddress;
	}
}

#endif