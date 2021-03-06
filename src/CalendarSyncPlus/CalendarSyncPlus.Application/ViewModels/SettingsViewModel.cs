﻿#region File Header

// /******************************************************************************
//  * 
//  *      Copyright (C) Ankesh Dave 2015 All Rights Reserved. Confidential
//  * 
//  ******************************************************************************
//  * 
//  *      Project:        CalendarSyncPlus
//  *      SubProject:     CalendarSyncPlus.Application
//  *      Author:         Dave, Ankesh
//  *      Created On:     02-02-2015 11:15 AM
//  *      Modified On:    04-02-2015 12:40 PM
//  *      FileName:       SettingsViewModel.cs
//  * 
//  *****************************************************************************/

#endregion

#region Imports

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Applications;
using CalendarSyncPlus.Application.Views;
using CalendarSyncPlus.Common.Log;
using CalendarSyncPlus.Domain;
using CalendarSyncPlus.Domain.Models;
using CalendarSyncPlus.ExchangeWebServices.ExchangeWeb;
using CalendarSyncPlus.GoogleServices.Google;
using CalendarSyncPlus.OutlookServices.Outlook;
using CalendarSyncPlus.Services.Interfaces;
using MahApps.Metro.Controls.Dialogs;

#endregion

namespace CalendarSyncPlus.Application.ViewModels
{
    [Export]
    public class SettingsViewModel : ViewModel<ISettingsView>
    {
        #region Constructors

        [ImportingConstructor]
        public SettingsViewModel(ISettingsView view,
            IGoogleCalendarService googleCalendarService,
            Settings settings,
            ISettingsSerializationService serializationService, IOutlookCalendarService outlookCalendarService,
            IMessageService messageService, IExchangeWebCalendarService exchangeWebCalendarService,
            ApplicationLogger applicationLogger, IWindowsStartupService windowsStartupService, IAccountAuthenticationService accountAuthenticationService)
            : base(view)
        {
            Settings = settings;
            ExchangeWebCalendarService = exchangeWebCalendarService;
            ApplicationLogger = applicationLogger;
            WindowsStartupService = windowsStartupService;
            AccountAuthenticationService = accountAuthenticationService;
            GoogleCalendarService = googleCalendarService;
            SettingsSerializationService = serializationService;
            OutlookCalendarService = outlookCalendarService;
            MessageService = messageService;
        }

        #endregion

        #region Fields

        private bool _checkForUpdates = true;
        private bool _createNewFileForEverySync;
        private DelegateCommand _createProfileCommand;
        private DelegateCommand _deleteProfileCommand;
        private bool _hideSystemTrayTooltip;
        private bool _isLoading;
        private bool _isloaded;
        private bool _logSyncInfo;
        private bool _minimizeToSystemTray = true;
        private DelegateCommand _moveDownCommand;
        private DelegateCommand _moveUpCommand;
        private bool _isManualSynchronization = true;
        private bool _runApplicationAtSystemStartup = true;
        private DelegateCommand _saveCommand;
        private ProfileViewModel _selectedProfile;
        private Settings _settings;
        private bool _settingsSaved;
        private ObservableCollection<ProfileViewModel> _syncProfileList;
        private ProxySettingsDataModel _proxySettings;
        private bool _isValid;
        private DelegateCommand _addNewGoogleAccount;
        private ObservableCollection<GoogleAccount> _googleAccounts;
        private DelegateCommand _disconnectGoogleCommand;
        private bool _allowManualGoogleAuth;
        private string _googleAuthCode;
        private bool _isAuthCodeAvailable;
        private bool _checkForAlphaReleases;

        #endregion

        #region Properties

        public IGoogleCalendarService GoogleCalendarService { get; set; }
        public ISettingsSerializationService SettingsSerializationService { get; set; }
        public IOutlookCalendarService OutlookCalendarService { get; set; }
        public IMessageService MessageService { get; set; }
        public IExchangeWebCalendarService ExchangeWebCalendarService { get; private set; }
        public ApplicationLogger ApplicationLogger { get; private set; }
        public IWindowsStartupService WindowsStartupService { get; set; }
        public IAccountAuthenticationService AccountAuthenticationService { get; set; }


