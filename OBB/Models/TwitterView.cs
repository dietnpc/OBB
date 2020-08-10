using System.Collections.Generic;

namespace OBB.Models
{
    public class TwitterView
    {
        public string ScreenName { get; set; }
        public string ProfileImgB64 { get; set; }
        public List<long> FollowerIds { get; set; }
        public List<long> NewFollowerIds { get; set; }
        public List<long> LostFollowerIds { get; set; }
        public List<long> SameFollowerIds { get; set; }
        public List<long> FollowingIds { get; set; }
        public List<long> NewFollowingIds { get; set; }
        public List<long> LostFollowingIds { get; set; }
        public List<long> SameFollowingIds { get; set; }
    }
}