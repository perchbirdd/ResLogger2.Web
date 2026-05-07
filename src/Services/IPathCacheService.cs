namespace ResLogger2.Web.Services;

public interface IPathCacheService
{
	bool Contains(string path);
	void Add(string path);
}
