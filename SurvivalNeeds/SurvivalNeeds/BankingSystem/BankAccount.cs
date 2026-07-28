using System;

namespace SurvivalNeeds.BankingSystem
{
    public class BankAccount
    {
        private readonly Random random =
            new Random();

        public bool HasAccount
        {
            get;
            private set;
        }

        public string AccountNumber
        {
            get;
            private set;
        }

        public int Balance
        {
            get;
            private set;
        }

        public BankAccount()
        {
            HasAccount = false;
            Balance = 0;
            AccountNumber = string.Empty;
        }

        public bool CreateAccount()
        {
            if (HasAccount)
            {
                return false;
            }

            HasAccount = true;

            Balance = 0;

            AccountNumber =
                GenerateAccountNumber();

            return true;
        }

        public bool Deposit(int amount)
        {
            if (!HasAccount)
            {
                return false;
            }

            if (amount <= 0)
            {
                return false;
            }

            Balance += amount;

            return true;
        }

        public bool Withdraw(int amount)
        {
            if (!HasAccount)
            {
                return false;
            }

            if (amount <= 0)
            {
                return false;
            }

            if (Balance < amount)
            {
                return false;
            }

            Balance -= amount;

            return true;
        }

        public bool CanWithdraw(int amount)
        {
            if (!HasAccount)
            {
                return false;
            }

            return Balance >= amount;
        }

        public void SetBalance(int amount)
        {
            Balance =
                Math.Max(0, amount);
        }

        public void SetAccount(
            bool hasAccount,
            string accountNumber,
            int balance)
        {
            HasAccount =
                hasAccount;

            AccountNumber =
                accountNumber ?? "";

            Balance =
                Math.Max(0, balance);
        }

        private string GenerateAccountNumber()
        {
            return string.Format(
                "{0:D4}-{1:D4}-{2:D2}",
                random.Next(0, 10000),
                random.Next(0, 10000),
                random.Next(0, 100)
            );
        }
    }
}