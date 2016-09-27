using Battlefleet.Server.Matchmaking.Models;
using Newtonsoft.Json.Linq;
using Stormancer.Diagnostics;
using Stormancer.Matchmaking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Server;

namespace Sample.Server.Matchmaking
{
    public class Matchmaker : IMatchmaker
    {
        #region Constructor-injected
        private readonly ILogger _logger;
        private readonly RulesService _rulesService;
        #endregion

        #region Config

        private bool _useMatchmakerStub = false;

        private int _eloRange;
        private int _eloRangeDelta;
        private int[] _acceptableTeamSizes = new int[] { 1 };

        private int _teamSizeExpiration;
        #endregion

        public Matchmaker(ILogger logger, RulesService rulesService)
        {
            _logger = logger;
            _rulesService = rulesService;
        }

        #region IConfigurationRefresh
        public void Init(dynamic config)
        {
            ApplyConfig(config);
        }


        public void ConfigChanged(dynamic newConfig)
        {
            ApplyConfig(newConfig);
        }


        private void ApplyConfig(dynamic config)
        {
            var matchmakerConfig = config.matchmaking;

            _rulesService.Applyconfig(matchmakerConfig);

            var useStubNode = matchmakerConfig?.usestub;
            _useMatchmakerStub = (useStubNode != null) && ((bool)useStubNode);

            if (_useMatchmakerStub)
            {
                _logger.Log(LogLevel.Debug, "matchmaking.matchaker", "Matchmaker configured.", new
                {
                    _useMatchmakerStub
                });
                return;
            }
            if (matchmakerConfig.acceptableTeamSizes != null)
            {
                _acceptableTeamSizes = ((JArray)matchmakerConfig.acceptableTeamSizes).Select(t => (int)t).ToArray();
            }
            _eloRange = (int)matchmakerConfig.eloRange;
            _eloRangeDelta = (int)(matchmakerConfig?.eloRangeDelta ?? _eloRange);

            _teamSizeExpiration = (int)(matchmakerConfig?.teamSizeExpiration ?? 10);


            _logger.Log(LogLevel.Debug, "matchmaking.matchaker", "Matchmaker configured.", new
            {
                _useMatchmakerStub,

                _eloRange,
                _eloRangeDelta,

                _teamSizeExpiration,

            });
        }
        #endregion


        private Stopwatch _stopwatch = new Stopwatch();
        public Task<MatchmakingResult> FindMatches(IEnumerable<Group> candidates)
        {
            _stopwatch.Restart();
            var result = new MatchmakingResult();

            if (!candidates.Any())
            {
                return Task.FromResult(result); ;
            }

            _logger.Log(LogLevel.Trace, "matchmaking.matchmaker", "matchmaking pass started", new { count = candidates.Count() });

            if (_useMatchmakerStub)
            {

                result.Matches.AddRange(FindMatchesStub(candidates));
                return Task.FromResult(result); ;
            }


            var perfectMatches = QuickFindMatches(candidates, true).ToList();
            //_logger.Log(LogLevel.Trace, "matchmaking.matchmaker", "Perfect match finished.", new { time = _stopwatch.ElapsedMilliseconds });

            var imperfectMatches = QuickFindMatches(candidates, false).ToList();
            //_logger.Log(LogLevel.Trace, "matchmaking.matchmaker", "Imperfect match finished.", new { time = _stopwatch.ElapsedMilliseconds });


            var retainedMatches = _rulesService.Score(perfectMatches) >= _rulesService.Score(imperfectMatches) ? perfectMatches : imperfectMatches;
            //_logger.Log(LogLevel.Trace, "matchmaking.matchmaker", "Scores compared.", new { time = _stopwatch.ElapsedMilliseconds });
            
            result.Matches.AddRange(retainedMatches);

            _logger.Log(LogLevel.Trace, "matchmaking.matchmaker", "matchmaking pass finished", new { matchFound = retainedMatches.Count, count = candidates.Count() });


            foreach (var candidate in candidates)
            {
                ((MatchmakingGroupData)candidate.GroupData).PastMatchmakingPasses++;
            }

            return Task.FromResult(result); ;
        }

        public IEnumerable<Match> QuickFindMatches(IEnumerable<Group> candidates, bool perfect)
        {
            var orderedCandidates = candidates.OrderByDescending(_rulesService.GetGroupElo).ToList();

            while (orderedCandidates.Count > 0)
            {
                var pivot = orderedCandidates[0];

                orderedCandidates.RemoveAt(0);

                var match = FirstMatch(pivot, orderedCandidates, perfect);

                if (match != null)
                {
                    yield return match;
                }
            }
        }

