﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer.Core;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.IO;
using Stormancer.Server.Components;
using Server.Plugins.Steam;
using Stormancer.Diagnostics;
using Stormancer;
using System.Collections.Concurrent;

namespace Server.Users
{
    public class SteamAuthenticationProvider : IAuthenticationProvider, IUserSessionEventHandler
    {
        private ConcurrentDictionary<ulong, string> _vacSessions = new ConcurrentDictionary<ulong, string>();
        public const string PROVIDER_NAME = "steam";
        private const string ClaimPath = "steamid";
        private bool _vacEnabled = false;
        private ISteamUserTicketAuthenticator _authenticator;
        private ILogger _logger;
        private ISteamService _steamService;
        public SteamAuthenticationProvider()
        {
        }

        public void AddMetadata(Dictionary<string, string> result)
        {
            result.Add("provider.steamauthentication", "enabled");
        }

        public void Initialize(ISceneHost scene)
        {
            var environment = scene.DependencyResolver.Resolve<IEnvironment>();
            _logger = scene.DependencyResolver.Resolve<ILogger>();
            ApplyConfig(environment, scene);

            environment.ConfigurationChanged += (sender, e) => ApplyConfig(environment, scene);
        }

        private void ApplyConfig(IEnvironment environment, ISceneHost scene)
        {
            var steamConfig = environment.Configuration.steam;
            _steamService = scene.DependencyResolver.Resolve<ISteamService>();
            if (steamConfig?.usemockup != null && (bool)(steamConfig.usemockup))
            {
                _authenticator = new SteamUserTicketAuthenticatorMockup();
            }
            else
            {
                _authenticator =

                    new SteamUserTicketAuthenticator(_steamService);
            }
            _vacEnabled = steamConfig?.vac != null && (bool)steamConfig.vac;
        }


        public async Task<AuthenticationResult> Authenticate(Dictionary<string, string> authenticationCtx, IUserService userService)
        {
            if (authenticationCtx["provider"] != PROVIDER_NAME)
            {
                return null;
            }

            string ticket;
            if (!authenticationCtx.TryGetValue("ticket", out ticket) || string.IsNullOrWhiteSpace(ticket))
            {
                return AuthenticationResult.CreateFailure("Steam session ticket must not be empty.", PROVIDER_NAME, authenticationCtx);
            }
            try
            {
                var steamId = await _authenticator.AuthenticateUserTicket(ticket);

                if (!steamId.HasValue)
                {
                    return AuthenticationResult.CreateFailure("Invalid steam session ticket.", PROVIDER_NAME, authenticationCtx);
                }

                if (_vacEnabled)
                {
                    AuthenticationResult result = null;
                    string vacSessionId = null;
                    try
                    {
                        vacSessionId = await _steamService.OpenVACSession(steamId.Value.ToString());
                        _vacSessions[steamId.Value] = vacSessionId;


                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, "authenticator.steam", $"Failed to start VAC session for {steamId}", ex);
                        result = AuthenticationResult.CreateFailure($"Failed to start VAC session.", PROVIDER_NAME, authenticationCtx);
                    }

                    try
                    {
                        if (!await _steamService.RequestVACStatusForUser(steamId.Value.ToString(), vacSessionId))
                        {
                            result = AuthenticationResult.CreateFailure($"Connection refused by VAC.", PROVIDER_NAME, authenticationCtx);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, "authenticator.steam", $"Failed to check VAC status for  {steamId}", ex);
                        result = AuthenticationResult.CreateFailure($"Failed to check VAC status for user.", PROVIDER_NAME, authenticationCtx);
                    }

                    if (result != null)//Failed
                    {
                        if (_vacSessions.TryRemove(steamId.Value, out vacSessionId))
                        {
                            try
                            {
                                await _steamService.CloseVACSession(steamId.ToString(), vacSessionId);
                            }
                            catch (Exception ex)
                            {
                                _logger.Log(LogLevel.Error, $"authenticator.steam", $"Failed to close vac session for user '{steamId}'", ex);
                            }
                        }
                        return result;
                    }

                }
                var steamIdString = steamId.GetValueOrDefault().ToString();
                var user = await userService.GetUserByClaim(PROVIDER_NAME, ClaimPath, steamIdString);
                var playerSummary  = await _steamService.GetPlayerSummary(steamId.Value);
                if (user == null)
                {
                    var uid = Guid.NewGuid().ToString("N");
                    
                    user = await userService.CreateUser(uid, JObject.FromObject(new { steamid = steamIdString, pseudo= playerSummary.personaname, avatar = playerSummary.avatarfull }));

                    var claim = new JObject();
                    claim[ClaimPath] = steamIdString;
                    user = await userService.AddAuthentication(user, PROVIDER_NAME, claim, steamIdString);
                }
                else
                {
                    user.UserData["pseudo"] = playerSummary.personaname;
                    user.UserData["avatar"] = playerSummary.avatarfull;
                    await userService.UpdateUserData(user.Id, user.UserData);
                }

                return AuthenticationResult.CreateSuccess(user, PROVIDER_NAME, authenticationCtx);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Debug, "authenticator.steam", $"Steam authentication failed. Ticket : {ticket}", ex);
                return AuthenticationResult.CreateFailure($"Invalid steam session ticket.", PROVIDER_NAME, authenticationCtx);
            }
        }

        public Task OnLoggedIn(IScenePeerClient client, User user, string provider)
        {
            return Task.FromResult(true);
        }

        public async Task OnLoggedOut(IScenePeerClient client, User user)
        {

            var steamId = user.GetSteamId();
            string vacSessionId;
            if (_vacSessions.TryRemove(steamId, out vacSessionId))
            {
                try
                {
                    await _steamService.CloseVACSession(steamId.ToString(), vacSessionId);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, $"authenticator.steam", "Failed to close vac session for user '{steamId}'", ex);
                }
            }
        }
    }
}
