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

        public const int PaymentsRequiredForOwnership = 18;

        private readonly BankAccount
            bankAccount;

        private readonly string
            rentalSaveFolder;

        private readonly Dictionary<string, RentalRecord>
            rentalRecords =
                new Dictionary<string, RentalRecord>(
                    StringComparer.OrdinalIgnoreCase
                );


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
        // IS RENTED
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

            ProcessDuePayment(
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
            if (record.PermanentlyOwned)
            {
                message =
                    "You already own this house permanently.";

                return false;
            }

            if (record.Active)
            {
                message =
                    "You already rent this property.";

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
                WeeklyRent))
            {
                message =
                    "You need $" +
                    WeeklyRent.ToString("N0") +
                    " in your bank account.";

                return false;
            }

            if (!bankAccount.Withdraw(
                WeeklyRent))
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
                WeeklyRent;

            record.DueDate =
                GetCurrentGameDate()
                    .AddDays(7);

            SaveRecord(
                profileId,
                propertyId,
                record
            );

            message =
                "Rental started. Next payment: " +
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

            if (record.PermanentlyOwned)
            {
                message =
                    "You already own this house permanently.";

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
                    EarlyCancellationFee))
                {
                    message =
                        "You need $" +
                        EarlyCancellationFee.ToString("N0") +
                        " in your bank account to cancel early.";

                    return false;
                }

                if (!bankAccount.Withdraw(
                    EarlyCancellationFee))
                {
                    message =
                        "The cancellation payment failed.";

                    return false;
                }
            }

            record.Active =
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

            message =
                cancellingBeforeDue
                    ? "Rental cancelled. $" +
                      EarlyCancellationFee.ToString("N0") +
                      " cancellation fee paid. Progress reset."
                    : "Rental cancelled. Progress reset.";

            return true;
        }

        //====================================================
        // PROCESS WEEKLY PAYMENT
        //====================================================

        private void ProcessDuePayment(
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
                        WeeklyRent
                    ) &&
                    bankAccount.Withdraw(
                        WeeklyRent
                    );

                if (!paymentSuccessful)
                {
                    record.Active =
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
                        "You lost access to the house.~n~" +
                        "Rent-to-own progress was reset.",
                        false
                    );

                    return;
                }

                record.CompletedPayments++;

                record.TotalPaid +=
                    WeeklyRent;

                record.DueDate =
                    record.DueDate.AddDays(
                        7
                    );

                if (record.CompletedPayments >=
                    PaymentsRequiredForOwnership)
                {
                    record.CompletedPayments =
                        PaymentsRequiredForOwnership;

                    record.TotalPaid =
                        WeeklyRent *
                        PaymentsRequiredForOwnership;

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
                        "~g~House fully paid!~n~" +
                        "You now permanently own this property.~n~" +
                        "Total paid: $" +
                        record.TotalPaid.ToString("N0"),
                        false
                    );

                    return;
                }

                SaveRecord(
                    profileId,
                    propertyId,
                    record
                );

                GTA.UI.Notification.Show(
                    "~b~Weekly rent paid.~n~" +
                    "$" +
                    WeeklyRent.ToString("N0") +
                    " withdrawn from your bank.~n~" +
                    "Progress: " +
                    record.CompletedPayments +
                    " / " +
                    PaymentsRequiredForOwnership,
                    false
                );
            }
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
        // GET RECORD
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
        // LOAD RECORD
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
                    bool active;

                    if (bool.TryParse(
                        value,
                        out active))
                    {
                        record.Active =
                            active;
                    }
                }
                else if (key.Equals(
                    "PermanentlyOwned",
                    StringComparison.OrdinalIgnoreCase))
                {
                    bool permanentlyOwned;

                    if (bool.TryParse(
                        value,
                        out permanentlyOwned))
                    {
                        record.PermanentlyOwned =
                            permanentlyOwned;
                    }
                }
                else if (key.Equals(
                    "CompletedPayments",
                    StringComparison.OrdinalIgnoreCase))
                {
                    int completedPayments;

                    if (int.TryParse(
                        value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out completedPayments))
                    {
                        record.CompletedPayments =
                            Math.Max(
                                0,
                                Math.Min(
                                    PaymentsRequiredForOwnership,
                                    completedPayments
                                )
                            );
                    }
                }
                else if (key.Equals(
                    "TotalPaid",
                    StringComparison.OrdinalIgnoreCase))
                {
                    int totalPaid;

                    if (int.TryParse(
                        value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out totalPaid))
                    {
                        record.TotalPaid =
                            Math.Max(
                                0,
                                totalPaid
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
                record.Active =
                    false;

                record.CompletedPayments =
                    PaymentsRequiredForOwnership;

                record.TotalPaid =
                    WeeklyRent *
                    PaymentsRequiredForOwnership;
            }

            return record;
        }

        //====================================================
        // SAVE RECORD
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
                year = 2013;
            }

            month =
                Math.Max(
                    1,
                    Math.Min(12, month)
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
        // PATHS
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

        private string GetRentalSaveFolder()
        {
            return Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "scripts",
                "SurvivalNeeds",
                "apartments",
                "rentals"
            );
        }

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