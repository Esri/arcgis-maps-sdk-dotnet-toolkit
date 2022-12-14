#if __IOS__ && NETCOREAPP || NETCOREAPP && NETSTANDARD
[assembly: System.Runtime.Versioning.UnsupportedOSPlatform("maccatalyst")]
#endif