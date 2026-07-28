using GTA.UI;
using LemonUI;
using LemonUI.Menus;
using SurvivalNeeds.Systems;
using System;
using System.Collections.Generic;

namespace SurvivalNeeds.BankingSystem
{
    public class ATMMenu
    {
        private readonly ObjectPool menuPool;

        private readonly BankAccount bankAccount;
        private readonly MoneySystem moneySystem;

        private const int AccountCreationFee = 500;

        private readonly NativeMenu createAccountMenu;
        private readonly NativeMenu mainMenu;
        private readonly NativeMenu depositMenu;
        private readonly NativeMenu withdrawMenu;
        private readonly NativeMenu balanceMenu;

        private readonly NativeItem cashDisplayItem;
        private readonly NativeItem bankDisplayItem;

        private readonly NativeItem balanceAccountItem;
        private readonly NativeItem balanceCashItem;
        private readonly NativeItem balanceBankItem;

        private readonly NativeListItem<int> depositAmountItem;
        private readonly NativeListItem<int> withdrawAmountItem;

        private readonly List<int> transactionAmounts =
            new List<int>()
            {
                10,
                25,
                50,
                100,
                250,
                500,
                1000,
                2500,
                5000,
                10000
            };

        public bool Visible
        {
            get
            {
                return createAccountMenu.Visible ||
                       mainMenu.Visible ||
                       depositMenu.Visible ||
                       withdrawMenu.Visible ||
                       balanceMenu.Visible;
            }
        }

        public ATMMenu(
            BankAccount bankAccount,
            MoneySystem moneySystem)
        {
            this.bankAccount =
                bankAccount ??
                throw new ArgumentNullException(
                    nameof(bankAccount)
                );

            this.moneySystem =
                moneySystem ??
                throw new ArgumentNullException(
                    nameof(moneySystem)
                );

            menuPool =
                new ObjectPool();

            createAccountMenu =
                new NativeMenu(
                    "FLEECA ATM",
                    "BANK ACCOUNT"
                );

            mainMenu =
                new NativeMenu(
                    "FLEECA ATM",
                    "BANKING"
                );

            depositMenu =
                new NativeMenu(
                    "FLEECA ATM",
                    "DEPOSIT"
                );

            withdrawMenu =
                new NativeMenu(
                    "FLEECA ATM",
                    "WITHDRAW"
                );

            balanceMenu =
                new NativeMenu(
                    "FLEECA ATM",
                    "ACCOUNT SUMMARY"
                );

            menuPool.Add(createAccountMenu);
            menuPool.Add(mainMenu);
            menuPool.Add(depositMenu);
            menuPool.Add(withdrawMenu);
            menuPool.Add(balanceMenu);

            //================================================
            // CREATE ACCOUNT MENU
            //================================================

            NativeItem noAccountItem =
                new NativeItem(
                    "No Bank Account",
                    "You do not currently have a Fleeca bank account."
                );

            noAccountItem.Enabled =
                false;

            NativeItem createAccountItem =
            new NativeItem(
            "Create Bank Account ($500)",
            "A one-time $500 account opening fee will be charged."
            );

            NativeItem exitCreateMenuItem =
                new NativeItem(
                    "Exit",
                    "Close the ATM."
                );

            createAccountMenu.Add(
                noAccountItem
            );

            createAccountMenu.Add(
                createAccountItem
            );

            createAccountMenu.Add(
                exitCreateMenuItem
            );

            createAccountItem.Activated +=
                OnCreateAccountActivated;

            exitCreateMenuItem.Activated +=
                OnExitActivated;

            //================================================
            // MAIN ATM MENU
            //================================================

            cashDisplayItem =
                new NativeItem(
                    "Cash: $0",
                    "Cash currently carried by the player."
                );

            cashDisplayItem.Enabled =
                false;

            bankDisplayItem =
                new NativeItem(
                    "Bank: $0",
                    "Money currently stored in your bank account."
                );

            bankDisplayItem.Enabled =
                false;

            NativeItem depositItem =
                new NativeItem(
                    "Deposit",
                    "Move cash into your bank account."
                );

            NativeItem withdrawItem =
                new NativeItem(
                    "Withdraw",
                    "Take money out of your bank account."
                );

            NativeItem checkBalanceItem =
                new NativeItem(
                    "Check Balance",
                    "View your bank account information."
                );

            NativeItem exitMainMenuItem =
                new NativeItem(
                    "Exit",
                    "Close the ATM."
                );

            mainMenu.Add(
                cashDisplayItem
            );

            mainMenu.Add(
                bankDisplayItem
            );

            mainMenu.Add(
                depositItem
            );

            mainMenu.Add(
                withdrawItem
            );

            mainMenu.Add(
                checkBalanceItem
            );

            mainMenu.Add(
                exitMainMenuItem
            );

            depositItem.Activated +=
                OnDepositMenuActivated;

            withdrawItem.Activated +=
                OnWithdrawMenuActivated;

            checkBalanceItem.Activated +=
                OnBalanceMenuActivated;

            exitMainMenuItem.Activated +=
                OnExitActivated;

            //================================================
            // DEPOSIT MENU
            //================================================

            depositAmountItem =
                new NativeListItem<int>(
                    "Deposit Amount",
                    "Select how much cash to deposit.",
                    transactionAmounts.ToArray()
                );

            NativeItem confirmDepositItem =
                new NativeItem(
                    "Confirm Deposit",
                    "Deposit the selected amount."
                );

            NativeItem depositAllItem =
                new NativeItem(
                    "Deposit All Cash",
                    "Deposit all cash currently carried."
                );

            NativeItem backFromDepositItem =
                new NativeItem(
                    "Back",
                    "Return to the main ATM menu."
                );

            depositMenu.Add(
                depositAmountItem
            );

            depositMenu.Add(
                confirmDepositItem
            );

            depositMenu.Add(
                depositAllItem
            );

            depositMenu.Add(
                backFromDepositItem
            );

            confirmDepositItem.Activated +=
                OnConfirmDepositActivated;

            depositAllItem.Activated +=
                OnDepositAllActivated;

            backFromDepositItem.Activated +=
                OnBackToMainActivated;

            //================================================
            // WITHDRAW MENU
            //================================================

            withdrawAmountItem =
                new NativeListItem<int>(
                    "Withdraw Amount",
                    "Select how much money to withdraw.",
                    transactionAmounts.ToArray()
                );

            NativeItem confirmWithdrawItem =
                new NativeItem(
                    "Confirm Withdrawal",
                    "Withdraw the selected amount."
                );

            NativeItem withdrawAllItem =
                new NativeItem(
                    "Withdraw Entire Balance",
                    "Withdraw all money stored in the account."
                );

            NativeItem backFromWithdrawItem =
                new NativeItem(
                    "Back",
                    "Return to the main ATM menu."
                );

            withdrawMenu.Add(
                withdrawAmountItem
            );

            withdrawMenu.Add(
                confirmWithdrawItem
            );

            withdrawMenu.Add(
                withdrawAllItem
            );

            withdrawMenu.Add(
                backFromWithdrawItem
            );

            confirmWithdrawItem.Activated +=
                OnConfirmWithdrawActivated;

            withdrawAllItem.Activated +=
                OnWithdrawAllActivated;

            backFromWithdrawItem.Activated +=
                OnBackToMainActivated;

            //================================================
            // BALANCE MENU
            //================================================

            balanceAccountItem =
                new NativeItem(
                    "Account Number: None",
                    "Your permanent Fleeca account number."
                );

            balanceAccountItem.Enabled =
                false;

            balanceCashItem =
                new NativeItem(
                    "Cash: $0",
                    "Cash currently carried by the player."
                );

            balanceCashItem.Enabled =
                false;

            balanceBankItem =
                new NativeItem(
                    "Bank Balance: $0",
                    "Money stored in your Fleeca account."
                );

            balanceBankItem.Enabled =
                false;

            NativeItem backFromBalanceItem =
                new NativeItem(
                    "Back",
                    "Return to the main ATM menu."
                );

            balanceMenu.Add(
                balanceAccountItem
            );

            balanceMenu.Add(
                balanceCashItem
            );

            balanceMenu.Add(
                balanceBankItem
            );

            balanceMenu.Add(
                backFromBalanceItem
            );

            backFromBalanceItem.Activated +=
                OnBackToMainActivated;
        }

