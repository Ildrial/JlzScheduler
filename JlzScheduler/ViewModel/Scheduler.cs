﻿using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JlzScheduler
{
    public class Scheduler
    {
        private static ILog Log = LogManager.GetLogger(typeof(Scheduler));

        public List<MatchupPair> MatchupPairs { get; }
        public List<Matchup> Matchups { get; }
        public List<Team> Teams { get; }

        public Scheduler(List<Team> teams, List<Matchup> matchups, List<MatchupPair> matchupPairs)
        {
            this.Teams = teams;
            this.Matchups = matchups;
            this.MatchupPairs = matchupPairs;
        }

        public void Run()
        {
            var schedules = this.CreateSchedule(new Schedule(this.Teams), MatchupPairs.ToList(), float.MaxValue);

            Log.Info("Top 10 schedules selected:");
            var rank = 0;
            foreach (var schedule in schedules)
            {
                // TODO write into file 'stead of log
                Log.Info($"Rank {rank++} with Score {schedule.Score}:\n{schedule.ToCsv()}");
                Log.Info("-------------------------------------------");
            }
        }

        private List<Schedule> CreateSchedule(Schedule currentSchedule, List<MatchupPair> availablePairs, float scoreToBeat)
        {
            var fixedMatchupPairs = currentSchedule.MatchupPairs.Count;
            string logPrefix = $"L{fixedMatchupPairs + 1}: {currentSchedule.Matchups}\t\t";

            var currentSchedules = new List<Schedule>();

            foreach (var mp in availablePairs)
            {
                // TODO maybe move available pairs in Schedule and manage in there...
                var newAvailable = availablePairs.ToList();
                newAvailable.RemoveAll(p => p.HasCommonMatchups(mp));
                var newSchedule = currentSchedule.Choose(mp);

                var isValid = newSchedule.IsValid(newAvailable);

                if (isValid)
                {
                    Log.Debug($"{logPrefix} Selecting {mp}");
                }
                else
                {
                    Log.Debug($"{logPrefix} Rejecting {mp}");
                    continue;
                }

                // TODO terminate early on too high scores! ok so?
                if (newSchedule.Score > scoreToBeat)
                {
                    Log.Debug($"{logPrefix} Current score too high after selection ({mp}): {newSchedule.Score} > {scoreToBeat}");
                    continue;
                }

                if (newSchedule.IsComplete)
                {
                    Log.Debug($"{logPrefix} Completed with score {newSchedule.Score}");
                    currentSchedules.Add(newSchedule);
                }
                else
                {
                    if (newAvailable.Count < 1)
                    {
                        // TODO that simple?
                        continue;
                    }

                    var schedules = this.CreateSchedule(newSchedule, newAvailable, scoreToBeat);
                    currentSchedules.AddRange(schedules);
                }
            }

            // TODO where to calculate top 20? inside or outside loop?
            var top50 = currentSchedules.OrderBy(cs => cs.Score).Take(50).ToList();
            if (top50.Any())
            {
                scoreToBeat = Math.Min(top50.Last().Score, scoreToBeat);
            }

            if (top50.Any())
            {
                Log.Debug($"{logPrefix} Level completed, returning Top{top50.Count} with scores from {top50.First().Score} to {top50.Last().Score}.");
            }
            else
            {
                Log.Debug($"{logPrefix} Branch failed with no possible schedule: {string.Join(", ", currentSchedule.MatchupPairs)}");
            }

            return top50;
        }
    }
}