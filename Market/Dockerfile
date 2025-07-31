# Krok 1: Użyj obrazu .NET SDK, który zawiera wszystkie narzędzia deweloperskie,
# w tym kluczowe dla nas "dotnet watch".
FROM mcr.microsoft.com/dotnet/sdk:8.0

# Ustawienie głównego katalogu na kod źródłowy
WORKDIR /src

# Wystawienie portów, na których aplikacja będzie nasłuchiwać.
EXPOSE 8080
EXPOSE 8081

# --- Optymalizacja budowania obrazu ---
# Najpierw kopiujemy tylko plik .csproj, aby przywrócić zależności.
# To przyspiesza kolejne budowanie, bo ta warstwa jest cachowana.
COPY ["Market/Market.csproj", "Market/"]
RUN dotnet restore "Market/Market.csproj"

# Następnie kopiujemy całą resztę plików projektu.
COPY . .

# Zmieniamy katalog roboczy na folder konkretnego projektu.
# Dzięki temu komendy `dotnet` nie potrzebują dodatkowych ścieżek.
WORKDIR "/src/Market"

# --- Punkt uruchomienia (Entrypoint) ---
# Uruchamiamy `dotnet watch`, które monitoruje pliki i automatycznie
# restartuje aplikację po wykryciu zmian.
# Argument --urls jest kluczowy dla dostępu do aplikacji z zewnątrz kontenera.
ENTRYPOINT ["dotnet", "watch", "run", "--urls", "http://*:8080"]
