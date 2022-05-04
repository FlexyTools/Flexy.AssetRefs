using System;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Flexy.AssetRefs
{
	[CreateAssetMenu(fileName = "ResourceRefBuilder", menuName = "Flexy/AssetRefs/ResourcesIRefSourceBuilder")]
	public class ResourcesIRefSourceBuilder : ScriptableObject, IPreprocessBuildWithReport
	{
		public Object[] Resources;
		
		public Int32 callbackOrder { get; }
		public void OnPreprocessBuild( BuildReport report )
		{
			Debug.Log			( $"[ResourcesIRefSourceBuilder] - OnPreprocessBuild: PreProcessBuild" );
			CreateResourcesAssetForeachAssetRefSource();
		}

		[ContextMenu("Create Resources Refs")]
		private void CreateResourcesAssetForeachAssetRefSource( )
		{
			Debug.Log			( $"[ResourcesIRefSourceBuilder] - CreateResourcesAssetForeachAssetRefSource" );
			
			Directory.CreateDirectory( "Assets/Resources/AssetRefs" );
			
			foreach ( var r in Resources )
			{
				if( r is IAssetRefsSource ars )
				{
					foreach ( var ca in ars.CollectAssets( ) )
					{
						var rref = CreateInstance<ResourceRef>( );
						rref.Ref = ca;
						var guid = UnityEditor.AssetDatabase.AssetPathToGUID( UnityEditor.AssetDatabase.GetAssetPath( ca ) );
						UnityEditor.AssetDatabase.CreateAsset( rref, $"Assets/Resources/AssetRefs/{guid}.asset" );	
					}
				}
				else
				{
					var rref = CreateInstance<ResourceRef>( );
					rref.Ref = r;
					var guid = UnityEditor.AssetDatabase.AssetPathToGUID( UnityEditor.AssetDatabase.GetAssetPath( r ) );
					UnityEditor.AssetDatabase.CreateAsset( rref, $"Assets/Resources/AssetRefs/{guid}.asset" );
				}
			}
		}
	}
}