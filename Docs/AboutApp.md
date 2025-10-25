Nazwa aplikacji

**AI Context Packer**

- **Dlaczego:** Jest bardzo opisowa. Od razu mówi, że "pakuje" ("Packer") "kontekst" ("Context") i jest przeznaczona dla "AI".

### Dokumentacja Projektu: "Code Context Copier"

Poniżej znajduje się kompletny, szczegółowy dokument (prompt) definiujący aplikację, gotowy do przekazania zespołowi deweloperskiemu (lub agentowi AI).

*(Zgodnie z Twoją prośbą, ta dokumentacja jest po polsku, ale cały kod i UI wynikowy mają być po angielsku).*

---

### **Projekt: Code Context Copier**

### **1. Streszczenie (Executive Summary)**

**Cel:** Stworzenie desktopowej aplikacji narzędziowej (.NET WPF) dla programistów, która usprawnia proces kopiowania kontekstu projektu (wielu plików kodowych) do modeli AI (jak Gemini czy ChatGPT).

**Problem:** Ręczne otwieranie, zaznaczanie i kopiowanie dziesiątek plików jest czasochłonne i podatne na błędy.

**Rozwiązanie:** Aplikacja, która wizualizuje strukturę projektu, pozwala na inteligentne filtrowanie i zaznaczanie plików, a następnie dzieli je na "części" (party) gotowe do wklejenia, respektując limity znaków i nie dzieląc pojedynczych plików.

**Platformy docelowe:** Windows 11.
**Język UI i Kodu:** Angielski.

### **2. Architektura i Wymagania Techniczne**

- **Framework:** .NET WPF.
- **Wzorzec:** MVVM (Model-View-ViewModel).
- **Zasady:** SOLID, DRY, KISS. Kod musi być czysty, z komentarzami tylko tam, gdzie jest to absolutnie konieczne (złożone algorytmy).
- **DI:** Pełne wykorzystanie wstrzykiwania zależności dla serwisów.
- **Serwisy:** Należy zaimplementować co najmniej:
    - `IFileSystemService` (logika operacji na plikach)
    - `ISettingsService` (trwałe przechowywanie ustawień i stanu aplikacji)
    - `INotificationService` (globalne powiadomienia)
    - `IClipboardService` (interfejs dla schowka)

### **3. Główny Układ Aplikacji (Main Layout)**

Interfejs użytkownika będzie składał się z następujących sekcji:

1. **Pasek Menu Systemowego:** Zawiera menu "File" -> "Open Project..." i "Recent Projects".
2. **Górny Pasek Narzędzi (Toolbar):**
    - Przycisk "Select Project Folder" (alternatywa dla Drag&Drop).
    - Pole numeryczne (Input) "Max Chars Limit" (domyślnie np. 10 000).
    - Lista rozwijana (Dropdown) "Select Global Prompt" (z opcjami "None", "Edit...", "Create New...").
3. **Panel Główny (Dwie kolumny):**
    - **Kolumna Lewa (Selection Panel):**
        - Lista "Pinned Files" (Priorytetowe).
        - Przyciski "Select All" / "Deselect All".
        - Widok drzewa (TreeView) projektu.
    - **Kolumna Prawa (Output Panel):**
        - Przycisk "Generate" (Zatwierdź).
        - Przycisk "Copy Structure".
        - Dynamicznie generowana lista przycisków "Part 1...X".
4. **Przycisk Ustawień (Settings ⚙️):** W rogu okna, otwiera osobne okno/panel modalny.

### **4. Specyfikacja Funkcjonalna (Panel po Panelu)**

### **4.1. Ładowanie Projektu i Filtrowanie**

1. **Sposoby ładowania:**
    - Przycisk "Select Project Folder".
    - Przeciągnij-i-upuść folderu na okno aplikacji.
    - Menu "File" -> "Recent Projects".
