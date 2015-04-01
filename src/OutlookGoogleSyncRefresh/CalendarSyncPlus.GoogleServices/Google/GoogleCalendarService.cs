﻿#region File Header

// /******************************************************************************
//  * 
//  *      Copyright (C) Ankesh Dave 2015 All Rights Reserved. Confidential
//  * 
//  ******************************************************************************
//  * 
//  *      Project:        OutlookGoogleSyncRefresh
//  *      SubProject:     OutlookGoogleSyncRefresh.Application
//  *      Author:         Dave, Ankesh
//  *      Created On:     02-02-2015 5:56 PM
//  *      Modified On:    04-02-2015 12:39 PM
//  *      FileName:       GoogleCalendarService.cs
//  * 
//  *****************************************************************************/

#endregion

#region Imports

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CalendarSyncPlus.Common.Log;
using CalendarSyncPlus.Common.MetaData;
using CalendarSyncPlus.Domain.Models;
using CalendarSyncPlus.Services;
using CalendarSyncPlus.Services.Interfaces;
using CalendarSyncPlus.Services.Utilities;
using CalendarSyncPlus.Services.Wrappers;
using Google;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Requests;
using Calendar = CalendarSyncPlus.Domain.Models.Calendar;

#endregion

namespace CalendarSyncPlus.GoogleServices.Google
{
    [Export(typeof(ICalendarService)), Export(typeof(IGoogleCalendarService))]
    [ExportMetadata("ServiceType", CalendarServiceType.Google)]
    public class GoogleCalendarService : IGoogleCalendarService
    {
        #region Static and Constants

        private const string dictionaryKey_CalendarId = "CalendarId";

        #endregion

        #region Fields

        private string calendarId;
        private ApplicationLogger ApplicationLogger { get; set; }

        #endregion

        #region Constructors

        [ImportingConstructor]
        public GoogleCalendarService(IAccountAuthenticationService accountAuthenticationService,
            ApplicationLogger applicationLogger)
        {
            ApplicationLogger = applicationLogger;
            AccountAuthenticationService = accountAuthenticationService;
        }

        #endregion

        #region Properties

        public IAccountAuthenticationService AccountAuthenticationService { get; set; }

        private string CalendarId
        {
            get { return calendarId; }
            set { calendarId = value; }
        }

        public Category EventCategory { get; set; }

        public string CalendarServiceName
        {
            get { return "Google"; }
        }

        #endregion

        #region Private Methods

        private Event CreateGoogleCalendarEvent(Appointment calenderAppointment, bool addDescription, bool addReminder,
            bool addAttendees, bool attendeesToDescription)
        {
            //Create Event
            var googleEvent = new Event
            {
                Start = new EventDateTime(),
                End = new EventDateTime(),
                Summary = calenderAppointment.Subject,
                Description = calenderAppointment.GetDescriptionData(addDescription, attendeesToDescription),
                Location = calenderAppointment.Location,
                Visibility = calenderAppointment.Privacy,
                Transparency = (calenderAppointment.BusyStatus == BusyStatusEnum.Free) ? "transparent" : "opaque",
                //Need to make recurring appointment IDs unique - append the item's date   
                ExtendedProperties =
                    new Event.ExtendedPropertiesData
                    {
                        Private =
                            new Dictionary<string, string>
                            {
                                {calenderAppointment.GetSourceEntryKey(), calenderAppointment.AppointmentId}
                            }
                    }
            };


            //Add Start/End Time
            if (calenderAppointment.AllDayEvent)
            {
                if (calenderAppointment.StartTime.HasValue)
                {
                    googleEvent.Start.Date = calenderAppointment.StartTime.Value.ToString("yyyy-MM-dd");
                }
                if (calenderAppointment.EndTime.HasValue)
                {
                    googleEvent.End.Date = calenderAppointment.EndTime.Value.ToString("yyyy-MM-dd");
                }
            }
            else
            {
                googleEvent.Start.DateTimeRaw = calenderAppointment.Rfc339FormatStartTime;
                googleEvent.End.DateTimeRaw = calenderAppointment.Rfc339FormatEndTime;
            }

            //Add Reminder
            if (addReminder && calenderAppointment.ReminderSet)
            {
                googleEvent.Reminders = new Event.RemindersData
                {
                    UseDefault = false,
                    Overrides = new List<EventReminder>
                    {
                        new EventReminder
                        {
                            Method = "popup",
                            Minutes = calenderAppointment.ReminderMinutesBeforeStart
                        }
                    }
                };
            }

            if (googleEvent.Attendees == null)
            {
                googleEvent.Attendees = new List<EventAttendee>();
            }

            if (addAttendees && !attendeesToDescription)
            {
                //Add Required Attendees
                AddEventAttendees(calenderAppointment.RequiredAttendees, googleEvent, false);

                //Add optional Attendees
                AddEventAttendees(calenderAppointment.OptionalAttendees, googleEvent, true);
            }
            //Add Organizer
            if (calenderAppointment.Organizer != null && calenderAppointment.Organizer.Email.IsValidEmailAddress())
            {
                googleEvent.Organizer = new Event.OrganizerData
                {
                    DisplayName = calenderAppointment.Organizer.Name,
                    Email = calenderAppointment.Organizer.Email
                };
            }

            return googleEvent;
        }