        public DelegateCommand CreateProfileCommand
        {
            get { return _createProfileCommand ?? (_createProfileCommand = new DelegateCommand(CreateProfile)); }
        }

        public DelegateCommand DeleteProfileCommand
        {
            get { return _deleteProfileCommand ?? (_deleteProfileCommand = new DelegateCommand(DeleteProfile)); }
        }

        public DelegateCommand MoveUpCommand
        {
            get { return _moveUpCommand ?? (_moveUpCommand = new DelegateCommand(MoveProfileUp)); }
        }

        public DelegateCommand MoveDownCommand
        {
            get { return _moveDownCommand ?? (_moveDownCommand = new DelegateCommand(MoveProfileDown)); }
        }

        public DelegateCommand SaveCommand
        {
            get { return _saveCommand ?? (_saveCommand = new DelegateCommand(SaveSettings)); }
        }

        public DelegateCommand DisconnectGoogleCommand
        {
            get { return _disconnectGoogleCommand ?? (_disconnectGoogleCommand = new DelegateCommand(DisconnectGoogleHandler)); }
        }


        public bool LogSyncInfo
        {
            get { return _logSyncInfo; }
            set { SetProperty(ref _logSyncInfo, value); }
        }

        public bool CreateNewFileForEverySync
        {
            get { return _createNewFileForEverySync; }
            set { SetProperty(ref _createNewFileForEverySync, value); }
        }

        public Settings Settings
        {
            get { return _settings; }
            set { SetProperty(ref _settings, value); }
        }

        public ObservableCollection<ProfileViewModel> SyncProfileList
        {
            get { return _syncProfileList; }
            set { SetProperty(ref _syncProfileList, value); }
        }

