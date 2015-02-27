﻿using System;
using System.ComponentModel.Composition;
using System.Windows.Controls.Primitives;
using Hardcodet.Wpf.TaskbarNotification;
using OutlookGoogleSyncRefresh.Application.Views;

namespace OutlookGoogleSyncRefresh.Presentation.Views
{
    /// <summary>
    ///     Interaction logic for SystemTrayNotifierView.xaml
    /// </summary>
    [Export, Export(typeof(ISystemTrayNotifierView))]
    public partial class SystemTrayNotifierView : TaskbarIcon, ISystemTrayNotifierView
    {
        public SystemTrayNotifierView()
        {
            InitializeComponent();
        }

        #region ISystemTrayNotifierView Members

        public void ShowCustomBalloon()
        {
            var syncBalloon = new SyncBalloon();
            syncBalloon.DataContext = DataContext;
            ShowCustomBalloon(syncBalloon, PopupAnimation.Slide, null);
        }

        #endregion
    }
}