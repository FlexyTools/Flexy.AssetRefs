#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
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
			
			static List<ResourcesIRefSourceBuilder> GetAllBuilders()
	        {
	            var getAssets = AssetDatabase.FindAssets( $"t:{typeof(ResourcesIRefSourceBuilder)}" ).Select( guid => (ResourcesIRefSourceBuilder)AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( guid ), typeof(ResourcesIRefSourceBuilder) ) );
	            var assetBundleDefinitions = getAssets.Where( asset => asset != null ).ToList( );
	            return assetBundleDefinitions;
	        }
			
			var allAssets = GetAllBuilders( );
			
			foreach ( var builder in allAssets )
				builder.CreateResourcesAssetForeachAssetRefSource();
		}

		[ContextMenu("Create Resources Refs")]
		private void CreateResourcesAssetForeachAssetRefSource( )
		{
			Debug.Log			( $"[ResourcesIRefSourceBuilder] - CreateResourcesAssetForeachAssetRefSource" );
			
			Directory.CreateDirectory( "Assets/Resources/AssetRefs" );
			
			foreach ( var r in Resources )
			{
				if ( !r )
				{
					Debug.LogError( $"[ResourcesIRefSourceBuilder] - CreateResourcesAssetForeachAssetRefSource: resource is null in {this.name} object. Skipped", this );
					continue;
				}

				if( r is IAssetRefsSource ars )
				{
					foreach ( var ca in ars.CollectAssets( ) )
					{
						var rref = CreateInstance<ResourceRef>( );
						rref.Ref = ca;
						var guid = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( ca ) );
						AssetDatabase.CreateAsset( rref, $"Assets/Resources/AssetRefs/{guid}.asset" );	
					}
				}
				else
				{
					var rref = CreateInstance<ResourceRef>( );
					rref.Ref = r;
					var guid = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( r ) );
					AssetDatabase.CreateAsset( rref, $"Assets/Resources/AssetRefs/{guid}.asset" );
				}
			}
			
			
		}
	}
}

#endif