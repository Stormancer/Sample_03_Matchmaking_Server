using Battlefleet.Server.Matchmaking.Models;
using Newtonsoft.Json.Linq;
using Server.Profiles;
using Stormancer.Diagnostics;
using Stormancer.Matchmaking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Server.Matchmaking
{
    public class RulesService
    {
        private readonly ILogger _logger;
        private readonly Func<PlayerProfile, int> _eloSelector;

        public RulesService(ILogger logger, Func<PlayerProfile, int> eloSelector)
        {
            _logger = logger;
            _eloSelector = eloSelector;
        }

        #region Config

        #region Constraints
     
        #endregion

        #region Score
        private int _baseMatchValue;
        private int _teamSizeMalus;

        private int _ageBonus;

        #endregion

        public void Applyconfig(dynamic matchmakerConfig)
        {



            _baseMatchValue = (int?)matchmakerConfig.baseMatchValue ?? 0;

            _teamSizeMalus = (int?)matchmakerConfig.teamSizeMalus ?? 0;

            if (matchmakerConfig.ageBonus == null)
            {
                _logger.Log(LogLevel.Error, "matchmaking.rules", "missing configuration value : 'matchmaking.ageBonus'. Defaulting to 5", new { });
                _ageBonus = 5;
            }
            else
            {
                _ageBonus = (int?)matchmakerConfig.ageBonus??0;
            }


            _logger.Log(LogLevel.Debug, "matchmaking.rules", "Matchmaker rules configured.", new
            {


                _baseMatchValue,

                _teamSizeMalus,

            });
        }
        #endregion

        public Tuple<string, string> AssignAlliances(IEnumerable<Group> team1, IEnumerable<Group> team2)
        {
            //foreach (var alliance1 in GetAlliances(team1))
            //{
            //    foreach (var alliance2 in GetAlliances(team2).Where(al2 => al2 != alliance1))
            //    {
            //        return Tuple.Create(alliance1, alliance2);
            //    }
            //}

            return null;
        }


        public int GetGroupElo(Group group)
        {
            return (int)group.Players.Select(m => m.Data).Cast<PlayerProfile>().Average(_eloSelector);
        }


        #region Score
        public int Score(IEnumerable<Match> matches) => matches.Sum(Score);

        private int Score(Match match)
        {
            double globalScore = 0;
            var teams = new List<Player>[match.Teams.Count];
            //for (int i = 0; i < match.Teams.Count; i++)
            //{
            //    teams[i] = match.Teams[i].AllPlayers.OrderByDescending(p => ((PlayerProfile)p.Data).Admiral.Level).ToList();

            //}
            //for (int i = 0; i < teams[0].Count; i++)
            //{
            //    globalScore -= Math.Abs(((PlayerProfile)teams[0][i].Data).Admiral.Level - ((PlayerProfile)teams[1][i].Data).Admiral.Level) * _admiralLevelMalus;
            //}


            var grpsScore = match.AllGroups.Sum(p => Score(match, p));

            return grpsScore + (int)globalScore;
        }

        private int Score(Match match, Group group)
        {
            var matchmakingData = (MatchmakingGroupData)(group.GroupData);

            var matchData = ((MatchData)match.CustomData);

            var matchValue = _baseMatchValue;

            //if (matchmakingData.Request.teamSize != match.Teams[0].Groups.Sum(g => g.Players.Count))
            //{
            //    matchValue -= _teamSizeMalus;
            //}

            //if (GetMaxFleetSize(group) != matchData.FleetSize)
            //{
            //    matchValue -= _fleetSizeMalus;
            //}

            //if (matchmakingData.Request.rp && !matchData.RespectsRp)
            //{
            //    matchValue -= _rpMalus;
            //}

            return matchValue + (matchmakingData.PastMatchmakingPasses + 1) * group.Players.Count * _ageBonus;
        }
        #endregion
    }
}