2. **Filtrowanie (Proces 3-etapowy):**
    - **Etap 1: Whitelist (Dozwolone Rozszerzenia):** Aplikacja musi mieć w Ustawieniach listę dozwolonych rozszerzeń (np. `.cs`, `.html`, `.css`, `.js`, `.ts`, `.json`, `.md`, `.java`, `.py`). Pliki niepasujące do tej listy są **całkowicie ukryte** w drzewie wyboru (Q6.1).
    - **Etap 2: Filtry Ignorowania (Blacklist):** W Ustawieniach użytkownik zarządza plikami `.gitignore` (np. `dotnet.gitignore`, `angular.gitignore`, `python.gitignore`). Te pliki muszą być puste przy pierwszej instalacji. Na głównym ekranie (obok drzewa) znajduje się **lista checkboxów** pozwalająca włączyć wiele filtrów naraz (np. ".net" i "angular") (Q2).
    - **Etap 3: Lokalny `.gitignore`:** Po załadowaniu folderu, aplikacja sprawdza, czy istnieje w nim plik `.gitignore`. Jeśli tak, obok listy filtrów (Etap 2) pojawia się checkbox **"Use detected .gitignore"**, domyślnie zaznaczony (Q2).
3. **Logika Filtrowania:** Plik jest widoczny w drzewie, jeśli: (A) jest na Whitelist ORAZ (B) nie jest ignorowany przez aktywne filtry z Etapu 2 ORAZ (C) nie jest ignorowany przez `.gitignore` z Etapu 3 (jeśli ten jest aktywny).

### **4.2. Lewa Kolumna (Selection Panel)**

1. **Pinned Files (Priorytetowe):**
    - Nad głównym drzewem znajduje się lista "Pinned Files" (Q1).
    - Każdy plik w głównym drzewie ma obok siebie ikonę **pinezki 📌** (Q1).
    - Kliknięcie pinezki usuwa plik z drzewa i przenosi go do listy "Pinned Files". Kliknięcie pinezki na liście "Pinned Files" przenosi go z powrotem do drzewa.
2. **Główne Drzewo (TreeView):**
    - Wyświetla przefiltrowaną strukturę folderów i plików.
    - Każdy element (plik i folder) ma **checkbox**.
    - Zaznaczenie folderu automatycznie zaznacza wszystkie jego dzieci (które przeszły filtr) (Q6).
    - Przyciski **"Select All" / "Deselect All"** zaznaczają/odznaczają wszystko w głównym drzewie. **Nie mają one wpływu** na pliki z listy "Pinned Files" (Q14).

### **4.3. Prawa Kolumna (Output Panel)**

1. **Przycisk "Generate":**
    - Jest domyślnie nieaktywny.
    - Staje się aktywny, gdy użytkownik dokona zmiany w zaznaczeniu, filtrach, limicie znaków lub "Pinned Files" (Q12).
    - Kliknięcie uruchamia logikę generowania "Partów".
    - Po pomyślnym wygenerowaniu, przycisk znów staje się nieaktywny, dopóki nie nastąpi kolejna zmiana.
2. **Przycisk "Copy Structure":**
    - Kopiuje do schowka strukturę drzewa opartą na **aktualnym zaznaczeniu** (w tym "Pinned Files") i **filtrach ignorowania**.
    - **Ważne:** Ta funkcja **MUSI** pokazywać pliki, które są ukryte przez Whitelist (np. `.png`, `.dll`), aby AI wiedziało o ich istnieniu (Q6.2, Q18).
3. **Przyciski "Part 1...X":**
    - Generowane dynamicznie po kliknięciu "Generate".
    - Format tekstu: "Part X (Znaki: 9850 / 10000)" (Q14).
    - **Akcja (Klik):** Kopiuje zawartość danego "Partu" do schowka. Wyświetla powiadomienie "Part X copied to clipboard!" (Q13). Przycisk zmienia kolor (np. na zielony), aby zasygnalizować, że został użyty (Q13).
    - **Podgląd:** Obok każdego przycisku "Part" znajduje się ikona **lupy 🔎**. Kliknięcie jej otwiera nowe okno modalne z **nieedytowalnym** polem tekstowym, pokazującym DOKŁADNĄ zawartość, która zostanie skopiowana (Q14, Q5).

### **4.4. Logika Biznesowa: Generowanie "Partów"**

1. **Walidacja:** Po kliknięciu "Generate", system najpierw sprawdza, czy jakikolwiek *pojedynczy* plik (zaznaczony lub przypięty) nie przekracza "Max Chars Limit".
    - **Błąd:** Jeśli tak (`PlikA.cs` ma 12k, limit 10k), proces jest przerywany, a użytkownikowi wyświetlany jest **globalny popup błędu** (Q11).
2. **Kolejność łączenia:**
    1. Globalny Prompt (jeśli wybrany) (Q16).
    2. Wszystkie "Pinned Files" (w kolejności z listy) (Q14).
    3. Wszystkie zaznaczone pliki (w kolejności z drzewa) (Q9).
