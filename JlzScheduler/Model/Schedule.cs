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
        private int _score = -1;

        // TODO do not hard code number
        public bool IsComplete => MatchupPairs.Count == 9;

        public List<MatchupPair> MatchupPairs { get; } = new List<MatchupPair>();

        public string Matchups => string.Join("-", this.MatchupPairs);

        // TODO always ok to cache value? clear required at some point?
        public int Score => _score == -1 ? _score = this.CalculateScore() : _score;

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

            var latestPair = this.MatchupPairs.Last();

            // check for too long breaks -> abort
            foreach (var team in latestPair.Teams)
            {
                var breaks = 0;
                for (var i = this.MatchupPairs.Count - 2; i >= 0; i--)
                {
                    if (this.MatchupPairs[i].HasTeam(team))
                    {
                        if (breaks > 3)
                        {
                            // TODO verify that this never happens when using previous check
                            return ScheduleValidity.Abort;
                        }

                        breaks = 0;
                    }
                    else
                    {
                        breaks++;
                    }
                }
            }

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

        private int CalculateScore()
        {
            if (this.MatchupPairs.Count < 4)
            {
                return 0;
            }

            var score = 0;
            foreach (var team in this.Teams)
            {
                score += this.GetScoreByTeam(team);
            }

            return score;
        }

        private int GetScoreByTeam(Team team)
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
                //float weightedDifference = (float) difference / positions.Count();

                return difference * difference;
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
            // TODO or <=
            return latestMatches.Where(x => x.Value > 0 && x.Value <= threshold).Select(l => l.Key).ToList();
        }
    }
}