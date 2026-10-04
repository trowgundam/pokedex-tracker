# Keep user progress outside the site operator's storage

The site is a public standalone Blazor WebAssembly application hosted on GitHub Pages. Jeff requires that the operator never store user progress, so cross-device synchronization communicates directly between the browser and user-controlled storage. An operator-managed database would violate that requirement. The initial provider is a user-selected folder. Cloud authentication and providers are deferred.
