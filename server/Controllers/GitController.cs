using GitWeb.Api.Services;
using LibGit2Sharp;
using Microsoft.AspNetCore.Mvc;
namespace GitWeb.Api.Controllers
{
    [ApiController]
    [Route("api/git")]
    public class GitController : ControllerBase
    {
        private readonly GitService _git;
        public GitController(GitService git)
        {
            _git = git;
        }

        public record CommitRequest(string path, IEnumerable<string> files, string message, string authorName, string authorEmail);
        public record PullRequest(string path, string name, string email, bool rebase);
        public record PushRequest(string path, string remoteName, string? username, string? password);
        public record DownloadCommitRequest(string path, string sha);
        public record DownloadCommitsRequest(string path, IEnumerable<string> shas);

        [HttpPost("commit")]
        public IActionResult Commit([FromBody] CommitRequest req)
        {
            if (!_git.IsValidRepoPath(req.path)) return BadRequest("Invalid repo path");
            var sha = _git.Commit(req.path, req.files, req.message, req.authorName, req.authorEmail);
            return Ok(new { sha });
        }

        [HttpPost("pull")]
        public IActionResult Pull([FromBody] PullRequest req)
        {
            if (!_git.IsValidRepoPath(req.path)) return BadRequest("Invalid repo path");
            var status = _git.Pull(req.path, req.name, req.email, req.rebase);
            return Ok(new { status });
        }

        [HttpPost("push")]
        public IActionResult Push([FromBody] PushRequest req)
        {
            if (!_git.IsValidRepoPath(req.path)) return BadRequest("Invalid repo path");
            var result = _git.Push(req.path, req.remoteName, req.username, req.password);
            return Ok(new { result });
        }

        //[HttpPost("download-commit")]
        //public IActionResult DownloadCommit([FromBody] DownloadCommitRequest req)
        //{
        //    if (!_git.IsValidRepoPath(req.path)) return BadRequest("Invalid repo path");
        //    var bytes = _git.ZipCommitFilesBytes(req.path, req.sha);
        //    return File(bytes, "application/zip", $"commit_{req.sha[..7]}.zip");
        //}
        [HttpPost("download-commit")]
        public IActionResult DownloadCommit([FromBody] DownloadCommitRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.path) || string.IsNullOrWhiteSpace(req.sha))
                return BadRequest("参数不完整");

            if (!_git.IsValidRepoPath(req.path))
                return BadRequest("无效仓库路径");

            try
            {
                var bytes = _git.ZipCommitFilesBytes(req.path, req.sha);
                if (bytes.Length == 0)
                    return NotFound("该提交没有文件");

                return File(bytes, "application/zip", $"commit_{req.sha[..7]}.zip");
            }
            catch (Exception ex)
            {
                // 记录日志
                return StatusCode(500, $"生成ZIP失败: {ex.Message}");
            }
        }

        //[HttpPost("download-commits")]
        //public IActionResult DownloadCommits([FromBody] DownloadCommitsRequest req)
        //{
        //    if (!_git.IsValidRepoPath(req.path)) return BadRequest("Invalid repo path");
        //    var bytes = _git.ZipCommitsFilesBytes(req.path, req.shas);
        //    return File(bytes, "application/zip", "commits.zip");
        //}
        [HttpPost("download-commits")]
        public IActionResult DownloadCommits([FromBody] DownloadCommitsRequest req)
        {
            // 参数验证
            if (req == null || string.IsNullOrWhiteSpace(req.path))
                return BadRequest("参数不完整：仓库路径为空");

            if (req.shas == null || !req.shas.Any())
                return BadRequest("参数不完整：未指定任何提交");

            if (req.shas.Count() > 100) // 防止滥用
                return BadRequest("请求提交数量过多（最多100个）");

            // 验证仓库
            if (!_git.IsValidRepoPath(req.path))
                return BadRequest("无效仓库路径");

            try
            {
                var bytes = _git.ZipCommitsFilesBytes(req.path, req.shas);

                if (bytes == null || bytes.Length == 0)
                    return NotFound("这些提交中没有文件");

                // 添加时间戳避免文件名冲突
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                return File(bytes, "application/zip", $"commits_{timestamp}.zip");
            }
            catch (ArgumentException ex) // 无效的SHA格式
            {
                return BadRequest($"提交哈希格式错误: {ex.Message}");
            }
            catch (NotFoundException ex) // 提交不存在
            {
                return NotFound($"提交不存在: {ex.Message}");
            }
            catch (Exception ex)
            {
                // 建议记录日志：_logger.LogError(ex, "下载多个提交ZIP失败");
                return StatusCode(500, $"生成ZIP失败: {ex.Message}");
            }
        }
    }
}