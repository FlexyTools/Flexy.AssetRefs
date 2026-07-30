namespace Flexy.AssetRefs;

public static class ObjectAssetsCollector
{
	private static readonly Dictionary<String, Int32[]>		_textureNameIdsPerShader = new(10);
	private static readonly HashSet<Object>		_tempSet		= new(150);
	private static readonly List<Material>		_tempMtls		= new(20);
	private static readonly List<Renderer>		_tempRenderers	= new(20);
	private static readonly List<MeshFilter>	_tempMeshFilters= new(20);
	
	public static IReadOnlyCollection<Object> CollectedAssets => _tempSet;
	
	public static	void	UnloadObjectAssets		( Object? obj )		
	{
		_tempSet.Clear();
		CollectAssetsInternal(obj);
		
		foreach (var o in _tempSet)
			Resources.UnloadAsset(o);
		
		_tempSet.Clear();
	}
	public static	void	CollectAssets			( Object obj )		
	{
		_tempSet.Clear();
		CollectAssetsInternal(obj);
	}
	public static	void	Clear					( )					
	{
		_tempSet.Clear();
	}	
		
	private	static	void	CollectAssetsInternal	( Object? obj )		
	{
		if (obj == null) 
			return;
		
		switch(obj)
		{
			case Texture texture:
			{
				_tempSet.Add(texture);
				break;
			} 
			
			case Mesh mesh:
			{
				_tempSet.Add(mesh);
				break;
			}

			case Material material:
			{
				if (!_textureNameIdsPerShader.TryGetValue( material.shader.name, out var ids))
					_textureNameIdsPerShader[material.shader.name] = ids = material.GetTexturePropertyNameIDs();
				
				_tempSet.Add(material);
				
				foreach (var nameID in ids)
					if (material.HasTexture(nameID))
						CollectAssetsInternal(material.GetTexture(nameID));

				break;
			}

			case SkinnedMeshRenderer skinnedMeshRenderer:
			{
				CollectAssetsInternal(skinnedMeshRenderer.sharedMesh);
			
				_tempMtls.Clear();
				skinnedMeshRenderer.GetMaterials(_tempMtls);
				foreach (var mat in _tempMtls)
					CollectAssetsInternal(mat);
				_tempMtls.Clear();

				break;
			}

			case MeshRenderer meshRenderer:
			{
				_tempMtls.Clear();
				meshRenderer.GetMaterials(_tempMtls);
				foreach (var mat in _tempMtls)
					CollectAssetsInternal(mat);
				_tempMtls.Clear();
				
				break;
			}

			case MeshFilter meshFilter:
			{
				CollectAssetsInternal(meshFilter.sharedMesh); 
				break;
			}

			case MonoBehaviour monoBehaviour:
			{
				CollectAssetsInternal(monoBehaviour.gameObject); 
				break;
			}

			case Transform transform:
			{
				CollectAssetsInternal(transform.gameObject); 
				break;
			}

			case GameObject gameObject:
			{
				_tempRenderers.Clear();
				gameObject.GetComponentsInChildren(true, _tempRenderers);
				
				foreach (var rend in _tempRenderers)
				{
					switch(rend)
					{
						case SkinnedMeshRenderer skinned: CollectAssetsInternal(skinned); break;
						case MeshRenderer meshRend:       CollectAssetsInternal(meshRend); break;
					}
				}
				_tempRenderers.Clear();
			
				_tempMeshFilters.Clear();
				gameObject.GetComponentsInChildren(true, _tempMeshFilters);
				
				foreach (var filter in _tempMeshFilters)
					CollectAssetsInternal(filter);
				
				_tempMeshFilters.Clear();

				break;
			}
		}			
	}
}