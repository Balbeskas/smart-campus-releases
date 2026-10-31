-- =====================================================================
-- CAMPUS-104 | Фаза 3. CONTRACT — удаление старой структуры
--
-- Запускается только когда:
--   1) все экземпляры работают в режиме AmountOnly (CanteenMigration:AmountMode),
--   2) отчеты бухгалтерии переведены на amount,
--   3) прошел контрольный период (неделя) без обращений к price.
-- Защитная проверка в начале не даст удалить price, если хоть одна строка
-- не перенесена: транзакция откатится целиком, данные не потеряются.
-- =====================================================================
SET lock_timeout = '3s';

BEGIN;

DO $$
DECLARE
    not_migrated bigint;
BEGIN
    SELECT count(*) INTO not_migrated FROM canteen_checks WHERE amount IS NULL;
    IF not_migrated > 0 THEN
        RAISE EXCEPTION 'CAMPUS-104: CONTRACT ОТМЕНЕН — % строк без amount. Повторите 02_migrate.sql', not_migrated;
    END IF;
END $$;

-- NOT NULL через проверочное ограничение: VALIDATE не блокирует запись в таблицу
ALTER TABLE canteen_checks ADD CONSTRAINT canteen_checks_amount_not_null CHECK (amount IS NOT NULL) NOT VALID;
ALTER TABLE canteen_checks VALIDATE CONSTRAINT canteen_checks_amount_not_null;
ALTER TABLE canteen_checks ALTER COLUMN amount SET NOT NULL;
ALTER TABLE canteen_checks DROP CONSTRAINT canteen_checks_amount_not_null;
ALTER TABLE canteen_checks ADD CONSTRAINT canteen_checks_amount_positive CHECK (amount > 0);

ALTER TABLE canteen_checks DROP COLUMN price;
DROP PROCEDURE IF EXISTS campus104_backfill_amount(int);

COMMIT;
