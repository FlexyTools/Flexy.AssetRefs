namespace Flexy.AssetRefs;

/// <summary>
/// Collects and optionally unloads selected memory-heavy rendering assets referenced by an object hierarchy.
/// This is not a complete dependency collector and does not collect or unload every referenced asset.
/// Unloading is unconditional and can invalidate assets that are still used by other objects.
/// The caller is responsible for ensuring that collected assets are ok to unload.
/// </summary>
public static class ObjectAssetsCollector
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private	static	void	InitRuntime	 ( ) => Clear();

	private static readonly Dictionary<String, Int32[]>		_textureNameIdsPerShader = new(10);
	private static readonly HashSet<Object>		_tempSet		= new(150);
	private static readonly List<Material>		_tempMtls		= new(20);
	private static readonly List<Renderer>		_tempRenderers	= new(20);
	private static			Mesh[]				_tempMeshes		= new Mesh[20];
	private static			SecondarySpriteTexture[] _tempSecondaryTextures = new SecondarySpriteTexture[4];
	
	public static	void	UnloadObjectAssets		( Object? obj )			
	{
		ClearTempCollections();
		try
		{
			CollectAssetsInternal(obj);
			
			foreach (var o in _tempSet)
				Resources.UnloadAsset(o);
		}
		finally
		{
			ClearTempCollections();
		}
	}
	public static	void	CollectAssets			( Object? obj, List<Object> collectedAssets )	
	{
		collectedAssets.Clear();
		ClearTempCollections();
		try
		{
			CollectAssetsInternal(obj);
			
			foreach (var asset in _tempSet)
				collectedAssets.Add(asset);
		}
		finally
		{
			ClearTempCollections();
		}
	}
	public static	void	Clear					( )						
	{
		_textureNameIdsPerShader.Clear();
		ClearTempCollections();
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
			
			case Sprite sprite:
			{
				if (!_tempSet.Add(sprite))
					break;
				
				CollectAssetsInternal(sprite.texture);
				CollectAssetsInternal(sprite.associatedAlphaSplitTexture);
				
				var secondaryTextureCount = sprite.GetSecondaryTextureCount();
				if (_tempSecondaryTextures.Length < secondaryTextureCount)
					Array.Resize(ref _tempSecondaryTextures, secondaryTextureCount);
				
				secondaryTextureCount = sprite.GetSecondaryTextures(_tempSecondaryTextures);
				for (var i = 0; i < secondaryTextureCount; i++)
					CollectAssetsInternal(_tempSecondaryTextures[i].texture);
				Array.Clear(_tempSecondaryTextures, 0, secondaryTextureCount);
				
				break;
			}
			
			case BillboardAsset billboard:
			{
				if (!_tempSet.Add(billboard))
					break;
				
				CollectAssetsInternal(billboard.material);
				break;
			}

			case Material material:
			{
				var shader = material.shader;
				if (!_tempSet.Add(material) || shader == null)
					break;
			
				var shaderName = shader.name;
				if (!_textureNameIdsPerShader.TryGetValue(shaderName, out var ids))
					_textureNameIdsPerShader[shaderName] = ids = material.GetTexturePropertyNameIDs();
				
				foreach (var nameID in ids)
					if (material.HasTexture(nameID))
						CollectAssetsInternal(material.GetTexture(nameID));

				break;
			}

			case SkinnedMeshRenderer skinnedMeshRenderer:
			{
				CollectAssetsInternal(skinnedMeshRenderer.sharedMesh);
				CollectRendererMaterials(skinnedMeshRenderer);
				break;
			}
			
			case SpriteRenderer spriteRenderer:
			{
				CollectAssetsInternal(spriteRenderer.sprite);
				CollectRendererMaterials(spriteRenderer);
				break;
			}
			
			case SpriteMask spriteMask:
			{
				CollectAssetsInternal(spriteMask.sprite);
				CollectRendererMaterials(spriteMask);
				break;
			}
			
			case ParticleSystemRenderer particleRenderer:
			{
				if (_tempMeshes.Length < particleRenderer.meshCount)
					Array.Resize(ref _tempMeshes, particleRenderer.meshCount);
				
				var meshCount = particleRenderer.GetMeshes(_tempMeshes);
				for (var i = 0; i < meshCount; i++)
					CollectAssetsInternal(_tempMeshes[i]);
				Array.Clear(_tempMeshes, 0, meshCount);
				
				CollectAssetsInternal(particleRenderer.trailMaterial);
				CollectRendererMaterials(particleRenderer);
				break;
			}
			
			case BillboardRenderer billboardRenderer:
			{
				CollectAssetsInternal(billboardRenderer.billboard);
				CollectRendererMaterials(billboardRenderer);
				break;
			}
			
			case MeshRenderer meshRenderer:
			{
				CollectAssetsInternal(meshRenderer.GetComponent<MeshFilter>());
				CollectRendererMaterials(meshRenderer);
				break;
			}
			
			case Renderer renderer:
			{
				CollectRendererMaterials(renderer);
				break;
			}

			case MeshFilter meshFilter:
			{
				CollectAssetsInternal(meshFilter.sharedMesh); 
				break;
			}

			case Component c:
			{
				CollectAssetsInternal(c.gameObject); 
				break;
			}

			case GameObject gameObject:
			{
				_tempRenderers.Clear();
				gameObject.GetComponentsInChildren(true, _tempRenderers);
				
				foreach (var renderer in _tempRenderers)
					CollectAssetsInternal(renderer);
				
				_tempRenderers.Clear();

				break;
			}
		}			
	}
	private	static	void	CollectRendererMaterials( Renderer renderer )	
	{
		_tempMtls.Clear();
		renderer.GetSharedMaterials(_tempMtls);
		
		foreach (var material in _tempMtls)
			CollectAssetsInternal(material);
		
		_tempMtls.Clear();
	}
	private	static	void	ClearTempCollections	( )						
	{
		_tempSet		.Clear();
		_tempMtls		.Clear();
		_tempRenderers	.Clear();
		Array.Clear(_tempMeshes, 0, _tempMeshes.Length);
		Array.Clear(_tempSecondaryTextures, 0, _tempSecondaryTextures.Length);
	}
}
