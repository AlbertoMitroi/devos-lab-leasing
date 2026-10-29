namespace Leasing.Host.Authorization;

public static class Policies
{
    public const string DefaultRead = "DEFAULT_READ";
    public const string DefaultWrite = "DEFAULT_WRITE";
    public const string Admin = "ADMIN";
}

public static class Roles
{
    public const string Reader = "Leasing.Read";
    public const string Writer = "Leasing.Write";
    public const string Administrator = "Leasing.Admin";
}
