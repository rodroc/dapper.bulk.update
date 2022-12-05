using System;
using System.Collections.Generic;

namespace MyProfileOnly.Models
{
    public class Profile
    {
#nullable enable
        public int id { get; set; }
        public string? display_name { get; set; }
        public int? id_user { get; set; }
#nullable enable
        public string? email { get; set; }
        public int is_valid_email { get; set; }
        public int is_local { get; set; }
    }

    public class UserProfile
    {
#nullable enable
        public int? id_user { get; set; }
#nullable enable
        public string? userEmail { get; set; }
#nullable enable
        public string? userDisplayName { get; set; }
        public int id_profile { get; set; }
        public string? profileEmail { get; set; }
        public string? profileDisplayName { get; set; }
        public int is_valid_email { get; set; }
        public int is_local { get; set; }
    }

    public class ProfileOnly
    {
        public int id_profile { get; set; }
    }

    public class FunctionInput
    {
        public bool Deploy { get; set; }
        public List<int>? InstanceIDs { get; set; }
    }

}