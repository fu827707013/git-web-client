using GitWeb.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace GitWeb.Api.Controllers
{
    [ApiController]
    [Route("api/repo")]
    public class RepoController : ControllerBase
    {
        private readonly GitService _git;
        public RepoController(GitService git)
        {
            _git = git;
        }

        public record LoadRepoRequest(string path);

        [HttpPost("load")]
        public IActionResult Load([FromBody] LoadRepoRequest req)
        {
            if (!_git.IsValidRepoPath(req.path)) return BadRequest("Invalid repo path");
            return Ok(new { path = req.path, ok = true });
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
    }
}