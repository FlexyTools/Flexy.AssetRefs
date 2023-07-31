#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Serialization;


namespace Flexy.AssetRefs.LoaderResources
{
	[CreateAssetMenu(fileName = "ResourceRefBuilder", menuName = "Flexy/AssetRefs/ResourcesIRefSourceBuilder")]
	public class ResourcesIRefSourceBuilder : ScriptableObject, IPreprocessBuildWithReport
	{
		public ECatchWay	CatchWay;
		public String		CatchType;
		[FormerlySerializedAs("Resources")] 
		public Object[]		DirectReferences;
		
		public Int32 callbackOrder { get; }
		public void OnPreprocessBuild( BuildReport report )
		{
			Debug.Log			( $"[ResourcesIRefSourceBuilder] - OnPreprocessBuild: PreProcessBuild" );
			
			GenerateAssetRefAssets( );
		}
		
		[MenuItem("Tools/Flexy/AssetRefs/Generate AssetRef Assets")]
		public static void GenerateAssetRefAssets ( )
		{
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
			
			try						
			{
				AssetDatabase.StartAssetEditing( );
			
				var ress = GrabResources( );
				
				foreach ( var r in ress )
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
							rref.Name = ca.name;
							
							var assetAddress	= AssetRef.EditorCreateAssetAddress( ca );
							AssetDatabase.CreateAsset( rref, $"Assets/Resources/AssetRefs/{assetAddress.ToString().Replace(":", "@")}.asset" );	
						}
					}
					else
					{
						var rref = CreateInstance<ResourceRef>( );
						rref.Ref = r;
						var assetAddress	= AssetRef.EditorCreateAssetAddress( r );
						AssetDatabase.CreateAsset( rref, $"Assets/Resources/AssetRefs/{assetAddress.ToString().Replace(":", "@")}.asset" );
					}
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing( );
			}
		}

		private List<Object> GrabResources( )
		{
			var list = new List<Object>( ); 
			
			if( DirectReferences != null )
				list.AddRange( DirectReferences );
			
			if( CatchWay != 0 )
			{
				var thisPath	= Path.GetDirectoryName( AssetDatabase.GetAssetPath( this ) );
				var filter		= "";
				
				if( !String.IsNullOrWhiteSpace( CatchType ) )
					filter = $"t:{CatchType.Trim()}";
				
				var assetGuids	= AssetDatabase.FindAssets( filter, new []{ thisPath } );
				
				foreach ( var assetGuid in assetGuids )
				{
					var path	= AssetDatabase.GUIDToAssetPath( assetGuid );
					
					if( filter == "" )
					{
						var asset  = AssetDatabase.LoadMainAssetAtPath( path );
						list.Add( asset );
					}
					else
					{
						var assets  = AssetDatabase.LoadAllAssetsAtPath( path );
						
						foreach ( var asset in assets )
						{
							if( asset.GetType( ).Name.Contains( CatchType ) )
							{
								list.Add( asset );
							}
						}
					}
				}
			}
			
			return list;
		}
	}

	public enum ECatchWay : Byte
	{
		OnlyDirectReferences		= 0,
		//AddCurrentDirAssets		= 1,
		AddCurrentAndSubDirAssets	= 2,
	}

	public class ResourcesDefaultScenesIRefSourceBuilder : IPreprocessBuildWithReport
	{
		public Int32 callbackOrder { get; }
		public void OnPreprocessBuild(BuildReport report)
		{
			CreateDefaultSceneRefs( );
		}
		
		[MenuItem("Tools/Flexy/AssetRefs/Create Default SceneRefs")]
		public static void CreateDefaultSceneRefs ( )
		{
			Directory.CreateDirectory( "Assets/Resources/AssetRefs" );
			
			foreach ( var s in EditorBuildSettings.scenes )
			{
				if ( !s.enabled )
					continue;

				var rref = ResourceRef.CreateInstance<ResourceRef>( );
				rref.Ref = null;
				rref.Name = Path.GetFileNameWithoutExtension( s.path );
				
				AssetDatabase.CreateAsset( rref, $"Assets/Resources/AssetRefs/{s.guid}.asset" );
			}
		}
	}
}

#endif