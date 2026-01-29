# Smart Hotel Assistant

# 🏨 Smart Hotel Assistant

> **Cloud-native system rezerwacji hotelowej oparty o architekturę Serverless w chmurze Microsoft Azure.**

![Build Status](https://img.shields.io/github/actions/workflow/status/TWOJ_NICK/SmartHotelAssistant/deploy.yml?label=Build%20%26%20Deploy)
![Azure Functions](https://img.shields.io/badge/Azure-Functions-0062AD?logo=azurefunctions&logoColor=white)
![React](https://img.shields.io/badge/Frontend-React-61DAFB?logo=react&logoColor=black)
![Terraform](https://img.shields.io/badge/Infra-Terraform-7B42BC?logo=terraform&logoColor=white)

## 📋 Opis Projektu
Smart Hotel Assistant to kompleksowy system umożliwiający przeglądanie oferty hotelowej, dokonywanie rezerwacji w czasie rzeczywistym oraz zarządzanie nimi. Projekt demonstruje wykorzystanie nowoczesnych wzorców chmurowych, takich jak **Serverless**, **Event-Driven Architecture** oraz **Infrastructure as Code**.

### ✅ Zrealizowane Funkcjonalności
* **Rezerwacja Pokoi:** Wybór pokoju z bazy, walidacja terminów (blokada nakładających się dat), obliczanie kosztu pobytu.
* **Sprawdzanie Dostępności:** Mechanizm zapobiegający "overbookingowi" (Double Booking Prevention).
* **Panel Klienta:** Możliwość sprawdzenia swoich rezerwacji po adresie e-mail.
* **Powiadomienia (Symulacja):** Asynchroniczna wysyłka potwierdzeń rezerwacji (kolejki).
* **Automatyczne Przypomnienia:** Codzienny CRON job (Timer Trigger) wysyłający przypomnienia o nadchodzącym pobycie.

---

## 🏗️ Architektura i Technologie

System został zbudowany w oparciu o usługi **Azure PaaS / Serverless**, co minimalizuje koszty utrzymania (model *Pay-as-you-go*).

| Warstwa | Technologia | Opis |
| :--- | :--- | :--- |
| **Frontend** | React + TypeScript + Vite | Responsywna aplikacja SPA, hostowana jako *Static Website* na Azure Storage. |
| **Backend** | Azure Functions (.NET 8) | Model *Isolated Worker*. Logika biznesowa, API REST. |
| **Baza Danych** | Azure SQL Database | Przechowywanie relacyjne (Rezerwacje, Pokoje). Użycie Entity Framework Core. |
| **Integracja** | Azure Storage Queues | Komunikacja asynchroniczna (Backend -> Worker wysyłający maile). |
| **Infrastruktura** | Terraform | Pełna definicja środowiska w kodzie (IaC). |
| **CI/CD** | GitHub Actions | Automatyczny build, test i deployment przy każdym pushu. |
| **Monitoring** | Application Insights | Logowanie błędów, mapowanie zależności i alerty. |

### Diagram Przepływu Danych
1.  **Klient** wysyła żądanie HTTP z Frontendu.
2.  **Azure Function (HTTP Trigger)** waliduje dostępność pokoju w **SQL Database**.
3.  Po poprawnym zapisie, ID rezerwacji trafia na **Storage Queue**.
4.  **Azure Function (Queue Trigger)** odbiera wiadomość i procesuje wysyłkę e-maila.
5.  **Timer Trigger** uruchamia się raz dziennie, skanując bazę w poszukiwaniu rezerwacji na "jutro".

---

## 🚀 Jak uruchomić projekt lokalnie?

### Wymagania
* Node.js & npm
* .NET 8 SDK
* Azure Functions Core Tools
* SQL Server (lokalny lub w chmurze)

### 1. Konfiguracja Backendu
1.  Przejdź do folderu: `cd src/backend`
2.  Utwórz plik `local.settings.json` (jest ignorowany przez gita):
    ```json
    {
      "IsEncrypted": false,
      "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "SqlConnectionString": "Server=...;Database=...;User Id=...;Password=...;"
      },
      "Host": { "CORS": "*" }
    }
    ```
3.  Uruchom emulator Azurite (dla kolejek) w VS Code (`F1 -> Azurite: Start`).
4.  Uruchom backend:
    ```bash
    dotnet build
    func start
    ```

### 2. Konfiguracja Frontendu
1.  Przejdź do folderu: `cd frontend`
2.  Zainstaluj zależności: `npm install`
3.  Uruchom aplikację:
    ```bash
    npm run dev
    ```
4.  Aplikacja dostępna pod adresem: `http://localhost:5173`

---

## ☁️ Wdrożenie (CI/CD)

Projekt posiada skonfigurowany pipeline w **GitHub Actions** (`.github/workflows/deploy.yml`).
Proces wdrażania odbywa się automatycznie po wykonaniu `git push` na gałąź `main`:

1.  **Build:** Kompilacja kodu .NET oraz budowanie paczki React (Vite).
2.  **Infrastructure Check:** Weryfikacja zasobów Azure.
3.  **Deploy:**
    * Backend trafia do Azure Function App.
    * Frontend trafia do kontenera `$web` na Azure Storage Account.
4.  **Config:** Automatyczna konfiguracja CORS dla nowego adresu.

---

## 🔌 API Endpoints

| Metoda | Endpoint | Opis |
| :--- | :--- | :--- |
| `GET` | `/api/rooms` | Zwraca listę dostępnych pokoi i ich ceny. |
| `GET` | `/api/my-reservations/{email}` | Zwraca historię rezerwacji dla danego klienta. |
| `POST` | `/api/reservation` | Tworzy nową rezerwację (z walidacją dat). |
| `TIMER`| `DailyReminder` | (Internal) Uruchamia się codziennie o 8:00 rano. |

---

## 🧹 Sprzątanie zasobów (Cleanup)

Aby uniknąć kosztów po zakończeniu testów, całą infrastrukturę można usunąć jedną komendą dzięki Terraform:

```bash
terraform destroy -auto-approve
