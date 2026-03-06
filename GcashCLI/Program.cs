using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;

namespace GcashCLI
{
    public enum TransactionType
    {
        Deposit,
        Withdrawal,
        Transfer
    }

    public class Transaction
    {
        public DateTime Date { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfterTransaction { get; set; }
        public string Description { get; set; }

        public Transaction(TransactionType type, decimal amount, decimal balanceAfter, string description = "")
        {
            Date = DateTime.Now;
            Type = type;
            Amount = amount;
            BalanceAfterTransaction = balanceAfter;
            Description = description;
        }

        public override string ToString()
        {
            return $"{Date:yyyy-MM-dd HH:mm:ss} | {Type,-10} | Amount: P{Amount,10} | Balance: P{BalanceAfterTransaction,10} | {Description}";
        }
    }

    public abstract class BankAccount
    {
        private decimal balance;
        private string pin;
        private List<Transaction> transactionHistory;
        private bool isLocked;
        private int failedLoginAttempts;
        private DateTime lastWithdrawalDate;
        private decimal dailyWithdrawnAmount;

        public string TelephoneNumber { get; protected set; }
        public string AccountHolderName { get; set; }
        public string AccountType { get; protected set; }
        protected abstract decimal DailyWithdrawalLimit { get; }

        public decimal Balance
        {
            get { return balance; }
            protected set { balance = value; }
        }

        public bool IsLocked
        {
            get { return isLocked; }
            private set { isLocked = value; }
        }

        public List<Transaction> TransactionHistory
        {
            get { return transactionHistory; }
        }

        protected BankAccount(string telephoneNumber, string accountHolderName, string pin, decimal initialDeposit, string accountType)
        {
            TelephoneNumber = telephoneNumber;
            AccountHolderName = accountHolderName;
            this.pin = pin;
            balance = initialDeposit;
            AccountType = accountType;
            transactionHistory = new List<Transaction>();
            isLocked = false;
            failedLoginAttempts = 0;
            lastWithdrawalDate = DateTime.MinValue;
            dailyWithdrawnAmount = 0;

            RecordTransaction(TransactionType.Deposit, initialDeposit, balance, "Initial deposit");
        }

        public bool ValidatePIN(string inputPin)
        {
            if (IsLocked)
            {
                Console.WriteLine("Account is locked due to multiple failed login attempts.");
                return false;
            }

            if (pin == inputPin)
            {
                failedLoginAttempts = 0;
                return true;
            }
            else
            {
                failedLoginAttempts++;
                Console.WriteLine($"Invalid PIN. Attempt {failedLoginAttempts} of 3.");

                if (failedLoginAttempts >= 3)
                {
                    IsLocked = true;
                    Console.WriteLine("Account has been locked after 3 failed attempts.");
                }
                return false;
            }
        }

        public virtual bool Deposit(decimal amount)
        {
            if (amount <= 0)
            {
                Console.WriteLine("Deposit amount must be positive.");
                return false;
            }

            if (amount > 50000)
            {
                Console.WriteLine("Maximum deposit per transaction is 50,000.");
                return false;
            }

            Balance += amount;
            RecordTransaction(TransactionType.Deposit, amount, Balance);
            Console.WriteLine($"Successfully deposited P{amount}. New balance: P{Balance}");
            return true;
        }

        public void ShowBalance()
        {
            Console.WriteLine($"\nTelephone: {TelephoneNumber}");
            Console.WriteLine($"Holder: {AccountHolderName}");
            Console.WriteLine($"Type: {AccountType}");
            Console.WriteLine($"Current Balance: P{Balance}");
            Console.WriteLine($"Status: {(IsLocked ? "LOCKED" : "Active")}");
        }

        public abstract bool Withdraw(decimal amount);

        protected bool ValidateWithdrawal(decimal amount)
        {
            if (amount <= 0)
            {
                Console.WriteLine("Withdrawal amount must be positive.");
                return false;
            }

            if (amount > Balance)
            {
                Console.WriteLine("Insufficient balance.");
                return false;
            }

            CheckAndResetDailyLimit();

            if (dailyWithdrawnAmount + amount > DailyWithdrawalLimit)
            {
                Console.WriteLine($"Daily withdrawal limit exceeded. Remaining: P{DailyWithdrawalLimit - dailyWithdrawnAmount}");
                return false;
            }

            return true;
        }