        //====================================================
        // OPEN ATM
        //====================================================

        public void Open()
        {
            CloseAllMenus();

            if (bankAccount.HasAccount)
            {
                RefreshMainMenu();

                mainMenu.Visible =
                    true;
            }
            else
            {
                createAccountMenu.Visible =
                    true;
            }
        }

        //====================================================
        // CLOSE ATM
        //====================================================

        public void Close()
        {
            CloseAllMenus();
        }

        //====================================================
        // DRAW / PROCESS MENUS
        //====================================================

        public void Process()
        {
            if (!Visible)
            {
                return;
            }

            menuPool.Process();
        }

        //====================================================
        // CREATE ACCOUNT
        //====================================================

        private void OnCreateAccountActivated(
            object sender,
            EventArgs e)
        {
            if (bankAccount.HasAccount)
            {
                Notification.Show(
                    "~y~You already have a bank account.",
                    false
                );

                OpenMainMenu();
                return;
            }

            if (!moneySystem.CanAfford(AccountCreationFee))
            {
                Notification.Show(
                    "~r~You need $" +
                    AccountCreationFee.ToString("N0") +
                    " to open a bank account.",
                    false
                );

                return;
            }

            moneySystem.SpendMoney(
                AccountCreationFee
            );

            bankAccount.CreateAccount();

            Notification.Show(
                "~g~Bank account created!",
                false
            );

            Notification.Show(
                "~r~-$" +
                AccountCreationFee.ToString("N0") +
                " account opening fee",
                false
            );

            Notification.Show(
                "~b~Account Number: ~w~" +
                bankAccount.AccountNumber,
                false
            );

            OpenMainMenu();
        }

