using System.Collections.Concurrent;

namespace ResLogger2.Web.Services;

public class PathCacheService : IPathCacheService
{
	private readonly ConcurrentDictionary<string, bool> _cache = new();

	public bool Contains(string path) => _cache.ContainsKey(path);
	public void Add(string path) => _cache[path] = true;
}
