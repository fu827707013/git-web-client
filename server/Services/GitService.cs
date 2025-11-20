using LibGit2Sharp;
using System.Diagnostics;
using System.IO.Compression;
namespace GitWeb.Api.Services
{
    public class GitService
    {
        // 从系统 git credential helper 获取凭据
        private (string? username, string? password) GetCredentialsFromGit(string url)
        {
            try
            {
                var uri = new Uri(url);
                var input = $"protocol={uri.Scheme}\nhost={uri.Host}\n\n";

                var psi = new ProcessStartInfo("git", "credential fill")
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return (null, null);

                process.StandardInput.Write(input);
                process.StandardInput.Close();

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0) return (null, null);

                string? username = null, password = null;
                foreach (var line in output.Split('\n'))
                {
                    if (line.StartsWith("username="))
                        username = line.Substring(9).Trim();
                    else if (line.StartsWith("password="))
                        password = line.Substring(9).Trim();
                }

                return (username, password);
            }
            catch
            {
                return (null, null);
            }
        }

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

        // 树节点类 - 优化：扁平化结构，使用短字段名减少 JSON 大小
        public class TreeNode
        {
            public string n { get; set; }  // name
            public string p { get; set; }  // path
            public bool f { get; set; }    // isFolder
            public List<TreeNode>? c { get; set; }  // children (nullable to save space)
            public string? s { get; set; }  // status (null for folders, "M"/"A"/"D"/"R" for files)

            public TreeNode(string name, string path, bool isFolder = false, string? status = null)
            {
                this.n = name;
                this.p = path;
                this.f = isFolder;
                this.c = isFolder ? new List<TreeNode>() : null;
                this.s = status;
            }
        }

        public object GetStatus(string path)
        {
            using var repo = Open(path);
            var branch = repo.Head.FriendlyName;
            var remote = repo.Network.Remotes.FirstOrDefault()?.Url ?? "";
            var status = repo.RetrieveStatus();

            // 辅助方法：将 FileStatus 转换为简洁的状态字符串
            string GetSimpleStatus(FileStatus state)
            {
                if ((state & FileStatus.NewInWorkdir) != 0 || (state & FileStatus.NewInIndex) != 0)
                    return "A"; // Added
                if ((state & FileStatus.DeletedFromWorkdir) != 0 || (state & FileStatus.DeletedFromIndex) != 0)
                    return "D"; // Deleted
                if ((state & FileStatus.ModifiedInWorkdir) != 0 || (state & FileStatus.ModifiedInIndex) != 0)
                    return "M"; // Modified
                if ((state & FileStatus.RenamedInWorkdir) != 0 || (state & FileStatus.RenamedInIndex) != 0)
                    return "R"; // Renamed
                return "U"; // Unknown
            }

            // 构建树形结构 - 优化版本
            TreeNode BuildTree(IEnumerable<StatusEntry> entries)
            {
                var root = new TreeNode("root", "", true);

                foreach (var entry in entries)
                {
                    var filePath = entry.FilePath.Replace('\\', '/');
                    var pathParts = filePath.Split('/');
                    var currentNode = root;

                    for (int i = 0; i < pathParts.Length; i++)
                    {
                        var part = pathParts[i];
                        var isLastPart = i == pathParts.Length - 1;
                        var currentPath = string.Join("/", pathParts.Take(i + 1));

                        // 查找是否已存在该节点
                        var existingNode = currentNode.c?.FirstOrDefault(c => c.n == part);

                        if (existingNode == null)
                        {
                            // 创建新节点（扁平化结构）
                            var newNode = new TreeNode(
                                part,
                                currentPath,
                                !isLastPart,
                                isLastPart ? GetSimpleStatus(entry.State) : null
                            );
                            currentNode.c?.Add(newNode);
                            currentNode = newNode;
                        }
                        else
                        {
                            currentNode = existingNode;
                        }
                    }
                }

                // 递归排序：文件夹在前，文件在后
                void SortTree(TreeNode node)
                {
                    if (node.c != null && node.c.Count > 0)
                    {
                        node.c = node.c
                            .OrderBy(c => c.f ? 0 : 1)
                            .ThenBy(c => c.n)
                            .ToList();
                        foreach (var child in node.c)
                        {
                            SortTree(child);
                        }
                    }
                }

                SortTree(root);
                return root;
            }

            // 获取未暂存和已暂存的文件
            var unstagedEntries = status.Where(s => s.State != FileStatus.Ignored && s.State != FileStatus.Unaltered)
                                        .Where(s => (s.State & FileStatus.ModifiedInWorkdir) != 0 ||
                                                   (s.State & FileStatus.NewInWorkdir) != 0 ||
                                                   (s.State & FileStatus.DeletedFromWorkdir) != 0)
                                        .ToList();

            var stagedEntries = status.Where(s => (s.State & FileStatus.ModifiedInIndex) != 0 ||
                                                 (s.State & FileStatus.NewInIndex) != 0 ||
                                                 (s.State & FileStatus.DeletedFromIndex) != 0)
                                     .ToList();

