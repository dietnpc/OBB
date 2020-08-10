using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace OBB.Services
{
    public class TwitterService : ITwitterService
    {
        private readonly IMemoryCache _memoryCache;
        public TwitterService(IMemoryCache memoryCache) => _memoryCache = memoryCache;
        public void SendTweet(string message = "Testing") => Tweet.PublishTweet(message);
        public void SendMessage(IList<long> userIds, string message = "Testing")
        {
            foreach (var userId in userIds) { Message.PublishMessage(new PublishMessageParameters(message, userId)); }
        }
        public void Follow(IList<long> userIds)
        {
            foreach (var userId in userIds) { User.FollowUser(userId); }
        }
        public void UnFollow(IList<long> userIds)
        {
            foreach(var userId in userIds) { User.UnFollowUser(userId); }
        }
        public void Block(IList<long> userIds)
        {
            foreach(var userId in userIds) { User.BlockUser(userId); }
        }
        public void UnBlock(IList<long> userIds)
        {
            foreach(var userId in userIds) { User.UnBlockUser(userId); }
        }
        public void ReportUserForSpam(IList<long> userIds)
        {
            foreach(var userId in userIds) { User.ReportUserForSpam(userId); }
        }
        public List<IUser> GetUsers(IList<long> userIds, UserTypes type, bool Override = false)
        {
            var bOverride = false;
            var cached = GetCache($"_{type}", () => User.GetUsersFromIds(userIds.AsEnumerable()).ToList(), false);
            bOverride = cached.Count != userIds.Count;
            return bOverride
                ? GetCache($"_{type}", () => User.GetUsersFromIds(userIds.AsEnumerable()).ToList(), true)
                : cached;
        }
        public string GetUserImageUrl(IUser user) => user.ProfileImageUrl;
        public string GetUserBannerImgUrl(IUser user) => user.ProfileBackgroundImageUrl;
        public int GetUserFollowerCount(IUser user) => user.FollowersCount;
        public int GetUserFollowingCount(IUser user) => user.FriendsCount;
        private T GetCache<T>(string key, Func<T> GetValue, bool Override = false)
        {
            if (!_memoryCache.TryGetValue(key, out T cacheValue) || Override)
            {
                cacheValue = GetValue();
                _memoryCache.Set(key, cacheValue, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(1) });
            }
            return cacheValue;
        }
    }
}