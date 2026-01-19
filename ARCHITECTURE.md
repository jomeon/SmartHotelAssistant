# Architektura SmartHotelAssistant

## Przegląd

Ten dokument opisuje architekturę aplikacji SmartHotelAssistant, z naciskiem na rozszerzenia dotyczące dynamicznego cennika (sezonowego i opartego na obłożeniu) oraz integracji z Azure Key Vault w celu bezpiecznego zarządzania sekretami. Aplikacja składa się z backendu (Azure Functions w .NET) i frontendowego interfejsu użytkownika (React/Vite).

## Kluczowe Komponenty

### Backend (Azure Functions)

*   **Program.cs**: Główny punkt wejścia aplikacji, odpowiedzialny za konfigurację hosta, wstrzykiwanie zależności (DI) oraz integrację z Azure Key Vault i bazą danych.
*   **HotelDbContext**: Kontekst bazy danych Entity Framework Core do zarządzania danymi hotelowymi (pokoje, rezerwacje, ceny sezonowe).
*   **Models**:
    *   `Room`: Reprezentuje pokój hotelowy, zawiera cenę bazową (`BasePrice`), typ pokoju (`RoomType`) oraz kolekcję powiązanych sezonowych cen (`SeasonalPrices`).
    *   `SeasonalPrice`: Definiuje sezonowe zasady cenowe z datami początkową/końcową, mnożnikiem ceny lub stałą ceną (`PriceMultiplier`, `FixedPrice`).
    *   `Reservation`: Obecny model rezerwacji, zawierający `RoomId`, `CheckInDate`, `CheckOutDate`.
*   **Functions**:
    *   `GetPriceEstimator` (HttpTrigger): Funkcja obliczająca szacowaną cenę rezerwacji. Pobiera daty i ID pokoju, uwzględniając ceny sezonowe oraz aktualne obłożenie pokoi danego typu.
    *   Istniejące funkcje (np. `CreateReservation`, `GetMyReservations`): Obsługują inne operacje związane z rezerwacjami.
*   **Azure Key Vault**: Bezpiecznie przechowuje wrażliwe dane, takie jak connection string do bazy danych. Dostęp do niego jest konfigurowany w `Program.cs` przy użyciu `DefaultAzureCredential`.
*   **Azure SQL Database**: Przechowuje dane aplikacji (pokoje, rezerwacje, ceny sezonowe).

### Frontend (React/Vite)

*   **src/components**: Komponenty UI, takie jak `ReservationForm.tsx`, `RoomSchedule.tsx`, `MyReservations.tsx`.
*   **src/App.tsx**: Główny komponent aplikacji frontendowej.
*   **vite.config.ts**: Konfiguracja Vite.

## Przepływ Danych dla Dynamicznego Cennika

1.  **Interakcja Użytkownika**: Użytkownik przegląda dostępne pokoje i wprowadza daty pobytu oraz wykwaterowania w formularzu rezerwacji (frontend).
2.  **Żądanie Szacowania Ceny**: Frontend wysyła zapytanie HTTP (GET lub POST) do endpointu `/api/priceEstimate` funkcji Azure `GetPriceEstimator`, przekazując ID pokoju, datę zameldowania i datę wymeldowania.
3.  **Obliczenie Ceny**:
    *   Funkcja `GetPriceEstimator` otrzymuje żądanie.
    *   Pobiera szczegóły pokoju (wraz z `RoomType` i powiązanymi regułami `SeasonalPrice`) z `HotelDbContext`.
    *   Iteruje przez każdy dzień okresu rezerwacji (`currentDate`).
    *   **Dla każdej daty**:
        *   **Sezonowość**: Oblicza cenę bazując na `BasePrice` pokoju i ewentualnych regułach `SeasonalPrice` dla danej daty.
        *   **Obłożenie**:
            *   Odpytuje bazę danych, aby określić całkowitą liczbę pokoi dostępnych dla danego `RoomType`.
            *   Zlicza, ile pokoi tego typu jest aktualnie zarezerwowanych na `currentDate`.
            *   Oblicza procentowe obłożenie (`currentOccupancyPercentage`) dla tego typu pokoju.
            *   Na podstawie predefiniowanych reguł obłożenia (np. 0-50% obłożenia -> mnożnik 1.0; 50-75% -> mnożnik 1.15; >90% -> mnożnik 1.50), określa mnożnik cenowy związany z obłożeniem.
        *   **Cena Końcowa Dnia**: Mnoży cenę z uwzględnieniem sezonowości przez mnożnik obłożenia.
    *   Sumuje ceny dzienne, aby uzyskać całkowitą cenę szacunkową.
