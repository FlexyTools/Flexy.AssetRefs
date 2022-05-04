using System.Collections;
using System.Collections.Generic;
using Flexy.AssetBundles;
using UnityEngine;

public class Awp3DModel : MonoBehaviour
{
	public AssetRef _refWeapon;
	public AssetRef	_refWeaponMaterial;
	public AssetRef _refGateMesh;

	public async void LoadPartOfWeapon( )
	{
		DestroyAllChildren();
		var material 	= await _refWeaponMaterial.LoadAssetTyped<Material>(  );
		var mesh 		= await _refGateMesh.LoadAssetTyped<Mesh>(  );
		var awpGate 	= new GameObject("awp_gate",typeof(MeshFilter),typeof(MeshRenderer));
		awpGate.transform.SetParent( transform );
		awpGate.transform.localPosition                 = Vector3.zero;
		awpGate.transform.localScale                    = Vector3.one;
		awpGate.transform.localEulerAngles              = Vector3.zero;
		awpGate.GetComponent<MeshFilter>(  ).mesh       = mesh;
		awpGate.GetComponent<MeshRenderer>(  ).material = material;
	}
	public async void LoadFullWeapon( )
	{
		Debug.Log			( $"[Awp3DModel] - LoadFullWeapon: {await _refWeapon.GetDownloadSize(  )}" );
		//return;
		DestroyAllChildren();
		var obj = await _refWeapon.LoadAsset(  );
		Instantiate( obj, transform);
	}
	
	private void DestroyAllChildren( )
	{
		foreach ( Transform child in transform )
		{
			Destroy( child.gameObject );
		}
	}
}