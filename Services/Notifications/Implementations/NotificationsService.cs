using Amazon.Runtime.Internal.Util;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Config.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Web.CodeGeneration;
using NuevaLuz.Fonoteca.Models;
using NuevaLuz.Fonoteca.Services.Fonoteca.Interfaces;
using NuevaLuz.Fonoteca.Services.Notifications.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NuevaLuz.Fonoteca.Services.Notifications.Implementations
{
    public class NotificationsService : INotificationsService
    {
        private AmazonSimpleNotificationServiceClient _client;
        private List<Topic> _topics;
        private ISettings _settings { get; }
        private IFonotecaService _fonotecaService { get; }
        private ILogger<NotificationsService> _logger { get; }

        public NotificationsService(ISettings settings, IFonotecaService fonotecaService, ILogger<NotificationsService> logger)
        {
            _settings = settings;
            _fonotecaService = fonotecaService;
            _client = new AmazonSimpleNotificationServiceClient();
            _logger = logger;
        }

        public async Task<string> CreateEndpoint(string deviceToken, string platform)
        {
            var endPointResponse = await _client.CreatePlatformEndpointAsync(
                new CreatePlatformEndpointRequest
                {
                    Token = deviceToken,
                    PlatformApplicationArn = platform == "iOS" ?
                        _settings.AwsPlatformApplicationArnIOS :
                        _settings.AwsPlatformApplicationArnAndroid
                }
            );

            if (endPointResponse.HttpStatusCode == HttpStatusCode.OK)
            {
                return endPointResponse.EndpointArn;
            }
            else
            {
                _logger.LogDebug("Error creando endpoint: " + endPointResponse.HttpStatusCode);
                return "";
            }
        }

        public async Task DeleteEndpoint(string endpoint)
        {
            try
            {
                var response = await _client.DeleteEndpointAsync(new DeleteEndpointRequest { EndpointArn = endpoint });

                if (response.HttpStatusCode != HttpStatusCode.OK)
                {
                    _logger.LogDebug($"Error eliminando endpoint: {response.HttpStatusCode}");
                }
            }
            catch { /*Silent error in case endpoint doesn´t exist */ }
        }

        public async Task<SNSSubscriptions> SynchSubscriptions(string session, string deviceToken, string platform, SNSSubscriptions notificationsSubscriptions)
        {
            var arnPlatform = platform == "iOS" ?
                        _settings.AwsPlatformApplicationArnIOS :
                        _settings.AwsPlatformApplicationArnAndroid;

            if (string.IsNullOrEmpty(deviceToken)) return notificationsSubscriptions;

            if (string.IsNullOrEmpty(notificationsSubscriptions.ApplicationEndPoint) ||
                notificationsSubscriptions.DeviceToken != deviceToken)
            {
                // **********************************************
                // de-register old endpoint and all subscriptions
                if (!string.IsNullOrEmpty(notificationsSubscriptions.ApplicationEndPoint))
                {
                    try
                    {
                        var response = await _client.DeleteEndpointAsync(new DeleteEndpointRequest { EndpointArn = notificationsSubscriptions.ApplicationEndPoint });
                    }
                    catch { /*Silent error in case endpoint doesn´t exist */ }

                    notificationsSubscriptions.ApplicationEndPoint = null;

                    foreach (var sub in notificationsSubscriptions.Subscriptions)
                    {
                        try
                        {
                            await _client.UnsubscribeAsync(sub.Value);
                        }
                        catch { /*Silent error in case endpoint doesn´t exist */ }
                    }

                    notificationsSubscriptions.Subscriptions.Clear();
                }

                _logger.LogError("Device token: " + deviceToken);

                // register with SNS to create a new endpoint
                var endPointResponse = await _client.CreatePlatformEndpointAsync(
                    new CreatePlatformEndpointRequest
                    {
                        Token = deviceToken,
                        PlatformApplicationArn = arnPlatform
                    }
                );

                // Save device token and application endpoint created
                notificationsSubscriptions.DeviceToken = deviceToken;
                notificationsSubscriptions.ApplicationEndPoint = endPointResponse.EndpointArn;
            }


            // Retrieve subscriptions
            var subscriptions = await _fonotecaService.GetUserSubscriptions(session, false);

            if (subscriptions == null) subscriptions = new UserSubscriptions { Subscriptions = new List<Models.Subscription>() };

            // Register non existings subscriptions
            var subscriptionsCodes = subscriptions.Subscriptions.Select(s => s.Code).ToList();
            foreach (var code in subscriptionsCodes)
            {
                if (!notificationsSubscriptions.Subscriptions.ContainsKey(code))
                {
                    var topicArn = _settings.AwsTopicArn;
                    topicArn += string.IsNullOrEmpty(code) ? "" : $"-{code}";

                    if (!await TopicExists(topicArn, _client))
                    {
                        var topicResponse = await _client.CreateTopicAsync(new CreateTopicRequest { Name = $"{_settings.AwsTopicName}-{code}" });

                        topicArn = topicResponse.TopicArn;
                    }

                    // Subscribe
                    var subscribeResponse = await _client.SubscribeAsync(new SubscribeRequest
                    {
                        Protocol = "application",
                        Endpoint = notificationsSubscriptions.ApplicationEndPoint,
                        TopicArn = topicArn
                    });

                    // Add to the list
                    notificationsSubscriptions.Subscriptions.Add(code, subscribeResponse.SubscriptionArn);
                }
            }

            // Remove subscriptions not in user list
            var currentSubscriptions = notificationsSubscriptions.Subscriptions.ToList();
            foreach (var subs in currentSubscriptions)
            {
                if (!subscriptionsCodes.Contains(subs.Key))
                {
                    try
                    {
                        await _client.UnsubscribeAsync(subs.Value);
                    }
                    catch { /*Silent error in case endpoint doesn´t exist */ }

                    notificationsSubscriptions.Subscriptions.Remove(subs.Key);
                }
            }

            return notificationsSubscriptions;
        }

        public async Task<bool> TopicExists(string topic, AmazonSimpleNotificationServiceClient client)
        {
            string nextToken = null;
            if (_topics == null)
            {
                _topics = new List<Topic>();
                do
                {
                    var topicsResponse = await client.ListTopicsAsync(nextToken);
                    _topics.AddRange(topicsResponse.Topics);
                    nextToken = topicsResponse.NextToken;
                } while (nextToken != null);
            }

            return _topics.Any(a => a.TopicArn == topic);
        }

        public async Task Unsubscribe(string endpoint)
        {
            try
            {
                await _client.UnsubscribeAsync(endpoint);
            }
            catch { /*Silent error in case endpoint doesn´t exist */ }
        }
    }
}
