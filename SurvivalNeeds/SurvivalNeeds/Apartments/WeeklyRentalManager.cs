using GTA;
using GTA.Native;
using SurvivalNeeds.BankingSystem;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SurvivalNeeds.Apartments
{
    public class WeeklyRentalManager
    {
        public const int WeeklyRent =
            2000;

        public const int EarlyCancellationFee =
            2000;

        public const int PaymentsRequiredForOwnership =
            18;

        private readonly BankAccount
            bankAccount;

        private readonly string
            rentalSaveFolder;

        private readonly Dictionary<string, RentalRecord>
            rentalRecords =
                new Dictionary<string, RentalRecord>(
                    StringComparer.OrdinalIgnoreCase
                );

        //====================================================
        // RENTAL RECORD
        //====================================================

        private class RentalRecord
        {
            public bool Active;

            public bool PermanentlyOwned;

            public int CompletedPayments;

            public int TotalPaid;

            public DateTime DueDate;
        }

        //====================================================
        // CONSTRUCTOR
        //====================================================

        public WeeklyRentalManager(
            BankAccount bankAccount)
        {
            this.bankAccount =
                bankAccount;

            rentalSaveFolder =
                GetRentalSaveFolder();

            Directory.CreateDirectory(
                rentalSaveFolder
            );
        }

        //====================================================
        // PROPERTY WEEKLY RENT
        //====================================================

        public int GetWeeklyRent(
            string propertyId)
        {
            if (string.Equals(
                propertyId,
                "DEL_PERRO_HEIGHTS_RENTAL",
                StringComparison.OrdinalIgnoreCase))
            {
                return 10000;
            }

            return WeeklyRent;
        }

        public int GetCancellationFee(
    string propertyId)
        {
            if (string.Equals(
                propertyId,
                "DEL_PERRO_HEIGHTS_RENTAL",
                StringComparison.OrdinalIgnoreCase))
            {
                return 10000;
            }

            return EarlyCancellationFee;
        }

        //====================================================
        // PROPERTY REQUIRED PAYMENTS
        //====================================================

        public int GetRequiredPayments(
            string propertyId)
        {
            if (string.Equals(
                propertyId,
                "DEL_PERRO_HEIGHTS_RENTAL",
                StringComparison.OrdinalIgnoreCase))
            {
                return 20;
            }

            return PaymentsRequiredForOwnership;
        }

        //====================================================
        // CHECK PERMANENT OWNERSHIP
        //====================================================

        public bool IsPermanentlyOwned(
            string profileId,
            string propertyId)
        {
            RentalRecord record =
                GetRecord(
                    profileId,
                    propertyId
                );

            return record.PermanentlyOwned;
        }

        //====================================================
        // CHECK ACTIVE RENTAL
        //====================================================

        public bool IsRented(
            string profileId,
            string propertyId)
        {
            RentalRecord record =
                GetRecord(
                    profileId,
                    propertyId
                );

            if (record.PermanentlyOwned)
            {
                return true;
            }

            if (!record.Active)
            {
                return false;
            }

            ProcessDuePayments(
                profileId,
                propertyId,
                record
            );

            return
                record.Active ||
                record.PermanentlyOwned;
        }

        //====================================================
        // START RENTAL
        //====================================================

        public bool StartRental(
            string profileId,
            string propertyId,
            out string message)
        {
            RentalRecord record =
                GetRecord(
                    profileId,
                    propertyId
                );

            int weeklyRent =
                GetWeeklyRent(
                    propertyId
                );

            int requiredPayments =
                GetRequiredPayments(
                    propertyId
                );

            if (record.PermanentlyOwned)
            {
                message =
                    "You already own this property permanently.";

                return false;
            }

            if (record.Active)
            {
                message =
                    "You are already renting this property.";

                return false;
            }

            if (bankAccount == null ||
                !bankAccount.HasAccount)
            {
                message =
                    "You need a bank account to rent this property.";

                return false;
            }

            if (!bankAccount.CanWithdraw(
                weeklyRent))
            {
                message =
                    "You need $" +
                    weeklyRent.ToString("N0") +
                    " in your bank account.";

                return false;
            }

            if (!bankAccount.Withdraw(
                weeklyRent))
            {
                message =
                    "The rental payment failed.";

                return false;
            }

            record.Active =
                true;

            record.PermanentlyOwned =
                false;

            record.CompletedPayments =
                1;

            record.TotalPaid =
                weeklyRent;

            record.DueDate =
                GetCurrentGameDate()
                    .AddDays(7);

            CheckForOwnership(
                profileId,
                propertyId,
                record
            );

            SaveRecord(
                profileId,
                propertyId,
                record
            );

            message =
                "Rental started.~n~" +
                "Payment: $" +
                weeklyRent.ToString("N0") +
                " per week~n~" +
                "Progress: " +
                record.CompletedPayments +
                " / " +
                requiredPayments +
                "~n~Next payment: " +
                record.DueDate.ToString(
                    "MMM d, yyyy"
                );

            return true;
        }

        //====================================================
        // CANCEL RENTAL
        //====================================================

        public bool CancelRental(
            string profileId,
            string propertyId,
            out string message)
        {
            RentalRecord record =
                GetRecord(
                    profileId,
                    propertyId
                );

            int cancellationFee =
    GetCancellationFee(
        propertyId
    );

            if (record.PermanentlyOwned)
            {
                message =
                    "You already own this property permanently.";

                return false;
            }

            if (!record.Active)
            {
                message =
                    "You are not renting this property.";

                return false;
            }

            DateTime currentDate =
                GetCurrentGameDate();

            bool cancellingBeforeDue =
                currentDate <
                record.DueDate;

            if (cancellingBeforeDue)
            {
                if (bankAccount == null ||
                    !bankAccount.HasAccount)
                {
                    message =
                        "A bank account is required to pay the cancellation fee.";

                    return false;
                }

                if (!bankAccount.CanWithdraw(
                    cancellationFee))
                {
                    message =
                        "You need $" +
                        cancellationFee.ToString("N0") +
                        " in your bank account to cancel early.";

                    return false;
                }

                if (!bankAccount.Withdraw(
                    cancellationFee))
                {
                    message =
                        "The cancellation fee could not be paid.";

                    return false;
                }
            }

            record.Active =
                false;

            record.PermanentlyOwned =
                false;

            record.CompletedPayments =
                0;

            record.TotalPaid =
                0;

            record.DueDate =
                currentDate;

            SaveRecord(
                profileId,
                propertyId,
                record
            );

            if (cancellingBeforeDue)
            {
                message =
                    "Rental cancelled.~n~" +
                    "$" +
                    cancellationFee.ToString("N0") +
                    " cancellation fee paid.~n~" +
                    "Rent-to-own progress reset.";
            }
            else
            {
                message =
                    "Rental cancelled.~n~" +
                    "Rent-to-own progress reset.";
            }

            return true;
        }

        //====================================================
        // PROCESS WEEKLY PAYMENTS
        //====================================================

        private void ProcessDuePayments(
            string profileId,
            string propertyId,
            RentalRecord record)
        {
            if (record == null ||
                !record.Active ||
                record.PermanentlyOwned)
            {
                return;
            }

            int weeklyRent =
                GetWeeklyRent(
                    propertyId
                );

            int requiredPayments =
                GetRequiredPayments(
                    propertyId
                );

            DateTime currentDate =
                GetCurrentGameDate();

            while (record.Active &&
                   !record.PermanentlyOwned &&
                   currentDate >= record.DueDate)
            {
                bool paymentSuccessful =
                    bankAccount != null &&
                    bankAccount.HasAccount &&
                    bankAccount.CanWithdraw(
                        weeklyRent
                    ) &&
                    bankAccount.Withdraw(
                        weeklyRent
                    );

                if (!paymentSuccessful)
                {
                    record.Active =
                        false;

                    record.PermanentlyOwned =
                        false;

                    record.CompletedPayments =
                        0;

                    record.TotalPaid =
                        0;

                    record.DueDate =
                        currentDate;

                    SaveRecord(
                        profileId,
                        propertyId,
                        record
                    );

                    GTA.UI.Notification.Show(
                        "~r~Rental payment failed.~n~" +
                        "You lost access to the property.~n~" +
                        "Rent-to-own progress was reset.",
                        false
                    );

                    return;
                }

                record.CompletedPayments++;

                record.TotalPaid +=
                    weeklyRent;

                record.DueDate =
                    record.DueDate.AddDays(
                        7
                    );

                CheckForOwnership(
                    profileId,
                    propertyId,
                    record
                );

                SaveRecord(
                    profileId,
                    propertyId,
                    record
                );

                if (record.PermanentlyOwned)
                {
                    return;
                }

                GTA.UI.Notification.Show(
                    "~b~Weekly rent paid.~n~" +
                    "$" +
                    weeklyRent.ToString("N0") +
                    " withdrawn from your bank.~n~" +
                    "Progress: " +
                    record.CompletedPayments +
                    " / " +
                    requiredPayments,
                    false
                );
            }
        }

        //====================================================
        // CHECK FOR OWNERSHIP
        //====================================================

        private void CheckForOwnership(
            string profileId,
            string propertyId,
            RentalRecord record)
        {
            if (record == null ||
                record.PermanentlyOwned)
            {
                return;
            }

            int weeklyRent =
                GetWeeklyRent(
                    propertyId
                );

            int requiredPayments =
                GetRequiredPayments(
                    propertyId
                );

            if (record.CompletedPayments <
                requiredPayments)
            {
                return;
            }

            record.CompletedPayments =
                requiredPayments;

            record.TotalPaid =
                weeklyRent *
                requiredPayments;

            record.Active =
                false;

            record.PermanentlyOwned =
                true;

            SaveRecord(
                profileId,
                propertyId,
                record
            );

            GTA.UI.Notification.Show(
                "~g~Property fully paid!~n~" +
                "You now permanently own this property.~n~" +
                "Total paid: $" +
                record.TotalPaid.ToString("N0"),
                false
            );
        }

        //====================================================
        // GET COMPLETED PAYMENTS
        //====================================================

        public int GetCompletedPayments(
            string profileId,
            string propertyId)
        {
            RentalRecord record =
                GetRecord(
                    profileId,
                    propertyId
                );

            return record.CompletedPayments;
        }

        //====================================================
        // GET TOTAL PAID
        //====================================================

        public int GetTotalPaid(
            string profileId,
            string propertyId)
        {
            RentalRecord record =
                GetRecord(
                    profileId,
                    propertyId
                );

            return record.TotalPaid;
        }

        //====================================================
        // GET DUE DATE
        //====================================================

        public DateTime GetDueDate(
            string profileId,
            string propertyId)
        {
            RentalRecord record =
                GetRecord(
                    profileId,
                    propertyId
                );

            return record.DueDate;
        }

        //====================================================
        // GET RENTAL RECORD
        //====================================================

        private RentalRecord GetRecord(
            string profileId,
            string propertyId)
        {
            profileId =
                NormalizeId(
                    profileId,
                    "DEFAULT"
                );

            propertyId =
                NormalizeId(
                    propertyId,
                    "RENTAL"
                );

            string key =
                profileId +
                "::" +
                propertyId;

            RentalRecord existingRecord;

            if (rentalRecords.TryGetValue(
                key,
                out existingRecord))
            {
                return existingRecord;
            }

            RentalRecord loadedRecord =
                LoadRecord(
                    profileId,
                    propertyId
                );

            rentalRecords[key] =
                loadedRecord;

            return loadedRecord;
        }

        //====================================================
        // LOAD RENTAL RECORD
        //====================================================

        private RentalRecord LoadRecord(
            string profileId,
            string propertyId)
        {
            RentalRecord record =
                new RentalRecord
                {
                    Active = false,
                    PermanentlyOwned = false,
                    CompletedPayments = 0,
                    TotalPaid = 0,
                    DueDate = GetCurrentGameDate()
                };

            string savePath =
                GetSavePath(
                    profileId,
                    propertyId
                );

            if (!File.Exists(
                savePath))
            {
                return record;
            }

            string[] lines =
                File.ReadAllLines(
                    savePath
                );

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(
                    line))
                {
                    continue;
                }

                string[] parts =
                    line.Split(
                        new[] { '=' },
                        2
                    );

                if (parts.Length != 2)
                {
                    continue;
                }

                string key =
                    parts[0].Trim();

                string value =
                    parts[1].Trim();

                if (key.Equals(
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
                {
                    bool parsedValue;

                    if (bool.TryParse(
                        value,
                        out parsedValue))
                    {
                        record.Active =
                            parsedValue;
                    }
                }
                else if (key.Equals(
                    "PermanentlyOwned",
                    StringComparison.OrdinalIgnoreCase))
                {
                    bool parsedValue;

                    if (bool.TryParse(
                        value,
                        out parsedValue))
                    {
                        record.PermanentlyOwned =
                            parsedValue;
                    }
                }
                else if (key.Equals(
                    "CompletedPayments",
                    StringComparison.OrdinalIgnoreCase))
                {
                    int parsedValue;

                    if (int.TryParse(
                        value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out parsedValue))
                    {
                        int requiredPayments =
                            GetRequiredPayments(
                                propertyId
                            );

                        record.CompletedPayments =
                            Math.Max(
                                0,
                                Math.Min(
                                    requiredPayments,
                                    parsedValue
                                )
                            );
                    }
                }
                else if (key.Equals(
                    "TotalPaid",
                    StringComparison.OrdinalIgnoreCase))
                {
                    int parsedValue;

                    if (int.TryParse(
                        value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out parsedValue))
                    {
                        record.TotalPaid =
                            Math.Max(
                                0,
                                parsedValue
                            );
                    }
                }
                else if (key.Equals(
                    "DueDateTicks",
                    StringComparison.OrdinalIgnoreCase))
                {
                    long ticks;

                    if (long.TryParse(
                        value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out ticks))
                    {
                        try
                        {
                            record.DueDate =
                                new DateTime(
                                    ticks
                                );
                        }
                        catch
                        {
                            record.DueDate =
                                GetCurrentGameDate();
                        }
                    }
                }
            }

            if (record.PermanentlyOwned)
            {
                int weeklyRent =
                    GetWeeklyRent(
                        propertyId
                    );

                int requiredPayments =
                    GetRequiredPayments(
                        propertyId
                    );

                record.Active =
                    false;

                record.CompletedPayments =
                    requiredPayments;

                record.TotalPaid =
                    weeklyRent *
                    requiredPayments;
            }

            return record;
        }

        //====================================================
        // SAVE RENTAL RECORD
        //====================================================

        private void SaveRecord(
            string profileId,
            string propertyId,
            RentalRecord record)
        {
            string savePath =
                GetSavePath(
                    profileId,
                    propertyId
                );

            string[] lines =
            {
                "Active=" +
                record.Active,

                "PermanentlyOwned=" +
                record.PermanentlyOwned,

                "CompletedPayments=" +
                record.CompletedPayments.ToString(
                    CultureInfo.InvariantCulture
                ),

                "TotalPaid=" +
                record.TotalPaid.ToString(
                    CultureInfo.InvariantCulture
                ),

                "DueDateTicks=" +
                record.DueDate.Ticks.ToString(
                    CultureInfo.InvariantCulture
                )
            };

            File.WriteAllLines(
                savePath,
                lines
            );
        }

        //====================================================
        // SAVE ALL
        //====================================================

        public void SaveAll()
        {
            foreach (
                KeyValuePair<string, RentalRecord>
                pair in rentalRecords)
            {
                string[] keyParts =
                    pair.Key.Split(
                        new[] { "::" },
                        StringSplitOptions.None
                    );

                if (keyParts.Length != 2)
                {
                    continue;
                }

                SaveRecord(
                    keyParts[0],
                    keyParts[1],
                    pair.Value
                );
            }
        }

        //====================================================
        // CURRENT GTA DATE
        //====================================================

        private DateTime GetCurrentGameDate()
        {
            int year =
                Function.Call<int>(
                    Hash.GET_CLOCK_YEAR
                );

            int month =
                Function.Call<int>(
                    Hash.GET_CLOCK_MONTH
                ) + 1;

            int day =
                Function.Call<int>(
                    Hash.GET_CLOCK_DAY_OF_MONTH
                );

            if (year < 1)
            {
                year =
                    2013;
            }

            month =
                Math.Max(
                    1,
                    Math.Min(
                        12,
                        month
                    )
                );

            day =
                Math.Max(
                    1,
                    Math.Min(
                        DateTime.DaysInMonth(
                            year,
                            month
                        ),
                        day
                    )
                );

            return new DateTime(
                year,
                month,
                day
            );
        }

        //====================================================
        // SAVE PATH
        //====================================================

        private string GetSavePath(
            string profileId,
            string propertyId)
        {
            return Path.Combine(
                rentalSaveFolder,
                "rental_" +
                NormalizeId(
                    propertyId,
                    "RENTAL"
                ) +
                "_" +
                NormalizeId(
                    profileId,
                    "DEFAULT"
                ) +
                ".ini"
            );
        }

        //====================================================
        // SAVE FOLDER
        //====================================================

        private string GetRentalSaveFolder()
        {
            return Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "SurvivalNeeds",
                "apartments",
                "rentals"
            );
        }

        //====================================================
        // NORMALIZE ID
        //====================================================

        private string NormalizeId(
            string value,
            string fallback)
        {
            if (string.IsNullOrWhiteSpace(
                value))
            {
                value =
                    fallback;
            }

            foreach (
                char invalidCharacter
                in Path.GetInvalidFileNameChars())
            {
                value =
                    value.Replace(
                        invalidCharacter,
                        '_'
                    );
            }

            return value.Trim();
        }
    }
}