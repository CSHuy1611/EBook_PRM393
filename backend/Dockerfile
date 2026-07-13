FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép các file giải pháp và dự án (để tận dụng cache layer của Docker khi restore)
COPY ["MathIBook.sln", "./"]
COPY ["src/MathIBook.Api/MathIBook.Api.csproj", "src/MathIBook.Api/"]
COPY ["src/MathIBook.Application/MathIBook.Application.csproj", "src/MathIBook.Application/"]
COPY ["src/MathIBook.Domain/MathIBook.Domain.csproj", "src/MathIBook.Domain/"]
COPY ["src/MathIBook.Infrastructure/MathIBook.Infrastructure.csproj", "src/MathIBook.Infrastructure/"]
COPY ["tests/MathIBook.UnitTests/MathIBook.UnitTests.csproj", "tests/MathIBook.UnitTests/"]

# Restore các gói NuGet
RUN dotnet restore "src/MathIBook.Api/MathIBook.Api.csproj"

# Sao chép toàn bộ source code còn lại
COPY . .

# Build và publish ra thư mục /app/publish
RUN dotnet publish "src/MathIBook.Api/MathIBook.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Môi trường chạy siêu nhẹ (Runtime stage)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .

# Cài đặt thư viện ICU cho Alpine để tránh lỗi Timezone và Globalization trong C# (.NET)
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Kestrel trong Program.cs của dự án này đang được hard-code lắng nghe ở cổng 5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "MathIBook.Api.dll"]
