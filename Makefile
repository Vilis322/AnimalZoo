SOLUTION=AnimalZoo.sln
APP=AnimalZoo.App/AnimalZoo.App.csproj

.PHONY: restore build run clean publish

restore:
	dotnet restore $(SOLUTION)

build: restore
	dotnet build $(SOLUTION) -c Debug

run: build
	dotnet run --project $(APP) -f net9.0

clean:
	dotnet clean $(SOLUTION)
	rm -rf **/bin **/obj
