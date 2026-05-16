namespace BuildWise.Services;

public static class ProjectCacheKeys
{
    public static string SelectorProjects(int userId) => $"project-selector:{userId}";
}
