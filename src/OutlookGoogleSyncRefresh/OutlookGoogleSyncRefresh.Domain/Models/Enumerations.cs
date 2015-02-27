﻿using System;

namespace OutlookGoogleSyncRefresh.Domain.Models
{
    [Flags]
    public enum OutlookOptionsEnum
    {
        None = 0,
        DefaultProfile = 1,
        AlternateProfile = 2,
        DefaultCalendar = 4,
        AlternateCalendar = 8,
        ExchangeWebServices = 16,
    }

    [Flags]
    public enum CalendarEntryOptionsEnum
    {
        None = 0,
        Description = 1,
        Attendees = 2,
        Reminders = 4,
        Attachments = 8,
    }
}