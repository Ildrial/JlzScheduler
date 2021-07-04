using System;
using System.Collections.Generic;
using System.Globalization;

namespace JlzScheduler
{
    public class Team
    {
        public static readonly Team None = new Team("");
        public string Id { get; } = string.Empty;
        public List<Matchup> Matchups { get; } = new List<Matchup>();
        public string Name { get; } = string.Empty;

        public int NumberOfMatches => Matchups.Count;

        public Team(string id, string name = "")
        {
            this.Id = id;
            this.Name = string.IsNullOrEmpty(name) ? id : name;
        }

        public override string ToString()
        {
            return $"{this.Id} ({this.Name})";
        }

        public class TeamComparer : IComparer<Team>
        {
            public int Compare(Team? x, Team? y)
            {
                if (x is null || y is null)
                {
                    throw new InvalidOperationException("Team must not be null.");
                }
                return string.CompareOrdinal(x.Id, y.Id);
            }
        }
    }
}