        /// <summary>
        /// Returns a match containing the pivot, or null if it can't.
        /// Removes all players from the found match (if any) from the list of candidates.
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="candidates"></param>
        /// <param name="perfect"></param>
        /// <returns></returns>
        private Match FirstMatch(Group pivot, List<Group> candidates, bool perfect)
        {
            var matchmakingData = (MatchmakingGroupData)pivot.GroupData;
            int teamSize = 1;

            foreach (var otherplayers in candidates.Where(c => AreCompatible(pivot, c, perfect, teamSize)).GetCombinations(teamSize * 2 - pivot.Players.Count, g => g.Players.Count))
            {
                var matchData = new MatchData();
                var result = new Match(2, matchData);
                result.Teams[0].Groups.Add(pivot);




                if (TryAssingTeams(result, teamSize, matchmakingData, otherplayers, perfect))
                {
                    matchData.HostUserId = result.AllPlayers.RandomElement().UserId;
                   

                    foreach (var player in otherplayers)
                    {
                        candidates.Remove(player);
                    }

                    return result;
                }
            }

            //If we were unable to find a match and if perfect = false, we can try with a different team size... (only if compatible with the group size)
            if (pivot.Players.Count < teamSize && _teamSizeExpiration <= matchmakingData.PastMatchmakingPasses)
            {
                var acceptableGroupSizes = _acceptableTeamSizes.Where(size => size >= pivot.Players.Count && size != teamSize);
                foreach (var size in acceptableGroupSizes)
                {
                    foreach (var otherplayers in candidates.Where(c => AreCompatible(pivot, c, perfect, size)).GetCombinations(size * 2 - pivot.Players.Count, g => g.Players.Count))
                    {
                        var matchData = new MatchData();
                        var result = new Match(2, matchData);
                        result.Teams[0].Groups.Add(pivot);

                        if (TryAssingTeams(result, size, matchmakingData, otherplayers, perfect))
                        {
                            matchData.HostUserId = result.AllPlayers.RandomElement().UserId;
                          

                            foreach (var player in otherplayers)
                            {
                                candidates.Remove(player);
                            }

                            return result;
                        }
                    }
                }
            }
            return null;
        }

        private bool TryAssingTeams(Match match, int teamSize, MatchmakingGroupData matchmakingData, IEnumerable<Group> otherplayers, bool perfect)
        {

            var teammatesEnumerator = otherplayers.GetCombinations(teamSize - match.Teams[0].AllPlayers.Count(), g => g.Players.Count).GetEnumerator();

            if (!teammatesEnumerator.MoveNext())
            {
                return false;
            }

            Tuple<string, string> alliances;
            IEnumerable<Group> teammates;
            IEnumerable<Group> opponents;

            teammates = teammatesEnumerator.Current;
            opponents = otherplayers.Except(teammates);
            alliances = AssignAlliances(match.Teams[0].Groups.Concat(teammates), opponents);



            match.Teams[0].Groups.AddRange(teammates);
            match.Teams[1].Groups.AddRange(opponents);

            if (match.Teams[0].AllPlayers.Count() != match.Teams[1].AllPlayers.Count() || match.Teams[0].AllPlayers.Count() != teamSize)
            {
                return false;
            }
            if (alliances != null)
            {
                var matchData = (MatchData)match.CustomData;
                
            }

            return true;
        }

        /// <summary>
        /// Determines if the two clients can play together.
        /// </summary>
        /// <param name="first">first player, with the higher Elo</param>
        /// <param name="second">second player, with the lower Elo</param>
        /// <param name="perfect">Does the match has to be perfect?</param>
        /// <returns></returns>
        private bool AreCompatible(Group first, Group second, bool perfect, int teamSize)
        {
            var firstData = (MatchmakingGroupData)first.GroupData;
            var secondData = (MatchmakingGroupData)second.GroupData;

            var maxEloRange = _eloRange + Math.Min(firstData.PastMatchmakingPasses, secondData.PastMatchmakingPasses) * _eloRangeDelta;

            //Region must match
            //if (firstData.Request.region != secondData.Request.region)
            //{
            //    return false;
            //}
            //if (firstData.Request.version != secondData.Request.version)
            //{
            //    return false;
            //}

            //Elo must always match
            if (Math.Abs(_rulesService.GetGroupElo(first) - _rulesService.GetGroupElo(second)) > maxEloRange)
            {
                return false;
            }


            //should satisfy team size 
            //if (perfect || firstData.PastMatchmakingPasses < _teamSizeExpiration || secondData.PastMatchmakingPasses < _teamSizeExpiration)
            //{

            //    if ((teamSize != 0 && firstData.Request.teamSize != teamSize) || (secondData.Request.teamSize != 0 && secondData.Request.teamSize != teamSize))
            //    {
            //        return false;
            //    }
            //}

            return true;
        }


        #region Alliances
        private Tuple<string, string> AssignAlliances(IEnumerable<Group> team1, IEnumerable<Group> team2)
        {
            //is RP important for any of the players?
            //if (team1.Concat(team2).Any(g => ((MatchmakingGroupData)g.GroupData).Request.rp))
            //{
            //    return _rulesService.AssignAlliances(team1, team2);

            //}
            return null;
        }
        #endregion

        #region Stub
        private IEnumerable<Match> FindMatchesStub(IEnumerable<Group> candidates)
        {

            foreach (var group in candidates)
            {
                var playerId = group.Players[0].UserId;
                if (DateTime.UtcNow - group.CreationTimeUtc > TimeSpan.FromSeconds(10))
                {
                    //_logger.Log(LogLevel.Trace, "matchmaking.matchmaker", $"Successful match found for player {playerId}.", group);

                    var match = new Match();
                    var team = new Team();
                    team.Groups.Add(group);
                    match.Teams.Add(team);
                    yield return match;
                }
                else
                {
                    //_logger.Log(LogLevel.Trace, "matchmaking.matchmaker", $"No match found yet for player {playerId}.", group);
                }
            }
        }
        #endregion
    }
}
