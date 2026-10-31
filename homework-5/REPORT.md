# Практическая работа №5
## Проектирование безопасных релизных процессов, паттернов миграции данных и эксплуатационной документации ИС «Умный кампус»

| | |
|---|---|
| **Студент** | Горелова Александра Валерьевна |
| **Группа** | И13-24-11 |
| **Вариант** | 4 — «Столовая и безналичная оплата», задача **CAMPUS-104**: переименование колонки Price > Amount в чеке |
| **Репозиторий** | https://github.com/Balbeskas/smart-campus-releases |
| **Дисциплина** | Сопровождение и обслуживание программного обеспечения компьютерных систем (МДК 03.01) |

## Состав работы

| Блок | Артефакт | Файл |
|---|---|---|
| 1. Цифровой след | Контроллер с тегами OpenTelemetry `jira.issue`, `campus.check_id` | [CheckoutController.cs](src/Campus.Canteen.Payments/Controllers/CheckoutController.cs), [CampusTracing.cs](src/Campus.Canteen.Payments/Tracing/CampusTracing.cs) |
| 1. Цифровой след | Коммит `[CAMPUS-104] Add tracing tags and business logic` | история репозитория |
| 2. Change Management | Карточка Change Request на v1.9.0 | [CR-2026-104.md](docs/change-requests/CR-2026-104.md) |
| 2. Change Management | Branch Protection Rules для `main` | [branch-protection.md](docs/branch-protection.md) |
| 3. Expand and Contract | Фаза 1 — добавление `amount` | [01_expand.sql](db/01_expand.sql) |
| 3. Expand and Contract | Двойная запись в старую и новую колонку (C#) | [AmountColumnMode.cs](src/Campus.Canteen.Payments/Persistence/AmountColumnMode.cs) |
| 3. Expand and Contract | Фаза 2 — пакетный перенос данных | [02_migrate.sql](db/02_migrate.sql) |
| 3. Expand and Contract | Фаза 3 — удаление `price` с защитной проверкой | [03_contract.sql](db/03_contract.sql) |
| 4. Sanity / Docs-as-Code | Production Sanity-тест с Cleanup | [ProductionSanityTests.cs](tests/Campus.Canteen.Sanity/ProductionSanityTests.cs) |
| 4. Sanity / Docs-as-Code | ADR | [0009-canteen-check-amount.md](docs/adr/0009-canteen-check-amount.md) |
| 4. Sanity / Docs-as-Code | Журнал изменений | [CHANGELOG.md](CHANGELOG.md) |

## Change Request (кратко)
Релиз **v1.9.0** сервиса `campus-canteen-payments` переименовывает сумму чека `price → amount`
и добавляет теги цифрового следа. Окно обслуживания — **воскресенье 08.11.2026, 06:00–08:00**:
по метрикам октября в это время поток оплат нулевой (пик — будни 11:30–14:30, до 40 оплат/мин).
План: резервная копия таблицы → `01_expand.sql` → `kubectl set image ... :1.9.0` → проверка
`/health` (режим `DualWrite`) → `02_migrate.sql` → Production Sanity → час наблюдения за 5xx и p95.
Откат: `kubectl rollout undo deployment/canteen-payments -n campus` — безопасен, так как v1.9.0
пишет сумму в обе колонки. Фаза Contract — отдельное окно 15.11.2026 после перевода отчетов
бухгалтерии на `amount`.

## Проверка на стенде (PostgreSQL 18, .NET 8)
- 25 000 исторических чеков v1.8; после Expand новая оплата записана в `price` и `amount` одновременно;
- запуск `03_contract.sql` до переноса **отклонен**: «CONTRACT ОТМЕНЕН — 25000 строк без amount»;
- `02_migrate.sql`: 5 пакетов по 5 000 строк, `without_amount = 0`, `mismatched = 0`;
- `03_contract.sql`: `amount NOT NULL`, колонка `price` удалена;
- Production Sanity против запущенного сервиса: **5 из 5** тестов, технических чеков в БД — 0.

## Запуск
```powershell
dotnet test homework-5/CampusCanteen.sln                       # тесты в памяти
$env:ConnectionStrings__Payments = "Host=localhost;Port=5432;Database=campus_canteen;Username=postgres"
dotnet run --project homework-5/src/Campus.Canteen.Payments     # сервис на http://localhost:5104
$env:CAMPUS_SANITY_URL = "http://localhost:5104"; dotnet test homework-5/CampusCanteen.sln --filter Category=ProductionSanity
```
