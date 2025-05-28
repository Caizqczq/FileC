FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CloudFileHub/CloudFileHub.csproj", "CloudFileHub/"]
RUN dotnet restore "CloudFileHub/CloudFileHub.csproj"
COPY . .
WORKDIR "/src/CloudFileHub"
RUN dotnet build "./CloudFileHub.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CloudFileHub.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# 创建uploads目录并设置权限
RUN mkdir -p /app/wwwroot/uploads && \
    chown -R 1000:1000 /app/wwwroot/uploads

# 设置环境变量，可以在容器启动时覆盖
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Server=mysql;Port=3306;Database=CloudFileHub;User=root;Password=123456;"
ENV ASPNETCORE_URLS="http://+:8080"

# 健康检查
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# 使用非root用户运行
USER 1000:1000

# 定义卷，用于持久化存储用户上传的文件
VOLUME ["/app/wwwroot/uploads"]

ENTRYPOINT ["dotnet", "CloudFileHub.dll"]
