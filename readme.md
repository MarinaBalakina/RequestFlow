# Команда 3 — ТНСОД 2026/2027

В репозитории находятся сервисы и утилиты, которые последовательно разрабатываются в лабораторных работах:

- `UserService` — gRPC CRUD-сервис пользователей, лабораторная №1;
- `RateLimiter.Writer` — gRPC-сервис управления ограничениями в MongoDB, лабораторная №2;
- `RateLimiter.Reader` — загрузка и синхронизация ограничений в памяти, лабораторная №3;
- `UserRequestsKafkaGenerator` — заготовка генератора событий Kafka.

## Лабораторная №1

`UserService` реализует создание, получение, изменение и удаление пользователей. Данные хранятся в PostgreSQL, доступ к БД выполняется через Dapper и SQL-функции.

### Запуск

```powershell
cd .\UserService
docker compose --profile lab6 up -d --wait
cd ..
dotnet restore .\RateLimiter.sln
dotnet run --project .\UserService\UserService.csproj
```

Сервис запускается на `http://localhost:5002` по HTTP/2. Контракт для импорта в Postman находится в `UserService/Protos/user-service.proto`.

### Тесты

```powershell
dotnet test .\UserService.Tests\UserService.Tests.csproj -c Release
```

Подробное описание реализации и сценарий демонстрации: [личный отчёт по лабораторной №1](docs/lab-1-report.md).

## Лабораторная №3: Reader

При запуске Reader загружает правила из коллекции `rate_limits` курсором `FindAsync` с `BatchSize = 1000` в потокобезопасный кэш. Только после полной загрузки запускается gRPC-сервер. Затем отдельная фоновая задача получает события вставки, обновления, замены и удаления через MongoDB Change Stream и сразу изменяет кэш. Проверка превышения RPM здесь ещё не реализована: при уменьшении лимита сервис только сохраняет новое значение, а решение о превышении будет приниматься при будущем событии пользовательского запроса.

Change Stream требует MongoDB в режиме replica set. Локальный `docker-compose.yml` Writer запускает одноузловой replica set и инициализирует его через healthcheck. База доступна только с локального компьютера на порту `27018`.

### Запуск

```powershell
cd .\RateLimiter.Writer
docker compose up -d --wait
cd ..
dotnet run --project .\RateLimiter.Writer\RateLimiter.Writer.csproj
```

В другом терминале:

```powershell
dotnet run --project .\RateLimiter.Reader\RateLimiter.Reader.csproj
```

Writer слушает `localhost:5003`, Reader — `localhost:5000` (gRPC поверх HTTP/2 без TLS). В gRPC-клиент можно импортировать `RateLimiter.Reader/Protos/reader-service.proto` и вызвать `GetAllLimits` с пустым запросом. Для демонстрации изменений создайте, обновите и удалите правило через Writer: ответ `GetAllLimits` в Reader должен отражать каждое изменение без перезапуска.

### Тесты

```powershell
dotnet test .\RateLimiter.sln -c Release
```
