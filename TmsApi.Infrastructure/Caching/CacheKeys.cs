namespace TmsApi.Infrastructure.Caching;

public static class CacheKeys
{
    private const string SchemaVersion = "v2";

    public static string CoursesPage(int page, int pageSize) =>
        $"{SchemaVersion}:courses:page:{page}:size:{pageSize}";

    public const string CoursesTag = "courses";
}