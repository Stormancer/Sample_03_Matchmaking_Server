using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer.Diagnostics;
using Server.Database;
using Stormancer.Server.Components;
using System.Collections.Concurrent;
using Stormancer;
using System.IO;

namespace Server.Plugins.Leaderboard
{
    class LeaderboardService : ILeaderboardService
    {
        private readonly ILogger _logger;
        private readonly IESClientFactory _clientFactory;
        private readonly IEnvironment _environment;
        private readonly string _indexNameSubPath;

        /// <summary>
        /// True if the leaderboards treats exequo as same rank. False if they are ordered by ascending creation date.
        /// </summary>
        public bool EnableExequo { get; set; } = false;
        public LeaderboardService(ILogger logger, IESClientFactory clientFactory, IEnvironment environment)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _environment = environment;
            _indexNameSubPath = (string)(environment.Configuration.index);
        }

        private Task CreateRankedScoreMapping(Nest.IElasticClient client)
        {
            return client.MapAsync<ScoreRecord>(m =>
                m.DynamicTemplates(templates => templates
                    .DynamicTemplate("dates", t => t
                         .Match("CreatedOn")
                         .Mapping(ma => ma.Date(s => s))
                        )
                     .DynamicTemplate("score", t =>
                        t.MatchMappingType("string")
                         .Mapping(ma => ma.String(s => s.Index(Nest.FieldIndexOption.NotAnalyzed)))
                         )


                    )
            );
        }

        private static readonly ConcurrentDictionary<string, bool> _initializedIndices = new ConcurrentDictionary<string, bool>();
        public async Task<Nest.IElasticClient> CreateClient(string name)
        {
            var result = await _clientFactory.CreateClient(await GetIndexName(name));

            if (_initializedIndices.TryAdd(name, true))
            {
                await CreateRankedScoreMapping(result);
            }
            return result;
        }


        private Task<Nest.IElasticClient> CreateClient(LeaderboardQuery rq)
        {
            return CreateClient(rq.Name);
        }

        public Nest.QueryContainer CreatePreviousPaginationFilter(Nest.QueryContainerDescriptor<ScoreRecord> q, ScoreRecord pivot)
        {

            // ( score > pivot.score) OR (score == pivot.score AND createdOn < pivot.createdOn)
            return q.Bool(b1 => b1.Should(
                q1 => q1.Range(r => r.Field(record => record.Score).GreaterThan(pivot.Score)),
                q1 => q1.Bool(b2 => b2.Must(
                      q2 => q2.Term(t => t.Field(record => record.Score).Value(pivot.Score)),
                      q2 => q2.DateRange(r => r.Field(record => record.CreatedOn).LessThan(pivot.CreatedOn))
                      ))

                ));

        }

        public Nest.QueryContainer CreateNextPaginationFilter(Nest.QueryContainerDescriptor<ScoreRecord> q, ScoreRecord pivot)
        {

            // ( score < pivot.score) OR (score == pivot.score AND createdOn > pivot.createdOn)
            return q.Bool(b1 => b1.Should(
                q1 => q1.Range(r => r.Field(record => record.Score).LessThan(pivot.Score)),
                q1 => q1.Bool(b2 => b2.Must(
                      q2 => q2.Term(t => t.Field(record => record.Score).Value(pivot.Score)),
                      q2 => q2.DateRange(r => r.Field(record => record.CreatedOn).GreaterThan(pivot.CreatedOn))
                      ))

                ));

        }
        public async Task<ScoreRecord> GetScore(string id, string leaderboardName)
        {
            var client = await CreateClient(leaderboardName);
            var startResult = await client.GetAsync<ScoreRecord>(id);
            if (!startResult.Found)
            {
                return null;
            }
            return startResult.Source;
        }

        public async Task<long> GetRanking(ScoreRecord score, LeaderboardQuery filters, string leaderboardName)
        {

            var client = await CreateClient(leaderboardName);
            var rankResult = await client.CountAsync<ScoreRecord>(desc => desc
                    .Query(query =>
                        CreateQuery(query, filters,
                            q =>
                            {
                                var mustClauses = new List<Func<Nest.QueryContainerDescriptor<ScoreRecord>, Nest.QueryContainer>>();

                                mustClauses.Add(q1 => q1.Range(r => r.Field(record => record.Score).GreaterThan(score.Score)));
                                if (!EnableExequo)
                                {
                                    mustClauses.Add(q1 => q1.Bool(b2 => b2.Must(
                                        q2 => q2.Term(t => t.Field(record => record.Score).Value(score.Score)),
                                        q2 => q2.DateRange(r => r.Field(record => record.CreatedOn).LessThan(score.CreatedOn))
                                    )));
                                }
                                return q.Bool(b => b.Should(mustClauses));
                            }
                            )));

            if (!rankResult.IsValid)
            {
                throw new InvalidOperationException($"Failed to compute rank. {rankResult.ServerError.Error.Reason}");
            }
            return rankResult.Count + 1;
        }

