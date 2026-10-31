-- =====================================================================
-- CAMPUS-104 | Фаза 1. EXPAND — добавляем новую структуру рядом со старой
-- Переименование колонки чека price > amount без простоя столовой.
--
-- * Колонка amount добавляется как NULL без DEFAULT: в PostgreSQL это изменение
--   только метаданных, таблица не переписывается, блокировка — доли секунды.
-- * Старое ограничение NOT NULL на price снимается: после Contract новые
--   экземпляры перестанут его заполнять, а пока v1.9.0 пишет в обе колонки
--   (CheckAmount.Write, режим DualWrite).
-- * lock_timeout: если таблицу держит долгая транзакция, миграция завершится
--   ошибкой через 3 с, а не выстроит очередь блокировок для касс.
-- * Скрипт идемпотентен — повторный запуск безопасен.
-- Откат фазы: ALTER TABLE canteen_checks DROP COLUMN IF EXISTS amount;
-- =====================================================================
SET lock_timeout = '3s';

ALTER TABLE canteen_checks
    ADD COLUMN IF NOT EXISTS amount numeric(10,2);

ALTER TABLE canteen_checks
    ALTER COLUMN price DROP NOT NULL;

COMMENT ON COLUMN canteen_checks.amount IS 'Сумма чека, руб. (CAMPUS-104, заменяет price)';
COMMENT ON COLUMN canteen_checks.price  IS 'УСТАРЕЛО: будет удалено в 03_contract.sql (CAMPUS-104)';
