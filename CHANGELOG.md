## [1.1.14] - Cumulative Update
- Core updater, supports Git and registry remotes. Will popup to offer version update.
- Fixed some 2020 incompatible parts
- Added workflow to publish in public registry
- Fully remade SerializedPropertyExt.cs, now all property Get/Set supports nesting, lists, arrays, serialized references and multi-editing.
For type getting built-in internal unity methods from ScriptAttributeUtility was used.
- GUIDField is now a class, and has hashing optimizations

## [1.0.0] - First release
- Fixed/removed external dependencies
- Removed CodeGeneration~ ignored folder (was causing warnings)
- Removed EmbeddedPackages
- Removed CoreModule def
- Better package/modules define support (now package can update defines even after deletion)

## [0.0.1] - Init
- Core package were created