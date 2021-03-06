﻿using System;
using System.Collections.Generic;
using System.Waf.Foundation;
using CalendarSyncPlus.Domain.Helpers;

namespace CalendarSyncPlus.Domain.Models
{
    public class Appointment : Model, ICloneable
    {
        /// <summary>
        ///     Flag if the appointment is an all day event
        /// </summary>
        private bool _allDayEvent;

        /// <summary>
        ///     Id to identify the appointment
        /// </summary>
        private string _appointmentId;
        /// <summary>
        /// 
        /// </summary>
        private string _calendarId;
        /// <summary>
        /// 
        /// </summary>
        private DateTime? _created;

        /// <summary>
        /// </summary>
        private string _description;
        /// <summary>
        /// 
        /// </summary>
        private DateTime? _endTime;
        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, string> _extendedProperties;
        /// <summary>
        /// 
        /// </summary>
        private bool _isRecurring;
        /// <summary>
        /// 
        /// </summary>
        private DateTime? _lastModified;
        private string _location;
        private List<Recipient> _optionalAttendees;
        private Recipient _organizer;
        private string _privacy;
        private List<Recipient> _recipients;
        private int _reminderMinutesBeforeStart;
        private bool _reminderSet;
        private List<Recipient> _requiredAttendees;
        private string _sourceId;
        private string _childId;
        private DateTime? _startTime;
        private string _subject;
        private MeetingStatusEnum _meetingStatus;
        private DateTime? _oldStartTime;

        public Appointment(string description, string location, string subject, DateTime? endTime, DateTime? startTime,
            string appointmentId)
            : this(description, location, subject, endTime, startTime)
        {
            _appointmentId = appointmentId;
        }

        public Appointment(string description, string location, string subject, DateTime? endTime, DateTime? startTime)
        {
            _description = description;
            _location = location;
            _subject = subject;
            _endTime = endTime;
            _startTime = startTime;
            ExtendedProperties = new Dictionary<string, string>();
            RequiredAttendees = new List<Recipient>();
            OptionalAttendees = new List<Recipient>();
        }

        public DateTime? OldStartTime
        {
            get { return _oldStartTime; }
            set { SetProperty(ref _oldStartTime, value); }
        }

        public DateTime? StartTime
        {
            get { return _startTime; }
            set { SetProperty(ref _startTime, value); }
        }

        public DateTime? EndTime
        {
            get { return _endTime; }
            set { SetProperty(ref _endTime, value); }
        }

        public string Subject
        {
            get { return _subject; }
            set { SetProperty(ref _subject, value); }
        }

        public string Location
        {
            get { return _location; }
            set { SetProperty(ref _location, value); }
        }

        public string Description
        {
            get { return _description; }
            set { SetProperty(ref _description, value); }
        }

        public string AppointmentId
        {
            get { return _appointmentId; }
            set { SetProperty(ref _appointmentId, value); }
        }

        public string SourceId
        {
            get { return _sourceId; }
            set { SetProperty(ref _sourceId, value); }
        }

        public string ChildId
        {
            get { return _childId; }
            set { SetProperty(ref _childId, value); }
        }

        public string CalendarId
        {
            get { return _calendarId; }
            set { SetProperty(ref _calendarId, value); }
        }

        public Dictionary<string, string> ExtendedProperties
        {
            get { return _extendedProperties; }
            set { SetProperty(ref _extendedProperties, value); }
        }

        public string Rfc339FormatStartTime
        {
            get { return StartTime.Value.Rfc339FFormat(); }
        }


        public string Rfc339FormatEndTime
        {
            get { return EndTime.Value.Rfc339FFormat(); }
        }

        public bool AllDayEvent
        {
            get { return _allDayEvent; }
            set { SetProperty(ref _allDayEvent, value); }
        }

        public bool ReminderSet
        {
            get { return _reminderSet; }
            set { SetProperty(ref _reminderSet, value); }
        }

        public int ReminderMinutesBeforeStart
        {
            get { return _reminderMinutesBeforeStart; }
            set { SetProperty(ref _reminderMinutesBeforeStart, value); }
        }

        public BusyStatusEnum BusyStatus { get; set; }

        public Recipient Organizer
        {
            get { return _organizer; }
            set { SetProperty(ref _organizer, value); }
        }

        public List<Recipient> RequiredAttendees
        {
            get { return _requiredAttendees; }
            set { SetProperty(ref _requiredAttendees, value); }
        }

        public List<Recipient> OptionalAttendees
        {
            get { return _optionalAttendees; }
            set { SetProperty(ref _optionalAttendees, value); }
        }

        public MeetingStatusEnum MeetingStatus
        {
            get { return _meetingStatus; }
            set { SetProperty(ref _meetingStatus, value); }
        }

        public string Privacy
        {
            get { return _privacy; }
            set { SetProperty(ref _privacy, value); }
        }

        public bool IsRecurring
        {
            get { return _isRecurring; }
            set { SetProperty(ref _isRecurring, value); }
        }


        public DateTime? LastModified
        {
            get { return _lastModified; }
            set { SetProperty(ref _lastModified, value); }
        }

        public DateTime? Created
        {
            get { return _created; }
            set { SetProperty(ref _created, value); }
        }

        #region ICloneable Members

        public object Clone()
        {
            var appointment = new Appointment(Description, Location, Subject, EndTime, StartTime, AppointmentId);
            appointment.Organizer = Organizer;
            appointment.RequiredAttendees = RequiredAttendees;
            appointment.OptionalAttendees = OptionalAttendees;
            appointment.Created = Created;
            appointment.LastModified = LastModified;
            appointment.CalendarId = CalendarId;
            appointment.ExtendedProperties = ExtendedProperties;
            appointment.AllDayEvent = AllDayEvent;
            return appointment;
        }

        #endregion

        public override bool Equals(Object obj)
        {
            // Check if the object is a Appointment.
            // The initial null check is unnecessary as the cast will result in null
            // if obj is null to start with.
            var appointment = obj as Appointment;

            if (appointment == null)
            {
                // If it is null then it is not equal to this instance.
                return false;
            }
            // Instances are considered equal if the ToString matches.
            return this.ToString().Equals(appointment.ToString());
        }


        public override int GetHashCode()
        {
            // Returning the hashcode of the Guid used for the ToString() will be 
            // sufficient and would only cause a problem if Appointments objects
            // were stored in a non-generic hash set along side other guid instances
            // which is very unlikely!
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return Rfc339FormatStartTime + ";" + Rfc339FormatEndTime + ";" + Subject + ";" + Location;
        }
    }
}