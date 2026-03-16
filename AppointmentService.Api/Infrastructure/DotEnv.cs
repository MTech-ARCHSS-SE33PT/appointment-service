namespace AppointmentService.Api.Infrastructure;

internal static class DotEnv
{
    public static void LoadFromWellKnownLocations(string fileName = ".env", bool overwrite = false, int maxParentDepth = 6)
    {
        foreach (var startDir in new[]
        {
            new DirectoryInfo(Directory.GetCurrentDirectory()),
            new DirectoryInfo(AppContext.BaseDirectory),
        })
        {
            var current = startDir;
            for (var depth = 0; depth <= maxParentDepth && current is not null; depth++)
            {
                var direct = Path.Combine(current.FullName, fileName);
                if (File.Exists(direct))
                {
                    Load(direct, overwrite);
                    return;
                }

                current = current.Parent;
            }
        }
    }

    private static void Load(string path, bool overwrite)
    {
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var equalsIndex = line.IndexOf('=');
            if (equalsIndex <= 0)
                continue;

            var key = line[..equalsIndex].Trim();
            if (key.Length == 0)
                continue;

            var value = line[(equalsIndex + 1)..].Trim();

            if ((value.StartsWith('"') && value.EndsWith('"')) ||
                (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                value = value[1..^1];
            }

            if (!overwrite && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                continue;

            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

