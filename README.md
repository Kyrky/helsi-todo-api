# Helsi Todo Lists API

[English](#english) · [Українська](#українська)

## English

A small REST API for todo lists that can be shared with other users. Built on .NET 8 with ASP.NET Core and MongoDB.

### Running it

You need the .NET 8 SDK and MongoDB on `localhost:27017`. The connection string lives in `src/Helsi.TodoApi.Api/appsettings.json`.

```bash
dotnet run --project src/Helsi.TodoApi.Api --launch-profile http
```

| What | Where |
|---|---|
| Swagger | http://localhost:5094/swagger |
| Database check | http://localhost:5094/health |

There is no login. Every request says who the user is in the `X-User-Id` header (a GUID). If the header is missing or empty, the API answers `400`.

### How the code is split

```
Api  ──►  Application  ──►  Domain
 │             ▲
 └──►  Infrastructure
```

| Layer | What's in it | Depends on |
|---|---|---|
| Domain | the `TodoList` entity | nothing |
| Application | `TodoListService` with all the rules, the repository interface, DTOs, `Result`, mapping (Mapster) | Domain |
| Infrastructure | the MongoDB repository, indexes, the health check | Application |
| Api | the controller, request models, the `X-User-Id` header, turning errors into HTTP codes | Application (and Infrastructure only to register it in DI) |

To switch databases you write another `ITodoListRepository`. To switch protocols (gRPC, a message queue) you add another transport layer that calls the same `ITodoListService`. The rules don't move.

### Where each requirement is handled

Technical requirements:

| Requirement | Where |
|---|---|
| RESTful Web API on .NET 8+ | ASP.NET Core 8, resource `/api/todo-lists`, standard HTTP verbs and status codes |
| Storage, MongoDB preferred | MongoDB through `MongoDB.Driver` 3.x |
| Transport, logic and storage kept apart | the four layers above. Domain and Application know nothing about HTTP or MongoDB |
| No MediatR | one application service, `ITodoListService`, called straight from the controller |
| User id comes with the request | `X-User-Id` header, checked by the `RequireUserIdAttribute` filter |

The todo list entity:

| Requirement | Where |
|---|---|
| Title, 1 to 255 characters | `TodoListService.ValidateTitle`. Empty or spaces only gives `400 TodoList.TitleEmpty`, more than 255 gives `400 TodoList.TitleTooLong` |
| Owner | `TodoList.OwnerId`, taken from `X-User-Id` on create and never changed |

Actions:

| Action | Endpoint | Service method |
|---|---|---|
| Create | `POST /api/todo-lists` → `201` | `CreateAsync` |
| Update | `PUT /api/todo-lists/{id}` → `200` | `UpdateAsync` |
| Delete | `DELETE /api/todo-lists/{id}` → `204` | `DeleteAsync` |
| Get one | `GET /api/todo-lists/{id}` → `200` | `GetByIdAsync` |
| Get my lists: id and title only, newest first, paged | `GET /api/todo-lists?page=1&pageSize=20` → `200` | `GetListAsync` |
| Share with a user | `POST /api/todo-lists/{id}/shares` with `{ "userId": "..." }` → `204` | `ShareAsync` |
| Get who it's shared with | `GET /api/todo-lists/{id}/shares` → `200` | `GetSharesAsync` |
| Stop sharing with a user | `DELETE /api/todo-lists/{id}/shares/{userId}` → `204` | `UnshareAsync` |

Paging: `page` starts at 1, `pageSize` is 1 to 100, and the response also has `totalCount`. Lists come newest first. Two lists created at the same moment are ordered by id, so pages don't shift.

Access rules:

| Rule | Where |
|---|---|
| My lists show only lists I own or that are shared with me | a database filter, `ownerId = me OR sharedWith contains me`, in `TodoListRepository.GetPageForUserAsync`, with an index for each half |
| Get, update and manage sharing: owner or a user it's shared with | every such service method starts with `GetAccessibleTodoListAsync` |
| Delete: owner only | `DeleteAsync` checks `IsOwner` |

| Who | Get, update, sharing | Delete |
|---|---|---|
| Owner | yes | yes |
| User it's shared with | yes | `403` |
| Anyone else | `404` | `404` |

Anyone else gets `404` instead of `403` on purpose: the API doesn't tell strangers that a list with that id exists.

### Example requests

```bash
B=http://localhost:5094/api/todo-lists
OWNER=11111111-1111-1111-1111-111111111111
FRIEND=22222222-2222-2222-2222-222222222222

# create a list
curl -X POST $B -H "X-User-Id: $OWNER" -H "Content-Type: application/json" -d '{"title":"Groceries"}'

# share it
curl -X POST $B/{id}/shares -H "X-User-Id: $OWNER" -H "Content-Type: application/json" -d "{\"userId\":\"$FRIEND\"}"

# the friend now sees it
curl "$B?page=1&pageSize=20" -H "X-User-Id: $FRIEND"
```

Errors come back as `ProblemDetails`. The `title` field holds a machine-readable code:

```json
{ "status": 404, "title": "TodoList.NotFound", "detail": "Todo list was not found." }
```

### Decisions and trade-offs

| Decision | Why | Cost |
|---|---|---|
| Sharing lives inside the list document, in a `sharedWith` array | one read gets the list with everyone who can see it, one write changes both | the array grows with every share, fine for a handful of users per list |
| Sharing twice, or removing a share that isn't there, just succeeds | a retried request can't fail with a confusing error | the client can't tell "added" from "was already there" |
| Updates replace the whole document (`ReplaceOne`) | simple: load, change, save | if two people change the same list at the same moment, one change can be lost. A version field or atomic `$addToSet` / `$pull` would fix that |

---

## Українська

Невеликий REST API для списків задач, якими можна ділитися з іншими користувачами. .NET 8, ASP.NET Core, MongoDB.

### Запуск

Потрібні .NET 8 SDK і MongoDB на `localhost:27017`. Рядок підключення лежить у `src/Helsi.TodoApi.Api/appsettings.json`.

```bash
dotnet run --project src/Helsi.TodoApi.Api --launch-profile http
```

| Що | Де |
|---|---|
| Swagger | http://localhost:5094/swagger |
| Перевірка бази | http://localhost:5094/health |

Входу в систему немає. Кожен запит каже, хто користувач, у заголовку `X-User-Id` (GUID). Якщо заголовка немає або він порожній, API відповідає `400`.

### Як поділений код

```
Api  ──►  Application  ──►  Domain
 │             ▲
 └──►  Infrastructure
```

| Шар | Що всередині | Від чого залежить |
|---|---|---|
| Domain | сутність `TodoList` | ні від чого |
| Application | `TodoListService` з усіма правилами, інтерфейс репозиторію, DTO, `Result`, мапінг (Mapster) | Domain |
| Infrastructure | репозиторій на MongoDB, індекси, health check | Application |
| Api | контролер, моделі запитів, заголовок `X-User-Id`, переклад помилок у HTTP-коди | Application (і Infrastructure тільки для реєстрації в DI) |

Щоб змінити базу, пишеш ще одну реалізацію `ITodoListRepository`. Щоб змінити протокол (gRPC, черга повідомлень), додаєш ще один транспортний шар, який викликає той самий `ITodoListService`. Правила при цьому лишаються на місці.

### Де виконується кожна вимога

Технічні вимоги:

| Вимога | Де |
|---|---|
| RESTful Web API на .NET 8+ | ASP.NET Core 8, ресурс `/api/todo-lists`, стандартні HTTP-методи й коди |
| Сховище, бажано MongoDB | MongoDB через `MongoDB.Driver` 3.x |
| Транспорт, логіка і сховище окремо | чотири шари вище. Domain і Application нічого не знають про HTTP чи MongoDB |
| Без MediatR | один сервіс застосунку, `ITodoListService`, контролер викликає його напряму |
| Id користувача приходить із запитом | заголовок `X-User-Id`, його перевіряє фільтр `RequireUserIdAttribute` |

Сутність «Список задач»:

| Вимога | Де |
|---|---|
| Назва від 1 до 255 символів | `TodoListService.ValidateTitle`. Порожня або з самих пробілів дає `400 TodoList.TitleEmpty`, довша за 255 дає `400 TodoList.TitleTooLong` |
| Власник | `TodoList.OwnerId`, береться з `X-User-Id` під час створення і більше не змінюється |

Дії:

| Дія | Ендпоінт | Метод сервісу |
|---|---|---|
| Створити | `POST /api/todo-lists` → `201` | `CreateAsync` |
| Змінити | `PUT /api/todo-lists/{id}` → `200` | `UpdateAsync` |
| Видалити | `DELETE /api/todo-lists/{id}` → `204` | `DeleteAsync` |
| Отримати один | `GET /api/todo-lists/{id}` → `200` | `GetByIdAsync` |
| Отримати мої списки: тільки id і назва, від нових до старих, посторінково | `GET /api/todo-lists?page=1&pageSize=20` → `200` | `GetListAsync` |
| Поділитися з користувачем | `POST /api/todo-lists/{id}/shares` з `{ "userId": "..." }` → `204` | `ShareAsync` |
| Подивитися, з ким поділено | `GET /api/todo-lists/{id}/shares` → `200` | `GetSharesAsync` |
| Прибрати доступ користувачу | `DELETE /api/todo-lists/{id}/shares/{userId}` → `204` | `UnshareAsync` |

Пагінація: `page` починається з 1, `pageSize` від 1 до 100, у відповіді є ще `totalCount`. Спершу нові списки. Якщо два списки створені в одну мить, порядок між ними визначає id, тож сторінки не з'їжджають.

Правила доступу:

| Правило | Де |
|---|---|
| У «моїх списках» тільки ті, де я власник або з якими зі мною поділилися | фільтр у базі `ownerId = я АБО sharedWith містить мене` у `TodoListRepository.GetPageForUserAsync`, під кожну половину свій індекс |
| Отримати, змінити, керувати доступом: власник або той, з ким поділилися | кожен такий метод сервісу спершу викликає `GetAccessibleTodoListAsync` |
| Видалити: тільки власник | `DeleteAsync` перевіряє `IsOwner` |

| Хто | Отримати, змінити, доступи | Видалити |
|---|---|---|
| Власник | так | так |
| Той, з ким поділилися | так | `403` |
| Будь-хто інший | `404` | `404` |

Сторонній отримує `404`, а не `403`, навмисно: API не підказує чужим, що список з таким id взагалі існує.

### Приклади запитів

```bash
B=http://localhost:5094/api/todo-lists
OWNER=11111111-1111-1111-1111-111111111111
FRIEND=22222222-2222-2222-2222-222222222222

# створити список
curl -X POST $B -H "X-User-Id: $OWNER" -H "Content-Type: application/json" -d '{"title":"Groceries"}'

# поділитися
curl -X POST $B/{id}/shares -H "X-User-Id: $OWNER" -H "Content-Type: application/json" -d "{\"userId\":\"$FRIEND\"}"

# тепер друг його бачить
curl "$B?page=1&pageSize=20" -H "X-User-Id: $FRIEND"
```

Помилки приходять у форматі `ProblemDetails`. У полі `title` машинний код помилки:

```json
{ "status": 404, "title": "TodoList.NotFound", "detail": "Todo list was not found." }
```

### Рішення і компроміси

| Рішення | Навіщо | Ціна |
|---|---|---|
| Доступи лежать прямо в документі списку, в масиві `sharedWith` | одне читання дає і список, і всіх, хто його бачить, один запис змінює все разом | масив росте з кожним доступом, для кількох користувачів на список це нормально |
| Поділитися вдруге або прибрати доступ, якого немає, це просто успіх | повторний запит не впаде з незрозумілою помилкою | клієнт не відрізнить «додано» від «вже було» |
| Зміни перезаписують увесь документ (`ReplaceOne`) | просто: завантажив, змінив, зберіг | якщо двоє одночасно змінять той самий список, одна зміна може загубитися. Лікується полем версії або атомарними `$addToSet` / `$pull` |
