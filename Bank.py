import json
import os
from datetime import datetime

DATA_FILE = "bank_data.json"

class Transaction:
    @staticmethod
    def create(amount: float, t_type: str, details: str = "") -> dict:
        return {
            "timestamp": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
            "amount": amount,
            "type": t_type,
            "details": details
        }

    @staticmethod
    def format(t: dict) -> str:
        return f"[{t['timestamp']}] {t['type'].ljust(20)} | {t['amount']:>10.2f} PLN | {t['details']}"


class Bank:
    def __init__(self):
        self.accounts = self._load_data()

    def _load_data(self) -> dict:
        if os.path.exists(DATA_FILE):
            try:
                with open(DATA_FILE, 'r', encoding='utf-8') as f:
                    return json.load(f)
            except Exception:
                return {}
        return {}

    def _save_data(self):
        with open(DATA_FILE, 'w', encoding='utf-8') as f:
            json.dump(self.accounts, f, indent=4, ensure_ascii=False)

    def create_account(self, acc_number: str, owner: str, pin: str):
        if acc_number in self.accounts:
            return False, "Konto o tym numerze już istnieje."
        
        self.accounts[acc_number] = {
            "owner": owner,
            "pin": pin,
            "balance": 0.0,
            "history": []
        }
        self._save_data()
        return True, "Konto zostało pomyślnie utworzone."

    def authenticate(self, acc_number: str, pin: str):
        account = self.accounts.get(acc_number)
        if account and account["pin"] == pin:
            return account
        return None

    def deposit(self, acc_number: str, amount: float):
        if amount <= 0:
            return False, "Kwota musi być większa niż zero."
        
        self.accounts[acc_number]["balance"] += amount
        self.accounts[acc_number]["history"].append(
            Transaction.create(amount, "Wpłata", "Wpłata własna")
        )
        self._save_data()
        return True, "Wpłata zakończona sukcesem."

    def withdraw(self, acc_number: str, amount: float):
        if amount <= 0:
            return False, "Kwota musi być większa niż zero."
        
        if self.accounts[acc_number]["balance"] < amount:
            return False, "Brak wystarczających środków."

        self.accounts[acc_number]["balance"] -= amount
        self.accounts[acc_number]["history"].append(
            Transaction.create(-amount, "Wypłata", "Wypłata w gotówce")
        )
        self._save_data()
        return True, "Wypłata zakończona sukcesem."

    def transfer(self, from_acc: str, to_acc: str, amount: float):
        if amount <= 0:
            return False, "Kwota przelewu musi być większa niż zero."
        if from_acc == to_acc:
            return False, "Nie można wykonać przelewu na to samo konto."
        if to_acc not in self.accounts:
            return False, "Konto docelowe nie istnieje."
        if self.accounts[from_acc]["balance"] < amount:
            return False, "Brak wystarczających środków na koncie."

        # Atomowa operacja w słowniku
        self.accounts[from_acc]["balance"] -= amount
        self.accounts[to_acc]["balance"] += amount

        self.accounts[from_acc]["history"].append(
            Transaction.create(-amount, "Przelew WYCHODZĄCY", f"Do: {to_acc}")
        )
        self.accounts[to_acc]["history"].append(
            Transaction.create(amount, "Przelew PRZYCHODZĄCY", f"Od: {from_acc}")
        )
        self._save_data()
        return True, "Przelew zrealizowany pomyślnie."


# === INTERFEJS UŻYTKOWNIKA ===
def get_float_input(prompt: str) -> float:
    while True:
        try:
            return float(input(prompt).replace(',', '.'))
        except ValueError:
            print("Błąd: Wprowadź poprawną liczbę (np. 150.50).")

def main():
    bank = Bank()

    while True:
        print("\n" + "="*30)
        print("    SYSTEM BANKOWY v1.0")
        print("="*30)
        print("1. Zaloguj się na konto")
        print("2. Załóż nowe konto")
        print("3. Wyjście")
        
        choice = input("Wybierz opcję (1-3): ")

        if choice == '3':
            print("Zamykanie systemu. Do widzenia!")
            break

        elif choice == '2':
            print("\n--- ZAKŁADANIE KONTA ---")
            acc_num = input("Podaj nowy numer konta (np. 1234): ")
            owner = input("Podaj imię i nazwisko właściciela: ")
            pin = input("Ustal 4-cyfrowy PIN: ")
            
            success, msg = bank.create_account(acc_num, owner, pin)
            print(f">>> {msg}")

        elif choice == '1':
            print("\n--- LOGOWANIE ---")
            acc_num = input("Numer konta: ")
            pin = input("PIN: ")
            
            account = bank.authenticate(acc_num, pin)
            if not account:
                print(">>> Błąd: Nieprawidłowy numer konta lub PIN.")
                continue

            # Pętla zalogowanego użytkownika
            while True:
                print(f"\n--- ZALOGOWANO: {account['owner']} (Konto: {acc_num}) ---")
                print(f"Bieżące saldo: {account['balance']:.2f} PLN")
                print("1. Wpłata gotówki")
                print("2. Wypłata gotówki")
                print("3. Przelew na inne konto")
                print("4. Historia transakcji")
                print("5. Wyloguj się")

                sub_choice = input("Wybierz akcję (1-5): ")

                if sub_choice == '5':
                    print("Wylogowano pomyślnie.")
                    break
                
                elif sub_choice == '1':
                    amount = get_float_input("Podaj kwotę do wpłaty: ")
                    success, msg = bank.deposit(acc_num, amount)
                    print(f">>> {msg}")

                elif sub_choice == '2':
                    amount = get_float_input("Podaj kwotę do wypłaty: ")
                    success, msg = bank.withdraw(acc_num, amount)
                    print(f">>> {msg}")

                elif sub_choice == '3':
                    to_acc = input("Podaj numer konta docelowego: ")
                    amount = get_float_input("Podaj kwotę przelewu: ")
                    success, msg = bank.transfer(acc_num, to_acc, amount)
                    print(f">>> {msg}")

                elif sub_choice == '4':
                    print("\n--- HISTORIA TRANSAKCJI ---")
                    if not account["history"]:
                        print("Brak operacji na koncie.")
                    else:
                        for t in account["history"]:
                            print(Transaction.format(t))

if __name__ == "__main__":
    main()
