using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace BombermanAspNet.Pages
{
    public class LobbyModel : PageModel
    {
        private readonly ILogger<LobbyModel> _logger;

        public LobbyModel(ILogger<LobbyModel> logger)
        {
            _logger = logger;
        }
    }
}
