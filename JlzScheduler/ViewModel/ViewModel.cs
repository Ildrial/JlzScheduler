using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace JlzScheduler
{
    [DataContract]
    public class ViewModel : INotifyPropertyChanged
    {
        private static ILog Log = LogManager.GetLogger(typeof(ViewModel));

        public ICommand GenerateScheduleCommand => new CommandHandler(this.GenerateScheduleAsync, true);

        public bool IsBusy { get; set; }

        public bool IsSilent => !IsBusy;

        public List<Matchup> Matchups { get; } = new List<Matchup>();

        public List<Team> Teams { get; } = new List<Team>();

        public event PropertyChangedEventHandler? PropertyChanged;

        public ViewModel()
        {
            //Options.Start(Environment.GetCommandLineArgs());
            //if (Options.Current.File != null)
            //{
            //    LoadData(Path.Combine(Options.Current.File));
            //}
        }

        public static void GlobalExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            Log.Fatal("Unexpected exception occured.", args.Exception);
            MessageBox.Show(args.Exception.Message, Resources.UnexpectedException, MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void ClearAll()
        {
            this.Teams.Clear();
            this.Matchups.Clear();

            Log.Debug("All data cleared...");
        }

        private async void GenerateScheduleAsync()
        {
            this.ClearAll();

            this.LoadMatchupsAndTeams();

            var scheduler = new Scheduler(this.Teams, this.Matchups);

            Log.Debug("Start scheduling...");
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            this.IsBusy = true;
            OnPropertyChanged(nameof(IsSilent));
            await Task.Run(() => scheduler.Run());
            this.IsBusy = false;
            OnPropertyChanged(nameof(IsSilent));

            stopWatch.Stop();
            var ts = stopWatch.Elapsed;
            //var elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            var elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:0000}";

            Log.Debug($"Scheduling finished after {elapsedTime}");
        }

        private void LoadMatchupsAndTeams()
        {
            // TODO file selector
            var lines = File.ReadLines(@"Matchups.txt", Encoding.Default);
            var e = lines.GetEnumerator();
            while (e.MoveNext())
            {
                var teams = e.Current.Split(' ');
                if (teams.Length != 2)
                {
                    throw new InvalidOperationException($"Invalid matchup: '{e.Current}'.");
                }

                var teamComparison = string.CompareOrdinal(teams[0], teams[1]);
                string t1;
                string t2;
                if (teamComparison < 0)
                {
                    t1 = teams[0];
                    t2 = teams[1];
                }
                else if (teamComparison > 0)
                {
                    t1 = teams[1];
                    t2 = teams[0];
                }
                else
                {
                    throw new InvalidOperationException($"Teams must not be equal: {teams[0]}");
                }

                if (!this.Teams.Any(t => t.Id == t1))
                {
                    this.Teams.Add(new Team(t1));
                }

                if (!this.Teams.Any(t => t.Id == t2))
                {
                    this.Teams.Add(new Team(t2));
                }

                var matchup = new Matchup(this.Teams.Single(t => t.Id == t1), this.Teams.Single(t => t.Id == t2));

                this.Matchups.Add(matchup);
            }

            this.Teams.Sort(new Team.TeamComparer());
            this.Matchups.Sort(new Matchup.MatchupComparer());

            Log.Debug("Matchup and Teams loaded:");
            Log.Debug($"  Teams: {string.Join(", ", this.Teams)}");
            Log.Debug($"  Matchups: {string.Join(", ", this.Matchups)}");
        }

        public class CommandHandler : ICommand
        {
            private readonly Action action;
            private readonly bool canExecute;

            public event EventHandler? CanExecuteChanged;

            public CommandHandler(Action action, bool canExecute)
            {
                this.action = action;
                this.canExecute = canExecute;
            }

            public bool CanExecute(object? parameter)
            {
                return this.canExecute;
            }

            public void Execute(object? parameter)
            {
                this.action();
            }
        }
    }
}