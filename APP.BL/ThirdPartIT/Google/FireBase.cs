using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP.BL.ThirdPartIT.Google
{
    public static class FireBaseUtils
    {

        private static readonly string ServiceAccountFile = AppDomain.CurrentDomain.BaseDirectory +  @"\ExternalApiKeyAndCert\FireBaseIntegration_cert.json";
        private static readonly string ApiKeyFile = AppDomain.CurrentDomain.BaseDirectory + @"\ExternalApiKeyAndCert\FireBaseIntegration_apikey.txt";

        private static readonly Lazy<FirebaseApp> DefaultFirebaseApp = new Lazy<FirebaseApp>(
            () =>
            {
                var options = new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(ServiceAccountFile),
                };
                return FirebaseApp.Create(options);
            }, true);

        public static FirebaseApp EnsureDefaultApp()
        {
            return DefaultFirebaseApp.Value;
        }

        public static string GetApiKey()
        {
            return System.IO.File.ReadAllText(ApiKeyFile).Trim();
        }
    }

    public class FirebaseMessagingTest
    {
        public FirebaseMessagingTest()
        {
            FireBaseUtils.EnsureDefaultApp();
        }

    
        public async Task Send()
        {
            var message = new Message()
            {
                Topic = "foo-bar",
                Notification = new Notification()
                {
                    Title = "Title",
                    Body = "Body",
                    ImageUrl = "https://example.com/image.png",
                },
                Android = new AndroidConfig()
                {
                    Priority = Priority.Normal,
                    TimeToLive = TimeSpan.FromHours(1),
                    RestrictedPackageName = "com.myfirebase",
                },
            };
            var id = await FirebaseMessaging.DefaultInstance.SendAsync(message, dryRun: false);
          
        }

        
        public async Task SendAll()
        {
            var message1 = new Message()
            {
                Topic = "test-topic",
                Notification = new Notification()
                {
                    Title = "Title",
                    Body = "Body",
                    ImageUrl = "https://example.com/image.png",
                },
                Android = new AndroidConfig()
                {
                    Priority = Priority.Normal,
                    TimeToLive = TimeSpan.FromHours(1),
                    RestrictedPackageName = "com.myfirebase",
                },
            };
            var message2 = new Message()
            {
                Topic = "test-topic",
                Notification = new Notification()
                {
                    Title = "Title",
                    Body = "Body",
                },
                Android = new AndroidConfig()
                {
                    Priority = Priority.Normal,
                    TimeToLive = TimeSpan.FromHours(1),
                    RestrictedPackageName = "com.myfirebase",
                },
            };
            var response = await FirebaseMessaging.DefaultInstance.SendAllAsync(new[] { message1, message2 }, dryRun: false);

            var res = response;
            var count = response.SuccessCount;
            var msgid = response.Responses[0].MessageId;

            //Assert.NotNull(response);
            //Assert.Equal(2, response.SuccessCount);
            //Assert.True(!string.IsNullOrEmpty(response.Responses[0].MessageId));
            //Assert.Matches(new Regex("^projects/.*/messages/.*$"), response.Responses[0].MessageId);
            //Assert.True(!string.IsNullOrEmpty(response.Responses[1].MessageId));
            //Assert.Matches(new Regex("^projects/.*/messages/.*$"), response.Responses[1].MessageId);
        }

        
        public async Task SendMulticast()
        {
            var multicastMessage = new MulticastMessage
            {
                Notification = new Notification()
                {
                    
                    Title = "Title",
                    Body = "Body",
                },
                Android = new AndroidConfig()
                {
                    Priority = Priority.Normal,
                    TimeToLive = TimeSpan.FromHours(1),
                    RestrictedPackageName = "com.myfirebase",
                },
                Tokens = new[]
                {
                    app_server_token                  
                },
            };
            //var response = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(multicastMessage, dryRun: true);

            var response = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(multicastMessage, dryRun: false);
            //Assert.NotNull(response);
            //Assert.Equal(2, response.FailureCount);
            //Assert.NotNull(response.Responses[0].Exception);
            //Assert.NotNull(response.Responses[1].Exception);

            //FirebaseMessaging.DefaultInstance.
        }
        //[Mon Sep 28 2020 13:16:35.527]
        //LOG fcm token 
                  //
        static readonly string app_server_token = System.Environment.GetEnvironmentVariable("FIREBASE_SERVER_TOKEN") ?? string.Empty;
        public async Task SubscribeToTopic()
        {
            var response = await FirebaseMessaging.DefaultInstance.SubscribeToTopicAsync(
                new List<string> { app_server_token}, "test-topic");
            //Assert.NotNull(response);
            //Assert.Equal(2, response.FailureCount);
            //Assert.Equal("invalid-argument", response.Errors[0].Reason);
            //Assert.Equal(0, response.Errors[0].Index);
            //Assert.Equal("invalid-argument", response.Errors[1].Reason);
            //Assert.Equal(1, response.Errors[1].Index);
        }

        
        public async Task UnsubscribeFromTopic()
        {
            var response = await FirebaseMessaging.DefaultInstance.UnsubscribeFromTopicAsync(
                new List<string> { app_server_token, "token2" }, "test-topic");
            //Assert.NotNull(response);
            //Assert.Equal(2, response.FailureCount);
            //Assert.Equal("invalid-argument", response.Errors[0].Reason);
            //Assert.Equal(0, response.Errors[0].Index);
            //Assert.Equal("invalid-argument", response.Errors[1].Reason);
            //Assert.Equal(1, response.Errors[1].Index);
        }
    }

}