        private void AddEventAttendees(IEnumerable<Recipient> recipients, Event googleEvent, bool optional)
        {
            IEnumerable<Recipient> recipeintList = recipients as IList<Recipient> ?? recipients.ToList();
            if (!recipeintList.Any() && googleEvent == null)
            {
                return;
            }

            foreach (Recipient recipient in recipeintList)
            {
                //Ignore recipients with invalid Email
                if (!recipient.Email.IsValidEmailAddress())
                {
                    continue;
                }
                var eventAttendee = new EventAttendee
                {
                    DisplayName = recipient.Name,
                    Email = recipient.Email,
                    Optional = optional
                };
                googleEvent.Attendees.Add(eventAttendee);
            }
        }

        private CalendarService GetCalendarService()
        {
            return AccountAuthenticationService.AuthenticateCalenderOauth();
        }

        private void InsertEventErrorMessage(Event content, RequestError error, int index, HttpResponseMessage message,
            Dictionary<KeyValuePair<int, Appointment>, HttpResponseMessage> eventListIndex)
        {
            //var getKey = eventListIndex.FirstOrDefault(pair => pair.Key.Key == index);
            // eventListIndex[getKey.Key] = message;
        }

        private Appointment CreateAppointment(Event googleEvent)
        {
            Appointment appointment;

            if (googleEvent.Start.DateTime == null && googleEvent.End.DateTime == null)
            {
                appointment = new Appointment(googleEvent.Description, googleEvent.Location, googleEvent.Summary,
                    DateTime.Parse(googleEvent.End.Date),
                    DateTime.Parse(googleEvent.Start.Date), googleEvent.Id) { AllDayEvent = true };
            }
            else
            {
                appointment = new Appointment(googleEvent.Description, googleEvent.Location, googleEvent.Summary,
                    googleEvent.End.DateTime,
                    googleEvent.Start.DateTime, googleEvent.Id);
            }

            appointment.CalendarId = CalendarId;
            if (googleEvent.ExtendedProperties != null && googleEvent.ExtendedProperties.Private != null)
            {
                foreach (var property in googleEvent.ExtendedProperties.Private)
                {
                    appointment.ExtendedProperties.Add(property.Key, property.Value);
                }
            }

            appointment.Created = googleEvent.Created;
            appointment.LastModified = googleEvent.Updated;

            if (googleEvent.Organizer != null)
            {
                //Add Organizer
                appointment.Organizer = new Recipient
                {
                    Name = googleEvent.Organizer.DisplayName,
                    Email = googleEvent.Organizer.Email
                };
            }

            //Add Required Attendee
            GetAttendees(googleEvent, appointment.RequiredAttendees, false);

            //Add optional Attendee
            GetAttendees(googleEvent, appointment.OptionalAttendees, true);


            return appointment;
        }

        private void GetAttendees(Event googleEvent, List<Recipient> recipients, bool isOptional)
        {
            if (googleEvent != null && googleEvent.Attendees != null)
            {
                IEnumerable<EventAttendee> attendees =
                    googleEvent.Attendees.Where(attendee => attendee.Optional.GetValueOrDefault() == isOptional);

                foreach (EventAttendee eventAttendee in attendees)
                {
                    recipients.Add(new Recipient { Name = eventAttendee.DisplayName, Email = eventAttendee.Email });
                }
            }
        }

