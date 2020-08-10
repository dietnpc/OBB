using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Http;
using Tweetinvi;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTwitter(this IServiceCollection services, string consumerKey, string consumerSecret, string appTokenKey, string appTokenSecret)
        {
            services.Configure<TwitterOptions>(c =>
            {
                c.ConsumerKey = consumerKey;
                c.ConsumerSecret = consumerSecret;
                c.RetrieveUserDetails = true;
                c.SaveTokens = true;
                c.ClaimActions.MapJsonKey("urn:twitter:profilepicture", "profile_image_url", ClaimTypes.Uri);
                c.Events = new TwitterEvents
                {
                    OnRemoteFailure = HandleOnRemoteFailure
                };
            });
            var twitterCreds = Auth.SetUserCredentials(consumerKey, consumerSecret, appTokenKey, appTokenSecret);
            return services;
        }
        private static async Task HandleOnRemoteFailure(RemoteFailureContext context)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync("<html><body>");
            await context.Response.WriteAsync("A remote failure has occurred: <br>" +
                context.Failure.Message.Split(Environment.NewLine).Select(s => HtmlEncoder.Default.Encode(s) + "<br>").Aggregate((s1, s2) => s1 + s2));
            if (context.Properties != null)
            {
                await context.Response.WriteAsync("Properties:<br>");
                foreach (var pair in context.Properties.Items)
                {
                    await context.Response.WriteAsync($"-{ HtmlEncoder.Default.Encode(pair.Key)}={ HtmlEncoder.Default.Encode(pair.Value)}<br>");
                }
            }
            await context.Response.WriteAsync("<a href=\"/\">Home</a>");
            await context.Response.WriteAsync("</body></html>");
            context.HandleResponse();
        }
    }
}