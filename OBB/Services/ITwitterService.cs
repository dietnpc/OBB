using System.Collections.Generic;
using Tweetinvi.Models;

namespace OBB.Services
{
    public interface ITwitterService
    {
        public void SendTweet(string message);
        public void SendMessage(IList<long> userIds, string message);
        public void Follow(IList<long> userIds);
        public void UnFollow(IList<long> userIds);
        public void Block(IList<long> userIds);
        public void UnBlock(IList<long> userIds);
        public void ReportUserForSpam(IList<long> userIds);
        public List<IUser> GetUsers(IList<long> userIds, UserTypes type, bool Override = false);
        public string GetUserImageUrl(IUser user);
        public string GetUserBannerImgUrl(IUser user);
        public int GetUserFollowerCount(IUser user);
        public int GetUserFollowingCount(IUser user);
    }
}