        protected void CompleteWithdrawal(decimal amount)
        {
            Balance -= amount;
            AddToDailyWithdrawn(amount);
            RecordTransaction(TransactionType.Withdrawal, amount, Balance);
            Console.WriteLine($"Successfully withdrawn P{amount}. New balance: P{Balance}");
        }

        protected void CheckAndResetDailyLimit()
        {
            if (lastWithdrawalDate.Date != DateTime.Now.Date)
            {
                dailyWithdrawnAmount = 0;
                lastWithdrawalDate = DateTime.Now;
            }
        }

        protected decimal GetDailyWithdrawnAmount()
        {
            CheckAndResetDailyLimit();
            return dailyWithdrawnAmount;
        }

        protected void AddToDailyWithdrawn(decimal amount)
        {
            CheckAndResetDailyLimit();
            dailyWithdrawnAmount += amount;
        }

        protected void RecordTransaction(TransactionType type, decimal amount, decimal balanceAfter, string description = "")
        {
            TransactionHistory.Add(new Transaction(type, amount, balanceAfter, description));
        }

        public void DisplayTransactionHistory()
        {
            Console.WriteLine($"\n=== Transaction History for {TelephoneNumber} ===");
            Console.WriteLine($"Holder: {AccountHolderName}");
            Console.WriteLine(new string('-', 100));

            if (TransactionHistory.Count == 0)
            {
                Console.WriteLine("No transactions found.");
            }
            else
            {
                foreach (var transaction in TransactionHistory)
                {
                    Console.WriteLine(transaction);
                }
            }

            Console.WriteLine(new string('-', 100));
            Console.WriteLine($"Current Balance: P{Balance}");
        }

        public bool Transfer(BankAccount recipient, decimal amount)
        {
            if (recipient == this)
            {
                Console.WriteLine("Cannot transfer to the same account.");
                return false;
            }

            if (recipient == null)
            {
                Console.WriteLine("Recipient account not found.");
                return false;
            }

            if (amount <= 0)
            {
                Console.WriteLine("Transfer amount must be positive.");
                return false;
            }

            if (amount > Balance)
            {
                Console.WriteLine("Insufficient balance for transfer.");
                return false;
            }

            Balance -= amount;
            recipient.Balance += amount;

            RecordTransaction(TransactionType.Transfer, amount, Balance, $"Transfer to {recipient.TelephoneNumber}");
            recipient.RecordTransaction(TransactionType.Deposit, amount, recipient.Balance, $"Transfer from {TelephoneNumber}");

            Console.WriteLine($"Successfully transferred P{amount} to {recipient.TelephoneNumber}");
            Console.WriteLine($"Your new balance: P{Balance}");
            return true;
        }

        public void UnlockAccount()
        {
            IsLocked = false;
            failedLoginAttempts = 0;
        }
    }

    public class SavingsAccount : BankAccount
    {
        protected override decimal DailyWithdrawalLimit => 10000m;

        public SavingsAccount(string telephoneNumber, string accountHolderName, string pin, decimal initialDeposit)
            : base(telephoneNumber, accountHolderName, pin, initialDeposit, "Savings Account") { }

        public override bool Withdraw(decimal amount)
        {
            if (!ValidateWithdrawal(amount))
                return false;

            CompleteWithdrawal(amount);
            return true;
        }
    }

    public class PremiumAccount : BankAccount
    {
        protected override decimal DailyWithdrawalLimit => 50000m;

        public PremiumAccount(string telephoneNumber, string accountHolderName, string pin, decimal initialDeposit)
            : base(telephoneNumber, accountHolderName, pin, initialDeposit, "Premium Account") { }

        public override bool Withdraw(decimal amount)
        {
            if (!ValidateWithdrawal(amount))
                return false;

            CompleteWithdrawal(amount);
            return true;
        }
    }

    public class BankSystem
    {
        private List<BankAccount> accounts;
        private BankAccount currentAccount;

        public BankSystem()
        {
            accounts = new List<BankAccount>();
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            phoneNumber = phoneNumber.Trim();

            string pattern = @"^09\d{9}$";
            return Regex.IsMatch(phoneNumber, pattern);
        }