        #endregion

        #region IGoogleCalendarService Members

        public async void SetCalendarColor(Category background, IDictionary<string, object> calendarSpecificData)
        {
            CheckCalendarSpecificData(calendarSpecificData);

            CalendarService calendarService = GetCalendarService();

            CalendarListEntry calendarListEntry = await calendarService.CalendarList.Get(CalendarId).ExecuteAsync();

            calendarListEntry.BackgroundColor = background.HexValue;

            await calendarService.CalendarList.Update(calendarListEntry, calendarId).ExecuteAsync();
        }

        public void CheckCalendarSpecificData(IDictionary<string, object> calendarSpecificData)
        {
            if (calendarSpecificData == null)
            {
                throw new ArgumentNullException("calendarSpecificData", "Calendar Specific Data cannot be null");
            }

            object keyValue;
            if (!calendarSpecificData.TryGetValue(dictionaryKey_CalendarId, out keyValue))
            {
                throw new InvalidOperationException(string.Format("{0} is a required.", dictionaryKey_CalendarId));
            }

            CalendarId = keyValue as string;

            if (string.IsNullOrEmpty(calendarId))
            {
                throw new InvalidOperationException(string.Format("{0} cannot be null or empty.",
                    dictionaryKey_CalendarId));
            }

            object eventCategory;
            if (calendarSpecificData.TryGetValue("EventCategory", out eventCategory))
            {
                EventCategory = eventCategory as Category;
            }
            else
            {
                EventCategory = null;
            }
        }

        public async Task<List<Calendar>> GetAvailableCalendars(IDictionary<string, object> calendarSpecificData)
        {
            //Get Calendar Service
            CalendarService calendarService = GetCalendarService();

            CalendarList calenderList = await calendarService.CalendarList.List().ExecuteAsync();

            List<Calendar> localCalendarList =
                calenderList.Items.Select(
                    calendarListEntry => new Calendar { Id = calendarListEntry.Id, Name = calendarListEntry.Summary })
                    .ToList();
            return localCalendarList;
        }

        public async Task<bool> AddCalendarEvent(List<Appointment> calendarAppointments, bool addDescription,
            bool addReminder, bool addAttendees, bool attendeesToDescription,
            IDictionary<string, object> calendarSpecificData)
        {
            if (!calendarAppointments.Any())
            {
                return true;
            }

            CheckCalendarSpecificData(calendarSpecificData);

            var eventIndexList = new Dictionary<KeyValuePair<int, Appointment>, HttpResponseMessage>();
            //Get Calendar Service
            CalendarService calendarService = GetCalendarService();

            if (calendarAppointments == null || string.IsNullOrEmpty(CalendarId))
            {
                return false;
            }
            try
            {

                if (calendarAppointments.Any())
                {
                    //Create a Batch Request
                    var batchRequest = new BatchRequest(calendarService);

                    //Split the list of calendarAppointments by 1000 per list

                    //Iterate over each appointment to create a event and batch it 
                    for (int i = 0; i < calendarAppointments.Count; i++)
                    {
                        if (i != 0 && i % 999 == 0)
                        {
                            await batchRequest.ExecuteAsync();
                            batchRequest = new BatchRequest(calendarService);
                        }

                        Appointment appointment = calendarAppointments[i];
                        Event calendarEvent = CreateGoogleCalendarEvent(appointment, addDescription, addReminder,
                            attendeesToDescription,
                            addAttendees);
                        if (EventCategory != null)
                        {
                            calendarEvent.ColorId = "1";
                        }
                        EventsResource.InsertRequest insertRequest = calendarService.Events.Insert(calendarEvent,
                            CalendarId);
                        insertRequest.SendNotifications = false;
                        batchRequest.Queue<Event>(insertRequest,
                            (content, error, index, message) =>
                                InsertEventErrorMessage(content, error, index, message, eventIndexList));
                    }

                    await batchRequest.ExecuteAsync();
                }
            }
            catch (Exception exception)
            {
                ApplicationLogger.LogError(exception.ToString());
                return false;
            }
            return true;
        }