        public ProfileViewModel SelectedProfile
        {
            get { return _selectedProfile; }
            set
            {
                SetProperty(ref _selectedProfile, value);
                if (_selectedProfile != null)
                {
                    _selectedProfile.LoadSyncProfile();
                }
            }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        public bool CheckForAlphaReleases
        {
            get { return _checkForAlphaReleases; }
            set { SetProperty(ref _checkForAlphaReleases, value); }
        }

        public bool CheckForUpdates
        {
            get { return _checkForUpdates; }
            set
            {
                SetProperty(ref _checkForUpdates, value);
                if (!_checkForUpdates)
                {
                    CheckForAlphaReleases = false;
                }
            }
        }

        public bool RunApplicationAtSystemStartup
        {
            get { return _runApplicationAtSystemStartup; }
            set { SetProperty(ref _runApplicationAtSystemStartup, value); }
        }

        public bool IsManualSynchronization
        {
            get { return _isManualSynchronization; }
            set { SetProperty(ref _isManualSynchronization, value); }
        }

        public bool SettingsSaved
        {
            get { return _settingsSaved; }
            set { SetProperty(ref _settingsSaved, value); }
        }

        public bool MinimizeToSystemTray
        {
            get { return _minimizeToSystemTray; }
            set { SetProperty(ref _minimizeToSystemTray, value); }
        }

        public bool HideSystemTrayTooltip
        {
            get { return _hideSystemTrayTooltip; }
            set { SetProperty(ref _hideSystemTrayTooltip, value); }
        }

        public ProxySettingsDataModel ProxySettings
        {
            get { return _proxySettings; }
            set { SetProperty(ref _proxySettings, value); }
        }

        public bool IsValid
        {
            get { return _isValid; }
            set { SetProperty(ref _isValid, value); }
        }

        private async void CreateProfile()
        {
            if (SyncProfileList.Count > 4)
            {
                MessageService.ShowMessageAsync("You have reached the maximum number of profiles.");
                return;
            }

            string result = await MessageService.ShowInput("Please enter profile name.");

            if (!string.IsNullOrEmpty(result))
            {
                if (SyncProfileList.Any(t => !string.IsNullOrEmpty(t.Name) && t.Name.Equals(result)))
                {
                    MessageService.ShowMessageAsync(
                        string.Format("A Profile with name '{0}' already exists. Please try again.", result));
                    return;
                }

                CalendarSyncProfile syncProfile = CalendarSyncProfile.GetDefaultSyncProfile();
                syncProfile.Name = result;
                syncProfile.IsDefault = false;
                var viewModel = new ProfileViewModel(syncProfile, GoogleCalendarService, OutlookCalendarService,
                    MessageService,
                    ExchangeWebCalendarService, ApplicationLogger, AccountAuthenticationService);
                PropertyChangedEventManager.AddHandler(viewModel, ProfilePropertyChangedHandler, "IsLoading");
                viewModel.Initialize(null);
                SyncProfileList.Add(viewModel);

            }
        }

        private async void DeleteProfile(object parameter)
        {
            var profile = parameter as ProfileViewModel;
            if (profile != null)
            {
                MessageDialogResult task =
                    await MessageService.ShowConfirmMessage("Are you sure you want to delete the profile?");
                if (task == MessageDialogResult.Affirmative)
                {
                    SyncProfileList.Remove(profile);
                    PropertyChangedEventManager.RemoveHandler(profile, ProfilePropertyChangedHandler, "IsLoading");
                    SelectedProfile = SyncProfileList.FirstOrDefault();
                }
            }
        }

        private void MoveProfileUp(object parameter)
        {
            var profile = parameter as ProfileViewModel;
            if (profile != null)
            {
                int index = SyncProfileList.IndexOf(profile);
                if (index > 0)
                {
                    SyncProfileList.Move(index, index - 1);
                }
            }
        }

        private void MoveProfileDown(object parameter)
        {
            var profile = parameter as ProfileViewModel;
            if (profile != null)
            {
                int index = SyncProfileList.IndexOf(profile);
                if (index < SyncProfileList.Count - 1)
                {
                    SyncProfileList.Move(index, index + 1);
                }
            }
        }

        public bool AllowManualGoogleAuth
        {
            get { return _allowManualGoogleAuth; }
            set { SetProperty(ref _allowManualGoogleAuth, value); }
        }

        public DelegateCommand AddNewGoogleAccount
        {
            get
            {
                return _addNewGoogleAccount = _addNewGoogleAccount ?? new DelegateCommand(AddNewGoogleAccountHandler);
            }
        }
        public ObservableCollection<GoogleAccount> GoogleAccounts
        {
            get { return _googleAccounts; }
            set { SetProperty(ref _googleAccounts, value); }
        }

        private async void AddNewGoogleAccountHandler()
        {
            //Accept Email Id
            string accountName = await MessageService.ShowInput("Enter your Google Email", "Adding Google Account");

            if (string.IsNullOrEmpty(accountName))
            {
                return;
            }

            if (GoogleAccounts != null &&
                GoogleAccounts.Any(t => t.Name.Equals(accountName, StringComparison.InvariantCultureIgnoreCase)))
            {
                MessageService.ShowMessageAsync("An account with the same email already exists. Please try again.");
                return;
            }

            //Create cancellation token to support cancellation
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            if (AllowManualGoogleAuth)
            {
                var authResult = await AccountAuthenticationService.ManualAccountAuthetication(accountName, tokenSource.Token, GetGoogleAuthCode);

                if (!authResult)
                {
                    MessageService.ShowMessageAsync("Account Not Added, Authorization Interrupted, Try Again");
                }
                else
                {
                    AddGoogleAccountDetailsToApplication(accountName);
                }
            }
            else
            {
                // Start progress controller
                var progressDialogController =
                    await MessageService.ShowProgress("Authenticate and Authorize in the browser window", "Add Google Account");
                //Delay for Preparedness
                await Task.Delay(5000);

                progressDialogController.SetIndeterminate();
                progressDialogController.SetCancelable(true);

                var authorizeGoogleAccountTask = AccountAuthenticationService.AuthorizeGoogleAccount(accountName, tokenSource.Token);

                //Wait for 120 seconds
                int timeInSeconds = 120;
                while (timeInSeconds > 0)
                {
                    progressDialogController.SetMessage(String.Format("Authenticate and Authorize in the browser window in {0} secs",
                        timeInSeconds));

                    //cancel task if cancellation is requested
                    if (progressDialogController.IsCanceled)
                    {
                        tokenSource.Cancel();
                        break;
                    }

                    //break loop if task changes its status
                    if (authorizeGoogleAccountTask.IsCanceled || authorizeGoogleAccountTask.IsFaulted || authorizeGoogleAccountTask.IsCompleted)
                    {
                        break;
                    }
                    timeInSeconds--;
                    await Task.Delay(1000, tokenSource.Token);
                }

                if (timeInSeconds < 0)
                {
                    tokenSource.Cancel();
                }

                await progressDialogController.CloseAsync();

                if (authorizeGoogleAccountTask.IsCanceled || authorizeGoogleAccountTask.IsFaulted || tokenSource.Token.IsCancellationRequested ||
                    progressDialogController.IsCanceled)
                {
                    MessageService.ShowMessageAsync("Account Not Added, Authorization Interrupted, Try Again");
                }
                else
                {
                    AddGoogleAccountDetailsToApplication(accountName);
                }
            }
        }

        private async void AddGoogleAccountDetailsToApplication(string accountName)
        {
            var account = new GoogleAccount() { Name = accountName };
            if (GoogleAccounts == null)
            {
                GoogleAccounts = new ObservableCollection<GoogleAccount>();
            }
            GoogleAccounts.Add(account);
            SelectedProfile.SelectedGoogleAccount = account;
            SelectedProfile.GoogleCalendars = null;
            SelectedProfile.GetGoogleCalendar();

            Settings.GoogleAccounts = GoogleAccounts;

            await SettingsSerializationService.SerializeSettingsAsync(Settings);
        }

        private async Task<string> GetGoogleAuthCode()
        {
            return await MessageService.ShowInput("Enter Auth Code after authorization in browser window", "Manual Authentication");
        }

        #endregion

        #region Private Methods

        private async void SaveSettings()
        {
            IsLoading = true;
            SettingsSaved = false;
            Settings.GoogleAccounts = GoogleAccounts;
            Settings.SettingsVersion = ApplicationInfo.Version;
            Settings.AppSettings.MinimizeToSystemTray = MinimizeToSystemTray;
            Settings.AppSettings.HideSystemTrayTooltip = HideSystemTrayTooltip;
            Settings.AppSettings.CheckForUpdates = CheckForUpdates;
            Settings.AppSettings.CheckForAlphaReleases = CheckForAlphaReleases;
            Settings.AppSettings.RunApplicationAtSystemStartup = RunApplicationAtSystemStartup;
            Settings.AppSettings.IsManualSynchronization = IsManualSynchronization;
            Settings.AllowManualAuthentication = AllowManualGoogleAuth;
            Settings.AppSettings.ProxySettings = new ProxySetting()
            {
                BypassOnLocal = ProxySettings.BypassOnLocal,
                Domain = ProxySettings.Domain,
                Password = ProxySettings.Password,
                Port = ProxySettings.Port,
                ProxyAddress = ProxySettings.ProxyAddress,
                ProxyType = ProxySettings.ProxyType,
                UseDefaultCredentials = ProxySettings.UseDefaultCredentials,
                UserName = ProxySettings.UserName
            };
            ApplyProxySettings();
            Settings.SyncProfiles.Clear();
            foreach (ProfileViewModel profileViewModel in SyncProfileList)
            {
                Settings.SyncProfiles.Add(profileViewModel.SaveCurrentSyncProfile());
            }

            if (RunApplicationAtSystemStartup)
            {
                WindowsStartupService.RunAtWindowsStartup();
            }
            else
            {
                WindowsStartupService.RemoveFromWindowsStartup();
            }

            try
            {
                bool result = await SettingsSerializationService.SerializeSettingsAsync(Settings);
                await
                    MessageService.ShowMessage(result ? "Settings Saved Successfully" : "Error Saving Settings",
                        "Settings");
                SettingsSaved = true;
            }
            catch (AggregateException exception)
            {
                SettingsSaved = false;
                AggregateException flattenException = exception.Flatten();
                MessageService.ShowMessageAsync(flattenException.Message);
            }
            catch (Exception exception)
            {
                SettingsSaved = false;
                MessageService.ShowMessageAsync(exception.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        public void Load()
        {
            if (_isloaded)
            {
                return;
            }
            LoadSettingsAndGetCalendars();
            _isloaded = true;
        }

        private void LoadSettingsAndGetCalendars()
        {
            try
            {
                if (Settings != null)
                {
                    ProxySettings = new ProxySettingsDataModel()
                    {
                        BypassOnLocal = Settings.AppSettings.ProxySettings.BypassOnLocal,
                        Domain = Settings.AppSettings.ProxySettings.Domain,
                        Password = Settings.AppSettings.ProxySettings.Password,
                        Port = Settings.AppSettings.ProxySettings.Port,
                        ProxyAddress = Settings.AppSettings.ProxySettings.ProxyAddress,
                        ProxyType = Settings.AppSettings.ProxySettings.ProxyType,
                        UseDefaultCredentials = Settings.AppSettings.ProxySettings.UseDefaultCredentials,
                        UserName = Settings.AppSettings.ProxySettings.UserName
                    };
                    ApplyProxySettings();
                    AllowManualGoogleAuth = Settings.AllowManualAuthentication;
                    GoogleAccounts = Settings.GoogleAccounts;
                    MinimizeToSystemTray = Settings.AppSettings.MinimizeToSystemTray;
                    HideSystemTrayTooltip = Settings.AppSettings.HideSystemTrayTooltip;
                    CheckForUpdates = Settings.AppSettings.CheckForUpdates;
                    CheckForAlphaReleases = Settings.AppSettings.CheckForAlphaReleases;
                    RunApplicationAtSystemStartup = Settings.AppSettings.RunApplicationAtSystemStartup;
                    IsManualSynchronization = Settings.AppSettings.IsManualSynchronization;
                    LoadProfiles();
                }
                else
                {
                    LogSyncInfo = true;
                    CreateNewFileForEverySync = false;
                    MinimizeToSystemTray = true;
                    HideSystemTrayTooltip = false;
                    CheckForUpdates = true;
                }
            }
            catch (AggregateException exception)
            {
                AggregateException flattenException = exception.Flatten();
                MessageService.ShowMessageAsync(flattenException.Message);
            }
            catch (Exception exception)
            {
                MessageService.ShowMessageAsync(exception.Message);
            }
        }

        private void LoadProfiles()
        {
            var profileList = new ObservableCollection<ProfileViewModel>();
            foreach (CalendarSyncProfile syncProfile in Settings.SyncProfiles)
            {
                var viewModel = new ProfileViewModel(syncProfile, GoogleCalendarService, OutlookCalendarService,
                    MessageService,
                    ExchangeWebCalendarService, ApplicationLogger, AccountAuthenticationService);
                PropertyChangedEventManager.AddHandler(viewModel, ProfilePropertyChangedHandler, "IsLoading");
                var googleAccount = GetGoogleAccount(syncProfile);

                viewModel.Initialize(googleAccount);
                profileList.Add(viewModel);
            }
            SyncProfileList = profileList;
            SelectedProfile = SyncProfileList.FirstOrDefault();
        }

        private GoogleAccount GetGoogleAccount(CalendarSyncProfile syncProfile)
        {
            GoogleAccount googleAccount = null;
            if (syncProfile.GoogleAccount != null)
            {
                if (GoogleAccounts.Any())
                {
                    googleAccount = GoogleAccounts.FirstOrDefault(
                        account => account.Name == syncProfile.GoogleAccount.Name);
                }
            }

            if (googleAccount != null)
            {
                googleAccount.GoogleCalendar = syncProfile.GoogleAccount.GoogleCalendar;
            }
            return googleAccount;
        }

        private async void DisconnectGoogleHandler()
        {
            if (SelectedProfile.SelectedGoogleAccount == null)
            {
                MessageService.ShowMessageAsync("No account selected");
                return;
            }

            var dialogResult = await MessageService.ShowConfirmMessage("Disconnection of Google account cannot be reverted.\nClick Yes to continue.");
            if (dialogResult == MessageDialogResult.Negative)
            {
                return;
            }

            var result = AccountAuthenticationService.DisconnectGoogle(SelectedProfile.SelectedGoogleAccount.Name);
            if (result)
            {
                //Remove google account
                var googleAccount =
                    GoogleAccounts.FirstOrDefault(account => account.Name == SelectedProfile.SelectedGoogleAccount.Name);

                if (googleAccount != null)
                {
                    foreach (var profileViewModel in SyncProfileList)
                    {
                        if (profileViewModel.SelectedGoogleAccount != null &&
                            profileViewModel.SelectedGoogleAccount.Name.Equals(googleAccount.Name))
                        {
                            profileViewModel.SelectedGoogleAccount = null;
                            profileViewModel.GoogleCalendars = null;
                            profileViewModel.SelectedCalendar = null;
                        }
                    }

                    GoogleAccounts.Remove(googleAccount);

                    await MessageService.ShowMessage("Google account successfully disconnected");

                    foreach (var calendarSyncProfile in Settings.SyncProfiles)
                    {
                        if (calendarSyncProfile.GoogleAccount != null &&
                            calendarSyncProfile.GoogleAccount.Name.Equals(googleAccount.Name))
                        {
                            calendarSyncProfile.GoogleAccount = null;
                        }
                    }

                    await SettingsSerializationService.SerializeSettingsAsync(Settings);
                }
            }
            else
            {
                MessageService.ShowMessageAsync("Account wasn't authenticated earlier or disconnection failed.");
            }
        }

        internal void ApplyProxySettings()
        {
            IWebProxy proxy;
            try
            {
                var proxySettings = Settings.AppSettings.ProxySettings;
                switch (proxySettings.ProxyType)
                {
                    case ProxyType.NoProxy:
                        WebRequest.DefaultWebProxy = null;
                        break;
                    case ProxyType.ProxyWithAuth:
                        proxy = new WebProxy(new Uri(string.Format("{0}:{1}", proxySettings.ProxyAddress, proxySettings.Port)), proxySettings.BypassOnLocal)
                        {
                            UseDefaultCredentials = proxySettings.UseDefaultCredentials
                        };

                        if (!proxySettings.UseDefaultCredentials)
                        {
                            proxy.Credentials = string.IsNullOrEmpty(proxySettings.Domain)
                                ? new NetworkCredential(proxySettings.UserName, proxySettings.Password)
                                : new NetworkCredential(proxySettings.UserName, proxySettings.Password,
                                    proxySettings.Domain);
                        }
                        WebRequest.DefaultWebProxy = proxy;
                        break;
                    default:
                        proxy = WebRequest.GetSystemWebProxy();
                        WebRequest.DefaultWebProxy = proxy;
                        break;
                }
            }
            catch (Exception exception)
            {
                ApplicationLogger.LogError(exception.ToString(), typeof(SettingsViewModel));
                MessageService.ShowMessageAsync("Invalid Proxy Settings. Proxy settings cannot be applied");
            }
        }

        private void ProfilePropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsLoading":
                    var viewModel = sender as ProfileViewModel;
                    if (viewModel != null)
                    {
                        IsLoading = viewModel.IsLoading;
                    }
                    break;
            }
        }

        public void Shutdown()
        {
            if (SyncProfileList != null)
            {
                foreach (ProfileViewModel profileViewModel in SyncProfileList)
                {
                    PropertyChangedEventManager.RemoveHandler(profileViewModel, ProfilePropertyChangedHandler,
                        "IsLoading");
                }
            }
        }
    }
}