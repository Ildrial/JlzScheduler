using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JlzScheduler
{
    public class Matchup
    {
        public static readonly Matchup None = new Matchup(Team.None, Team.None);

        public Team Away { get; }
        public Team Home { get; }
        public string Id => $"{this.Home.Id}{this.Away.Id}";

        public Matchup(Team home, Team away)
        {
            if (home.Id.Equals(away.Id) && home != Team.None)
            {
                throw new InvalidOperationException($"Equal teams: {home}");
            }

            this.Home = home;
            this.Away = away;
        }

        public bool HasCommonTeams(Matchup matchup)
        {
            return matchup.Home == this.Home || matchup.Home == this.Away || matchup.Away == this.Home || matchup.Away == this.Away;
        }

        public bool HasTeam(Team team)
        {
            return this.Home == team || this.Away == team;
        }

        public string ToCsv()
        {
            return $"{this.Home.Id},{this.Away.Id}";
        }

        public override string ToString()
        {
            return this.Id;
        }

        public class MatchupComparer : IComparer<Matchup>
        {
            public int Compare(Matchup? x, Matchup? y)
            {
                if (x is null || y is null)
                {
                    throw new InvalidOperationException("Matchup must not be null.");
                }
                return string.CompareOrdinal(x.Id, y.Id);
            }
        }
    }
}