        private string GetValidTelephoneNumber()
        {
            while (true)
            {
                Console.Write("Enter Telephone Number (09XXXXXXXXX): ");
                string telephoneNumber = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(telephoneNumber))
                {
                    Console.WriteLine("Telephone number cannot be empty. Please try again.");
                    continue;
                }

                if (!IsValidPhoneNumber(telephoneNumber))
                {
                    Console.WriteLine("Invalid phone number. Must be 11 digits starting with 09 (e.g., 09123456789).");
                    continue;
                }

                if (accounts.Any(a => a.TelephoneNumber == telephoneNumber))
                {
                    Console.WriteLine("Telephone number already registered. Please try again.");
                    continue;
                }

                return telephoneNumber;
            }
        }

        private string GetValidName()
        {
            while (true)
            {
                Console.Write("Enter Account Holder Name: ");
                string name = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(name))
                {
                    Console.WriteLine("Name cannot be empty. Please try again.");
                    continue;
                }

                return name;
            }
        }

        private string GetValidPin()
        {
            while (true)
            {
                Console.Write("Enter 4-digit PIN: ");
                string pin = Console.ReadLine();

                if (pin.Length != 4 || !int.TryParse(pin, out _))
                {
                    Console.WriteLine("PIN must be exactly 4 digits. Please try again.");
                    continue;
                }

                return pin;
            }
        }

        private decimal GetValidInitialDeposit()
        {
            while (true)
            {
                Console.Write("Enter Initial Deposit (minimum 500): ");
                string input = Console.ReadLine();

                if (!decimal.TryParse(input, out decimal initialDeposit))
                {
                    Console.WriteLine("Invalid amount entered. Please try again.");
                    continue;
                }

                if (initialDeposit < 500)
                {
                    Console.WriteLine("Initial deposit must be at least 500. Please try again.");
                    continue;
                }

                return initialDeposit;
            }
        }

        private string GetValidAccountType()
        {
            while (true)
            {
                Console.WriteLine("Select Account Type:");
                Console.WriteLine("1. Savings Account (Daily limit: 10,000)");
                Console.WriteLine("2. Premium Account (Daily limit: 50,000)");
                Console.Write("Choice: ");
                string typeChoice = Console.ReadLine();

                if (typeChoice == "1" || typeChoice == "2")
                {
                    return typeChoice;
                }

                Console.WriteLine("Invalid account type. Please try again.");
            }
        }

        public void CreateAccount()
        {
            try
            {
                Console.WriteLine("\n=== Create New Account ===");

                string typeChoice = GetValidAccountType();
                string telephoneNumber = GetValidTelephoneNumber();
                string name = GetValidName();
                string pin = GetValidPin();
                decimal initialDeposit = GetValidInitialDeposit();

                BankAccount newAccount;

                switch (typeChoice)
                {
                    case "1":
                        newAccount = new SavingsAccount(telephoneNumber, name, pin, initialDeposit);
                        break;
                    case "2":
                        newAccount = new PremiumAccount(telephoneNumber, name, pin, initialDeposit);
                        break;
                    default:
                        Console.WriteLine("Invalid account type.");
                        return;
                }

                accounts.Add(newAccount);

                Console.WriteLine($"\nAccount created successfully!");
                Console.WriteLine($"Telephone Number: {newAccount.TelephoneNumber}");
                Console.WriteLine($"Account Type: {newAccount.AccountType}");
                Console.WriteLine($"Initial Balance: P{newAccount.Balance}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating account: {ex.Message}");
            }
        }

