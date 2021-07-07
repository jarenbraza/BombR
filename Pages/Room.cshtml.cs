using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BombermanAspNet.Pages
{
    public class RoomModel : PageModel
    {
        private const string PlayerNameCookieKey = "playerName";

        public string RoomName { get; set; }

        [FromQuery(Name = "playerName")]
        public string PlayerName { get; set; }

        public void OnGet(string roomName)
        {
            RoomName = roomName;

            if (Request.Cookies.ContainsKey(PlayerNameCookieKey))
            {
                PlayerName = Request.Cookies[PlayerNameCookieKey];
            }
        }
    }
}
