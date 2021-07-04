using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JlzScheduler
{
    public class MatchupPair
    {
        /// <summary>
        /// Tracks equivalent matchup pairs which can also be discarded if this one is discarded
        /// </summary>
        public List<MatchupPair> EquilvalentMatchupPairs { get; } = new List<MatchupPair>();

        public Matchup Matchup1 { get; }
        public Matchup Matchup2 { get; }
        public string SortedTeamIds => string.Join("-", this.Teams.OrderBy(t => t.Id));
        public List<Team> Teams => new List<Team> { this.Matchup1.Home, this.Matchup1.Away, this.Matchup2.Home, this.Matchup2.Away };
        public string Id => string.CompareOrdinal(Matchup1.Id, Matchup2.Id) < 0 ? Matchup1.Id + Matchup2.Id : Matchup2.Id + Matchup1.Id;

        public MatchupPair(Matchup m1, Matchup m2)
        {
            if (m1.Id.Equals(m2.Id))
            {
                throw new InvalidOperationException($"Equal Matchups: '{m1.Id}'.");
            }

            this.Matchup1 = m1;
            this.Matchup2 = m2;
        }

        public bool HasCommonMatchups(MatchupPair mp)
        {
            return mp.Matchup1 == this.Matchup1 || mp.Matchup1 == this.Matchup2 || mp.Matchup2 == this.Matchup1 || mp.Matchup2 == this.Matchup2;
        }

        public bool HasTeam(Team team)
        {
            return Matchup1.HasTeam(team) || Matchup2.HasTeam(team);
        }

        public override string ToString()
        {
            return this.Id;
        }
    }
}