        public async Task<bool> DeleteCalendarEvent(List<Appointment> calendarAppointments,
            IDictionary<string, object> calendarSpecificData)
        {
            if (!calendarAppointments.Any())
            {
                return true;
            }

            CheckCalendarSpecificData(calendarSpecificData);

            var eventIndexList = new Dictionary<KeyValuePair<int, Appointment>, HttpResponseMessage>();
            //Get Calendar Service
            CalendarService calendarService = GetCalendarService();

            if (calendarAppointments == null || string.IsNullOrEmpty(CalendarId))
            {
                return false;
            }

            try
            {
                if (calendarAppointments.Any())
                {
                    //Create a Batch Request
                    var batchRequest = new BatchRequest(calendarService);

                    //Split the list of calendarAppointments by 1000 per list

                    //Iterate over each appointment to create a event and batch it 
                    for (int i = 0; i < calendarAppointments.Count; i++)
                    {
                        if (i != 0 && i % 999 == 0)
                        {
                            await batchRequest.ExecuteAsync();
                            batchRequest = new BatchRequest(calendarService);
                        }

                        Appointment appointment = calendarAppointments[i];
                        EventsResource.DeleteRequest deleteRequest = calendarService.Events.Delete(CalendarId,
                            appointment.AppointmentId);
                        batchRequest.Queue<Event>(deleteRequest,
                            (content, error, index, message) =>
                                InsertEventErrorMessage(content, error, index, message, eventIndexList));
                    }

                    await batchRequest.ExecuteAsync();
                }
            }
            catch (Exception exception)
            {
                ApplicationLogger.LogError(exception.ToString());
                return false;
            }
            return true;
        }

        public async Task<CalendarAppointments> GetCalendarEventsInRangeAsync(int daysInPast, int daysInFuture,
            IDictionary<string, object> calendarSpecificData)
        {
            CheckCalendarSpecificData(calendarSpecificData);

            //Get Calendar Service
            CalendarService calendarService = GetCalendarService();

            var finalEventList = new List<Appointment>();

            Events result = null;

            EventsResource.ListRequest eventListRequest = calendarService.Events.List(CalendarId);

            // Add Filters to event List Request
            eventListRequest.TimeMin = DateTime.Now.Date.AddDays(-(daysInPast));
            eventListRequest.TimeMax = DateTime.Now.Date.AddDays((daysInFuture + 1));
            eventListRequest.MaxAttendees = 1000;

            try
            {
                result = eventListRequest.Execute();
                if (result != null)
                {
                    while (result.Items != null)
                    {
                        // Add events to list, Split recurring appointments
                        foreach (Event eventItem in result.Items)
                        {
                            Appointment appointment = CreateAppointment(eventItem);
                            if (eventItem.Recurrence != null && eventItem.Recurrence.Count > 0)
                            {
                                finalEventList.AddRange(RecurrenceHelper.SplitRecurringAppointments(appointment,
                                    eventItem.Recurrence.FirstOrDefault(),
                                    DateTime.Now.Date.AddDays(-(daysInPast)),
                                    DateTime.Now.Date.AddDays((daysInFuture + 1))));
                            }
                            else
                            {
                                finalEventList.Add(appointment);
                            }
                        }

                        //If all pages are over break
                        if (result.NextPageToken == null)
                        {
                            break;
                        }

                        //Set the next page to pull from request
                        eventListRequest.PageToken = result.NextPageToken;

                        result = await eventListRequest.ExecuteAsync();
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (GoogleApiException exception)
            {
                ApplicationLogger.LogError(exception.ToString());
                return null;
            }
            catch (Exception exception)
            {
                ApplicationLogger.LogError(exception.ToString());
                return null;
            }

            var calendarAppointments = new CalendarAppointments { CalendarId = CalendarId };
            calendarAppointments.AddRange(finalEventList);
            return calendarAppointments;
        }

        public async Task<bool> ResetCalendar(IDictionary<string, object> calendarSpecificData)
        {
            CalendarAppointments appointments =
                await GetCalendarEventsInRangeAsync(10 * 365, 10 * 365, calendarSpecificData);
            if (appointments != null)
            {
                bool success = await DeleteCalendarEvent(appointments, calendarSpecificData);
                return success;
            }
            return false;
        }

        #endregion
    }
}