3. **Formatowanie Nagłówka:** Jeśli w Ustawieniach zaznaczono "Include file headers", przed treścią *każdego* pliku dodawany jest nagłówek w formacie: `// Plik: [relatywna ścieżka do pliku]` (Q7).
4. **Logika Pakowania (Kluczowe!):**
    - System iteruje po plikach w ustalonej kolejności.
    - **Zasada 1 (Brak podziału):** Plik *nigdy* nie jest dzielony między "Party" (Q10).
    - **Zasada 2 (Priorytet "Pinned"):** "Pinned Files" są pakowane jako pierwsze. Jeśli "Pinned File A" (100 znaków) zostanie dodany do "Part 1" (limit 10k), **żaden** normalny (nie-przypięty) plik nie może być do niego dodany, nawet jeśli jest miejsce. Następny "Pinned File B" (5k) jest dodawany do "Part 1" (razem 5100). Dopiero gdy "Part" jest pełny (w ramach limitu) lub skończą się "Pinned Files", system przechodzi do "Part 2" (Q14).
    - **Zasada 3 (Normalne Pliki):** Normalne pliki są pakowane do kolejnych "Partów" (tych po "Pinned Files"), aż do zapełnienia limitu.

### **5. Globalne Funkcjonalności**

1. **Globalne Prompty (Q16):**
    - Dropdown "Select Global Prompt" pozwala wybrać prompt (np. "Przeanalizuj ten kod pod kątem refaktoryzacji").
    - Użytkownik może tworzyć nowe, edytować i usuwać własne prompty.
    - Wybrany tekst jest automatycznie dodawany na *samym początku* "Part 1".
2. **Okno Ustawień (Q17):**
    - Zarządzanie motywem (Jasny / Ciemny / Systemowy).
    - Zarządzanie "Max Chars Limit".
    - Checkbox "Include file headers" (Q7).
    - Edytor listy dozwolonych rozszerzeń (Whitelist) (Q2).
    - Edytor plików filtrów ignorowania (Blacklist) (Q5).
3. **Serwis Powiadomień (`INotificationService`) (Q11):**
    - `ShowError(string message)`: Wyświetla modalny popup na środku ekranu, który trzeba zamknąć.
    - `ShowSuccess(string message)`: Wyświetla krótki "toast" w prawym dolnym rogu, który znika po kilku sekundach.
4. **Trwałość Stanu (`ISettingsService`) (Q4):**
    - Aplikacja musi zapisywać przy wyjściu i odczytywać przy starcie:
        - Wszystkie ustawienia (limit, motyw, filtry, prompty).
        - Listę "Recent Projects".
        - Stan ostatniej sesji: ścieżkę do ostatnio otwartego folderu, stan zaznaczenia w drzewie oraz listę "Pinned Files".

Jasne. Oto lista uzupełnień i doprecyzowań, które dodałem do dokumentacji na podstawie naszej rozmowy:

---

### Sekcja 3. Główny Układ Aplikacji (Main Layout)

**Czego dotyczy:** Doprecyzowania umiejscowienia elementów UI w lewej kolumnie, o których rozmawialiśmy (Q1, Q2, Q5).

- **[PRECYZOWANIE]** Lista "Pinned Files" (Priorytetowe) – wyświetlana **nad** głównym drzewem plików.
- **[UZUPEŁNIENIE]** Lista checkboxów "Ignore Filters" (np. ".net", "angular", "python") wyświetlana **obok** drzewa.
- **[UZUPEŁNIENIE]** Checkbox "Use detected .gitignore" (pojawi się tylko, gdy plik zostanie wykryty) (również obok drzewa).

---

### Sekcja 5. Globalne Funkcjonalności

**Czego dotyczy:** Uzupełnienia sekcji Ustawień oraz Trwałości Stanu o funkcje, które omówiliśmy (Q4, Q5).

- **[UZUPEŁNIENIE]** (W punkcie 'Edytor plików filtrów ignorowania (Blacklist)' w Ustawieniach):
    - Musi zapewniać możliwość tworzenia nowych, edycji i usuwania istniejących list filtrów. Domyślne listy (np. "dotnet", "angular") powinny być puste przy pierwszej instalacji.
- **[UZUPEŁNIENIE]** (W punkcie 'Trwałość Stanu (ISettingsService)'):
    - Aplikacja musi zapisywać również: Stan zaznaczenia filtrów ignorowania (np. czy ".net" był zaznaczony).