﻿using System;
using CalendarSyncPlus.Domain.Helpers;

namespace CalendarSyncPlus.Domain.Models
{
    public class DailySyncFrequency : SyncFrequency
    {
        public DailySyncFrequency()
        {
            Name = "Daily";
        }

        public DateTime StartDate { get; set; }

        public bool EveryWeekday { get; set; }

        public bool CustomDay { get; set; }

        public int DayGap { get; set; }

        public DateTime TimeOfDay { get; set; }

        public override bool ValidateTimer(DateTime dateTimeNow)
        {
            if (IsDayValid(dateTimeNow))
            {
                if (dateTimeNow.IsTimeValid(TimeOfDay))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsDayValid(DateTime dateTime)
        {
            if (EveryWeekday && dateTime.DayOfWeek != DayOfWeek.Saturday &&
                dateTime.DayOfWeek != DayOfWeek.Sunday)
            {
                return true;
            }

            if (CustomDay)
            {
                if (DayGap == 1)
                {
                    return true;
                }

                if (StartDate.Date.Subtract(dateTime.Date).Days%DayGap == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public override DateTime GetNextSyncTime(DateTime dateTimeNow)
        {
            if (dateTimeNow.CompareTo(TimeOfDay) > 0)
            {
                dateTimeNow = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, TimeOfDay.Hour,
                    TimeOfDay.Minute,
                    TimeOfDay.Second);
                dateTimeNow = dateTimeNow.Add(new TimeSpan(1, 0, 0, 0));
            }

            while (!ValidateTimer(dateTimeNow))
            {
                if (IsDayValid(dateTimeNow))
                {
                    return new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, TimeOfDay.Hour,
                        TimeOfDay.Minute,
                        TimeOfDay.Second);
                }
                dateTimeNow = dateTimeNow.Add(new TimeSpan(1, 0, 0, 0));
            }
            return dateTimeNow;
        }

        public override string ToString()
        {
            string str = string.Format("{0} : {1}", GetType().Name, TimeOfDay);
            return str;
        }
    }
}