# build - kéo về image sdk về để biên dịch code
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release # Setup Variable for build configuration
WORKDIR /src


## build vào bên trong container để ra dll sau đó dotnet run bằng entrypoint để chạy cái dll lên
# copy vào container của mình
COPY ["FloodRescue/FloodRescue.API/FloodRescue.API.csproj", "FloodRescue.API/"]
COPY ["FloodRescue/FloodRescue.Services/FloodRescue.Services.csproj", "FloodRescue.Services/"]
COPY ["FloodRescue/FloodRescue.Repositories/FloodRescue.Repositories.csproj", "FloodRescue.Repositories/"]

# tải các thư viện về container để đảm bảo máy khứa nào cũng chạy được
RUN dotnet restore "./FloodRescue.API/FloodRescue.API.csproj"

# copy toàn bộ code vào /src
COPY . .
WORKDIR "/src/FloodRescue/FloodRescue.API"

# build project -> xuất ra dll chưa được dọn dẹp
RUN dotnet build "./FloodRescue.API.csproj" -c $BUILD_CONFIGURATION -o /app/build 

# publish ra file chạy .dll đã được dọn dẹp
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FloodRescue.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# run - run cái dll đó lên bằng dotnet runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FloodRescue.API.dll"] # tương đương với dotnet FloodRescue.API.dll - để chạy file dll lên


