using System.Collections.Generic;

namespace TaskManagerPro.Models
{
    public class UserRoleViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }  // Change from IList<string> to string
        public List<string> AvailableRoles { get; set; }
    }

}
