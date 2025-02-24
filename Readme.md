# **Flexy.AssetRefs**

---

Load assets **on demand** without Addressables and Bundles  
Almost **zero** editor setup!  
Fast, extendable, production-proven and **Open Source!**

**Want to load assets ondemand but**
-----------------------
- don't want to mess around with Addressables
- don't want to use any type of Bundles
- don't want to manage addressable assets separately
- don't want any big system yet (on early stage of project)

Flexy.AssetRefs will help you to Load assets on demand from prototyping stage and add bundles 
complexity only when game grows up! or **Never :)**

Flexy.AssetRefs provides an efficient way to indirectly reference assets and scenes, offering a 
cleaner alternative to Unity Addressables. It focuses solely on asset referencing, allowing full control over 
how assets are loaded at runtime without enforcing specific bundling or loading methods.

Designed for flexibility, Flexy.AssetRefs is easy to use from the prototyping stage. 
It is well-suited for small projects where Addressables can create more issues than they solve and easily expand to 
more complex systems later.

**Key Strengths**
------------------
- It is Open Source :)
- Fast: pure struct based 
- ECS-Compatible: because it is struct
- Customisable: load methods can be totally replaced 
- GDD Friendly: store asset references directly inside GameDesignData
- Asset Loader can be totally replaced (Bundles, Addressables, Custom, ...)
- Editor-Friendly: works seamlessly in the Editor without extra setup
- SceneRef: allows loading scenes in the editor without adding to Build Settings
- Toggle runtime/editor behavior with simple menu item click
- Clean Inspector: looks like regular asset reference
- Production-Proven: used in released games since 2019
- Minimal Inspector Clutter:
  - Looks like regular reference in inspector
  - Does not clutter GO inspector
  - Only one simple file in project to collect necessary data for runtime
   
**Flexy.AssetRefs is**
-----------------------
**Modular and Simple to use:** we separate the reference system from complex asset bundles bundling and downloading. 
Flexy.AssetRefs focuses only on asset references for on-demand loading. Flexy.Bundles adds bundles building 
and downloading capabilities. This modular approach avoids the complexity of a heavy solution like Addressables

**Easily Extendable:** asset loading methods is C# extension methods, which means that users have flexibility to define 
their own loading methods with any indirections, additional checks or better knowing used loading backend

**Double Easily Extendable:** asset loading done through AssetLoader instance that is backend for loading any ref 
and can be replaced with your own implementation. It is only 5 virtual methods to implement 

**Already Used in Games like:** Sniper League, Animals Happy Run, Cyberstrike

**Technical details**
---------------------
- Ref is struct with 2 fields: Hash128 & Int64
- C# Extensions based load methods
- AssetLoader interface is 5 virtual methods
- Sync loading 
- UniTask based async loading 
- Native C# Nullability annotations
- C# 10