# Helsi Todo Lists API: overkill branch

[English](#english) · [Українська](#українська)

## English

This branch is the [`master`](https://github.com/Kyrky/helsi-todo-api/tree/master) solution plus things the task didn't ask for. The API, the endpoints, the access rules and the architecture are the same, so read the `master` README for those. Below is only what's different.

### What's added

| Area | `master` | `overkill` |
|---|---|---|
| Running it | `dotnet run` with your own MongoDB | also `docker compose up --build`: API and MongoDB together |
| Tests | none | 41 unit tests (service rules and request validators), 7 integration tests against a real MongoDB |
| Input validation | checks inside `TodoListService` | FluentValidation validators for request models in the Api, run by one action filter before the controller; same error codes and response shape; the service keeps only rules that need data (sharing with the owner, access) |
| Logging | default ASP.NET logs | Serilog: one line per request with the user id, a line for each change to a list, a warning when a non-owner tries to delete |
| Unexpected errors | default behavior | `500` ProblemDetails, no stack trace for the client, the full error goes to the log |

### Docker

```bash
docker compose up --build
```

| What | Where |
|---|---|
| Swagger | http://localhost:8080/swagger |
| Database check | http://localhost:8080/health |
| MongoDB from the host | `localhost:27018` (so it doesn't clash with one on `27017`) |

Data is kept in a named volume and survives rebuilds.

### Tests

```bash
dotnet test
```

| Project | What it checks | Needs |
|---|---|---|
| `tests/Helsi.TodoApi.UnitTests` | titles, paging, who can read, change, share and delete, against an in-memory repository | nothing |
| `tests/Helsi.TodoApi.IntegrationTests` | the real API over HTTP with MongoDB in Testcontainers: status codes, the `X-User-Id` header, the sharing flow, feed order and paging, 20 parallel shares plus a rename with nothing lost (the atomic writes from `master`) | Docker running |

### Code changes compared to `master`

| File | Change |
|---|---|
| `Program.cs` | Serilog, request logging, `UseExceptionHandler`, validator registration and the validation filter |
| `Api/Validation`, `Api/Filters/ValidationFilter.cs`, `Contracts/GetTodoListsQuery.cs` | FluentValidation validators, the filter that runs them, paging parameters as a model |
| `appsettings*.json` | `Serilog` section instead of `Logging` |
| `TodoListService` | `ILogger` for actions; input checks moved to validators |
| new | `Dockerfile`, `.dockerignore`, `docker-compose.yml`, `tests/` |

---

## Українська

Ця гілка це рішення з [`master`](https://github.com/Kyrky/helsi-todo-api/tree/master) плюс те, чого завдання не вимагало. API, ендпоінти, правила доступу й архітектура ті самі, про них дивись README у `master`. Нижче тільки відмінності.

### Що додано

| Що | `master` | `overkill` |
|---|---|---|
| Запуск | `dotnet run` зі своєю MongoDB | ще й `docker compose up --build`: API і MongoDB разом |
| Тести | немає | 41 unit-тест (правила сервісу і валідатори запитів), 7 інтеграційних зі справжньою MongoDB |
| Валідація вводу | перевірки всередині `TodoListService` | валідатори FluentValidation для моделей запитів в Api, їх запускає один action filter до контролера; ті самі коди помилок і формат відповіді; у сервісі лишилися тільки правила, яким потрібні дані (поширення з власником, доступ) |
| Логування | стандартні логи ASP.NET | Serilog: рядок на кожен запит з id користувача, рядок на кожну зміну списку, попередження, коли не власник пробує видалити |
| Непередбачені помилки | стандартна поведінка | `500` ProblemDetails, без stack trace для клієнта, повна помилка в лог |

### Docker

```bash
docker compose up --build
```

| Що | Де |
|---|---|
| Swagger | http://localhost:8080/swagger |
| Перевірка бази | http://localhost:8080/health |
| MongoDB з хоста | `localhost:27018` (щоб не конфліктувати з тією, що на `27017`) |

Дані лежать в іменованому volume і переживають перезбирання.

### Тести

```bash
dotnet test
```

| Проєкт | Що перевіряє | Що потрібно |
|---|---|---|
| `tests/Helsi.TodoApi.UnitTests` | назви, пагінацію, хто може читати, змінювати, ділитися і видаляти, на репозиторії в пам'яті | нічого |
| `tests/Helsi.TodoApi.IntegrationTests` | справжній API через HTTP з MongoDB у Testcontainers: коди відповідей, заголовок `X-User-Id`, сценарій спільного доступу, порядок і пагінацію стрічки, 20 паралельних поширень плюс перейменування без жодної втрати (атомарні записи з `master`) | запущений Docker |

### Зміни в коді порівняно з `master`

| Файл | Зміна |
|---|---|
| `Program.cs` | Serilog, логування запитів, `UseExceptionHandler`, реєстрація валідаторів і фільтр валідації |
| `Api/Validation`, `Api/Filters/ValidationFilter.cs`, `Contracts/GetTodoListsQuery.cs` | валідатори FluentValidation, фільтр, що їх запускає, параметри пагінації як модель |
| `appsettings*.json` | секція `Serilog` замість `Logging` |
| `TodoListService` | `ILogger` для дій; перевірки вводу переїхали у валідатори |
| нове | `Dockerfile`, `.dockerignore`, `docker-compose.yml`, `tests/` |