4.  **Odpowiedź**: Funkcja `GetPriceEstimator` zwraca obiekt JSON z szacowaną ceną, walutą i dodatkowymi uwagami do frontendu.
5.  **Wyświetlanie**: Frontend wyświetla szacowaną cenę użytkownikowi.
6.  **Dalsza Rezerwacja**: Jeśli użytkownik kontynuuje, dane z formularza (wraz z wyliczoną ceną) są przesyłane do funkcji `CreateReservation` do faktycznego utworzenia rezerwacji.

## Bezpieczeństwo (Azure Key Vault)

*   **Zarządzanie Sekretami**: Wrażliwe dane, takie jak connection string do bazy danych, są przechowywane w Azure Key Vault.
*   **Dostęp Programowy**: Aplikacja backendowa (Azure Functions) wykorzystuje `DefaultAzureCredential` do uwierzytelnienia w Azure Key Vault. W środowisku produkcyjnym zaleca się użycie Managed Identity.
*   **Konfiguracja**: Konfiguracja dostępu do Key Vault i nazwy sekretów odbywa się poprzez `appsettings.json` (dla lokalnego rozwoju) oraz zmienne środowiskowe w Azure. Kod w `Program.cs` ładuje te ustawienia i integruje je z systemem konfiguracji .NET.
*   **Dostęp do Connection Stringa**: `Program.cs` pobiera connection string z `IConfiguration`, która jest skonfigurowana tak, aby ładować sekrety z Key Vault. Następnie `HotelDbContext` jest inicjalizowany przy użyciu tego connection stringa.

## Operacyjność

*   **Azure Functions**: Umożliwiają skalowalne i bezserwerowe uruchamianie logiki backendowej.
*   **Entity Framework Core**: Ułatwia interakcję z bazą danych SQL.
*   **Azure SQL Database**: Zarządza danymi relacyjnymi, zapewniając trwałość i spójność danych.
*   **Dynamiczne Cenniki**: Logika cenowa w `GetPriceEstimator` uwzględnia:
    *   **Ceny sezonowe**: Oparte na `SeasonalPrice` powiązanych z pokojami.
    *   **Ceny zależne od obłożenia**: Obliczane na bieżąco na podstawie aktualnej rezerwacji pokoi danego typu. Zapytania o obłożenie mogą być kosztowne; w środowiskach produkcyjnych należy rozważyć optymalizację (np. buforowanie, agregacja danych o obłożeniu).
*   **Monitoring i Logowanie**: Wbudowane mechanizmy logowania Azure Functions w połączeniu z Application Insights (konfiguracja poza zakresem tego dokumentu) umożliwiają monitorowanie wydajności i debugowanie, w tym śledzenie procesu obliczania cen.

## Przyszłe Rozszerzenia

*   Implementacja mechanizmu zarządzania regułami cenowymi (sezonowymi i obłożenia) jako danych konfiguracyjnych w bazie danych lub w Azure Key Vault.
*   Optymalizacja zapytań o obłożenie, np. poprzez agregację danych o obłożeniu w osobnym procesie lub w widoku bazy danych.
*   Dodanie możliwości dynamicznego aktualizowania cen w odpowiedzi na zmieniające się obłożenie w czasie rzeczywistym (wymagałoby bardziej zaawansowanych mechanizmów reagowania).