        //====================================================
        // OPEN DEPOSIT MENU
        //====================================================

        private void OnDepositMenuActivated(
            object sender,
            EventArgs e)
        {
            mainMenu.Visible =
                false;

            depositMenu.Visible =
                true;
        }

        //====================================================
        // CONFIRM DEPOSIT
        //====================================================

        private void OnConfirmDepositActivated(
            object sender,
            EventArgs e)
        {
            int amount =
                depositAmountItem.SelectedItem;

            DepositMoney(
                amount
            );
        }

        private void OnDepositAllActivated(
            object sender,
            EventArgs e)
        {
            int amount =
                moneySystem.Cash;

            DepositMoney(
                amount
            );
        }

        private void DepositMoney(
            int amount)
        {
            if (amount <= 0)
            {
                Notification.Show(
                    "~r~You do not have any cash to deposit.",
                    false
                );

                return;
            }

            if (!moneySystem.CanAfford(amount))
            {
                Notification.Show(
                    "~r~Not enough cash.",
                    false
                );

                return;
            }

            bool cashRemoved =
                moneySystem.SpendMoney(
                    amount
                );

            if (!cashRemoved)
            {
                Notification.Show(
                    "~r~Deposit failed.",
                    false
                );

                return;
            }

            bool deposited =
                bankAccount.Deposit(
                    amount
                );

            if (!deposited)
            {
                moneySystem.AddMoney(
                    amount
                );

                Notification.Show(
                    "~r~Deposit failed.",
                    false
                );

                return;
            }

            Notification.Show(
                "~g~Deposited $" +
                amount.ToString("N0"),
                false
            );

            RefreshMainMenu();
        }

        //====================================================
        // OPEN WITHDRAW MENU
        //====================================================

        private void OnWithdrawMenuActivated(
            object sender,
            EventArgs e)
        {
            mainMenu.Visible =
                false;

            withdrawMenu.Visible =
                true;
        }

        //====================================================
        // CONFIRM WITHDRAWAL
        //====================================================

        private void OnConfirmWithdrawActivated(
            object sender,
            EventArgs e)
        {
            int amount =
                withdrawAmountItem.SelectedItem;

            WithdrawMoney(
                amount
            );
        }

        private void OnWithdrawAllActivated(
            object sender,
            EventArgs e)
        {
            int amount =
                bankAccount.Balance;

            WithdrawMoney(
                amount
            );
        }

        private void WithdrawMoney(
            int amount)
        {
            if (amount <= 0)
            {
                Notification.Show(
                    "~r~Your bank account is empty.",
                    false
                );

                return;
            }

            if (!bankAccount.CanWithdraw(amount))
            {
                Notification.Show(
                    "~r~Insufficient bank balance.",
                    false
                );

                return;
            }

            bool withdrawn =
                bankAccount.Withdraw(
                    amount
                );

            if (!withdrawn)
            {
                Notification.Show(
                    "~r~Withdrawal failed.",
                    false
                );

                return;
            }

            moneySystem.AddMoney(
                amount
            );

            Notification.Show(
                "~g~Withdrew $" +
                amount.ToString("N0"),
                false
            );

            RefreshMainMenu();
        }

        //====================================================
        // BALANCE MENU
        //====================================================

        private void OnBalanceMenuActivated(
            object sender,
            EventArgs e)
        {
            RefreshBalanceMenu();

            mainMenu.Visible =
                false;

            balanceMenu.Visible =
                true;
        }

        //====================================================
        // RETURN TO MAIN MENU
        //====================================================

        private void OnBackToMainActivated(
            object sender,
            EventArgs e)
        {
            OpenMainMenu();
        }

        private void OpenMainMenu()
        {
            CloseAllMenus();

            RefreshMainMenu();

            mainMenu.Visible =
                true;
        }

        //====================================================
        // EXIT
        //====================================================

        private void OnExitActivated(
            object sender,
            EventArgs e)
        {
            Close();
        }

        //====================================================
        // REFRESH DISPLAY
        //====================================================

        private void RefreshMainMenu()
        {
            cashDisplayItem.Title =
                "Cash: $" +
                moneySystem.Cash.ToString(
                    "N0"
                );

            bankDisplayItem.Title =
                "Bank: $" +
                bankAccount.Balance.ToString(
                    "N0"
                );
        }

        private void RefreshBalanceMenu()
        {
            balanceAccountItem.Title =
                "Account Number: " +
                bankAccount.AccountNumber;

            balanceCashItem.Title =
                "Cash: $" +
                moneySystem.Cash.ToString(
                    "N0"
                );

            balanceBankItem.Title =
                "Bank Balance: $" +
                bankAccount.Balance.ToString(
                    "N0"
                );
        }

        //====================================================
        // CLOSE ALL MENUS
        //====================================================

        private void CloseAllMenus()
        {
            createAccountMenu.Visible =
                false;

            mainMenu.Visible =
                false;

            depositMenu.Visible =
                false;

            withdrawMenu.Visible =
                false;

            balanceMenu.Visible =
                false;
        }
    }
}