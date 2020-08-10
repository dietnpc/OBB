using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OBB.Models;
using OBB.Services;
using Tweetinvi.Models;
using TUser = Tweetinvi.User;

namespace OBB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IMemoryCache _memoryCache;
        private readonly ITwitterService _twitterService;

        List<long> _newFollowerIds = new List<long>();
        List<long> _lostFollowerIds = new List<long>();
        List<long> _sameFollowerIds = new List<long>();
        List<long> _newFollowingIds = new List<long>();
        List<long> _lostFollowingIds = new List<long>();
        List<long> _sameFollowingIds = new List<long>();

        public HomeController(IMemoryCache memoryCache, ITwitterService twitterService, SignInManager<IdentityUser> signInManager, ILogger<HomeController> logger)
        {
            _logger = logger;
            _signInManager = signInManager;
            _memoryCache = memoryCache;
            _twitterService = twitterService;
        }
        private T GetCache<T>(string key, Func<T> GetValue, bool Override = false)
        {
            if (!_memoryCache.TryGetValue(key, out T cacheValue) || Override)
            {
                cacheValue = GetValue();
                _memoryCache.Set(key, cacheValue, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(1) });
            }
            return cacheValue;
        }
        public IActionResult Index([FromQuery]bool Override = false)
        {
            ViewData.Add("TwitterView", GetModelData(Override));
            return View();
        }

        public IActionResult Save()
        {
            string contentType = "application/octet-stream";
            var model = GetModelData();
            var data = JsonConvert.SerializeObject(model);
            var dataEnc = Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
            var dataEncBytes = Encoding.UTF8.GetBytes(dataEnc);
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            var name = rgx.Replace(User.Identity.Name, "");
            var fileName = $"{name}{model.FollowerIds.Count() + model.FollowingIds.Count()}_{DateTime.Now.Ticks}.obb";
            FileContentResult fileResult = new FileContentResult(dataEncBytes, contentType)
            {
                FileDownloadName = fileName
            };
            return fileResult;
        }
        [HttpPost]
        public IActionResult SendMessage(IList<long> UserIds, string Message)
        {
            _twitterService.SendMessage(UserIds, Message);
            return Ok();
        }
        [HttpPost]
        public IActionResult Refollow(IList<long> UserIds)
        {
            _twitterService.Follow(UserIds);
            return Ok();
        }
        [HttpGet]
        public IActionResult Import()
        {
            ViewData.Add("TwitterView", GetModelData());
            return View("Index");
        }
        [HttpPost]
        public IActionResult Import(IFormFile file)
        {
            if (file == null) { ViewData.Add("TwitterView", GetModelData()); return View("Index"); }
            var dataEncBytes = file.OpenReadStream().ReadAllBytes();
            var dataEnc = Encoding.UTF8.GetString(dataEncBytes);
            var dataBytes = Convert.FromBase64String(dataEnc);
            var data = Encoding.UTF8.GetString(dataBytes);
            var ImportModel = JsonConvert.DeserializeObject<TwitterView>(data);
            var CurrentModel = GetModelData(true);

            _lostFollowerIds = ImportModel.FollowerIds.Except(CurrentModel.FollowerIds).ToList();
            _newFollowerIds = CurrentModel.FollowerIds.Except(CurrentModel.FollowerIds).ToList();
            _sameFollowerIds = ImportModel.FollowerIds.Intersect(CurrentModel.FollowerIds).ToList();

            _lostFollowingIds = ImportModel.FollowingIds.Except(CurrentModel.FollowingIds).ToList();
            _newFollowingIds = CurrentModel.FollowingIds.Except(ImportModel.FollowingIds).ToList();
            _sameFollowingIds = ImportModel.FollowingIds.Intersect(CurrentModel.FollowingIds).ToList();

            _memoryCache.Set(HomeCacheKeys.NewFollowerIds, _newFollowerIds, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(1) });
            _memoryCache.Set(HomeCacheKeys.LostFollowerIds, _lostFollowerIds, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(1) });
            _memoryCache.Set(HomeCacheKeys.SameFollowerIds, _sameFollowerIds, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(1) });

            _memoryCache.Set(HomeCacheKeys.NewFollowingIds, _newFollowingIds, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(1) });
            _memoryCache.Set(HomeCacheKeys.LostFollowingIds, _lostFollowingIds, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(1) });
            _memoryCache.Set(HomeCacheKeys.SameFollowingIds, _sameFollowingIds, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(1) });

            ViewData.Add("TwitterView", GetModelData());
            return View("Index");
        }

        public IActionResult Privacy()
        {
            ViewData.Add("TwitterView", GetModelData());
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        private TwitterView GetModelData(bool Override = false)
        {
            List<long> followerIds = new List<long>();
            List<long> followingIds = new List<long>();
            List<long> newFollowerIds = new List<long>();
            List<long> lostFollowerIds = new List<long>();
            List<long> sameFollowerIds = new List<long>();
            List<long> newFollowingIds = new List<long>();
            List<long> lostFollowingIds = new List<long>();
            List<long> sameFollowingIds = new List<long>();
            var profileImg = "";
            var screenName = string.Empty;
            if (_signInManager.IsSignedIn(User))
            {
                screenName = GetCache(HomeCacheKeys.ScreenName, () => Tweetinvi.Account.GetCurrentAccountSettings().ScreenName, Override);
                followerIds = GetCache(HomeCacheKeys.FollowerIds, () => TUser.GetFollowerIds(screenName, 5000).ToList(), Override);
                newFollowerIds = GetCache(HomeCacheKeys.NewFollowerIds, () => _newFollowerIds, Override);
                lostFollowerIds = GetCache(HomeCacheKeys.LostFollowerIds, () => _lostFollowerIds, Override);
                sameFollowerIds = GetCache(HomeCacheKeys.SameFollowerIds, () => _sameFollowerIds, Override);
                followingIds = GetCache(HomeCacheKeys.FollowingIds, () => TUser.GetFriendIds(screenName, 5000).ToList(), Override);
                newFollowingIds = GetCache(HomeCacheKeys.NewFollowingIds, () => _newFollowingIds, Override);
                lostFollowingIds = GetCache(HomeCacheKeys.LostFollowingIds, () => _lostFollowingIds, Override);
                sameFollowingIds = GetCache(HomeCacheKeys.SameFollowingIds, () => _sameFollowingIds, Override);
                profileImg = GetCache(HomeCacheKeys.ProfileImageB64, () => Convert.ToBase64String(TUser.GetProfileImageStream(TUser.GetAuthenticatedUser(), ImageSize.normal).ReadAllBytes()), Override);
            }
            return new TwitterView
            {
                ScreenName = screenName,
                FollowerIds = followerIds,
                NewFollowerIds = newFollowerIds,
                LostFollowerIds = lostFollowerIds,
                SameFollowerIds = sameFollowerIds,
                FollowingIds = followingIds,
                NewFollowingIds = newFollowingIds,
                LostFollowingIds = lostFollowingIds,
                SameFollowingIds = sameFollowingIds,
                ProfileImgB64 = profileImg
            };
        }
    }
}