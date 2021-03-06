using log4net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JlzScheduler
{
    public class Scheduler
    {
        private static ILog Log = LogManager.GetLogger(typeof(Scheduler));

        public List<MatchupPair> MatchupPairs { get; }
        public List<Matchup> Matchups { get; }
        public List<Team> Teams { get; }

        public Scheduler(List<Team> teams, List<Matchup> matchups)
        {
            this.Teams = teams;
            this.Matchups = matchups;
            this.MatchupPairs = CreateMatchupPairs();
        }

        public void Run()
        {
            var schedules = this.CreateSchedule(new Schedule(this.Teams, MatchupPairs.ToList()), float.MaxValue);

            Log.Info($"Top {schedules.Count} schedules selected:");
            var rank = 0;
            foreach (var schedule in schedules)
            {
                // TODO write into file 'stead of log
                Log.Info($"Rank {++rank} with Score {schedule.Score}:\n{schedule.ToCsv()}");
                Log.Info("-------------------------------------------");
            }
        }

        private List<MatchupPair> CreateMatchupPairs()
        {
            var matchupPairs = new List<MatchupPair>();
            // Note that algorithm is based on ordered matchups
            for (var i = 0; i < this.Matchups.Count; i++)
            {
                for (var j = i + 1; j < this.Matchups.Count; j++)
                {
                    if (!this.Matchups[i].HasCommonTeams(this.Matchups[j]))
                    {
                        var newPair = new MatchupPair(this.Matchups[i], this.Matchups[j]);
                        matchupPairs.Add(newPair);
                    }
                }
            }

            foreach (var pair in matchupPairs)
            {
                pair.EquivalentMatchupPairs.AddRange(matchupPairs.Where(mp => mp != pair && mp.SortedTeamIds.Equals(pair.SortedTeamIds)));
            }

            //List<MatchupPair>.Select(mp => string.Join("", mp.Teams.OrderBy(t => t.Id))).Distinct().ToList()

            // redundant, but done anyway
            matchupPairs = matchupPairs.OrderBy(mp => mp.Id).ToList();

            Log.Debug($"Generated {matchupPairs.Count} matchup pairs: {string.Join(", ", matchupPairs)}");

            // Duplicate check
            var duplicates = matchupPairs.GroupBy(x => x.Id)
              .Where(g => g.Count() > 1)
              .Select(y => y.Key)
              .ToList();

            if (duplicates.Any())
            {
                throw new InvalidOperationException($"Duplicate matchup pairs found: '{string.Join(", ", duplicates)}'.");
            }

            return matchupPairs;
        }

        private List<Schedule> CreateSchedule(Schedule currentSchedule, float bestScore)
        {
            var fixedMatchupPairs = currentSchedule.MatchupPairs.Count;
            string logPrefix = $"L{fixedMatchupPairs + 1}: {currentSchedule.Matchups}\t\t";

            var currentSchedules = new List<Schedule>();

            var maximum = currentSchedule.AvailableMatchupPairs.Count;
            for (var i = 0; i < maximum; i++)
            {
                var mp = currentSchedule.AvailableMatchupPairs[i];

                var newSchedule = currentSchedule.Choose(mp);
                var isValid = newSchedule.IsValid();

                // Wheter selected or rejected, equivalents do not need to be checked anymore
                if (mp.EquivalentMatchupPairs.Any())
                {
                    currentSchedule.AvailableMatchupPairs.RemoveAll(pair => mp.EquivalentMatchupPairs.Contains(pair));
                    maximum -= mp.EquivalentMatchupPairs.Count;
                }

                if (isValid)
                {
                    Log.Debug($"{logPrefix} Selecting {mp}");
                }
                else
                {
                    Log.Debug($"{logPrefix} Rejecting {mp}");

                    continue;
                }

                if (newSchedule.Score > bestScore)
                {
                    Log.Debug($"{logPrefix} Current score too high after selection ({mp}): {newSchedule.Score} > {bestScore}");
                    continue;
                }

                if (newSchedule.IsComplete)
                {
                    Log.Debug($"{logPrefix} Completed with score {newSchedule.Score}");
                    currentSchedules.Add(newSchedule);
                }
                else
                {
                    var schedules = this.CreateSchedule(newSchedule, bestScore);
                    currentSchedules.AddRange(schedules);
                }

                currentSchedules = currentSchedules.OrderBy(cs => cs.Score).Take(50).ToList();
                if (currentSchedules.Any())
                {
                    bestScore = Math.Min(currentSchedules.First().Score, bestScore);
                }
            }

            if (currentSchedules.Any())
            {
                Log.Debug($"{logPrefix} Level completed, returning Top{currentSchedules.Count} with scores from {currentSchedules.First().Score} to {currentSchedules.Last().Score}.");
            }
            else
            {
                Log.Debug($"{logPrefix} Branch failed with no possible schedule: {string.Join(", ", currentSchedule.MatchupPairs)}");
            }

            return currentSchedules;
        }
    }
}