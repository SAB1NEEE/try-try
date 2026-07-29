using GTA;
using GTA.UI;
using System;
using System.Globalization;
using System.IO;

namespace SurvivalNeeds.Systems
{
    public class SaveSystem
    {
        private readonly string saveFolder;
        private readonly string saveFile;
        public bool WeaponsConfiscated { get; private set; }

        public SaveSystem()
        {
            string baseFolder =
                AppDomain.CurrentDomain.BaseDirectory.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                );

            DirectoryInfo baseDirectory =
                new DirectoryInfo(baseFolder);

            string scriptsFolder =
                baseDirectory.Name.Equals(
                    "scripts",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? baseDirectory.FullName
                    : Path.Combine(
                        baseDirectory.FullName,
                        "scripts"
                    );

            saveFolder = Path.Combine(
                scriptsFolder,
                "SurvivalNeeds"
            );

            saveFile = Path.Combine(
                saveFolder,
                "save.ini"
            );

            try
            {
                Directory.CreateDirectory(saveFolder);
            }
            catch (Exception ex)
            {
                Notification.Show(
                    "~r~Save folder error:~s~ " +
                    ex.Message
                );
            }
        }

        public void Save(
        float hunger,
        float thirst,
        float stress,
        int cash,
        BankingSystem.BankAccount bankAccount)
        {
            try
            {
                Directory.CreateDirectory(saveFolder);

                string[] lines =
{
    "Hunger=" + hunger.ToString(
        CultureInfo.InvariantCulture),

    "Thirst=" + thirst.ToString(
        CultureInfo.InvariantCulture),

    "Stress=" + stress.ToString(
        CultureInfo.InvariantCulture),

    "Cash=" + cash.ToString(
        CultureInfo.InvariantCulture),

    "HasBankAccount=" +
        bankAccount.HasAccount,

    "AccountNumber=" +
        bankAccount.AccountNumber,

    "BankBalance=" +
        bankAccount.Balance.ToString(
            CultureInfo.InvariantCulture),

    "WeaponsConfiscated=" +     
        WeaponsConfiscated,

    "MoneyMode=GTA"
};

                File.WriteAllLines(
                    saveFile,
                    lines
                );
            }
            catch (Exception ex)
            {
                Notification.Show(
                    "~r~Save error:~s~ " +
                    ex.Message
                );
            }
        }

        public void MarkWeaponsConfiscated()
        {
            WeaponsConfiscated = true;
        }

        public void Load(
            HungerSystem hunger,
            ThirstSystem thirst,
            StressSystem stress,
            MoneySystem money,
            BankingSystem.BankAccount bankAccount)
        {
            try
            {
                if (!File.Exists(saveFile))
                {
                    // New save starts with $100.
                    money.Set(100);
                    return;
                }

                string[] lines =
                    File.ReadAllLines(saveFile);

                int savedCash = 0;
                bool hasSavedCash = false;
                bool usesGtaMoney = false;
                bool hasBankAccount = false;
                bool weaponsConfiscated = false;
                string accountNumber = "";
                int bankBalance = 0;

                foreach (string line in lines)
                {
                    string[] parts =
                        line.Split(new[] { '=' }, 2);

                    if (parts.Length != 2)
                        continue;

                    string key =
                        parts[0].Trim();

                    string valueText =
                        parts[1].Trim();

                    if (key.Equals(
                        "WeaponsConfiscated",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        bool.TryParse(
                            valueText,
                            out weaponsConfiscated
                        );

                        continue;
                    }

                    if (key.Equals(
                        "MoneyMode",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        usesGtaMoney =
                            valueText.Equals(
                                "GTA",
                                StringComparison.OrdinalIgnoreCase
                            );

                        continue;
                    }

                    if (key.Equals(
                        "Cash",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(
                            valueText,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out savedCash))
                        {
                            hasSavedCash = true;
                        }

                        continue;
                    }

                    if (key.Equals(
    "HasBankAccount",
    StringComparison.OrdinalIgnoreCase))
                    {
                        bool.TryParse(
                            valueText,
                            out hasBankAccount);

                        continue;
                    }

                    if (key.Equals(
                        "AccountNumber",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        accountNumber = valueText;
                        continue;
                    }

                    if (key.Equals(
                        "BankBalance",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(
                            valueText,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out bankBalance);

                        continue;
                    }

                    float value;

                    if (!float.TryParse(
                        valueText,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out value))
                    {
                        continue;
                    }

                    switch (key)
                    {
                        case "Hunger":
                            hunger.Set(value);
                            break;

                        case "Thirst":
                            thirst.Set(value);
                            break;

                        case "Stress":
                            stress.Set(value);
                            break;
                    }
                }

                WeaponsConfiscated = weaponsConfiscated;

                if (WeaponsConfiscated)
                {
                    GTA.Ped player =
                        GTA.Game.Player.Character;

                    if (player != null &&
                        player.Exists())
                    {
                        player.Weapons.RemoveAll();
                    }
                }

                // Old saves used a separate wallet.
                // Reset to $100 once when changing to GTA money.
                if (usesGtaMoney && hasSavedCash)
                {
                    money.Set(savedCash);
                }
                else
                {
                    money.Set(100);
                }

                // Always restore the bank account
                bankAccount.SetAccount(
                    hasBankAccount,
                    accountNumber,
                    bankBalance
                );
            }
            catch (Exception ex)
            {
                Notification.Show(
                    "~r~Load error:~s~ " +
                    ex.Message
                );
            }
        }
    }
}