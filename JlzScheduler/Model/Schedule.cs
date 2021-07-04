using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JlzScheduler
{
    public enum ScheduleValidity
    {
        OK,
        Reject,
        Abort
    }

    public class Schedule
    {
        private float _score = -1f;

        // TODO do not hard code number
        public bool IsComplete => MatchupPairs.Count == 9;

        public List<MatchupPair> MatchupPairs { get; } = new List<MatchupPair>();

        public string Matchups => string.Join("-", this.MatchupPairs);

        // TODO always ok to cache value? clear required at some point?
        public float Score => _score == -1 ? _score = this.CalculateScore2() : _score;

        public List<Team> Teams { get; }

        public Schedule(List<Team> teams)
        {
            this.Teams = teams;
        }

        public Schedule Choose(MatchupPair newPair)
        {
            var newSchedule = new Schedule(this.Teams);
            newSchedule.MatchupPairs.AddRange(this.MatchupPairs);
            newSchedule.MatchupPairs.Add(newPair);

            return newSchedule;
        }

        public ScheduleValidity IsValid(List<MatchupPair> availableMatchups)
        {
            if (MatchupPairs.Count == 0)
            {
                return ScheduleValidity.OK;
            }

            // currently that means not 3 matches in row and no more than 3 matches breaks
            // additional TODO only once 3 matches break

            var longBreakTeams = this.GetTeamsWithThreeBreaks();
            foreach (var lbt in longBreakTeams)
            {
                if (availableMatchups.Any(m => m.HasTeam(lbt)))
                {
                    return ScheduleValidity.Abort;
                }
            }

            var teamMatches = GetNumberOfMatchesPerTeam();
            foreach (var team in teamMatches.Where(x => x.Value == 3).Select(x => x.Key))
            {
                if (this.GetTeamDistance(team) > 5)
                {
                    // todo not entirely correct since it might be ok if team has 4 matches... but may still be useful anyway....
                    return ScheduleValidity.Reject;
                }
            }

            foreach (var team in teamMatches.Where(x => x.Value == 4).Select(x => x.Key))
            {
                var distance = this.GetTeamDistance(team);
                if (distance > 7 || distance < 5)
                {
                    return ScheduleValidity.Reject;
                }
            }

            var latestPair = this.MatchupPairs.Last();
            // check for too few breaks -> reject
            foreach (var team in latestPair.Teams)
            {
                var matchesInRow = 0;
                for (var i = this.MatchupPairs.Count - 2; i >= 0; i--)
                {
                    if (this.MatchupPairs[i].HasTeam(team))
                    {
                        matchesInRow++;

                        if (matchesInRow > 1)
                        {
                            return ScheduleValidity.Reject;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return ScheduleValidity.OK;
        }

        public string ToCsv()
        {
            List<string> lines = new();
            foreach (var mp in this.MatchupPairs)
            {
                lines.Add($"{mp.Matchup1.ToCsv()},,{mp.Matchup2.ToCsv()}");
            }

            return string.Join("\n", lines);
        }

        private float CalculateScore()
        {
            if (this.MatchupPairs.Count < 4)
            {
                return 0;
            }

            var score = 0f;
            foreach (var team in this.Teams)
            {
                score += this.GetScoreByTeam(team);
            }

            return score;
        }

        private float CalculateScore2()
        {
            // TODO score to beat should be best score and applied earlier in loop

            // TODO track overall best score

            if (this.MatchupPairs.Count < 4)
            {
                return 0;
            }

            var score = 0f;
            foreach (var team in this.Teams)
            {
                score += this.GetScoreByTeam2(team);
            }

            return score;
        }

        private Dictionary<Team, int> GetNumberOfMatchesPerTeam()
        {
            var numberOfMatches = this.Teams.ToDictionary(t => t, t => 0);

            for (var i = 0; i < this.MatchupPairs.Count; i++)
            {
                var mp = this.MatchupPairs[i];
                foreach (var team in mp.Teams)
                {
                    numberOfMatches[team] = numberOfMatches[team] + 1;
                }
            }

            return numberOfMatches;
        }

        private float GetScoreByTeam(Team team)
        {
            var positions = new List<int>();

            for (var i = 0; i < this.MatchupPairs.Count; i++)
            {
                var mp = this.MatchupPairs[i];
                if (mp.HasTeam(team))
                {
                    positions.Add(i);
                }
            }

            if (positions.Any())
            {
                var difference = positions.Max() - positions.Min();
                // TODO maybe weigh by number of games?
                float weightedDifference = (float) difference / positions.Count();
                return weightedDifference * weightedDifference;

                //return difference * difference;
            }
            else
            {
                return 0;
            }
        }

        private float GetScoreByTeam2(Team team)
        {
            var positions = new List<int>();

            for (var i = 0; i < this.MatchupPairs.Count; i++)
            {
                var mp = this.MatchupPairs[i];
                if (mp.HasTeam(team))
                {
                    positions.Add(i);
                }
            }

            // TODO consider not played matches. maybe need to move available matchup pairs into
            // schedule for that

            if (positions.Count > 1)
            {
                var difference = positions.Max() - positions.Min();
                var weightedDifference = (float) difference / (positions.Count - 1);
                return weightedDifference * weightedDifference * weightedDifference;
            }
            else
            {
                return 0;
            }
        }

        private int GetTeamDistance(Team team)
        {
            // TODO merge with score
            var positions = new List<int>();

            for (var i = 0; i < this.MatchupPairs.Count; i++)
            {
                var mp = this.MatchupPairs[i];
                if (mp.HasTeam(team))
                {
                    positions.Add(i);
                }
            }

            if (positions.Any())
            {
                var difference = positions.Max() - positions.Min();
                return difference;

                //return difference * difference;
            }
            else
            {
                return 0;
            }
        }

        private List<Team> GetTeamsWithThreeBreaks()
        {
            var latestMatches = new Dictionary<Team, int>();

            for (var i = 0; i < this.MatchupPairs.Count; i++)
            {
                var mp = this.MatchupPairs[i];
                foreach (var team in mp.Teams)
                {
                    latestMatches[team] = i + 1;
                }
            }

            // TODO or 4?
            var threshold = this.MatchupPairs.Count - 3;
            // TODO or <
            return latestMatches
                .Where(x => x.Value > 0 && x.Value <= threshold)
                .Select(l => l.Key)
                .ToList();
        }
    }
}