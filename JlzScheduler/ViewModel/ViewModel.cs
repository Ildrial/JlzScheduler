﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Input;

namespace JlzQualiTool
{
    using log4net;
    using Microsoft.Win32;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Threading;

    [DataContract]
    public class ViewModel //: INotifyPropertyChanged
    {
        private static ILog Log = log4net.LogManager.GetLogger(typeof(ViewModel));

        public ViewModel()
        {
            // TODO check about using CollectionViewSource instead for data grid binding cf https://stackoverflow.com/questions/19112922/sort-observablecollectionstring-through-c-sharp

            Options.Start(Environment.GetCommandLineArgs());
            if (Options.Current.File != null)
            {
                LoadData(Path.Combine(Options.Current.File));
            }
        }

        public static void GlobalExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            Log.Fatal("Unexpected exception occured.", args.Exception);
            MessageBox.Show(args.Exception.Message, Resources.UnexpectedException, MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
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

            public bool CanExecute(object parameter)
            {
                return this.canExecute;
            }

            public void Execute(object parameter)
            {
                this.action();
            }
        }
    }
}