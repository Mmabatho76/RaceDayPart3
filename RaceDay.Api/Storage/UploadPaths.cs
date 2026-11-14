namespace RaceDay.Api.Storage;

/// <summary>
/// Where uploaded images are stored. This is deliberately OUTSIDE the project folder:
/// if the app writes files inside its own project directory, Visual Studio Hot Reload /
/// "dotnet watch" sees a file change and restarts or stops the running app.
/// Override with the "Uploads:Root" setting (appsettings.json or environment variable).
/// </summary>
public sealed class UploadPaths
{
    public string Root { get; }
    public string ProfilePictures { get; }
    public string EventBanners { get; }

    public UploadPaths(string root)
    {
        Root = root;
        ProfilePictures = Path.Combine(root, "profile-pictures");
        EventBanners = Path.Combine(root, "event-banners");
        Directory.CreateDirectory(ProfilePictures);
        Directory.CreateDirectory(EventBanners);
    }

    public static string DefaultRoot() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RaceDay", "uploads");
}

