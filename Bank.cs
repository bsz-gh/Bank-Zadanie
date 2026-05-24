using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace BankingSystem
{
    // Modele danych (muszą być publiczne dla serializacji XML)
    public class Transaction
    {
        public string Timestamp { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; }
        public string Details { get; set; }

        public override string ToString()
        {
            return $"[{Timestamp}] {Type,-20} | {Amount,10:F2} PLN | {Details}";
        }
    }

    public class Account
    {
        public string AccountNumber { get; set; }
        public string Owner { get; set; }
        public string Pin { get; set; }
        public decimal Balance { get; set; }
        public List<Transaction> History { get; set; } = new List<Transaction>();
    }

    // Logika banku
    public class Bank
    {
        private const string DataFile = "bank_data.xml";
        private List<Account> Accounts;

        public Bank()
        {
            Accounts = LoadData();
        }

        private List<Account> LoadData()
        {
            if (File.Exists(DataFile))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<Account>));
                    using (FileStream fs = new FileStream(DataFile, FileMode.Open))
                    {
                        return (List<Account>)serializer.Deserialize(fs);
                    }
                }
                catch
                {
                    return new List<Account>();
                }
            }
            return new List<Account>();
        }

        private void SaveData()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Account>));
                using (StreamWriter writer = new StreamWriter(DataFile))
                {
                    serializer.Serialize(writer, Accounts);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Błąd zapisu pliku: " + ex.Message);
            }
        }

        private Account GetAccount(string accountNumber)
        {
            return Accounts.Find(a => a.AccountNumber == accountNumber);
        }

        public (bool Success, string Message) CreateAccount(string accountNumber, string owner, string pin)
        {
            if (GetAccount(accountNumber) != null)
                return (false, "Konto o tym numerze już istnieje.");

            Accounts.Add(new Account
            {
                AccountNumber = accountNumber,
                Owner = owner,
                Pin = pin,
                Balance = 0m
            });
            
            SaveData();
            return (true, "Konto zostało pomyślnie utworzone.");
        }

        public Account Authenticate(string accountNumber, string pin)
        {
            Account account = GetAccount(accountNumber);
            if (account != null && account.Pin == pin)
                return account;
            
            return null;
        }

        public (bool Success, string Message) Deposit(string accountNumber, decimal amount)
        {
            if (amount <= 0) return (false, "Kwota musi być większa niż zero.");

            Account account = GetAccount(accountNumber);
            account.Balance += amount;
            account.History.Add(new Transaction 
            { 
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 
                Amount = amount, 
                Type = "Wpłata", 
                Details = "Wpłata własna" 
            });

            SaveData();
            return (true, "Wpłata zakończona sukcesem.");
        }

        public (bool Success, string Message) Withdraw(string accountNumber, decimal amount)
        {
            if (amount <= 0) return (false, "Kwota musi być większa niż zero.");

            Account account = GetAccount(accountNumber);
            if (account.Balance < amount) return (false, "Brak wystarczających środków.");

            account.Balance -= amount;
            account.History.Add(new Transaction 
            { 
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 
                Amount = -amount, 
                Type = "Wypłata", 
                Details = "Wypłata w gotówce" 
            });

            SaveData();
            return (true, "Wypłata zakończona sukcesem.");
        }

        public (bool Success, string Message) Transfer(string fromAccount, string toAccount, decimal amount)
        {
            if (amount <= 0) return (false, "Kwota musi być większa niż zero.");
            if (fromAccount == toAccount) return (false, "Nie można wykonać przelewu na to samo konto.");
            
            Account sender = GetAccount(fromAccount);
            Account receiver = GetAccount(toAccount);

            if (receiver == null) return (false, "Konto docelowe nie istnieje.");
            if (sender.Balance < amount) return (false, "Brak wystarczających środków na koncie.");

            // Atomowa operacja
            sender.Balance -= amount;
            receiver.Balance += amount;

            string timeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            sender.History.Add(new Transaction { Timestamp = timeNow, Amount = -amount, Type = "Przelew WYCH.", Details = $"Do: {toAccount}" });
            receiver.History.Add(new Transaction { Timestamp = timeNow, Amount = amount, Type = "Przelew PRZYCH.", Details = $"Od: {fromAccount}" });

            SaveData();
            return (true, "Przelew zrealizowany pomyślnie.");
        }
    }

    // Interfejs użytkownika
    class Program
    {
        static decimal GetDecimalInput(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine().Replace('.', ','); // Obsługa polskiego formatu liczb
                if (decimal.TryParse(input, out decimal result))
                {
                    return result;
                }
                Console.WriteLine("Błąd: Wprowadź poprawną liczbę (np. 150,50).");
            }
        }

        static void Main(string[] args)
        {
            Bank bank = new Bank();

            while (true)
            {
                Console.WriteLine("\n==============================");
                Console.WriteLine("    SYSTEM BANKOWY v1.0 (C#)");
                Console.WriteLine("==============================");
                Console.WriteLine("1. Zaloguj się na konto");
                Console.WriteLine("2. Załóż nowe konto");
                Console.WriteLine("3. Wyjście");
                Console.Write("Wybierz opcję (1-3): ");
                
                string choice = Console.ReadLine();

                if (choice == "3")
                {
                    Console.WriteLine("Zamykanie systemu. Do widzenia!");
                    break;
                }
                else if (choice == "2")
                {
                    Console.WriteLine("\n--- ZAKŁADANIE KONTA ---");
                    Console.Write("Podaj nowy numer konta (np. 1234): ");
                    string accNum = Console.ReadLine();
                    Console.Write("Podaj imię i nazwisko właściciela: ");
                    string owner = Console.ReadLine();
                    Console.Write("Ustal 4-cyfrowy PIN: ");
                    string pin = Console.ReadLine();

                    var result = bank.CreateAccount(accNum, owner, pin);
                    Console.WriteLine($"\n>>> {result.Message}");
                }
                else if (choice == "1")
                {
                    Console.WriteLine("\n--- LOGOWANIE ---");
                    Console.Write("Numer konta: ");
                    string accNum = Console.ReadLine();
                    Console.Write("PIN: ");
                    string pin = Console.ReadLine();

                    Account account = bank.Authenticate(accNum, pin);
                    if (account == null)
                    {
                        Console.WriteLine("\n>>> Błąd: Nieprawidłowy numer konta lub PIN.");
                        continue;
                    }

                    // Pętla zalogowanego użytkownika
                    while (true)
                    {
                        Console.WriteLine($"\n--- ZALOGOWANO: {account.Owner} (Konto: {accNum}) ---");
                        Console.WriteLine($"Bieżące saldo: {account.Balance:F2} PLN");
                        Console.WriteLine("1. Wpłata gotówki");
                        Console.WriteLine("2. Wypłata gotówki");
                        Console.WriteLine("3. Przelew na inne konto");
                        Console.WriteLine("4. Historia transakcji");
                        Console.WriteLine("5. Wyloguj się");
                        Console.Write("Wybierz akcję (1-5): ");

                        string subChoice = Console.ReadLine();

                        if (subChoice == "5")
                        {
                            Console.WriteLine("Wylogowano pomyślnie.");
                            break;
                        }
                        else if (subChoice == "1")
                        {
                            decimal amount = GetDecimalInput("Podaj kwotę do wpłaty: ");
                            var result = bank.Deposit(accNum, amount);
                            Console.WriteLine($"\n>>> {result.Message}");
                        }
                        else if (subChoice == "2")
                        {
                            decimal amount = GetDecimalInput("Podaj kwotę do wypłaty: ");
                            var result = bank.Withdraw(accNum, amount);
                            Console.WriteLine($"\n>>> {result.Message}");
                        }
                        else if (subChoice == "3")
                        {
                            Console.Write("Podaj numer konta docelowego: ");
                            string toAcc = Console.ReadLine();
                            decimal amount = GetDecimalInput("Podaj kwotę przelewu: ");
                            var result = bank.Transfer(accNum, toAcc, amount);
                            Console.WriteLine($"\n>>> {result.Message}");
                        }
                        else if (subChoice == "4")
                        {
                            Console.WriteLine("\n--- HISTORIA TRANSAKCJI ---");
                            if (account.History.Count == 0)
                            {
                                Console.WriteLine("Brak operacji na koncie.");
                            }
                            else
                            {
                                foreach (var t in account.History)
                                {
                                    Console.WriteLine(t);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
