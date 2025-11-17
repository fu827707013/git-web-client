using System.Text.Json;

namespace GitWeb.Api.Services
{
    public class RepoConfigService
    {
        private readonly string _configPath;
        private readonly object _lock = new object();

        public RepoConfigService()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "repo-config.json");
            EnsureConfigFileExists();
        }

        private void EnsureConfigFileExists()
        {
            if (!File.Exists(_configPath))
            {
                var emptyConfig = new RepoConfig { Repositories = new List<RepositoryItem>() };
                SaveConfig(emptyConfig);
            }
        }

        public RepoConfig LoadConfig()
        {
            lock (_lock)
            {
                try
                {
                    var json = File.ReadAllText(_configPath);
                    return JsonSerializer.Deserialize<RepoConfig>(json) ?? new RepoConfig { Repositories = new List<RepositoryItem>() };
                }
                catch
                {
                    return new RepoConfig { Repositories = new List<RepositoryItem>() };
                }
            }
        }

        private void SaveConfig(RepoConfig config)
        {
            lock (_lock)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(_configPath, json);
            }
        }

        public void AddOrUpdateRepository(string name, string path)
        {
            var config = LoadConfig();
            var existing = config.Repositories.FirstOrDefault(r => r.Name == name);

            if (existing != null)
            {
                existing.Path = path;
                existing.LastAccessed = DateTime.Now;
            }
            else
            {
                config.Repositories.Add(new RepositoryItem
                {
                    Name = name,
                    Path = path,
                    CreatedAt = DateTime.Now,
                    LastAccessed = DateTime.Now
                });
            }

            SaveConfig(config);
        }

        public void RemoveRepository(string name)
        {
            var config = LoadConfig();
            config.Repositories = config.Repositories.Where(r => r.Name != name).ToList();
            SaveConfig(config);
        }

        public void UpdateLastAccessed(string name)
        {
            var config = LoadConfig();
            var repo = config.Repositories.FirstOrDefault(r => r.Name == name);
            if (repo != null)
            {
                repo.LastAccessed = DateTime.Now;
                SaveConfig(config);
            }
        }

        public IEnumerable<RepositoryItem> GetRepositories()
        {
            var config = LoadConfig();
            return config.Repositories.OrderByDescending(r => r.LastAccessed);
        }

        public RepositoryItem? GetRepository(string name)
        {
            var config = LoadConfig();
            return config.Repositories.FirstOrDefault(r => r.Name == name);
        }
    }

    public class RepoConfig
    {
        public List<RepositoryItem> Repositories { get; set; } = new List<RepositoryItem>();
    }

    public class RepositoryItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessed { get; set; }
    }
}
