using LibGit2Sharp;
using System.IO.Compression;
namespace GitWeb.Api.Services
{
    public class GitService
    {
        public bool IsValidRepoPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            if (!Directory.Exists(path)) return false;
            var gitDir = Path.Combine(path, ".git");
            return Directory.Exists(gitDir) || Repository.IsValid(path);
        }

        public Repository Open(string path)
        {
            return new Repository(path);
        }

        public object GetStatus(string path)
        {
            using var repo = Open(path);
            var branch = repo.Head.FriendlyName;
            var remote = repo.Network.Remotes.FirstOrDefault()?.Url ?? "";
            var status = repo.RetrieveStatus();
            var notCommitted = status.Count();
            var ahead = repo.Head.TrackingDetails.AheadBy ?? 0;
            return new { branch, remote, notCommitted, notPushed = ahead };
        }

        public IEnumerable<object> GetCommits(string path, int page, int pageSize, string? author, string? q)
        {
            using var repo = Open(path);
            var commits = repo.Commits.QueryBy(new CommitFilter { SortBy = CommitSortStrategies.Time | CommitSortStrategies.Topological });
            var filtered = commits.Where(c =>
            {
                var ok = true;
                if (!string.IsNullOrWhiteSpace(author)) ok &= c.Author.Name.Contains(author, StringComparison.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(q)) ok &= (c.MessageShort?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false);
                return ok;
            });
            var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).Select(c => new
            {
                sha = c.Sha,
                shaShort = c.Sha[..7],
                author = c.Author.Name,
                date = c.Author.When.ToLocalTime(),
                message = c.MessageShort,
                changedFiles = GetChangedFilesCount(repo, c)
            });
            return items.ToList();
        }

        public object? GetCommitDetails(string path, string sha)
        {
            using var repo = Open(path);
            var commit = repo.Lookup<Commit>(sha);
            if (commit == null) return null;

            var parent = commit.Parents.FirstOrDefault();
            var changes = parent != null
                ? repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree)
                : null;

            var files = new List<object>();

            if (parent == null)
            {
                // First commit - all files are additions
                foreach (var entry in TraverseTreeWithStatus(commit.Tree))
                {
                    files.Add(new
                    {
                        path = entry.path,
                        status = "Added",
                        oldPath = (string?)null
                    });
                }
            }
            else
            {
                // Regular commit - show changes
                foreach (var change in changes!)
                {
                    files.Add(new
                    {
                        path = change.Path,
                        status = change.Status.ToString(),
                        oldPath = change.OldPath != change.Path ? change.OldPath : null
                    });
                }
            }

            return new
            {
                sha = commit.Sha,
                author = commit.Author.Name,
                authorEmail = commit.Author.Email,
                date = commit.Author.When.ToLocalTime(),
                message = commit.Message,
                messageShort = commit.MessageShort,
                filesChanged = files.Count,
                files = files.OrderBy(f => ((dynamic)f).path).ToList()
            };
        }

        IEnumerable<(string path, string status)> TraverseTreeWithStatus(Tree tree, string prefix = "")
        {
            foreach (var entry in tree)
            {
                var currentPath = string.IsNullOrEmpty(prefix)
                    ? entry.Name
                    : Path.Combine(prefix, entry.Name).Replace('\\', '/');

                if (entry.Target is Blob)
                {
                    yield return (currentPath, "Added");
                }
                else if (entry.Target is Tree sub)
                {
                    foreach (var item in TraverseTreeWithStatus(sub, currentPath))
                        yield return item;
                }
            }
        }

        int GetChangedFilesCount(Repository repo, Commit commit)
        {
            var parent = commit.Parents.FirstOrDefault();
            if (parent == null) return commit.Tree.Count();
            var changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);
            return changes.Count();
        }

        //IEnumerable<(string path, Blob blob)> GetCommitBlobs(Repository repo, Commit commit)
        //{
        //    var parent = commit.Parents.FirstOrDefault();
        //    if (parent == null)
        //    {
        //        foreach (var kv in TraverseTree(commit.Tree))
        //            yield return kv;
        //        yield break;
        //    }
        //    var changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);
        //    foreach (var c in changes)
        //    {
        //        if (c.Status == ChangeKind.Deleted) continue;
        //        var entry = commit.Tree[c.Path];
        //        if (entry?.Target is Blob blob)
        //            yield return (c.Path, blob);
        //    }
        //}
        IEnumerable<(string path, Blob blob)> GetCommitBlobs(Repository repo, Commit commit)
        {
            var parent = commit.Parents.FirstOrDefault();
            if (parent == null)
            {
                foreach (var kv in TraverseTree(commit.Tree))
                    yield return kv;
                yield break;
            }

            var changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);
            foreach (var c in changes)
            {
                if (c.Status == ChangeKind.Deleted) continue;
                var entry = commit.Tree[c.Path];
                if (entry?.Target is Blob blob)
                {
                    yield return (c.Path.Replace('\\', '/'), blob); // ✅ 标准化路径
                }
            }
        }

        //IEnumerable<(string path, Blob blob)> TraverseTree(Tree tree, string prefix = "")
        //{
        //    foreach (var entry in tree)
        //    {
        //        if (entry.Target is Blob blob)
        //        {
        //            yield return (entry.Path, blob);
        //        }
        //        else if (entry.Target is Tree sub)
        //        {
        //            foreach (var kv in TraverseTree(sub, entry.Path))
        //                yield return kv;
        //        }
        //    }
        //}
        //IEnumerable<(string path, Blob blob)> TraverseTree(Tree tree, string prefix = "")
        //{
        //    foreach (var entry in tree)
        //    {
        //        // 构建完整路径
        //        var fullPath = string.IsNullOrEmpty(prefix)
        //            ? entry.Name
        //            : Path.Combine(prefix, entry.Name).Replace('\\', '/');

        //        if (entry.Target is Blob blob)
        //        {
        //            yield return (fullPath, blob); // ✅ 返回完整路径
        //        }
        //        else if (entry.Target is Tree sub)
        //        {
        //            foreach (var kv in TraverseTree(sub, fullPath)) // ✅ 传递完整路径
        //                yield return kv;
        //        }
        //    }
        //}
        // 辅助方法：修复路径构建逻辑
        IEnumerable<(string path, Blob blob)> TraverseTree(Tree tree, string prefix = "")
        {
            foreach (var entry in tree)
            {
                // ✅ 正确构建完整路径
                var currentPath = string.IsNullOrEmpty(prefix)
                    ? entry.Name
                    : Path.Combine(prefix, entry.Name).Replace('\\', '/');

                if (entry.Target is Blob blob)
                {
                    yield return (currentPath, blob); // 返回完整路径
                }
                else if (entry.Target is Tree sub)
                {
                    foreach (var kv in TraverseTree(sub, currentPath)) // ✅ 传递完整路径
                        yield return kv;
                }
            }
        }

        //public byte[] ZipCommitFilesBytes(string path, string sha)
        //{
        //    using var ms = new MemoryStream();
        //    using var repo = Open(path);
        //    var commit = repo.Lookup<Commit>(sha);
        //    using var zip = new ZipArchive(ms, ZipArchiveMode.Create, true);
        //    foreach (var (file, blob) in GetCommitBlobs(repo, commit))
        //    {
        //        var entry = zip.CreateEntry(file);
        //        using var es = entry.Open();
        //        using var content = blob.GetContentStream();
        //        content.CopyTo(es);
        //    }
        //    return ms.ToArray();
        //}
        public byte[] ZipCommitFilesBytes(string path, string sha)
        {
            using var repo = Open(path);
            var commit = repo.Lookup<Commit>(sha);

            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true)) // ✅ 使用代码块
            {
                foreach (var (file, blob) in GetCommitBlobs(repo, commit))
                {
                    // 路径标准化（修复3）
                    var entryPath = file.Replace('\\', '/');
                    var entry = zip.CreateEntry(entryPath);

                    using var es = entry.Open();
                    using var content = blob.GetContentStream();
                    content.CopyTo(es);
                }
            } // ✅ zip在此处释放，完成ZIP结构写入

            return ms.ToArray(); // ✅ 此时ZIP已完整
        }

        //public byte[] ZipCommitsFilesBytes(string path, IEnumerable<string> shas)
        //{
        //    using var ms = new MemoryStream();
        //    using var repo = Open(path);
        //    var commits = shas.Select(sha => repo.Lookup<Commit>(sha)).Where(c => c != null).OrderBy(c => c!.Author.When).ToList();
        //    var map = new Dictionary<string, Blob>();
        //    foreach (var commit in commits)
        //    {
        //        var parent = commit!.Parents.FirstOrDefault();
        //        if (parent == null)
        //        {
        //            foreach (var (filePath, blob) in TraverseTree(commit!.Tree))
        //                map[filePath] = blob;
        //        }
        //        else
        //        {
        //            var changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit!.Tree);
        //            foreach (var c in changes)
        //            {
        //                if (c.Status == ChangeKind.Deleted)
        //                {
        //                    if (map.ContainsKey(c.Path)) map.Remove(c.Path);
        //                }
        //                else
        //                {
        //                    var entry = commit!.Tree[c.Path];
        //                    if (entry?.Target is Blob blob) map[c.Path] = blob;
        //                }
        //            }
        //        }
        //    }
        //    using var zip = new ZipArchive(ms, ZipArchiveMode.Create, true);
        //    foreach (var kv in map)
        //    {
        //        var entry = zip.CreateEntry(kv.Key);
        //        using var es = entry.Open();
        //        using var content = kv.Value.GetContentStream();
        //        content.CopyTo(es);
        //    }
        //    return ms.ToArray();
        //}
        //public byte[] ZipCommitsFilesBytes(string path, IEnumerable<string> shas)
        //{
        //    using var repo = Open(path);
        //    var commits = shas.Select(sha => repo.Lookup<Commit>(sha))
        //                      .Where(c => c != null)
        //                      .OrderBy(c => c!.Author.When)
        //                      .ToList();

        //    var map = new Dictionary<string, Blob>();
        //    foreach (var commit in commits)
        //    {
        //        var parent = commit!.Parents.FirstOrDefault();
        //        if (parent == null)
        //        {
        //            foreach (var (filePath, blob) in TraverseTree(commit!.Tree))
        //                map[filePath.Replace('\\', '/')] = blob; // ✅ 标准化
        //        }
        //        else
        //        {
        //            var changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit!.Tree);
        //            foreach (var c in changes)
        //            {
        //                var normalizedPath = c.Path.Replace('\\', '/'); // ✅ 标准化
        //                if (c.Status == ChangeKind.Deleted)
        //                    map.Remove(normalizedPath);
        //                else
        //                {
        //                    var entry = commit!.Tree[c.Path];
        //                    if (entry?.Target is Blob blob)
        //                        map[normalizedPath] = blob;
        //                }
        //            }
        //        }
        //    }

        //    using var ms = new MemoryStream();
        //    using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true)) // ✅ 使用代码块
        //    {
        //        foreach (var kv in map)
        //        {
        //            var entry = zip.CreateEntry(kv.Key); // kv.Key 已标准化
        //            using var es = entry.Open();
        //            using var content = kv.Value.GetContentStream();
        //            content.CopyTo(es);
        //        }
        //    }

        //    return ms.ToArray();
        //}
        public byte[] ZipCommitsFilesBytes(string path, IEnumerable<string> shas)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("仓库路径不能为空", nameof(path));
            if (shas == null || !shas.Any())
                throw new ArgumentException("提交列表不能为空", nameof(shas));

            using var repo = Open(path);
            var commits = shas.Select(sha => repo.Lookup<Commit>(sha))
                              .Where(c => c != null)
                              .OrderBy(c => c!.Author.When)
                              .ToList();

            if (commits.Count == 0)
                return Array.Empty<byte>();

            var map = new Dictionary<string, Blob>();
            foreach (var commit in commits)
            {
                var parent = commit!.Parents.FirstOrDefault();
                if (parent == null)
                {
                    // 首次提交：遍历所有文件
                    foreach (var (filePath, blob) in TraverseTree(commit!.Tree))
                    {
                        var normalizedPath = filePath.Replace('\\', '/'); // ✅ 标准化路径
                        map[normalizedPath] = blob;
                    }
                }
                else
                {
                    // 后续提交：只处理变更
                    var changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit!.Tree);
                    foreach (var c in changes)
                    {
                        var normalizedPath = c.Path.Replace('\\', '/'); // ✅ 标准化路径
                        if (c.Status == ChangeKind.Deleted)
                        {
                            map.Remove(normalizedPath);
                        }
                        else
                        {
                            var entry = commit!.Tree[c.Path];
                            if (entry?.Target is Blob blob)
                            {
                                map[normalizedPath] = blob;
                            }
                        }
                    }
                }
            }

            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true)) // ✅ 使用代码块确保释放
            {
                foreach (var kv in map)
                {
                    var entry = zip.CreateEntry(kv.Key); // kv.Key 已标准化
                    using var es = entry.Open();
                    using var content = kv.Value.GetContentStream();
                    content.CopyTo(es);
                }
            } // ✅ zip 在此处完成写入并关闭

            return ms.ToArray(); // ✅ 返回完整的 ZIP 数据
        }

        public string Commit(string path, IEnumerable<string> files, string message, string authorName, string authorEmail)
        {
            using var repo = Open(path);
            foreach (var f in files) Commands.Stage(repo, f);
            var sig = new Signature(authorName, authorEmail, DateTimeOffset.Now);
            var commit = repo.Commit(message, sig, sig);
            return commit.Sha;
        }

        public string Pull(string path, string name, string email, bool rebase)
        {
            using var repo = Open(path);
            var sig = new Signature(name, email, DateTimeOffset.Now);
            var opts = new PullOptions { FetchOptions = new FetchOptions(), MergeOptions = new MergeOptions { FastForwardStrategy = rebase ? FastForwardStrategy.FastForwardOnly : FastForwardStrategy.Default } };
            var result = Commands.Pull(repo, sig, opts);
            return result.Status.ToString();
        }

        public string Push(string path, string remoteName, string? username, string? password)
        {
            using var repo = Open(path);
            var remote = repo.Network.Remotes[remoteName];
            var opts = new PushOptions();
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                opts.CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = username, Password = password };
            }
            repo.Network.Push(remote, repo.Head.CanonicalName, opts);
            return "OK";
        }
    }
}