        private string GetLoginTelephoneNumber()
        {
            while (true)
            {
                Console.Write("Enter Telephone Number: ");
                string telephoneNumber = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(telephoneNumber))
                {
                    Console.WriteLine("Telephone number cannot be empty. Please try again.");
                    continue;
                }

                if (!IsValidPhoneNumber(telephoneNumber))
                {
                    Console.WriteLine("Invalid phone number format. Please try again.");
                    continue;
                }

                return telephoneNumber;
            }
        }

        public bool Login()
        {
            try
            {
                Console.WriteLine("\n=== Login ===");

                string telephoneNumber = GetLoginTelephoneNumber();

                Console.Write("Enter PIN: ");
                string pin = Console.ReadLine();

                var account = accounts.FirstOrDefault(a => a.TelephoneNumber == telephoneNumber);

                if (account == null)
                {
                    Console.WriteLine("Account not found.");
                    return false;
                }

                if (account.ValidatePIN(pin))
                {
                    currentAccount = account;
                    Console.WriteLine($"\nWelcome, {currentAccount.AccountHolderName}!");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return false;
            }
        }

        public BankAccount GetCurrentAccount()
        {
            return currentAccount;
        }

        public void Logout()
        {
            if (currentAccount != null)
            {
                Console.WriteLine($"Goodbye, {currentAccount.AccountHolderName}!");
                currentAccount = null;
            }
        }

        public BankAccount FindAccount(string telephoneNumber)
        {
            return accounts.FirstOrDefault(a => a.TelephoneNumber == telephoneNumber);
        }

        public void DisplayAllAccounts()
        {
            Console.WriteLine("\n=== All Accounts ===");
            foreach (var acc in accounts)
            {
                Console.WriteLine($"{acc.TelephoneNumber} - {acc.AccountHolderName} ({acc.AccountType})");
            }
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {

            BankSystem bankSystem = new BankSystem();
            bool running = true;

            Console.WriteLine("=========================================");
            Console.WriteLine("    CLI MOBILE BANKING APPLICATION");
            Console.WriteLine("=========================================");

            while (running)
            {
                Console.WriteLine("\n=== MAIN MENU ===");
                Console.WriteLine("1. Create Account");
                Console.WriteLine("2. Login");
                Console.WriteLine("3. Exit");
                Console.Write("Select option: ");

                try
                {
                    string choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            bankSystem.CreateAccount();
                            break;

                        case "2":
                            if (bankSystem.Login())
                            {
                                ShowAccountMenu(bankSystem);
                            }
                            break;

                        case "3":
                            running = false;
                            Console.WriteLine("Thank you for using our banking system!");
                            break;

                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private static decimal GetValidAmount(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine();

                if (!decimal.TryParse(input, out decimal amount))
                {
                    Console.WriteLine("Invalid amount entered. Please try again.");
                    continue;
                }

                if (amount <= 0)
                {
                    Console.WriteLine("Amount must be positive. Please try again.");
                    continue;
                }

                return amount;
            }
        }

        private static string GetValidRecipientNumber(BankSystem bankSystem, string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string telephoneNumber = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(telephoneNumber))
                {
                    Console.WriteLine("Telephone number cannot be empty. Please try again.");
                    continue;
                }

                var account = bankSystem.FindAccount(telephoneNumber);
                if (account == null)
                {
                    Console.WriteLine("Recipient account not found. Please try again.");
                    continue;
                }

                return telephoneNumber;
            }
        }

        static void ShowAccountMenu(BankSystem bankSystem)
        {
            bool loggedIn = true;
            var account = bankSystem.GetCurrentAccount();

            while (loggedIn)
            {
                Console.WriteLine("\n=== ACCOUNT MENU ===");
                Console.WriteLine("1. Check Balance");
                Console.WriteLine("2. Deposit");
                Console.WriteLine("3. Withdraw");
                Console.WriteLine("4. Transfer");
                Console.WriteLine("5. Transaction History");
                Console.WriteLine("6. Logout");
                Console.Write("Select option: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        account.ShowBalance();
                        break;
                    case "2":
                        decimal depAmount = GetValidAmount("Enter amount to deposit: ");
                        account.Deposit(depAmount);
                        break;
                    case "3":
                        decimal witAmount = GetValidAmount("Enter amount to withdraw: ");
                        account.Withdraw(witAmount);
                        break;
                    case "4":
                        string recipientTel = GetValidRecipientNumber(bankSystem, "Enter recipient telephone number: ");
                        var recipient = bankSystem.FindAccount(recipientTel);
                        decimal transAmount = GetValidAmount("Enter amount to transfer: ");
                        account.Transfer(recipient, transAmount);
                        break;
                    case "5":
                        account.DisplayTransactionHistory();
                        break;
                    case "6":
                        bankSystem.Logout();
                        loggedIn = false;
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }
    }
}