            // 构建树形结构
            var unstagedTree = BuildTree(unstagedEntries);
            var stagedTree = BuildTree(stagedEntries);

            // 递归计算树中的文件总数
            int CountFiles(TreeNode node)
            {
                if (!node.f) return 1;
                return node.c?.Sum(c => CountFiles(c)) ?? 0;
            }

            var unstagedCount = unstagedTree.c?.Sum(c => CountFiles(c)) ?? 0;
            var stagedCount = stagedTree.c?.Sum(c => CountFiles(c)) ?? 0;

            var notCommitted = status.Count();
            var ahead = repo.Head.TrackingDetails.AheadBy ?? 0;

            return new
            {
                branch,
                remote,
                notCommitted,
                notPushed = ahead,
                unstagedTree = unstagedTree.c,    // 树形结构（使用短字段名）
                stagedTree = stagedTree.c,        // 树形结构（使用短字段名）
                unstagedCount = unstagedCount,    // 文件数量
                stagedCount = stagedCount         // 文件数量
            };
        }

        public IEnumerable<object> GetBranches(string path)
        {
            using var repo = Open(path);
            var currentBranch = repo.Head.FriendlyName;
            var branches = repo.Branches.Where(b => !b.IsRemote).Select(b => new
            {
                name = b.FriendlyName,
                current = b.FriendlyName == currentBranch,
                isHead = b.IsCurrentRepositoryHead,
                tip = b.Tip?.Sha[..7],
                ahead = b.TrackingDetails.AheadBy ?? 0,
                behind = b.TrackingDetails.BehindBy ?? 0
            });
            return branches.ToList();
        }

        public IEnumerable<object> GetRemotes(string path)
        {
            using var repo = Open(path);
            var remotes = repo.Network.Remotes.Select(r => new
            {
                name = r.Name,
                url = r.Url,
                pushUrl = r.PushUrl
            });
            return remotes.ToList();
        }

        public IEnumerable<object> GetTags(string path)
        {
            using var repo = Open(path);
            var tags = repo.Tags.Select(t => new
            {
                name = t.FriendlyName,
                sha = t.Target?.Sha[..7],
                message = (t.Target as Commit)?.MessageShort ?? ""
            });
            return tags.ToList();
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

            // 获取 remote URL 并尝试从系统获取凭据
            var remote = repo.Network.Remotes.FirstOrDefault();
            var fetchOptions = new FetchOptions();
            if (remote != null)
            {
                var (username, password) = GetCredentialsFromGit(remote.Url);
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    fetchOptions.CredentialsProvider = (_url, _user, _cred) =>
                        new UsernamePasswordCredentials { Username = username, Password = password };
                }
            }

            var opts = new PullOptions
            {
                FetchOptions = fetchOptions,
                MergeOptions = new MergeOptions
                {
                    FastForwardStrategy = rebase ? FastForwardStrategy.FastForwardOnly : FastForwardStrategy.Default
                }
            };

            var result = Commands.Pull(repo, sig, opts);
            return result.Status.ToString();
        }

        public string Push(string path, string remoteName, string? username, string? password)
        {
            using var repo = Open(path);
            var remote = repo.Network.Remotes[remoteName];
            var opts = new PushOptions();

            // 如果没有提供凭据，尝试从系统 git credential helper 获取
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                var url = remote.Url;
                (username, password) = GetCredentialsFromGit(url);
            }

            // 设置凭据提供者
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                opts.CredentialsProvider = (_url, _user, _cred) =>
                    new UsernamePasswordCredentials { Username = username, Password = password };
            }

            repo.Network.Push(remote, repo.Head.CanonicalName, opts);
            return "OK";
        }

        public void Fetch(string path, string remoteName)
        {
            using var repo = Open(path);
            var remote = repo.Network.Remotes[remoteName];
            var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);

            // 尝试从系统获取凭据
            var fetchOptions = new FetchOptions();
            var (username, password) = GetCredentialsFromGit(remote.Url);
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                fetchOptions.CredentialsProvider = (_url, _user, _cred) =>
                    new UsernamePasswordCredentials { Username = username, Password = password };
            }

            Commands.Fetch(repo, remote.Name, refSpecs, fetchOptions, null);
        }

        public void StageFile(string path, string filePath)
        {
            using var repo = Open(path);
            Commands.Stage(repo, filePath);
        }

        public void StageAll(string path, IEnumerable<string> filePaths)
        {
            using var repo = Open(path);
            foreach (var filePath in filePaths)
            {
                Commands.Stage(repo, filePath);
            }
        }

        public void UnstageFile(string path, string filePath)
        {
            using var repo = Open(path);
            Commands.Unstage(repo, filePath);
        }

        public void UnstageAll(string path, IEnumerable<string> filePaths)
        {
            using var repo = Open(path);
            foreach (var filePath in filePaths)
            {
                Commands.Unstage(repo, filePath);
            }
        }
    }
}