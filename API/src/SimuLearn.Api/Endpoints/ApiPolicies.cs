namespace SimuLearn.Api.Endpoints;

/// <summary>Named authorization policies used by the endpoint map.</summary>
public static class ApiPolicies
{
    public const string Authenticated = "Authenticated";
    public const string Student = "Student";
    public const string ContentAuthor = "ContentAuthor";
    public const string Reviewer = "Reviewer";
    public const string Administrator = "Administrator";
}