        public async Task<long> GetTotal(LeaderboardQuery filters, string leaderboardName)
        {
            var client = await CreateClient(leaderboardName);

            var rankResult = await client.CountAsync<ScoreRecord>(desc => desc
                    .Query(query =>
                        CreateQuery(query, filters)));
            if (!rankResult.IsValid)
            {
                throw new InvalidOperationException($"Failed to compute total scores in filter. {rankResult.ServerError.Error.Reason}");
            }
            return rankResult.Count;
        }

        public async Task<LeaderboardResult<ScoreRecord>> Query(LeaderboardQuery rq)
        {
            var isContinuation = rq is LeaderboardContinuationQuery;
            var client = await CreateClient(rq);
            ScoreRecord start = null;
            if (!string.IsNullOrEmpty(rq.StartId))
            {
                start = await GetScore(rq.StartId, rq.Name);
                if (start == null)
                {
                    throw new ClientException("Admiral not found in leadeboard.");
                }
            }

            var result = await client.SearchAsync<ScoreRecord>(s =>
            {
                s = s.AllowNoIndices();
                s = s.Query(query => CreateQuery(query, rq, q =>
                {

                    if (start != null)//If we have a pivot we must add constraint to start the result around it.
                    {
                        //Create next/previous additional constraints
                        if ((rq as LeaderboardContinuationQuery)?.IsPrevious == true)
                        {
                            return CreatePreviousPaginationFilter(q, start);
                        }
                        else
                        {
                            return CreateNextPaginationFilter(q, start);
                        }
                    }
                    else
                    {
                        return q;
                    }
                })).AllowNoIndices();

                if ((rq as LeaderboardContinuationQuery)?.IsPrevious == true)
                {
                    s = s.Sort(sort => sort.Ascending(record => record.Score).Descending(record => record.CreatedOn));
                }
                else
                {
                    s = s.Sort(sort => sort.Descending(record => record.Score).Ascending(record => record.CreatedOn));
                }
                if ((isContinuation && (rq as LeaderboardContinuationQuery)?.IsPrevious == false) || start == null)
                {
                    s = s.Size(rq.Count + 1).From(rq.Skip);// We get one more document  than necessary to be able to determine if we can build a "next" continuation
                }
                else// The pivot is not included in the result set, if we are not running a continuation query, we must prefix the results with the pivot.
                {
                    s = s.Size(rq.Count).From(rq.Skip);
                }


                return s;
            });

            if (!result.IsValid)
            {
                if (result.ServerError.Status == 404)
                {
                    return new LeaderboardResult<ScoreRecord> { Results = new List<LeaderboardRanking<ScoreRecord>>() };
                }
                _logger.Log(LogLevel.Error, "leaderboard", "failed to process query request.", result.ServerError);
                throw new InvalidOperationException($"Failed to query leaderboard : {result.ServerError.Error.Reason}");
            }
            var documents = result.Documents.ToList();
            if (!isContinuation && start != null)
            {
                documents.Insert(0, start);
            }
            else if ((rq as LeaderboardContinuationQuery)?.IsPrevious == true)
            {
                documents.Reverse();
            }

            //Compute rankings
            if (documents.Any())
            {

                int firstRank = 0;
                try
                {
                    firstRank = (int)await GetRanking(documents.First(), rq, rq.Name);
                }
                catch (InvalidOperationException ex)
                {

                    _logger.Log(LogLevel.Error, "leaderboard", ex.Message, ex);
                    throw new InvalidOperationException($"Failed to query leaderboard : {ex.Message}");


                }
                var rank = firstRank;
                var lastScore = int.MaxValue;
                var lastRank = firstRank;
                var results = new List<LeaderboardRanking<ScoreRecord>>();


                foreach (var doc in documents.Take(rq.Count))
                {
                    if (EnableExequo)
                    {
                        int currentRank;
                        if (doc.Score == lastScore)
                        {
                            currentRank = lastRank;

                        }
                        else
                        {
                            currentRank = rank;
                        }

                        results.Add(new LeaderboardRanking<ScoreRecord> { Document = doc, Ranking = currentRank });
                        lastRank = currentRank;
                    }
                    else
                    {
                        results.Add(new LeaderboardRanking<ScoreRecord> { Document = doc, Ranking = rank });
                    }
                    rank++;
                }


                var leaderboardResult = new LeaderboardResult<ScoreRecord> { Results = results };

                if (firstRank > 1)//There are scores before the first in the list
                {
                    var previousQuery = new LeaderboardContinuationQuery(rq);
                    previousQuery.Skip = 0;
                    previousQuery.Count = rq.Count;
                    previousQuery.IsPrevious = true;
                    previousQuery.StartId = results.First().Document.Id;
                    leaderboardResult.Previous = SerializeContinuationQuery(previousQuery);
                }

                if (documents.Count > rq.Count || (rq as LeaderboardContinuationQuery)?.IsPrevious == true)//there are scores after the last in the list.
                {
                    var nextQuery = new LeaderboardContinuationQuery(rq);
                    nextQuery.Skip = 0;
                    nextQuery.Count = rq.Count;
                    nextQuery.IsPrevious = false;
                    nextQuery.StartId = results.Last().Document.Id;
                    leaderboardResult.Next = SerializeContinuationQuery(nextQuery);
                }

                return leaderboardResult;


            }
            else
            {
                return new LeaderboardResult<ScoreRecord>();
            }


        }

