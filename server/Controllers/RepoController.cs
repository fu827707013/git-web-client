using GitWeb.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace GitWeb.Api.Controllers
{
    [ApiController]
    [Route("api/repo")]
    public class RepoController : ControllerBase
    {
        private readonly GitService _git;
        private readonly RepoConfigService _config;

        public RepoController(GitService git, RepoConfigService config)
        {
            _git = git;
            _config = config;
        }

        public record LoadRepoRequest(string path, string? name = null);

        [HttpPost("load")]
        public IActionResult Load([FromBody] LoadRepoRequest req)
        {
            if (!_git.IsValidRepoPath(req.path)) return BadRequest("Invalid repo path");

            // 如果提供了仓库名称，保存到配置文件
            if (!string.IsNullOrWhiteSpace(req.name))
            {
                _config.AddOrUpdateRepository(req.name, req.path);
            }

            return Ok(new { path = req.path, ok = true });
        }

        [HttpGet("saved")]
        public IActionResult GetSavedRepositories()
        {
            var repos = _config.GetRepositories();
            return Ok(repos);
        }

        [HttpDelete("saved/{name}")]
        public IActionResult DeleteSavedRepository(string name)
        {
            _config.RemoveRepository(name);
            return Ok(new { ok = true });
        }

        [HttpGet("saved/{name}")]
        public IActionResult GetSavedRepository(string name)
        {
            var repo = _config.GetRepository(name);
            if (repo == null) return NotFound();

            _config.UpdateLastAccessed(name);
            return Ok(repo);
        }

        [HttpGet("status")]
        public IActionResult Status([FromQuery] string path)
        {
            if (!_git.IsValidRepoPath(path)) return BadRequest("Invalid repo path");
            var s = _git.GetStatus(path);
            return Ok(s);
        }

        [HttpGet("commits")]
        public IActionResult Commits([FromQuery] string path, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? author = null, [FromQuery] string? q = null)
        {
            if (!_git.IsValidRepoPath(path)) return BadRequest("Invalid repo path");
            var items = _git.GetCommits(path, page, pageSize, author, q);
            return Ok(new { items });
        }

        [HttpGet("commit-details")]
        public IActionResult GetCommitDetails([FromQuery] string path, [FromQuery] string sha)
        {
            if (!_git.IsValidRepoPath(path)) return BadRequest("Invalid repo path");
            if (string.IsNullOrWhiteSpace(sha)) return BadRequest("SHA is required");

            var details = _git.GetCommitDetails(path, sha);
            if (details == null) return NotFound("Commit not found");

            return Ok(details);
        }

        [HttpGet("branches")]
        public IActionResult GetBranches([FromQuery] string path)
        {
            if (!_git.IsValidRepoPath(path)) return BadRequest("Invalid repo path");
            var branches = _git.GetBranches(path);
            return Ok(branches);
        }

        [HttpGet("remotes")]
        public IActionResult GetRemotes([FromQuery] string path)
        {
            if (!_git.IsValidRepoPath(path)) return BadRequest("Invalid repo path");
            var remotes = _git.GetRemotes(path);
            return Ok(remotes);
        }

        [HttpGet("tags")]
        public IActionResult GetTags([FromQuery] string path)
        {
            if (!_git.IsValidRepoPath(path)) return BadRequest("Invalid repo path");
            var tags = _git.GetTags(path);
            return Ok(tags);
        }
    }
}