using System;

namespace FunctionAppWithDB.Shared
{
    public class UserSignup
    {
        public int UserSignupId { get; set; }
        public string ScreenName { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public string Location { get; set; }
        public DateTime CreatedUTC { get; set; }
    }
}