        private string SerializeContinuationQuery(LeaderboardContinuationQuery query)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(query);
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        }

        private LeaderboardContinuationQuery DeserializeContinuationQuery(string continuation)
        {
            var json = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(continuation));
            return Newtonsoft.Json.JsonConvert.DeserializeObject<LeaderboardContinuationQuery>(json);
        }
        public Task<LeaderboardResult<ScoreRecord>> QueryCursor(string cursor)
        {
            if (string.IsNullOrEmpty(cursor))
            {
                throw new ClientException("Invalid continuation: no more results available");
            }
            var query = DeserializeContinuationQuery(cursor);

            return Query(query);
        }

        public async Task UpdateScore(ScoreRecord score, string leaderboardName)
        {
            var client = await CreateClient(leaderboardName);
            score.CreatedOn = DateTime.UtcNow;
            await client.IndexAsync(score);
        }

        private async Task<string> GetIndexName(string name)
        {
            var app = await _environment.GetApplicationInfos();
            return "leaderboards-" + app.AccountId + "-" + _indexNameSubPath + "-" + name;
        }

        public async Task RemoveLeaderboardEntry(string leaderboardName, string entryId)
        {
            var client = await CreateClient(leaderboardName);
            await client.DeleteAsync<ScoreRecord>(entryId);
        }


        private Nest.QueryContainer CreateQuery(
            Nest.QueryContainerDescriptor<ScoreRecord> desc,
            LeaderboardQuery rq,
            Func<Nest.QueryContainerDescriptor<ScoreRecord>, Nest.QueryContainer> additionalContraints = null)
        {
            return desc.Bool(s2 =>
            {
                var mustClauses = Enumerable.Empty<Func<Nest.QueryContainerDescriptor<ScoreRecord>, Nest.QueryContainer>>();

                if (rq.FriendsIds.Any())
                {
                    mustClauses = mustClauses.Concat(new Func<Nest.QueryContainerDescriptor<ScoreRecord>, Nest.QueryContainer>[] {
                        q => q.Terms(t=>t.Field(s=>s.PlayerId).Terms(rq.FriendsIds.Select(i=>i.ToString())))
                    });
                }
                if (rq.FieldFilters != null && rq.FieldFilters.Any())
                {
                    mustClauses = mustClauses.Concat(rq.FieldFilters.Select<FieldFilter, Func<Nest.QueryContainerDescriptor<ScoreRecord>, Nest.QueryContainer>>(f =>
                    {
                        return q => q.Term("document." + f.Field, f.Value);
                    }));

                }
                if (rq.ScoreFilters != null && rq.ScoreFilters.Any())
                {
                    mustClauses = mustClauses.Concat(rq.ScoreFilters.Select<ScoreFilter, Func<Nest.QueryContainerDescriptor<ScoreRecord>, Nest.QueryContainer>>(f =>
                    {
                        return q => q.Range(r =>
                        {

                            r = r.Field(s3 => s3.Score);
                            switch (f.Type)
                            {
                                case ScoreFilterType.GreaterThan:
                                    r = r.GreaterThan(f.Value);
                                    break;
                                case ScoreFilterType.GreaterThanOrEqual:
                                    r = r.GreaterThanOrEquals(f.Value);
                                    break;
                                case ScoreFilterType.LesserThan:
                                    r = r.LessThan(f.Value);
                                    break;
                                case ScoreFilterType.LesserThanOrEqual:
                                    r = r.LessThanOrEquals(f.Value);
                                    break;
                                default:
                                    break;
                            }

                            return r;
                        });
                    }));
                }
                if (additionalContraints != null)
                {
                    mustClauses = mustClauses.Concat(new[] { additionalContraints });
                }
                return s2.Must(mustClauses);
            });
        }

    }
}
