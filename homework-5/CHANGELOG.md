# Changelog

Все значимые изменения сервиса `campus-canteen-payments` фиксируются в этом файле.
Формат — [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/),
версии — [Semantic Versioning](https://semver.org/lang/ru/).

## [Unreleased]
### Removed
- Колонка `canteen_checks.price` (фаза Contract CAMPUS-104, окно 15.11.2026, `db/03_contract.sql`).

## [1.9.0] - 2026-11-08
### Added
- Колонка `canteen_checks.amount` — сумма чека (CAMPUS-104, фаза Expand).
- Теги трассировки OpenTelemetry `jira.issue`, `campus.check_id`, `campus.cashier_id`,
  `campus.amount_mode` для операций оплаты, возврата и очистки.
- Эндпоинт `DELETE /api/v1/checks/{id}` — удаление технических чеков sanity-тестов (карты `SANITY-*`).
- Production Sanity-тест «оплата 1 ₽ + возврат + удаление».
- Настройка `CanteenMigration:AmountMode` (`DualWrite` / `AmountOnly`).

### Fixed
- Отчет сверки льготного питания путал итоговую сумму чека с ценой позиции меню —
  сумма теперь хранится в явно названной колонке `amount`.
- Возврат по уже возвращенному чеку возвращает `409 Conflict` вместо повторного возврата.

### Breaking Changes
- Поле `price` в ответах API заменено на `amount`. Интеграции (отчеты бухгалтерии, АРМ кассира)
  должны перейти на `amount` до фазы Contract.
- После `03_contract.sql` колонка `price` в БД отсутствует; прямые SQL-запросы к ней перестанут работать.

## [1.8.4] - 2026-10-12
### Fixed
- Округление суммы чека до копеек при оплате комплексного обеда.
